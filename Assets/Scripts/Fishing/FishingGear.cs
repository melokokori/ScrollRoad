using System;
using UnityEngine;

namespace ScrollRoad
{
    // Olta takımı skinleri (2026-10-15): kamış + makara + makara kadranı + makara sesi birlikte değişir.
    // Kozmetik, skora etkisiz; ömür boyu tutulan balıkla açılır (mekân merdiveninin arasına serpiştirildi:
    // Nehir 20, Kıyı 50, Açık deniz 100, Buz 180). Seçim olta kaydında (FishingProgress.gear).
    //  - Kamışın elden uca kısmı ve makara FishingSceneView'da kodla çizilir (balıkçı görsellerinden
    //    Tools/strip_rods.py ile silindi); sap, elin arkasında kalan kısım görselde kalır.
    //  - Bambu takımda makara yerine merkez makara (centerpin): tamburun kendisi döner, telleri görünür.
    //  - Makara kadranı Tools/make_gauges.py ile (Resources/Gauges/<Dial>_face|needle), ses Tools/gen_sound.py --env.
    public static class FishingGear
    {
        public sealed class Gear
        {
            public string Id;
            public string Name;
            public string Blurb;
            public int UnlockTotalFish;
            public Color Blank, Guide, Body, Spool, Accent, Handle;
            public bool Centerpin;
            public string ReelSound; // Resources/EnvSounds
            public string Dial;      // Resources/Gauges/<Dial>_face|needle
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static readonly Gear Classic = new Gear
        {
            Id = "classic", Name = "Classic", Blurb = "Graphite rod, aluminium spinning reel", UnlockTotalFish = 0,
            Blank = Hex("#26314A"), Guide = Hex("#9AA3AE"), Body = Hex("#C3CAD2"), Spool = Hex("#26314A"),
            Accent = Hex("#E8644A"), Handle = Hex("#26314A"), ReelSound = "reel_loop", Dial = "reel_classic",
        };

        public static readonly Gear Bamboo = new Gear
        {
            Id = "bamboo", Name = "Bamboo", Blurb = "Split-cane rod, wooden centrepin", UnlockTotalFish = 10,
            Blank = Hex("#C9A15E"), Guide = Hex("#6E4B2A"), Body = Hex("#8A5A32"), Spool = Hex("#B98A55"),
            Accent = Hex("#E7D3A8"), Handle = Hex("#5A3A1F"), Centerpin = true, ReelSound = "reel_loop_centerpin", Dial = "reel_bamboo",
        };

        public static readonly Gear Carbon = new Gear
        {
            Id = "carbon", Name = "Carbon", Blurb = "Carbon blank, sealed high-speed reel", UnlockTotalFish = 75,
            Blank = Hex("#1A1C20"), Guide = Hex("#D8DCE0"), Body = Hex("#2B2F36"), Spool = Hex("#16181B"),
            Accent = Hex("#E23B3B"), Handle = Hex("#E23B3B"), ReelSound = "reel_loop_carbon", Dial = "reel_carbon",
        };

        public static readonly Gear Gold = new Gear
        {
            Id = "gold", Name = "Gold", Blurb = "Gold-wrapped rod, polished brass reel", UnlockTotalFish = 250,
            Blank = Hex("#D9B455"), Guide = Hex("#3A2F1C"), Body = Hex("#E6C46E"), Spool = Hex("#9C7A2E"),
            Accent = Hex("#FFF1C4"), Handle = Hex("#B8902F"), ReelSound = "reel_loop_gold", Dial = "reel_gold",
        };

        public static readonly Gear[] All = { Classic, Bamboo, Carbon, Gold };

        public static event Action Changed;

        public static Gear ById(string id)
        {
            foreach (var g in All) if (g.Id == id) return g;
            return Classic;
        }

        public static bool IsUnlocked(Gear g, FishingProgress p) =>
            g.UnlockTotalFish <= 0 || CarCatalog.ReviewUnlockAll || (p != null && p.totalFish >= g.UnlockTotalFish);

        // Seçili takım (kilitliyse Klasik).
        public static Gear SelectedIn(FishingProgress p)
        {
            var g = ById(p != null ? p.gear : "");
            return IsUnlocked(g, p) ? g : Classic;
        }

        // Her karede okunur (sahne, kadran, ses): kayıt diskten okunmaz, olta yoksa Klasik.
        public static Gear Selected
        {
            get
            {
                var fishing = FishingController.Instance;
                return fishing != null ? SelectedIn(fishing.Progress) : Classic;
            }
        }

        public static void Select(Gear g)
        {
            var fishing = FishingController.Instance;
            if (g == null || fishing == null || !IsUnlocked(g, fishing.Progress)) return;
            fishing.SetGear(g.Id);
            Changed?.Invoke();
        }
    }
}
