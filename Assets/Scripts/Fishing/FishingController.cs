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
    // Mekânlar (2026-10-14): Göl → Nehir…; Ortam Standardı v2 (2026-10-25) ile Olta'da geçen seyir saatiyle
    // açılır (UnlockLadder), duyuruyu ContentUnlockWatcher yapar. Oyuncu açık bir mekâna geçebilir. Mekân
    // değişince sarılan puan korunur, yalnız süren atışın türü yeni mekândan yeniden çekilir.
    //
    // Yalnız Olta seçiliyken ilerler. Görünüm FishingSceneView'da, HUD FishingHudController'da.
    public class FishingController : MonoBehaviour, IEnvironmentContent
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
            UnlockLadder.IsVenueUnlocked(LifetimePointsOf(progress), Array.IndexOf(FishCatalog.Spots, spot));

        // Olta'da ömür boyu tutulan balıkların puanı (merdiven bununla açılır).
        public static double LifetimePointsOf(FishingProgress progress) =>
            progress == null ? 0.0 : progress.totalFish * EnvironmentCatalog.FishIntervalPoints;

        // ---- IEnvironmentContent (Ortam Standardı v2)
        static VenueInfo[] venueInfos;
        static SkinInfo[] skinInfos;

        public EnvironmentDef Def => EnvironmentCatalog.Fishing;
        public double LifetimePoints => LifetimePointsOf(state);
        public string VenueNoun => "spot";
        public VenueInfo[] Venues => venueInfos ??= Array.ConvertAll(FishCatalog.Spots, s => new VenueInfo { Id = s.Id, Name = s.Name });
        public string VenueId => Spot.Id;
        public void SetVenue(string id) => SelectSpot(FishCatalog.GetSpot(id));
        public RareInfo[] RaresAt(string venueId) =>
            Array.ConvertAll(FishCatalog.GetSpot(venueId).Species, f => new RareInfo { Id = f.Id, Name = f.Name, Frequency = f.Frequency });
        public bool IsLogged(string rareId) => state.Find(rareId) != null;
        public int TimesFound(string rareId) => state.Find(rareId)?.count ?? 0;
        public long NextItemIndex => state.totalFish + 1;
        public double MonthUnits => state.monthFish + Math.Min(Reeled, 0.999f);
        public string SkinNoun => "tackle";
        public string SkinBoxName => "Tackle box";
        public SkinInfo[] Skins => skinInfos ??= Array.ConvertAll(FishingGear.All, g => new SkinInfo { Id = g.Id, Name = g.Name, Blurb = g.Blurb, Dial = g.Dial });
        public string SkinId => FishingGear.SelectedIn(state).Id;
        public void SetSkin(string id)
        {
            SetGear(id);
            FishingGear.RaiseChanged();
        }
        public System.Collections.Generic.IEnumerable<string> LegacyAnnounced
        {
            get
            {
                foreach (var id in state.announcedSpots) yield return "venue:" + id;
                if (state.announcedGear != null) foreach (var id in state.announcedGear) yield return "skin:" + id;
            }
        }

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
            EnvironmentContent.Register(this);
            if (CheckMonth() || CastFish == null) NewCast(fromLoad: true);
            else CurrentPhase = state.castProgress / Math.Max(1e-6, RequiredPoints) >= state.strikeAt ? Phase.Hooked : Phase.Retrieving;
        }

        void OnDestroy()
        {
            Save();
            EnvironmentContent.Unregister(this);
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
            var species = FishCatalog.Roll(Spot, rng, state.totalFish, out float kg, EnvironmentContent.FocusOf(EnvironmentId.Fishing));
            // Sipariş ödülü (Ortam Standardı v2 · Dayanak 4): bu mekânın eksik türü bir sonraki atışta.
            var content = EnvironmentContent.State(EnvironmentId.Fishing);
            var reward = content.rewardVenue == Spot.Id ? FishCatalog.Get(content.rewardRare) : null;
            if (reward != null && Spot.Contains(reward.Id))
            {
                species = reward;
                double r = rng.NextDouble();
                kg = (float)Math.Round(reward.MinKg + (reward.MaxKg - reward.MinKg) * r * r, 2);
                content.rewardRare = "";
                EnvironmentContent.Save(EnvironmentId.Fishing);
            }
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
