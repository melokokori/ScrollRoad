using UnityEngine;

namespace ScrollRoad
{
    // Meyve soyacağı (2026-10-14 prototip; Docs/ortamlar.md "Meyve soyacağı"; 2026-10-25 makine yerine elle soyan
    // manav): manav meyveyi avucunda döndürür, bıçak yukarıdan aşağı iner, kabuk tek parça spiral hâlinde uzayıp masaya sarkar; soyulan meyve
    // kâseye düşer. Birim SOYULAN MEYVE (her EnvironmentCatalog.FruitIntervalPoints'te bir). Kabuğun uzunluğu
    // meyvenin özelliği: "kopmayan kabuk" rekoru (en uzun tek parça kabuk). Aşağı scroll: kabuk geri sarılır —
    // meyve kâseden avuca döner, kabuk yeniden meyveye sarılır, sayaç düşer. Başarısızlık yok: kabuk kopmaz.
    //
    // Bir meyvenin evreleri (f = Phase): 0–0,10 meyve kasadan avuca gelir; 0,10–0,85 soyulur; 0,85–1 kâseye düşer
    // (f = 1'de sayılır).
    // loc:catalog — öğe/mekân adları çeviri anahtarı olarak toplanır (Tools/loc/extract.py).
    public class PeelerEnvironment : IntervalEnvironment
    {
        public const float MountEnd = 0.10f;
        public const float PeelEnd = 0.85f;

        // Size: kartta meyvenin boyu; Value: taban kabuk uzunluğu (cm); Color: kabuk; Color2: meyve eti;
        // Shape[0]: görselin üstünde soyulmayan kısım (sap, yaprak, taç) oranı.
        public static readonly ItemSite Stall = new ItemSite
        {
            Id = "stall",
            Name = "The Fruit Stall",
            // Nadir meyve %7,5: 1 meyve = 1.250 puan; gerçek zamanda diğer ortamların nadir sıklığına yakın
            // (seyirde ~17 dk'da bir). En nadiri (karpuz) seyirde ~9 saatte bir.
            SpecialChance = 0.075f,
            SpecialMinIndex = 12,
            Commons = new[]
            {
                Fruit("red_apple", "Red apple", 0f, 0.9f, 62f, new Color(0.80f, 0.17f, 0.17f), new Color(0.98f, 0.93f, 0.76f), 0.22f),
                Fruit("green_apple", "Green apple", 0f, 0.9f, 60f, new Color(0.56f, 0.76f, 0.26f), new Color(0.97f, 0.95f, 0.78f), 0.2f),
                Fruit("pear", "Pear", 0f, 1.05f, 66f, new Color(0.76f, 0.78f, 0.30f), new Color(0.98f, 0.95f, 0.80f), 0.16f),
                Fruit("orange", "Orange", 0f, 0.9f, 56f, new Color(0.96f, 0.56f, 0.12f), new Color(0.99f, 0.66f, 0.25f), 0.06f),
                Fruit("lemon", "Lemon", 0f, 0.8f, 46f, new Color(0.98f, 0.86f, 0.25f), new Color(0.99f, 0.94f, 0.62f), 0.04f),
                Fruit("peach", "Peach", 0f, 0.85f, 50f, new Color(0.96f, 0.62f, 0.45f), new Color(0.99f, 0.80f, 0.45f), 0.1f),
            },
            Specials = new[]
            {
                Fruit("mango", "Mango", 30f, 1.0f, 72f, new Color(0.86f, 0.55f, 0.22f), new Color(0.99f, 0.76f, 0.22f), 0.05f),
                Fruit("pineapple", "Pineapple", 24f, 1.45f, 110f, new Color(0.72f, 0.55f, 0.24f), new Color(0.99f, 0.88f, 0.40f), 0.42f),
                Fruit("dragon_fruit", "Dragon Fruit", 18f, 1.1f, 78f, new Color(0.90f, 0.25f, 0.50f), new Color(0.97f, 0.96f, 0.94f), 0.02f),
                Fruit("cantaloupe", "Cantaloupe", 14f, 1.2f, 140f, new Color(0.80f, 0.74f, 0.54f), new Color(0.99f, 0.66f, 0.38f), 0.03f),
                Fruit("pomelo", "Pomelo", 9f, 1.25f, 120f, new Color(0.80f, 0.85f, 0.36f), new Color(0.99f, 0.82f, 0.74f), 0.05f),
                Fruit("watermelon", "Watermelon", 5f, 1.4f, 260f, new Color(0.24f, 0.50f, 0.22f), new Color(0.95f, 0.36f, 0.38f), 0.02f),
            },
        };

        static ItemType Fruit(string id, string name, float frequency, float size, float peelCm, Color peel, Color flesh, float topKeep) =>
            new ItemType { Id = id, Name = name, Frequency = frequency, Size = size, Value = peelCm, Color = peel, Color2 = flesh, Shape = new[] { topKeep } };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // Ortam Standardı v2 (2026-10-25): dört yeni tezgâh. Palette: [0] duvar, [1] masa üstü, [2] masa önü, [3] kâse.
        static ItemSite Stand(string id, string name, ItemType[] commons, ItemType[] specials, params string[] palette) => new ItemSite
        {
            Id = id, Name = name, SpecialChance = 0.075f, SpecialMinIndex = 12,
            Commons = commons, Specials = specials, Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly ItemSite Tropical = Stand("tropical", "The Tropical Market",
            new[] { Fruit("kiwi", "Kiwi", 0f, 0.75f, 48f, new Color(0.55f, 0.41f, 0.24f), new Color(0.59f, 0.77f, 0.27f), 0.02f),
                    Fruit("lime", "Lime", 0f, 0.7f, 40f, new Color(0.36f, 0.66f, 0.23f), new Color(0.81f, 0.91f, 0.55f), 0.03f),
                    Fruit("guava", "Guava", 0f, 0.8f, 50f, new Color(0.67f, 0.78f, 0.35f), new Color(0.96f, 0.55f, 0.55f), 0.05f),
                    Fruit("passion_fruit", "Passion fruit", 0f, 0.75f, 44f, new Color(0.43f, 0.20f, 0.35f), new Color(0.98f, 0.78f, 0.24f), 0.04f),
                    Fruit("lychee", "Lychee", 0f, 0.6f, 34f, new Color(0.82f, 0.27f, 0.31f), new Color(0.96f, 0.95f, 0.90f), 0.05f),
                    Fruit("avocado", "Avocado", 0f, 0.95f, 58f, new Color(0.24f, 0.35f, 0.16f), new Color(0.84f, 0.89f, 0.47f), 0.04f) },
            new[] { Fruit("papaya", "Papaya", 30f, 1.3f, 120f, new Color(0.89f, 0.67f, 0.24f), new Color(0.96f, 0.51f, 0.27f), 0.04f),
                    Fruit("coconut", "Coconut", 24f, 1.05f, 90f, new Color(0.47f, 0.31f, 0.20f), new Color(0.98f, 0.97f, 0.94f), 0.02f),
                    Fruit("rambutan", "Rambutan", 18f, 0.75f, 46f, new Color(0.86f, 0.20f, 0.20f), new Color(0.96f, 0.95f, 0.91f), 0.02f),
                    Fruit("mangosteen", "Mangosteen", 14f, 0.8f, 52f, new Color(0.35f, 0.12f, 0.24f), new Color(0.98f, 0.97f, 0.96f), 0.18f),
                    Fruit("jackfruit", "Jackfruit", 9f, 1.5f, 180f, new Color(0.59f, 0.67f, 0.24f), new Color(0.98f, 0.78f, 0.27f), 0.03f),
                    Fruit("durian", "Durian", 5f, 1.45f, 200f, new Color(0.59f, 0.63f, 0.24f), new Color(0.98f, 0.90f, 0.59f), 0.04f) },
            "#E9F4E4", "#C79A5E", "#A87C44", "#8A6A3A");

        public static readonly ItemSite Orchard = Stand("orchard", "The Orchard",
            new[] { Fruit("yellow_apple", "Yellow apple", 0f, 0.9f, 60f, new Color(0.93f, 0.78f, 0.27f), new Color(0.98f, 0.94f, 0.77f), 0.2f),
                    Fruit("plum", "Plum", 0f, 0.75f, 44f, new Color(0.43f, 0.16f, 0.35f), new Color(0.94f, 0.78f, 0.35f), 0.06f),
                    Fruit("apricot", "Apricot", 0f, 0.75f, 44f, new Color(0.96f, 0.63f, 0.27f), new Color(0.99f, 0.75f, 0.35f), 0.06f),
                    Fruit("nectarine", "Nectarine", 0f, 0.85f, 50f, new Color(0.86f, 0.27f, 0.24f), new Color(0.99f, 0.81f, 0.43f), 0.08f),
                    Fruit("quince", "Quince", 0f, 1.0f, 64f, new Color(0.86f, 0.78f, 0.27f), new Color(0.98f, 0.93f, 0.75f), 0.12f),
                    Fruit("tangerine", "Tangerine", 0f, 0.75f, 46f, new Color(0.96f, 0.51f, 0.12f), new Color(0.99f, 0.63f, 0.20f), 0.05f) },
            new[] { Fruit("pomegranate", "Pomegranate", 30f, 1.0f, 76f, new Color(0.71f, 0.12f, 0.16f), new Color(0.90f, 0.24f, 0.31f), 0.12f),
                    Fruit("fig", "Fig", 24f, 0.8f, 48f, new Color(0.35f, 0.20f, 0.31f), new Color(0.90f, 0.43f, 0.43f), 0.1f),
                    Fruit("blood_orange", "Blood Orange", 18f, 0.9f, 58f, new Color(0.86f, 0.35f, 0.16f), new Color(0.71f, 0.12f, 0.20f), 0.05f),
                    Fruit("honeydew", "Honeydew", 14f, 1.2f, 140f, new Color(0.86f, 0.90f, 0.71f), new Color(0.75f, 0.90f, 0.55f), 0.03f),
                    Fruit("persimmon", "Persimmon", 9f, 0.85f, 54f, new Color(0.96f, 0.47f, 0.12f), new Color(0.98f, 0.59f, 0.24f), 0.16f),
                    Fruit("golden_apple", "The Golden Apple", 5f, 0.95f, 70f, new Color(0.94f, 0.77f, 0.24f), new Color(0.99f, 0.94f, 0.75f), 0.2f) },
            "#F2EEDC", "#9B6B42", "#7E5333", "#B07A45");

        public static readonly ItemSite VeggieGarden = Stand("veggie", "The Veggie Garden",
            new[] { Fruit("potato", "Potato", 0f, 0.75f, 46f, new Color(0.77f, 0.59f, 0.35f), new Color(0.98f, 0.93f, 0.71f), 0.02f),
                    Fruit("carrot", "Carrot", 0f, 1.2f, 70f, new Color(0.94f, 0.47f, 0.12f), new Color(0.98f, 0.59f, 0.20f), 0.22f),
                    Fruit("cucumber", "Cucumber", 0f, 1.15f, 66f, new Color(0.24f, 0.47f, 0.20f), new Color(0.82f, 0.93f, 0.67f), 0.03f),
                    Fruit("onion", "Onion", 0f, 0.8f, 44f, new Color(0.77f, 0.47f, 0.24f), new Color(0.98f, 0.94f, 0.86f), 0.1f),
                    Fruit("beetroot", "Beetroot", 0f, 0.8f, 46f, new Color(0.47f, 0.12f, 0.24f), new Color(0.67f, 0.12f, 0.31f), 0.24f),
                    Fruit("turnip", "Turnip", 0f, 0.8f, 48f, new Color(0.90f, 0.86f, 0.90f), new Color(0.98f, 0.97f, 0.94f), 0.22f) },
            new[] { Fruit("sweet_potato", "Sweet Potato", 30f, 1.0f, 72f, new Color(0.71f, 0.31f, 0.27f), new Color(0.98f, 0.59f, 0.24f), 0.02f),
                    Fruit("butternut", "Butternut Squash", 24f, 1.3f, 110f, new Color(0.93f, 0.77f, 0.51f), new Color(0.98f, 0.63f, 0.24f), 0.05f),
                    Fruit("eggplant", "Eggplant", 18f, 1.2f, 84f, new Color(0.31f, 0.12f, 0.35f), new Color(0.96f, 0.94f, 0.82f), 0.16f),
                    Fruit("kohlrabi", "Kohlrabi", 14f, 0.95f, 60f, new Color(0.67f, 0.47f, 0.78f), new Color(0.94f, 0.96f, 0.86f), 0.3f),
                    Fruit("pumpkin", "Pumpkin", 9f, 1.3f, 150f, new Color(0.94f, 0.51f, 0.12f), new Color(0.98f, 0.67f, 0.24f), 0.08f),
                    Fruit("prize_marrow", "The Prize Marrow", 5f, 1.5f, 230f, new Color(0.43f, 0.59f, 0.27f), new Color(0.94f, 0.94f, 0.78f), 0.04f) },
            "#E6EEDC", "#A8875A", "#8A6A44", "#7A6A3A");

        public static readonly ItemSite NightMarket = Stand("night_market", "The Night Market",
            new[] { Fruit("asian_pear", "Asian pear", 0f, 0.85f, 54f, new Color(0.82f, 0.67f, 0.35f), new Color(0.98f, 0.96f, 0.86f), 0.08f),
                    Fruit("yuzu", "Yuzu", 0f, 0.75f, 46f, new Color(0.94f, 0.82f, 0.20f), new Color(0.98f, 0.93f, 0.59f), 0.08f),
                    Fruit("kumquat", "Kumquat", 0f, 0.55f, 30f, new Color(0.96f, 0.59f, 0.12f), new Color(0.99f, 0.75f, 0.31f), 0.06f),
                    Fruit("sugar_apple", "Sugar apple", 0f, 0.85f, 52f, new Color(0.59f, 0.75f, 0.43f), new Color(0.98f, 0.97f, 0.93f), 0.04f),
                    Fruit("longan", "Longan", 0f, 0.55f, 30f, new Color(0.77f, 0.63f, 0.39f), new Color(0.94f, 0.94f, 0.89f), 0.06f),
                    Fruit("jujube", "Jujube", 0f, 0.6f, 34f, new Color(0.67f, 0.20f, 0.16f), new Color(0.96f, 0.93f, 0.78f), 0.08f) },
            new[] { Fruit("hami_melon", "Hami Melon", 30f, 1.25f, 150f, new Color(0.86f, 0.78f, 0.47f), new Color(0.98f, 0.67f, 0.35f), 0.03f),
                    Fruit("buddhas_hand", "Buddha's Hand", 24f, 1.2f, 90f, new Color(0.96f, 0.82f, 0.20f), new Color(0.99f, 0.94f, 0.78f), 0.06f),
                    Fruit("horned_melon", "Horned Melon", 18f, 1.0f, 70f, new Color(0.96f, 0.59f, 0.12f), new Color(0.47f, 0.75f, 0.27f), 0.04f),
                    Fruit("snake_fruit", "Snake Fruit", 14f, 0.75f, 46f, new Color(0.47f, 0.24f, 0.16f), new Color(0.98f, 0.96f, 0.90f), 0.08f),
                    Fruit("pink_pineapple", "Pink Pineapple", 9f, 1.45f, 110f, new Color(0.75f, 0.51f, 0.35f), new Color(0.98f, 0.63f, 0.71f), 0.42f),
                    Fruit("square_watermelon", "The Square Watermelon", 5f, 1.3f, 260f, new Color(0.20f, 0.43f, 0.20f), new Color(0.94f, 0.31f, 0.35f), 0.02f) },
            "#2B2440", "#8A3A2A", "#6E2A20", "#C9A24A");

        static readonly ItemSite[] AllSites = { Stall, Tropical, Orchard, VeggieGarden, NightMarket };

        public override EnvironmentDef Def => EnvironmentCatalog.Peeler;
        protected override string SaveKey => "ScrollRoad_Peeler";
        protected override string HashKey => "peeler";
        public override ItemSite[] Sites => AllSites;

        // Kabuğun uzunluğu (cm, tam sayı): taban × 0,85–1,15.
        public static float PeelLength(EnvItem item) => Mathf.Round(item.Type.Value * (0.85f + 0.3f * item.Roll));

        public override float ItemValue(EnvItem item) => PeelLength(item);
        public override string FormatValue(float cm) => cm >= 100f ? L.F("{0:0.0#} m", cm / 100f) : L.F("{0:0} cm", cm);
        public override string RecordLabel => L.T("LONGEST PEEL");

        public override string TotalLabel => L.T("PEELED");
        public override string StatusActive => L.T("PEELING");
        public override string StatusIdle => L.T("IDLE · SLOW PEEL");
        public override string StatusBack => L.T("WINDING BACK");
        public override string RarePhrase => L.T("Peeling");
        public override string NextLine(long index, EnvItem item) => L.F("Peeling: {0} · {1}", L.Lower(L.T(item.Type.Name)), FormatValue(PeelLength(item)));
        public override string CollectionShort => L.T("FRUIT BOWL");
        public override string LogNoun => L.T("fruit");
        public override string MetDetail(ItemRecord r) => L.F("Peeled ×{0} · best {1}", r.count, FormatValue(r.best)) + "\n<alpha=#88>" + L.F("First peel {0}", r.firstSeen);
        public override string UnmetDetail => "<alpha=#88>" + L.T("Not peeled yet");

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce(L.F("First peel: {0}", L.T(type.Name)), RareNotes.For(EnvironmentId.Peeler, type.Id) ?? L.T("Stamped in your fruit bowl"), L.F("{0} · in the bowl", L.T(type.Name)));

        public override string LoopClip => "peel_loop";
        public override float LoopVolume => 0.24f;

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = 0f, Clip = "fruit_drop", Volume = 0.3f },
        };

        public override SoundCue[] Cues => CueList;
    }
}
