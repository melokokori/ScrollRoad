using UnityEngine;

namespace ScrollRoad
{
    // Meyve soyacağı (2026-10-14 prototip; Docs/ortamlar.md "Meyve soyacağı"): kollu bir soyma makinesi meyveyi
    // döndürür, bıçak yukarıdan aşağı iner, kabuk tek parça spiral hâlinde uzayıp masaya sarkar; soyulan meyve
    // kâseye düşer. Birim SOYULAN MEYVE (her EnvironmentCatalog.FruitIntervalPoints'te bir). Kabuğun uzunluğu
    // meyvenin özelliği: "kopmayan kabuk" rekoru (en uzun tek parça kabuk). Aşağı scroll: kabuk geri sarılır —
    // meyve kâseden makineye döner, kabuk yeniden meyveye sarılır, sayaç düşer. Başarısızlık yok: kabuk kopmaz.
    //
    // Bir meyvenin evreleri (f = Phase): 0–0,10 meyve makineye takılır; 0,10–0,85 soyulur; 0,85–1 kâseye düşer
    // (f = 1'de sayılır).
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

        static readonly ItemSite[] AllSites = { Stall };

        public override EnvironmentDef Def => EnvironmentCatalog.Peeler;
        protected override string SaveKey => "ScrollRoad_Peeler";
        protected override string HashKey => "peeler";
        public override ItemSite[] Sites => AllSites;

        // Kabuğun uzunluğu (cm, tam sayı): taban × 0,85–1,15.
        public static float PeelLength(EnvItem item) => Mathf.Round(item.Type.Value * (0.85f + 0.3f * item.Roll));

        public override float ItemValue(EnvItem item) => PeelLength(item);
        public override string FormatValue(float cm) => cm >= 100f ? $"{cm / 100f:0.0#} m" : $"{cm:0} cm";
        public override string RecordLabel => "LONGEST PEEL";

        public override string TotalLabel => "PEELED";
        public override string StatusActive => "PEELING";
        public override string StatusIdle => "IDLE · SLOW CRANK";
        public override string StatusBack => "WINDING BACK";
        public override string RarePhrase => "Peeling";
        public override string NextLine(long index, EnvItem item) => $"Peeling: {item.Type.Name.ToLowerInvariant()} · {FormatValue(PeelLength(item))}";
        public override string CollectionShort => "FRUIT BOWL";
        public override string LogNoun => "fruit";
        public override string MetDetail(ItemRecord r) => $"Peeled ×{r.count} · best {FormatValue(r.best)}\n<alpha=#88>First peel {r.firstSeen}";
        public override string UnmetDetail => "<alpha=#88>Not peeled yet";

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce($"First peel: {type.Name}", "Stamped in your fruit bowl", $"{type.Name} · in the bowl");

        public override string LoopClip => "peel_loop";
        public override float LoopVolume => 0.24f;

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = 0f, Clip = "fruit_drop", Volume = 0.3f },
        };

        public override SoundCue[] Cues => CueList;
    }
}
