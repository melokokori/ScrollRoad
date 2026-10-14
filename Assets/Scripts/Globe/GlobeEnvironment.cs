using UnityEngine;

namespace ScrollRoad
{
    // Dünyayı döndüren el (2026-10-14 prototip; Docs/ortamlar.md "Dünyayı döndüren el"): bir el küçük bir
    // gezegeni döndürür, gün ve gece yüzeyden akar. Birim YIL (her EnvironmentCatalog.YearIntervalPoints'te
    // bir yıl). Her yıl gezegenin kenarında bir yapı yükselir (kulübe, çadır, ağaç...); nadir yıllarda o çağın
    // bir harikası (Stonehenge, Piramitler...) — ilk yükselişinde kroniğe damgalanır. Aşağı scroll: zaman geri
    // akar, gezegen ters döner, son yıllarda yükselenler yeniden toprağa iner, sayaç düşer.
    // Mekân = çağ: prototipte yalnız İlk Çağ ("The Ancient World"); sonraki çağlar sonraki aşama.
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

        static readonly ItemSite[] AllSites = { Ancient };

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
            notice.Announce($"Wonder raised: {type.Name}", "Stamped in your chronicle", $"{type.Name} · in the chronicle");

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
    }
}
