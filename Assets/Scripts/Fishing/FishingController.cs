using System;
using UnityEngine;

namespace ScrollRoad
{
    // Olta döngüsü (2026-10-13 prototip; Docs/ortamlar.md "Olta — içerik", spinning/LRF, şamandıra yok):
    // balıkçı kendiliğinden atar → yem uzağa düşer → yukarı scroll makarayı sarar, yem kıyıya gelir →
    // yolda bir balık yeme vurur → sarılarak getirilir, kıyıda tutulur ve deftere girer → yeni atış.
    // Aşağı scroll makarayı geri çevirir (misina boşalır, yem/balık uzaklaşır); rölantide makara
    // kendiliğinden yavaş sarar (ScoreEngine temposu). Başarısızlık yok: misina kopmaz, balık kaçmaz.
    //
    // Birim: balık sayısı (2026-10-14 kullanıcı kararı; Yol'da "durağa varış = damga" gibi). Her atış
    // eşit aralıktır: EnvironmentCatalog.FishIntervalPoints sarılınca bir balık tutulur. Tür ve kg atışta
    // önceden belirlenir ama yalnız balığın özelliğidir (defter, rekor), süreyi etkilemez; büyük balık
    // yalnız görselde daha uzağa/derine atılır. Atış ve tutuş animasyonu sırasında gelen puan kaybolmaz
    // (bir sonraki atışa sayılır). Sayaç aylık: ay değişince balık sayısı ve süren atış sıfırlanır,
    // balık defteri kalıcıdır.
    //
    // Mekânlar (2026-10-14): Göl → Nehir…; her biri ömür boyu tutulan balık sayısıyla açılır, açıldığında
    // duyurulur ve olta oraya geçer. Oyuncu balık defterinden açık bir mekâna geri dönebilir. Mekân
    // değişince sarılan puan korunur, yalnız süren atışın türü yeni mekândan yeniden çekilir.
    //
    // Yalnız Olta seçiliyken ilerler. Görünüm FishingSceneView'da, HUD FishingHudController'da.
    public class FishingController : MonoBehaviour
    {
        public enum Phase { Casting, Retrieving, Hooked, Landing, Resting }

        [SerializeField] NoticeController notice;
        [SerializeField] float castSeconds = 1.3f;
        [SerializeField] float landSeconds = 1.4f;
        [SerializeField] float restSeconds = 0.9f;
        [SerializeField] float autosaveIntervalSeconds = 10f;

        FishingProgress state;
        readonly System.Random rng = new System.Random();
        float phaseTime;
        float autosaveTimer;
        double carry; // atış/tutuş sırasında gelen puan: bir sonraki atışa sayılır

        public static FishingController Instance { get; private set; }

        public Phase CurrentPhase { get; private set; } = Phase.Casting;
        public float PhaseTime => phaseTime;
        public float CastSeconds => castSeconds;
        public float LandSeconds => landSeconds;
        public FishingProgress Progress => state;

        public FishSpecies CastFish => FishCatalog.Get(state.castFishId);
        public float CastKg => state.castKg;
        public float CastDistance => state.castDistance;
        public float StrikeAt => state.strikeAt;
        public double RequiredPoints => EnvironmentCatalog.FishIntervalPoints;
        // 0 = yem atıldığı yerde, 1 = kıyıda.
        public float Reeled => RequiredPoints <= 0 ? 0f : Mathf.Clamp01((float)(state.castProgress / RequiredPoints));
        // Sarma yönü: + sarıyor, - misina boşalıyor, 0 duruyor (görünüm/HUD için).
        public float ReelDirection { get; private set; }

        public FishSpecies LastFish => FishCatalog.Get(state.lastFishId);
        public float LastKg => state.lastKg;
        public bool LastWasNew { get; private set; }
        public int SpeciesLogged => state.log.Count;
        public int MonthFish => state.monthFish;

        public FishingSpot Spot => FishCatalog.GetSpot(state.spot);
        public int SpeciesLoggedAt(FishingSpot spot)
        {
            int n = 0;
            foreach (var s in spot.Species) if (state.Find(s.Id) != null) n++;
            return n;
        }

        public event Action<FishSpecies, float, bool> Caught; // tür, kg, yeni tür mü
        public event Action<FishingSpot> SpotChanged;

        public bool IsUnlocked(FishingSpot spot) => IsUnlocked(spot, state);

        public static bool IsUnlocked(FishingSpot spot, FishingProgress progress) =>
            spot.UnlockTotalFish <= 0 || CarCatalog.ReviewUnlockAll || progress.totalFish >= spot.UnlockTotalFish;

        // Açık bir mekâna geç: sarılan puan korunur, süren atışın türü yeni mekândan çekilir.
        public bool SelectSpot(FishingSpot spot)
        {
            if (spot == null || !IsUnlocked(spot)) return false;
            if (spot == Spot) return true;
            state.spot = spot.Id;
            if (!spot.Contains(state.castFishId)) RollFish();
            Save();
            SpotChanged?.Invoke(spot);
            return true;
        }

        void Awake()
        {
            Instance = this;
            state = SaveService.LoadFishing();
            if (state.announcedSpots == null) state.announcedSpots = new System.Collections.Generic.List<string>();
            if (!IsUnlocked(Spot)) state.spot = FishCatalog.Lake.Id;
            if (CheckMonth() || CastFish == null) NewCast(fromLoad: true);
            else CurrentPhase = state.castProgress / Math.Max(1e-6, RequiredPoints) >= state.strikeAt ? Phase.Hooked : Phase.Retrieving;
        }

        void OnDestroy()
        {
            Save();
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() => Save();

        void Save()
        {
            if (state != null) SaveService.SaveFishing(state);
        }

        void Update()
        {
            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == EnvironmentId.Fishing;
            if (engine == null || !active || Time.deltaTime <= 0f)
            {
                ReelDirection = 0f;
                return;
            }

            if (CheckMonth())
            {
                carry = 0;
                NewCast(fromLoad: true);
            }
            CheckSpotUnlocks();
            CheckGearUnlocks();

            double raw = engine.FrameRawPoints;
            ReelDirection = engine.Pace > 1f ? 1f : engine.Pace < -1f ? -1f : 0f;
            phaseTime += Time.deltaTime;

            switch (CurrentPhase)
            {
                case Phase.Casting:
                    carry = Math.Max(0.0, carry + raw);
                    if (phaseTime >= castSeconds) SetPhase(Phase.Retrieving);
                    break;

                case Phase.Retrieving:
                case Phase.Hooked:
                    // Atış/tutuş sırasında biriken puan sınırlı hızla aktarılır (en çok 1,5 sn'de bir
                    // balık): küçük balık yeme vurmadan, görünmeden tutulmasın. Puan kaybolmaz, bekler.
                    double drain = Math.Min(carry, RequiredPoints * Time.deltaTime / 1.5);
                    carry -= drain;
                    // Misina atılan mesafeden fazla boşalmaz.
                    state.castProgress = Math.Max(0.0, state.castProgress + drain + raw);
                    if (CurrentPhase == Phase.Retrieving && Reeled >= state.strikeAt) SetPhase(Phase.Hooked);
                    if (state.castProgress >= RequiredPoints) Land();
                    break;

                case Phase.Landing:
                    carry = Math.Max(0.0, carry + raw);
                    if (phaseTime >= landSeconds) SetPhase(Phase.Resting);
                    break;

                case Phase.Resting:
                    carry = Math.Max(0.0, carry + raw);
                    if (phaseTime >= restSeconds) NewCast(fromLoad: false);
                    break;
            }

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        // Ay değiştiyse aylık sayacı sıfırlar (true döner); süren atışı çağıran yeniler.
        bool CheckMonth()
        {
            string month = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            if (state.month == month) return false;
            state.month = month;
            state.monthFish = 0;
            return true;
        }

        // Yeni açılan mekânı bir kez duyur ve oltayı oraya geçir (eşiği bu sürümden önce geçmiş oyuncuya
        // da ilk açılışta). Açılışın ilk saniyelerinde bekler: tabela pencere kurulurken görünmeden bitiyordu.
        void CheckSpotUnlocks()
        {
            if (Time.realtimeSinceStartup < 4f || Time.frameCount % 30 != 0) return;
            foreach (var spot in FishCatalog.Spots)
            {
                if (spot.UnlockTotalFish <= 0 || state.announcedSpots.Contains(spot.Id)) continue;
                // İnceleme modu (her şey açık) duyuru harcamasın: gerçek eşik geçilmiş olmalı.
                if (state.totalFish < spot.UnlockTotalFish) continue;
                state.announcedSpots.Add(spot.Id);
                SelectSpot(spot);
                Save();
                if (notice != null)
                {
                    notice.Announce($"New spot: {spot.Name}", $"{spot.Species.Length} new species to catch", $"{spot.Name} unlocked");
                }
            }
        }

        // Takım açılınca bir kez duyurulur (seçim kullanıcıda: takım kutusu, ≡ → Tackle box).
        void CheckGearUnlocks()
        {
            if (Time.realtimeSinceStartup < 4f || Time.frameCount % 30 != 15) return;
            foreach (var gear in FishingGear.All)
            {
                if (gear.UnlockTotalFish <= 0 || state.announcedGear.Contains(gear.Id)) continue;
                if (state.totalFish < gear.UnlockTotalFish) continue;
                state.announcedGear.Add(gear.Id);
                Save();
                if (notice != null) notice.Announce($"New tackle: {gear.Name}", gear.Blurb, $"{gear.Name} tackle unlocked");
            }
        }

        public void SetGear(string id)
        {
            if (state == null || state.gear == id) return;
            state.gear = id;
            Save();
        }

        void SetPhase(Phase phase)
        {
            CurrentPhase = phase;
            phaseTime = 0f;
        }

        void NewCast(bool fromLoad)
        {
            RollFish();
            // Büyük balık uzakta ve derinde: mesafe ağırlığın (mekânın en ağırına göre) kareköküyle artar.
            float size = Mathf.Sqrt(Mathf.Clamp01(state.castKg / Spot.HeaviestKg));
            state.castDistance = Mathf.Clamp01(Mathf.Lerp(0.5f, 1f, size) + (float)(rng.NextDouble() - 0.5) * 0.08f);
            // Balık yolun sonuna doğru vurur: atışın çoğu sarma, son kısmı tutulan balıkla boğuşma.
            state.strikeAt = Mathf.Lerp(0.75f, 0.9f, (float)rng.NextDouble());
            state.castProgress = 0;
            SetPhase(Phase.Casting);
            if (fromLoad) Save();
        }

        void RollFish()
        {
            var species = FishCatalog.Roll(Spot, rng, state.totalFish, out float kg);
            state.castFishId = species.Id;
            state.castKg = kg;
        }

        void Land()
        {
            var species = CastFish;
            float kg = state.castKg;
            carry = state.castProgress - RequiredPoints; // fazlası bir sonraki atışa
            state.castProgress = RequiredPoints;

            var record = state.Find(species.Id);
            bool isNew = record == null;
            if (isNew)
            {
                record = new FishRecord { id = species.Id, firstCaught = DateTime.Now.ToString("yyyy-MM-dd") };
                state.log.Add(record);
            }
            record.count++;
            record.bestKg = Mathf.Max(record.bestKg, kg);
            state.totalKg += kg;
            state.totalFish++;
            state.monthFish++;
            state.lastFishId = species.Id;
            state.lastKg = kg;
            LastWasNew = isNew;
            SetPhase(Phase.Landing);
            Save();

            Caught?.Invoke(species, kg, isNew);
            bool legendary = species == Spot.Legendary;
            if (legendary && notice != null)
            {
                notice.Announce($"Legendary: {species.Name}", $"{kg:0.00} kg · the rarest fish of {Spot.Name}", $"Legendary {species.Name}!");
            }
            else if (isNew && notice != null)
            {
                notice.Announce($"New species: {species.Name}", $"{kg:0.00} kg · stamped in your fish log", $"{species.Name} · new species");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: defteri ve süren atışı sıfırla.
        public void DebugReset()
        {
            state = new FishingProgress();
            carry = 0;
            NewCast(fromLoad: true);
        }
#endif
    }
}
