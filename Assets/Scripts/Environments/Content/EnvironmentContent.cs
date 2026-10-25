using System;
using System.Collections.Generic;

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
        public string Dial; // Resources/Gauges/<Dial>_face önizlemesi; boşsa yok
    }

    public interface IEnvironmentContent
    {
        EnvironmentDef Def { get; }
        // Bu ortamda ömür boyu biten öğelerin puanı (yalnız seçiliyken ilerler). Merdiven bununla açılır.
        double LifetimePoints { get; }

        string VenueNoun { get; }        // duyurularda: "spot", "venue", "place"
        VenueInfo[] Venues { get; }      // merdiven sırasıyla
        string VenueId { get; }
        void SetVenue(string id);        // açık olduğunu çağıran denetler (UnlockLadder)
        RareInfo[] RaresAt(string venueId);
        bool IsLogged(string rareId);

        string SkinNoun { get; }         // duyurularda: "tackle", "look"
        string SkinBoxName { get; }      // Dolap'ın adı (çekmece satırı ve panel başlığı): "Tackle box"
        SkinInfo[] Skins { get; }        // merdiven sırasıyla; ilki hep açık
        string SkinId { get; }
        void SetSkin(string id);

        // Bu sürümden önce ortamın kendi kaydında "duyuruldu" diye tutulan açılmalar (bir kez taşınır).
        IEnumerable<string> LegacyAnnounced { get; }
    }

    // Ortamdan bağımsız, ortak içerik kaydı (yerel PlayerPrefs JSON): Odak seçimi ve duyurulmuş açılmalar.
    [Serializable]
    public class ContentState
    {
        public bool migrated;
        public string focus = "";   // seçili mekânın Odak'taki nadiri (RareInfo.Id); boş = yok
        public List<string> announced = new List<string>(); // "venue:<id>" / "skin:<id>"
    }

    public static class EnvironmentContent
    {
        // Skin takımı henüz olmayan ortamların tek, hep açık takımı (Ortam Standardı v2'nin içerik adımına kadar).
        public static readonly SkinInfo[] ClassicOnly = { new SkinInfo { Id = "classic", Name = "Classic", Blurb = "The original look" } };

        static readonly Dictionary<EnvironmentId, IEnvironmentContent> Registry = new Dictionary<EnvironmentId, IEnvironmentContent>();
        static readonly Dictionary<EnvironmentId, ContentState> States = new Dictionary<EnvironmentId, ContentState>();

        public static event Action<EnvironmentId> Changed; // mekân, Odak ya da skin değişti

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
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
    }
}
