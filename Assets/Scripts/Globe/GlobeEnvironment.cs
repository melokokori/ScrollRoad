using UnityEngine;

namespace ScrollRoad
{
    // Dünyayı döndüren el (2026-10-14 prototip; Docs/ortamlar.md "Dünyayı döndüren el"): bir el küçük bir
    // gezegeni döndürür, gün ve gece yüzeyden akar. Birim YIL (her EnvironmentCatalog.YearIntervalPoints'te
    // bir yıl). Her yıl gezegenin kenarında bir yapı yükselir (kulübe, çadır, ağaç...); nadir yıllarda o çağın
    // bir harikası (Stonehenge, Piramitler...) — ilk yükselişinde kroniğe damgalanır. Aşağı scroll: zaman geri
    // akar, gezegen ters döner, son yıllarda yükselenler yeniden toprağa iner, sayaç düşer.
    // Mekân = çağ (Ortam Standardı v2, 2026-10-25): İlk Çağ → Orta Çağ → Rönesans → Sanayi Çağı → Uzay Çağı,
    // Dünya'da geçen seyir saatiyle açılır (UnlockLadder). Mekân uzayın ve güneşin rengini verir (Palette:
    // [0] uzay, [1] güneş); gezegenin renkleri skin takımından (SkinCatalog.Globe).
    public class GlobeEnvironment : IntervalEnvironment
    {
        public static readonly ItemSite Ancient = new ItemSite
        {
            Id = "ancient",
            Name = "The Ancient World",
            // Nadir yıl %6: 1 yıl = 1.000 puan; gerçek zamanda Kapı/Rüzgârgülü'ndeki nadir sıklığına yakın
            // (seyirde ~17 dk'da bir). En nadiri (Asma Bahçeler) seyirde ~9 saatte bir.
            SpecialChance = 0.06f,
            SpecialMinIndex = 12,
            Commons = new[]
            {
                new ItemType { Id = "hut", Name = "Hut", Size = 0.62f },
                new ItemType { Id = "tent", Name = "Tent", Size = 0.55f },
                new ItemType { Id = "olive_tree", Name = "Olive tree", Size = 0.75f },
                new ItemType { Id = "stone_house", Name = "Stone house", Size = 0.62f },
                new ItemType { Id = "haystack", Name = "Haystack", Size = 0.5f },
                new ItemType { Id = "watchtower", Name = "Watchtower", Size = 0.85f },
            },
            Specials = new[]
            {
                new ItemType { Id = "stonehenge", Name = "Stonehenge", Frequency = 30f, Size = 0.72f },
                new ItemType { Id = "pyramids", Name = "The Pyramids", Frequency = 24f, Size = 0.95f },
                new ItemType { Id = "great_wall", Name = "The Great Wall", Frequency = 18f, Size = 0.7f },
                new ItemType { Id = "colosseum", Name = "The Colosseum", Frequency = 14f, Size = 0.85f },
                new ItemType { Id = "lighthouse", Name = "The Great Lighthouse", Frequency = 9f, Size = 1.2f },
                new ItemType { Id = "hanging_gardens", Name = "The Hanging Gardens", Frequency = 5f, Size = 1.0f },
            },
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static ItemSite Era(string id, string name, ItemType[] commons, ItemType[] specials, string space, string sun) => new ItemSite
        {
            Id = id,
            Name = name,
            SpecialChance = 0.06f,
            SpecialMinIndex = 12,
            Commons = commons,
            Specials = specials,
            Palette = new[] { Hex(space), Hex(sun) },
        };

        static ItemType C(string id, string name, float size) => new ItemType { Id = id, Name = name, Size = size };
        static ItemType R(string id, string name, float freq, float size) => new ItemType { Id = id, Name = name, Frequency = freq, Size = size };

        public static readonly ItemSite MiddleAges = Era("middle_ages", "The Middle Ages",
            new[] { C("cottage", "Cottage", 0.6f), C("windmill", "Windmill", 0.85f), C("chapel", "Chapel", 0.75f),
                    C("market_stall", "Market stall", 0.5f), C("stone_well", "Stone well", 0.45f), C("oak_tree", "Oak tree", 0.8f) },
            new[] { R("tower_of_pisa", "The Leaning Tower", 30f, 1.05f), R("notre_dame", "Notre-Dame", 24f, 0.95f),
                    R("hagia_sophia", "Hagia Sophia", 18f, 0.9f), R("alhambra", "The Alhambra", 14f, 0.8f),
                    R("angkor_wat", "Angkor Wat", 9f, 0.9f), R("machu_picchu", "Machu Picchu", 5f, 0.85f) },
            "#1E1B36", "#FFD27A");

        public static readonly ItemSite Renaissance = Era("renaissance", "The Renaissance",
            new[] { C("townhouse", "Townhouse", 0.65f), C("cypress", "Cypress tree", 0.85f), C("fountain", "Fountain", 0.5f),
                    C("bell_tower", "Bell tower", 0.95f), C("stone_bridge", "Stone bridge", 0.5f), C("workshop", "Workshop", 0.6f) },
            new[] { R("florence_dome", "The Florence Dome", 30f, 0.95f), R("st_peters", "St Peter's Basilica", 24f, 0.95f),
                    R("taj_mahal", "The Taj Mahal", 18f, 0.95f), R("forbidden_city", "The Forbidden City", 14f, 0.8f),
                    R("st_basils", "St Basil's Cathedral", 9f, 1.0f), R("versailles", "Versailles", 5f, 0.75f) },
            "#2A1F3D", "#FFC870");

        public static readonly ItemSite Industrial = Era("industrial", "The Industrial Age",
            new[] { C("factory", "Factory", 0.8f), C("brick_house", "Brick house", 0.6f), C("water_tower", "Water tower", 0.9f),
                    C("locomotive", "Locomotive", 0.5f), C("gas_lamp", "Gas lamp", 0.7f), C("terrace", "Terraced houses", 0.6f) },
            new[] { R("big_ben", "Big Ben", 30f, 1.15f), R("eiffel_tower", "The Eiffel Tower", 24f, 1.25f),
                    R("statue_of_liberty", "The Statue of Liberty", 18f, 1.1f), R("tower_bridge", "Tower Bridge", 14f, 0.85f),
                    R("crystal_palace", "The Crystal Palace", 9f, 0.75f), R("brooklyn_bridge", "Brooklyn Bridge", 5f, 0.8f) },
            "#2B2933", "#FFA552");

        public static readonly ItemSite SpaceAge = Era("space_age", "The Space Age",
            new[] { C("apartments", "Apartment block", 0.8f), C("radio_mast", "Radio mast", 0.95f), C("satellite_dish", "Satellite dish", 0.6f),
                    C("modern_house", "Modern house", 0.55f), C("petrol_station", "Petrol station", 0.55f), C("wind_turbine", "Wind turbine", 0.95f) },
            new[] { R("sydney_opera", "The Sydney Opera House", 30f, 0.8f), R("cn_tower", "The CN Tower", 24f, 1.3f),
                    R("atomium", "The Atomium", 18f, 1.0f), R("burj_khalifa", "Burj Khalifa", 14f, 1.35f),
                    R("observatory_dome", "The Great Telescope", 9f, 0.85f), R("launch_pad", "The Moon Rocket", 5f, 1.3f) },
            "#0B1026", "#F5F1E0");

        static readonly ItemSite[] AllSites = { Ancient, MiddleAges, Renaissance, Industrial, SpaceAge };

        public override EnvironmentDef Def => EnvironmentCatalog.Globe;
        protected override string SaveKey => "ScrollRoad_Globe";
        protected override string HashKey => "globe";
        public override ItemSite[] Sites => AllSites;

        public override string TotalLabel => "ELAPSED";
        public override string StatusActive => "TURNING";
        public override string StatusIdle => "IDLE · SLOW DAYS";
        public override string StatusBack => "TIME REWINDS";
        public override string RarePhrase => "Rising";
        public override string NextLine(long index, EnvItem item) => "Next: year " + Fmt.Int(index);
        public override string CollectionShort => "CHRONICLE";
        public override string LogNoun => "wonders";
        public override string MetDetail(ItemRecord r) => $"Raised ×{r.count}\n<alpha=#88>First seen {r.firstSeen}";
        public override string UnmetDetail => "<alpha=#88>Not raised yet";

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce($"Wonder raised: {type.Name}", RareNotes.For(EnvironmentId.Globe, type.Id) ?? "Stamped in your chronicle", $"{type.Name} · in the chronicle");

        // Gezegen: puan başına derece (seyirde ~8 sn'de bir tur, tam boost'ta ~2 sn, rölantide ~48 sn).
        public const double DegreesPerPoint = 2.7;
        // Elin itişi tur başına bir kez: tur = 360 / 2,7 ≈ 133 puan.
        public const double PointsPerTurn = 360.0 / DegreesPerPoint;

        static readonly SoundCue[] CueList =
        {
            // El gezegeni ittiğinde yumuşak bir sürtünme/vuuş (tur başına bir; seyirde ~8 sn'de bir).
            new SoundCue { Cycle = PointsPerTurn, At = 0.02f, Clip = "globe_push", Volume = 0.3f, OnRewind = true },
            // Yıl dönünce sayfa çevirme (seyirde dakikada bir).
            new SoundCue { Cycle = 0, At = 0f, Clip = "year_page", Volume = 0.3f },
        };

        public override SoundCue[] Cues => CueList;

        // Ortam ambiyansı (Ortam Standardı v2 · ses paketi; Tools/gen_sound.py sentezi): yalnız kullanıcı aktifken.
        public override string LoopClip => "space_hum";
        public override float LoopVolume => 0.18f;
    }
}
