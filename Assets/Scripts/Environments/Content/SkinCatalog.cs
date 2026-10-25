using UnityEngine;

namespace ScrollRoad
{
    // Skin takımları (Ortam Standardı v2 · Dolap, 2026-10-25): Yol (Garaj) ve Olta (FishingGear) dışındaki
    // ortamların takımları tek yerde. Takım = sahnenin ana nesnesinin renkleri (Palette; sırası ortamın
    // sahnesine özel) + kadran (Dial: Tools/make_gauges.py, Resources/Gauges/<Dial>_face|needle) + kadran sayacı
    // mürekkebi + isteğe bağlı döngü sesi. Merdiven sırası dizinin sırası (UnlockLadder.SkinHours: 0/1/8/40 sa).
    public static class SkinCatalog
    {
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static SkinInfo Skin(string id, string name, string blurb, string dial, string ink, string inkLast, params string[] palette) =>
            new SkinInfo
            {
                Id = id, Name = name, Blurb = blurb, Dial = dial,
                DialInk = Hex(ink), DialInkLast = Hex(inkLast),
                Palette = System.Array.ConvertAll(palette, Hex),
            };

        // Döner kapı — palet: [0] kapı çerçevesi, [1] cam, [2] kanat kenarı, [3] saçak.
        public static readonly SkinInfo[] Door =
        {
            Skin("classic", "Classic", "Brushed steel frame, clear glass", "door_classic", "#E8EDF1", "#2F3640"),
        };

        // Rüzgârgülü — palet: [0] kule/gövde, [1] pervane, [2] pervane ucu, [3] gövde ayrıntısı.
        public static readonly SkinInfo[] Wind =
        {
            Skin("classic", "Classic", "White three-blade turbine", "wind_classic", "#EEF3EA", "#2E4A22"),
        };

        // Plak — palet: [0] pikap gövdesi, [1] tabla, [2] kol, [3] ayrıntı.
        public static readonly SkinInfo[] Vinyl =
        {
            Skin("classic", "Classic", "Walnut deck, chrome tonearm", "vinyl_classic", "#EADFC8", "#1E1A17"),
        };

        // Dünya — palet: [0] okyanus, [1] yeşil kara, [2] kum kara, [3] atmosfer.
        public static readonly SkinInfo[] Globe =
        {
            Skin("classic", "Classic", "Blue oceans, green lands", "globe_classic", "#F2E6C2", "#1C2547"),
            Skin("red_planet", "Red Planet", "Rust plains and dusty highlands", "globe_red", "#F6D9C4", "#5A1F12",
                "#A9502E", "#D9824E", "#E8B07A", "#FFB38A38"),
            Skin("ice_world", "Ice World", "Frozen seas, snow-white lands", "globe_ice", "#E4F6FC", "#123A4A",
                "#7FC4DD", "#F4FAFC", "#CFE6EE", "#DFF6FF40"),
            Skin("golden", "Golden", "A world cast in gold", "globe_gold", "#FFF0C0", "#3A2A08",
                "#B8892A", "#F2D06B", "#FFF0B8", "#FFE7A040"),
        };

        // Bumerang — palet: [0] bumerang ahşabı, [1] bumerang şeridi.
        public static readonly SkinInfo[] Boomerang =
        {
            Skin("classic", "Classic", "Hand-carved hardwood", "boomerang_classic", "#F3E2C7", "#4A2A18"),
            Skin("sky_blue", "Sky Blue", "Painted bright blue, quick in the air", "boomerang_blue", "#E2F1FA", "#123A5A",
                "#3E8FD1", "#BFE8FF"),
            Skin("carbon", "Carbon", "Black carbon fibre, sharp red edges", "boomerang_carbon", "#E6E8EA", "#16181B",
                "#2A2D33", "#FF5A48"),
            Skin("golden", "Golden", "A gleaming golden boomerang", "boomerang_gold", "#FFF0C0", "#3A2A08",
                "#E2B647", "#FFF4C2"),
        };

        // Çömlek — palet: [0] çark gövdesi, [1] çark başlığı, [2] tava, [3] çamur.
        public static readonly SkinInfo[] Pottery =
        {
            Skin("classic", "Classic", "Grey steel wheel, red clay", "pottery_classic", "#EFE3CF", "#3D2A1C"),
            Skin("kick_wheel", "Kick Wheel", "An old wooden kick wheel, stoneware clay", "pottery_wood", "#F3E7CF", "#3B2A1A",
                "#6E4B2A", "#B98A55", "#8A5A32", "#8C7A66"),
            Skin("porcelain", "Porcelain", "White studio wheel, porcelain clay", "pottery_porcelain", "#F2F4F6", "#2A3A4A",
                "#D5DADF", "#FFFFFF", "#B8C0C8", "#EEE8DC"),
            Skin("golden", "Golden", "A gilded wheel for a master potter", "pottery_gold", "#FFF0C0", "#3A2A08",
                "#3A2F1C", "#E6C46E", "#9C7A2E", "#B9733E"),
        };

        // Soyacak — palet: [0] demir gövde, [1] kol tutamağı, [2] bıçak.
        public static readonly SkinInfo[] Peeler =
        {
            Skin("classic", "Classic", "Cast iron with a red handle", "peeler_classic", "#F4EEDF", "#2A2D33"),
        };

        // Kebap — palet: [0] şiş çeliği, [1] ocak tuğlası, [2] tezgâh.
        public static readonly SkinInfo[] Kebab =
        {
            Skin("classic", "Classic", "Steel spit over a brick grill", "kebab_classic", "#F2E8D8", "#3A2015"),
        };

        public static SkinInfo[] For(EnvironmentId id) => id switch
        {
            EnvironmentId.Door => Door,
            EnvironmentId.Wind => Wind,
            EnvironmentId.Vinyl => Vinyl,
            EnvironmentId.Globe => Globe,
            EnvironmentId.Boomerang => Boomerang,
            EnvironmentId.Pottery => Pottery,
            EnvironmentId.Peeler => Peeler,
            EnvironmentId.Kebab => Kebab,
            _ => EnvironmentContent.ClassicOnly,
        };
    }
}
