using UnityEngine;

namespace ScrollRoad
{
    // Skin takımları (Ortam Standardı v2 · Dolap, 2026-10-25): Yol (Garaj) ve Olta (FishingGear) dışındaki
    // ortamların takımları tek yerde. Takım = sahnenin ana nesnesinin renkleri (Palette; sırası ortamın
    // sahnesine özel) + ortam nesnesi (Tally: Tools/make_tallies.py, Resources/Tallies/<Tally>_<parça>; 2026-10-25
    // kadranın yerine) + nesnedeki sayının renkleri + isteğe bağlı döngü sesi. Merdiven sırası dizinin sırası
    // (UnlockLadder.SkinHours: 0/1/8/40 sa).
    public static class SkinCatalog
    {
        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static SkinInfo Skin(string id, string name, string blurb, string tally, string ink, string accent, params string[] palette) =>
            new SkinInfo
            {
                Id = id, Name = name, Blurb = blurb, Tally = tally,
                Ink = Hex(ink), Accent = Hex(accent),
                Palette = System.Array.ConvertAll(palette, Hex),
            };

        // Döner kapı — palet: [0] kapı metali, [1] cam.
        public static readonly SkinInfo[] Door =
        {
            Skin("classic", L.N("Classic"), L.N("Brushed steel frame, clear glass"), "door_classic", "#F2F4F6", "#F2F4F6"),
            Skin("brass", L.N("Brass"), L.N("Polished brass, warm amber glass"), "door_brass", "#F2F4F6", "#F2F4F6",
                "#B08A3E", "#F2E2B0"),
            Skin("black_steel", L.N("Black Steel"), L.N("Matte black frame, smoked glass"), "door_black", "#F2F4F6", "#F2F4F6",
                "#1E2024", "#9FBBCB"),
            Skin("rose_gold", L.N("Rose Gold"), L.N("Rose gold frame, blush glass"), "door_rose", "#F2F4F6", "#F2F4F6",
                "#C98A7A", "#F6DCD8"),
        };

        // Rüzgârgülü — palet: [0] türbin, [1] türbin gölgesi, [2] enerji darbeleri.
        public static readonly SkinInfo[] Wind =
        {
            Skin("classic", L.N("Classic"), L.N("White three-blade turbine"), "wind_classic", "#F2F4F6", "#F2F4F6"),
            Skin("timber", L.N("Timber"), L.N("A wooden turbine, old-mill style"), "wind_timber", "#F3E7CF", "#F2F4F6",
                "#C9A06A", "#A07A4A", "#FFD866"),
            Skin("midnight", L.N("Midnight"), L.N("Matte black blades, cyan current"), "wind_midnight", "#7FE0FF", "#F2F4F6",
                "#3A3E46", "#2A2D33", "#7FE0FF"),
            Skin("golden", L.N("Golden"), L.N("A gilded turbine for a bright valley"), "wind_gold", "#FFF0C0", "#F2F4F6",
                "#E6C46E", "#C9A24A", "#FFF0B0"),
        };

        // Plak — palet: [0] gövde, [1] gövde kenarı, [2] tabla, [3] kol, [4] koyu metal.
        public static readonly SkinInfo[] Vinyl =
        {
            Skin("classic", L.N("Classic"), L.N("Walnut deck, chrome tonearm"), "vinyl_classic", "#EADFC8", "#6E4B2A"),
            Skin("white", L.N("Studio White"), L.N("A clean white studio deck"), "vinyl_white", "#F2F4F6", "#2A3A4A",
                "#F2F2F0", "#C9CED4", "#9AA0A8", "#E6E8EA", "#5E646C"),
            Skin("dj", L.N("DJ Black"), L.N("Black club deck with a red tonearm"), "vinyl_dj", "#2A2D33", "#E23B3B",
                "#2A2D33", "#16181B", "#3A3E46", "#E23B3B", "#1A1C20"),
            Skin("golden", L.N("Golden"), L.N("A gold-plated collector's deck"), "vinyl_gold", "#FFF0C0", "#9C7A2E",
                "#E6C46E", "#9C7A2E", "#C9A24A", "#FFF0B0", "#7A5E22"),
        };

        // Dünya — palet: [0] okyanus, [1] yeşil kara, [2] kum kara, [3] atmosfer.
        public static readonly SkinInfo[] Globe =
        {
            Skin("classic", L.N("Classic"), L.N("Blue oceans, green lands"), "globe_classic", "#2A2A2A", "#C8323C"),
            Skin("red_planet", L.N("Red Planet"), L.N("Rust plains and dusty highlands"), "globe_red", "#2A2A2A", "#A9502E",
                "#A9502E", "#D9824E", "#E8B07A", "#FFB38A38"),
            Skin("ice_world", L.N("Ice World"), L.N("Frozen seas, snow-white lands"), "globe_ice", "#2A2A2A", "#4AA3C4",
                "#7FC4DD", "#F4FAFC", "#CFE6EE", "#DFF6FF40"),
            Skin("golden", L.N("Golden"), L.N("A world cast in gold"), "globe_gold", "#2A2A2A", "#C9A24A",
                "#B8892A", "#F2D06B", "#FFF0B8", "#FFE7A040"),
        };

        // Bumerang — palet: [0] bumerang ahşabı, [1] bumerang şeridi.
        public static readonly SkinInfo[] Boomerang =
        {
            Skin("classic", L.N("Classic"), L.N("Hand-carved hardwood"), "boomerang_classic", "#4A2A18", "#4A2A18"),
            Skin("sky_blue", L.N("Sky Blue"), L.N("Painted bright blue, quick in the air"), "boomerang_blue", "#F2F8FC", "#F2F8FC",
                "#3E8FD1", "#BFE8FF"),
            Skin("carbon", L.N("Carbon"), L.N("Black carbon fibre, sharp red edges"), "boomerang_carbon", "#FF5A48", "#FF5A48",
                "#2A2D33", "#FF5A48"),
            Skin("golden", L.N("Golden"), L.N("A gleaming golden boomerang"), "boomerang_gold", "#3A2A08", "#3A2A08",
                "#E2B647", "#FFF4C2"),
        };

        // Çömlek — palet: [0] çark gövdesi, [1] çark başlığı, [2] tava, [3] çamur.
        public static readonly SkinInfo[] Pottery =
        {
            Skin("classic", L.N("Classic"), L.N("Grey steel wheel, red clay"), "pottery_classic", "#3D2A1C", "#3D2A1C"),
            Skin("kick_wheel", L.N("Kick Wheel"), L.N("An old wooden kick wheel, stoneware clay"), "pottery_wood", "#3B2A1A", "#3B2A1A",
                "#6E4B2A", "#B98A55", "#8A5A32", "#8C7A66"),
            Skin("porcelain", L.N("Porcelain"), L.N("White studio wheel, porcelain clay"), "pottery_porcelain", "#2A3A4A", "#2A3A4A",
                "#D5DADF", "#FFFFFF", "#B8C0C8", "#EEE8DC"),
            Skin("golden", L.N("Golden"), L.N("A gilded wheel for a master potter"), "pottery_gold", "#3A2A08", "#3A2A08",
                "#3A2F1C", "#E6C46E", "#9C7A2E", "#B9733E"),
        };

        // Soyacak — palet: [0] bıçak bileziği, [1] sap, [2] ağız (2026-10-25: elle soyma, makine yok).
        public static readonly SkinInfo[] Peeler =
        {
            Skin("classic", L.N("Classic"), L.N("A paring knife with a red handle"), "peeler_classic", "#F4EEDF", "#F4EEDF"),
            Skin("brass", L.N("Brass"), L.N("Brass bolster, walnut handle"), "peeler_brass", "#F6E7C4", "#F6E7C4",
                "#A07A34", "#5A3A1F", "#E6C46E"),
            Skin("chrome", L.N("Chrome"), L.N("Mirror chrome, black grip"), "peeler_chrome", "#E6E8EA", "#E6E8EA",
                "#B8BEC6", "#1A1C20", "#F2F4F6"),
            Skin("mint", L.N("Mint"), L.N("A retro mint kitchen knife"), "peeler_mint", "#E8F6F0", "#E8F6F0",
                "#6FBFA6", "#F2EDE0", "#DDE2E6"),
        };

        // Kebap — palet: [0] şiş çeliği, [1] ızgara ışığı 1, [2] ızgara ışığı 2, [3] rezistans (2026-10-25: dikey döner).
        public static readonly SkinInfo[] Kebab =
        {
            Skin("classic", L.N("Classic"), L.N("Steel spit, a classic doner grill"), "kebab_classic", "#FF3B30", "#FF3B30"),
            Skin("copper", L.N("Copper"), L.N("A hammered copper spit"), "kebab_copper", "#FFB020", "#FFB020",
                "#C77B4A", "#FF9E26", "#FFD866", "#FF7A28"),
            Skin("blue_flame", L.N("Blue Flame"), L.N("A roaring blue gas grill"), "kebab_blue", "#5CC8FF", "#5CC8FF",
                "#DDE2E6", "#3E8FD1", "#BFE8FF", "#2E6FA8"),
            Skin("golden", L.N("Golden"), L.N("A golden spit for the sultan's kitchen"), "kebab_gold", "#FFD866", "#FFD866",
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
