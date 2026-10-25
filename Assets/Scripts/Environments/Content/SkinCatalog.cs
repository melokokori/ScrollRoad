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
        };

        // Bumerang — palet: [0] bumerang ahşabı, [1] bumerang şeridi.
        public static readonly SkinInfo[] Boomerang =
        {
            Skin("classic", "Classic", "Hand-carved hardwood", "boomerang_classic", "#F3E2C7", "#4A2A18"),
        };

        // Çömlek — palet: [0] çark gövdesi, [1] çark başlığı, [2] tava, [3] çamur.
        public static readonly SkinInfo[] Pottery =
        {
            Skin("classic", "Classic", "Grey steel wheel, red clay", "pottery_classic", "#EFE3CF", "#3D2A1C"),
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
