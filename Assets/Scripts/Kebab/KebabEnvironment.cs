using UnityEngine;

namespace ScrollRoad
{
    // Tavuk/et döner, çağ kebabı (2026-10-14 prototip; Docs/ortamlar.md "Kebap"): yatay şiş (çağ kebabı) odun
    // ateşinin üstünde döner, et pişip kızarır; usta bıçağı şiş boyunca gezdirip kızarmış katmanı keser, porsiyon
    // tabağa konup tezgâha (pass) sürülür. Birim PORSİYON (her EnvironmentCatalog.PortionIntervalPoints'te bir).
    // Aşağı scroll: şiş ters döner, et soğur (kızarıklık geri gider), son porsiyon tezgâhtan geri gelir, sayaç
    // düşer. Başarısızlık yok: et yanmaz.
    //
    // Bir porsiyonun evreleri (f = Phase): 0–0,70 pişer; 0,70–0,90 kesilir; 0,90–1 tabak tezgâha sürülür
    // (f = 1'de sayılır).
    public class KebabEnvironment : IntervalEnvironment
    {
        public const float CookEnd = 0.70f;
        public const float SliceEnd = 0.90f;

        public static readonly ItemSite House = new ItemSite
        {
            Id = "kebab_house",
            Name = "The Kebab House",
            // Nadir porsiyon %4,8: 1 porsiyon = 800 puan; gerçek zamanda diğer ortamların nadir sıklığına yakın
            // (seyirde ~17 dk'da bir). En nadiri (Sultan tabağı) seyirde ~9 saatte bir.
            SpecialChance = 0.048f,
            SpecialMinIndex = 12,
            Commons = new[]
            {
                new ItemType { Id = "lavash_wrap", Name = "Lavash wrap", Size = 0.55f },
                new ItemType { Id = "half_bread", Name = "Half bread", Size = 0.6f },
                new ItemType { Id = "rice_plate", Name = "Rice plate", Size = 0.5f },
                new ItemType { Id = "salad_plate", Name = "Salad plate", Size = 0.5f },
                new ItemType { Id = "pita", Name = "Pita", Size = 0.6f },
                new ItemType { Id = "skewer", Name = "Skewer portion", Size = 0.45f },
            },
            Specials = new[]
            {
                new ItemType { Id = "iskender", Name = "Iskender", Frequency = 30f, Size = 0.55f },
                new ItemType { Id = "chicken_doner", Name = "Chicken Doner", Frequency = 24f, Size = 0.65f },
                new ItemType { Id = "gyros", Name = "Gyros", Frequency = 18f, Size = 0.7f },
                new ItemType { Id = "shawarma", Name = "Shawarma", Frequency = 14f, Size = 0.65f },
                new ItemType { Id = "al_pastor", Name = "Al Pastor", Frequency = 9f, Size = 0.6f },
                new ItemType { Id = "sultans_platter", Name = "The Sultan's Platter", Frequency = 5f, Size = 0.75f },
            },
        };

        static readonly ItemSite[] AllSites = { House };

        public override EnvironmentDef Def => EnvironmentCatalog.Kebab;
        protected override string SaveKey => "ScrollRoad_Kebab";
        protected override string HashKey => "kebab";
        public override ItemSite[] Sites => AllSites;

        public override string TotalLabel => "SERVED";
        public override string StatusActive => "COOKING";
        public override string StatusIdle => "IDLE · LOW FIRE";
        public override string StatusBack => "COOLING DOWN";
        public override string RarePhrase => "Order up";
        public override string NextLine(long index, EnvItem item) => "Next order: " + item.Type.Name.ToLowerInvariant();
        public override string CollectionShort => "MENU";
        public override string LogNoun => "dishes";
        public override string MetDetail(ItemRecord r) => $"Served ×{r.count}\n<alpha=#88>First served {r.firstSeen}";
        public override string UnmetDetail => "<alpha=#88>Not served yet";

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce($"New on the menu: {type.Name}", "Stamped in your menu", $"{type.Name} · on the menu");

        public override string LoopClip => "sizzle_loop";
        public override float LoopVolume => 0.3f;

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = CookEnd, Clip = "knife_slice", Volume = 0.28f },
            new SoundCue { Cycle = 0, At = 0f, Clip = "plate_serve", Volume = 0.28f },
        };

        public override SoundCue[] Cues => CueList;
    }
}
