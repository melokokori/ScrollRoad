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

        // Döner kapı — palet: [0] kapı metali, [1] cam.
        public static readonly SkinInfo[] Door =
        {
            Skin("classic", "Classic", "Brushed steel frame, clear glass", "door_classic", "#E8EDF1", "#2F3640"),
            Skin("brass", "Brass", "Polished brass, warm amber glass", "door_brass", "#F6E7C4", "#3A2A1A",
                "#B08A3E", "#F2E2B0"),
            Skin("black_steel", "Black Steel", "Matte black frame, smoked glass", "door_black", "#E6E8EA", "#16181B",
                "#1E2024", "#9FBBCB"),
            Skin("rose_gold", "Rose Gold", "Rose gold frame, blush glass", "door_rose", "#FBE9E4", "#4A2420",
                "#C98A7A", "#F6DCD8"),
        };

        // Rüzgârgülü — palet: [0] türbin, [1] türbin gölgesi, [2] enerji darbeleri.
        public static readonly SkinInfo[] Wind =
        {
            Skin("classic", "Classic", "White three-blade turbine", "wind_classic", "#EEF3EA", "#2E4A22"),
            Skin("timber", "Timber", "A wooden turbine, old-mill style", "wind_timber", "#F3E7CF", "#3B2A1A",
                "#C9A06A", "#A07A4A", "#FFD866"),
            Skin("midnight", "Midnight", "Matte black blades, cyan current", "wind_midnight", "#E6E8EA", "#16181B",
                "#3A3E46", "#2A2D33", "#7FE0FF"),
            Skin("golden", "Golden", "A gilded turbine for a bright valley", "wind_gold", "#FFF0C0", "#3A2A08",
                "#E6C46E", "#C9A24A", "#FFF0B0"),
        };

        // Plak — palet: [0] gövde, [1] gövde kenarı, [2] tabla, [3] kol, [4] koyu metal.
        public static readonly SkinInfo[] Vinyl =
        {
            Skin("classic", "Classic", "Walnut deck, chrome tonearm", "vinyl_classic", "#EADFC8", "#1E1A17"),
            Skin("white", "Studio White", "A clean white studio deck", "vinyl_white", "#F2F4F6", "#2A3A4A",
                "#F2F2F0", "#C9CED4", "#9AA0A8", "#E6E8EA", "#5E646C"),
            Skin("dj", "DJ Black", "Black club deck with a red tonearm", "vinyl_dj", "#E6E8EA", "#16181B",
                "#2A2D33", "#16181B", "#3A3E46", "#E23B3B", "#1A1C20"),
            Skin("golden", "Golden", "A gold-plated collector's deck", "vinyl_gold", "#FFF0C0", "#3A2A08",
                "#E6C46E", "#9C7A2E", "#C9A24A", "#FFF0B0", "#7A5E22"),
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
            Skin("brass", "Brass", "Polished brass with a walnut handle", "peeler_brass", "#F6E7C4", "#3A2A1A",
                "#A07A34", "#5A3A1F", "#E6C46E"),
            Skin("chrome", "Chrome", "Mirror chrome, black grip", "peeler_chrome", "#F2F4F6", "#1A1C20",
                "#B8BEC6", "#1A1C20", "#F2F4F6"),
            Skin("mint", "Mint", "A retro mint kitchen gadget", "peeler_mint", "#E8F6F0", "#1E4A3E",
                "#6FBFA6", "#F2EDE0", "#DDE2E6"),
        };

        // Kebap — palet: [0] şiş çeliği, [1] alev 1, [2] alev 2, [3] kor.
        public static readonly SkinInfo[] Kebab =
        {
            Skin("classic", "Classic", "Steel spit over a wood fire", "kebab_classic", "#F2E8D8", "#3A2015"),
            Skin("copper", "Copper", "A hammered copper spit", "kebab_copper", "#F6E2D2", "#4A2414",
                "#C77B4A", "#FF9E26", "#FFD866", "#FF7A28"),
            Skin("blue_flame", "Blue Flame", "A roaring gas burner", "kebab_blue", "#E2F1FA", "#123A5A",
                "#DDE2E6", "#3E8FD1", "#BFE8FF", "#2E6FA8"),
            Skin("golden", "Golden", "A golden spit for the sultan's kitchen", "kebab_gold", "#FFF0C0", "#3A2A08",
                "#E6C46E", "#FFC24A", "#FFF0B0", "#FF8A2A"),
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
