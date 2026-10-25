using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Ortam Standardı v2 (2026-10-25, Docs/ortamlar.md): her ortamın mekânları, nadirleri ve skin takımları
    // tek bir arayüzden okunur. Açılma merdiveni (UnlockLadder), açılma duyuruları (ContentUnlockWatcher),
    // Mekânlar/Odak ve Dolap panelleri bunun üzerinde ortak çalışır; verinin sahibi ortamın kendi
    // denetleyicisidir (FishingController, DoorController, WindController, VinylController, IntervalEnvironment).
    // Yol bu arayüzü uygulamaz: bölgeleri tek yol boyunca, skinleri (araçlar) Garaj'da.
    public sealed class VenueInfo
    {
        public string Id;
        public string Name;
    }

    public sealed class RareInfo
    {
        public string Id;
        public string Name;
        public float Frequency; // mekânın nadirleri arasında göreli sıklık (Odak bunu çarpar)
    }

    public sealed class SkinInfo
    {
        public string Id;
        public string Name;
        public string Blurb;
        public string Dial; // Resources/Gauges/<Dial>_face|needle (kadran + Dolap önizlemesi); boşsa yok
        public Color DialInk = new Color(0.91f, 0.93f, 0.95f);      // kadran sayacının rakamları
        public Color DialInkLast = new Color(0.12f, 0.16f, 0.27f);  // son (ondalık) hane
        // Sahnenin ana nesnesinin renkleri (sırası ortamın sahnesine özel; eksik olan varsayılan rengini kullanır).
        public Color[] Palette = Array.Empty<Color>();
        public string Sound; // ortamın döngü sesinin bu takımdaki karşılığı (Resources/EnvSounds); boşsa varsayılan
    }

    public interface IEnvironmentContent
    {
        EnvironmentDef Def { get; }
        // Bu ortamda ömür boyu biten öğelerin puanı (yalnız seçiliyken ilerler). Merdiven bununla açılır.
        double LifetimePoints { get; }
        // Bu ayın birimi, sıradaki öğenin kesriyle (kadran sayacı).
        double MonthUnits { get; }

        string VenueNoun { get; }        // duyurularda: "spot", "venue", "place"
        VenueInfo[] Venues { get; }      // merdiven sırasıyla
        string VenueId { get; }
        void SetVenue(string id);        // açık olduğunu çağıran denetler (UnlockLadder)
        RareInfo[] RaresAt(string venueId);
        bool IsLogged(string rareId);
        int TimesFound(string rareId);   // kaç kez geldi (dönüş özeti farkı)

        // Sıradaki tamamlanacak öğenin ayın kaçıncısı olduğu (sipariş ödülü bunun birkaç öğe ilerisine konur).
        long NextItemIndex { get; }

        string SkinNoun { get; }         // duyurularda: "tackle", "look"
        string SkinBoxName { get; }      // Dolap'ın adı (çekmece satırı ve panel başlığı): "Tackle box"
        SkinInfo[] Skins { get; }        // merdiven sırasıyla; ilki hep açık
        string SkinId { get; }
        void SetSkin(string id);

        // Bu sürümden önce ortamın kendi kaydında "duyuruldu" diye tutulan açılmalar (bir kez taşınır).
        IEnumerable<string> LegacyAnnounced { get; }
    }

    // Ortamdan bağımsız, ortak içerik kaydı (yerel PlayerPrefs JSON): Odak seçimi, duyurulmuş açılmalar ve
    // sipariş ödülü (Dayanak 4: o mekânın eksik bir nadiri ayın rewardIndex. öğesinde gelir).
    [Serializable]
    public class ContentState
    {
        public bool migrated;
        public string focus = "";   // seçili mekânın Odak'taki nadiri (RareInfo.Id); boş = yok
        public List<string> announced = new List<string>(); // "venue:<id>" / "skin:<id>"
        public string rewardMonth = "";
        public string rewardVenue = "";
        public string rewardRare = "";
        public long rewardIndex;
    }

    // Kimlik fonksiyonlarına (Identify) geçen garanti: ayın Index. öğesi nadirdir; Rare doluysa tam o nadir.
    // Varsayılan (Index 0) hiçbir öğeye denk gelmez (sıra 1'den başlar).
    public struct RareGuarantee
    {
        public long Index;
        public string Rare;

        public T Pick<T>(long index, T chosen, T[] specials, Func<T, string> id) where T : class
        {
            if (index != Index || string.IsNullOrEmpty(Rare)) return chosen;
            string rare = Rare;
            return Array.Find(specials, s => id(s) == rare) ?? chosen;
        }
    }

    public static class EnvironmentContent
    {
        // Skin takımı henüz olmayan ortamların tek, hep açık takımı (Ortam Standardı v2'nin içerik adımına kadar).
        public static readonly SkinInfo[] ClassicOnly = { new SkinInfo { Id = "classic", Name = "Classic", Blurb = "The original look" } };

        static readonly Dictionary<EnvironmentId, IEnvironmentContent> Registry = new Dictionary<EnvironmentId, IEnvironmentContent>();
        static readonly Dictionary<EnvironmentId, ContentState> States = new Dictionary<EnvironmentId, ContentState>();

        public static event Action<EnvironmentId> Changed; // mekân, Odak ya da skin değişti

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Registry.Clear();
            States.Clear();
            Changed = null;
        }

        public static void Register(IEnvironmentContent content) => Registry[content.Def.Id] = content;

        public static void Unregister(IEnvironmentContent content)
        {
            if (Registry.TryGetValue(content.Def.Id, out var c) && c == content) Registry.Remove(content.Def.Id);
        }

        public static IEnvironmentContent Get(EnvironmentId id) => Registry.TryGetValue(id, out var c) ? c : null;

        // Seçili ortamın içeriği; Yol'da null.
        public static IEnvironmentContent Current => Get(EnvironmentManager.Current.Id);

        static string Key(EnvironmentId id) => "ScrollRoad_Content_" + id;

        public static ContentState State(EnvironmentId id)
        {
            if (States.TryGetValue(id, out var s)) return s;
            s = SaveService.LoadJson<ContentState>(Key(id));
            if (s.announced == null) s.announced = new List<string>();
            States[id] = s;
            return s;
        }

        public static void Save(EnvironmentId id) => SaveService.SaveJson(Key(id), State(id));

        public static int VenueIndex(IEnvironmentContent c, string venueId) =>
            Array.FindIndex(c.Venues, v => v.Id == venueId);

        public static int SkinIndex(IEnvironmentContent c, string skinId) =>
            Array.FindIndex(c.Skins, s => s.Id == skinId);

        public static bool IsVenueUnlocked(IEnvironmentContent c, int index) =>
            UnlockLadder.IsVenueUnlocked(c.LifetimePoints, index);

        public static bool IsSkinUnlocked(IEnvironmentContent c, int index) =>
            UnlockLadder.IsSkinUnlocked(c.LifetimePoints, index);

        public static bool SelectVenue(IEnvironmentContent c, string venueId)
        {
            int index = VenueIndex(c, venueId);
            if (index < 0 || !IsVenueUnlocked(c, index)) return false;
            if (c.VenueId == venueId) return true;
            c.SetVenue(venueId);
            var state = State(c.Def.Id);
            if (state.focus != "")
            {
                state.focus = ""; // Odak mekânın nadirlerinden biridir; yeni mekânda geçersiz
                Save(c.Def.Id);
            }
            Notify(c.Def.Id);
            return true;
        }

        // ---- Odak (Dayanak 3): seçili mekânın bir nadiri seçilir, nadirler arasındaki göreli sıklığı ×3 olur.
        // Nadir çıkma olasılığını ve puanı değiştirmez; yalnız hangi nadirin geleceğini.
        public const float FocusMultiplier = 3f;

        public static string FocusOf(EnvironmentId id) => State(id).focus;

        public static float Weight(string rareId, float frequency, string focus) =>
            !string.IsNullOrEmpty(focus) && rareId == focus ? frequency * FocusMultiplier : frequency;

        // Aynı nadire yeniden basınca Odak kalkar.
        public static void ToggleFocus(IEnvironmentContent c, string rareId)
        {
            var state = State(c.Def.Id);
            state.focus = state.focus == rareId ? "" : rareId ?? "";
            Save(c.Def.Id);
            Notify(c.Def.Id);
        }

        public static bool SelectSkin(IEnvironmentContent c, string skinId)
        {
            int index = SkinIndex(c, skinId);
            if (index < 0 || !IsSkinUnlocked(c, index)) return false;
            if (c.SkinId == skinId) return true;
            c.SetSkin(skinId);
            Notify(c.Def.Id);
            return true;
        }

        public static void Notify(EnvironmentId id) => Changed?.Invoke(id);

        // Merdivende sıradaki açılma (mekân ya da skin, hangisi önce): ad ve kalan puan. Hepsi açıksa false.
        public static bool NextUnlock(IEnvironmentContent c, out string name, out double remaining)
        {
            name = null;
            remaining = double.MaxValue;
            double points = c.LifetimePoints;
            for (int i = 1; i < c.Venues.Length; i++)
            {
                double left = UnlockLadder.VenuePoints(i) - points;
                if (left > 0 && left < remaining) { remaining = left; name = c.Venues[i].Name; }
            }
            for (int i = 1; i < c.Skins.Length; i++)
            {
                double left = UnlockLadder.SkinPoints(i) - points;
                if (left > 0 && left < remaining) { remaining = left; name = $"{c.Skins[i].Name} {c.SkinNoun}"; }
            }
            return name != null;
        }

        // ---- İlk nadir garantisi (Dayanak 5, ödül eğrisi): ortamda henüz hiç nadir yoksa ayın ~10. seyir
        // dakikasındaki öğe nadirdir (hangi nadir olduğu yine ağırlıklı seçimle, Odak'a uyarak). Garanti bir kez
        // verilince o ay boyunca geçerli kalır (geri sarıp yeniden gelen aynı öğedir), sonraki aylarda yoktur.
        // Olta'da her balık zaten deftere girer (ilk balık ~20 dk), garanti yok.
        public const double FirstRarePoints = UnlockLadder.PointsPerHour / 6.0;

        // Bu ayın garantisi: önce bekleyen sipariş ödülü (bu mekân için), yoksa ilk nadir garantisi.
        public static RareGuarantee GuaranteeFor(EnvironmentId env, string venueId, double interval, bool logEmpty, string firstMonth, string month)
        {
            var st = State(env);
            if (st.rewardMonth == month && st.rewardVenue == venueId && !string.IsNullOrEmpty(st.rewardRare))
                return new RareGuarantee { Index = st.rewardIndex, Rare = st.rewardRare };
            return new RareGuarantee { Index = GuaranteeIndex(interval, logEmpty, firstMonth, month) };
        }

        // Sipariş ödülü: mekânın henüz bulunmamış nadirlerinden biri birkaç öğe sonra gelir (Olta'da bir sonraki
        // atışta). Mekânda eksik yoksa false (yalnız sipariş defterine damga).
        public static bool GrantReward(IEnvironmentContent c, string venueId, System.Random rng, out string rareName)
        {
            rareName = null;
            var missing = Array.FindAll(c.RaresAt(venueId), r => !c.IsLogged(r.Id));
            if (missing.Length == 0) return false;
            var rare = missing[rng.Next(missing.Length)];
            var st = State(c.Def.Id);
            st.rewardMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            st.rewardVenue = venueId;
            st.rewardRare = rare.Id;
            st.rewardIndex = c.NextItemIndex + rng.Next(2, 9);
            Save(c.Def.Id);
            Notify(c.Def.Id);
            rareName = rare.Name;
            return true;
        }

        // Garantili öğenin ayın kaçıncı öğesi olduğu; garanti yoksa -1.
        public static long GuaranteeIndex(double interval, bool logEmpty, string guaranteeMonth, string month)
        {
            bool active = guaranteeMonth == month || (logEmpty && string.IsNullOrEmpty(guaranteeMonth));
            return active ? Math.Max(1L, (long)Math.Ceiling(FirstRarePoints / interval - 1e-9)) : -1L;
        }
    }
}
