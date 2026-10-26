using UnityEngine;

namespace ScrollRoad
{
    // Tavuk/et döner (2026-10-14 prototip yatay çağ kebabıydı; 2026-10-25 dikey döner; Docs/ortamlar.md "Kebap"):
    // dikey şiş ızgaranın önünde döner, et pişip kızarır; usta yukarıdan aşağı bıçak çekip kızarmış katmanı keser, porsiyon
    // tabağa konup tezgâha (pass) sürülür. Birim PORSİYON (her EnvironmentCatalog.PortionIntervalPoints'te bir).
    // Aşağı scroll: şiş ters döner, et soğur (kızarıklık geri gider), son porsiyon tezgâhtan geri gelir, sayaç
    // düşer. Başarısızlık yok: et yanmaz.
    //
    // Bir porsiyonun evreleri (f = Phase): 0–0,70 pişer; 0,70–0,90 kesilir; 0,90–1 tabak tezgâha sürülür
    // (f = 1'de sayılır).
    // loc:catalog — öğe/mekân adları çeviri anahtarı olarak toplanır (Tools/loc/extract.py).
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
                new ItemType { Id = "doner_portion", Name = "Doner portion", Size = 0.5f },
                new ItemType { Id = "pita", Name = "Pita", Size = 0.6f },
                new ItemType { Id = "tombik", Name = "Tombik", Size = 0.55f },
            },
            Specials = new[]
            {
                new ItemType { Id = "iskender", Name = "Iskender", Frequency = 30f, Size = 0.55f },
                new ItemType { Id = "chicken_doner", Name = "Chicken Doner", Frequency = 24f, Size = 0.65f },
                new ItemType { Id = "pideli_doner", Name = "Pideli Doner", Frequency = 18f, Size = 0.6f },
                new ItemType { Id = "kasarli_durum", Name = "Cheese Durum", Frequency = 14f, Size = 0.6f },
                new ItemType { Id = "cag_kebab", Name = "Erzurum Cag Kebab", Frequency = 9f, Size = 0.6f },
                new ItemType { Id = "sultans_platter", Name = "The Sultan's Platter", Frequency = 5f, Size = 0.75f },
            },
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        static ItemType D(string id, string name, float size) => new ItemType { Id = id, Name = name, Size = size };
        static ItemType R(string id, string name, float freq, float size) => new ItemType { Id = id, Name = name, Frequency = freq, Size = size };

        // Ortam Standardı v2 (2026-10-25): dört döner kültürü daha (2026-10-25 gece kullanıcı: "ortaya çıkan yemekler
        // alakasız" → her mekânın her porsiyonu ustanın şişten kestiği etle yapılır). Palette: [0] duvar, [1] ocak,
        // [2] tezgâh, [3] tezgâh üstü.
        static ItemSite Kitchen(string id, string name, ItemType[] commons, ItemType[] specials, params string[] palette) => new ItemSite
        {
            Id = id, Name = name, SpecialChance = 0.048f, SpecialMinIndex = 12,
            Commons = commons, Specials = specials, Palette = System.Array.ConvertAll(palette, Hex),
        };

        // Atina: gyros.
        public static readonly ItemSite Taverna = Kitchen("taverna", "The Greek Taverna",
            new[] { D("pita_gyros", "Pita gyros", 0.6f), D("chicken_gyros", "Chicken pita", 0.6f), D("gyros_plate", "Gyros plate", 0.55f), D("gyros_baguette", "Gyros baguette", 0.6f), D("gyros_box", "Gyros box", 0.5f), D("open_pita", "Open pita", 0.55f) },
            new[] { R("yogurt_gyros", "Gyros with Yogurt", 30f, 0.55f), R("double_pita", "Double Pita", 24f, 0.65f), R("feta_gyros", "Feta Gyros", 18f, 0.6f), R("spicy_gyros", "Spicy Gyros", 14f, 0.6f), R("lamb_gyros", "Lamb Gyros", 9f, 0.6f), R("gyros_tower", "The Gyros Tower", 5f, 0.75f) },
            "#F4F6F8", "#C8B89A", "#3E6E9E", "#F2F4F6");

        // Beyrut: shawarma.
        public static readonly ItemSite ShawarmaStand = Kitchen("shawarma", "The Shawarma Stand",
            new[] { D("shawarma_wrap", "Shawarma wrap", 0.6f), D("arabi", "Arabi", 0.55f), D("shawarma_plate", "Shawarma plate", 0.55f), D("saj_shawarma", "Saj shawarma", 0.6f), D("samoon", "Samoon", 0.55f), D("shawarma_fries", "Shawarma fries", 0.5f) },
            new[] { R("beef_shawarma", "Beef Shawarma", 30f, 0.6f), R("toum_special", "Toum Special", 24f, 0.6f), R("shawarma_fatteh", "Shawarma Fatteh", 18f, 0.55f), R("kaak_shawarma", "Kaak Shawarma", 14f, 0.6f), R("lamb_shawarma", "Lamb Shawarma", 9f, 0.6f), R("royal_shawarma", "The Royal Platter", 5f, 0.8f) },
            "#F0E2C8", "#A8763E", "#2E6E5E", "#D8D2C4");

        // Mexico City: al pastor (trompo).
        public static readonly ItemSite Taqueria = Kitchen("taqueria", "The Taqueria",
            new[] { D("tacos_pastor", "Tacos al pastor", 0.6f), D("gringa", "Gringa", 0.55f), D("torta_pastor", "Torta al pastor", 0.6f), D("alambre", "Alambre", 0.55f), D("volcan", "Volcanes", 0.55f), D("pastor_quesadilla", "Pastor quesadilla", 0.55f) },
            new[] { R("costra", "Tacos de Costra", 30f, 0.6f), R("pastor_burrito", "Pastor Burrito", 24f, 0.6f), R("huarache_pastor", "Huarache al Pastor", 18f, 0.6f), R("pineapple_special", "Pineapple Special", 14f, 0.6f), R("tacos_arabes", "Tacos Arabes", 9f, 0.6f), R("golden_trompo", "The Golden Trompo", 5f, 0.75f) },
            "#F6D27A", "#C2412F", "#2E8F6E", "#F2E6D0");

        // Berlin: gece dönercisi.
        public static readonly ItemSite NightGrill = Kitchen("night_grill", "The Night Grill",
            new[] { D("doner_brot", "Doner in bread", 0.6f), D("berlin_durum", "Berlin durum", 0.6f), D("doner_teller", "Doner plate", 0.55f), D("doner_box", "Doner box", 0.5f), D("lahmacun_roll", "Lahmacun roll", 0.6f), D("doner_halloumi", "Halloumi doner", 0.6f) },
            new[] { R("gemuse_doner", "Gemuse Doner", 30f, 0.6f), R("cheese_doner", "Cheese Doner", 24f, 0.6f), R("mit_alles", "Doner with Everything", 18f, 0.65f), R("scharf_doner", "Extra Spicy Doner", 14f, 0.6f), R("midnight_teller", "The Midnight Plate", 9f, 0.7f), R("golden_doner", "The Golden Doner", 5f, 0.7f) },
            "#2A2433", "#5A4A40", "#3A2A22", "#8A6A4A");

        static readonly ItemSite[] AllSites = { House, Taverna, ShawarmaStand, Taqueria, NightGrill };

        public override EnvironmentDef Def => EnvironmentCatalog.Kebab;
        protected override string SaveKey => "ScrollRoad_Kebab";
        protected override string HashKey => "kebab";
        public override ItemSite[] Sites => AllSites;

        public override string TotalLabel => L.T("SERVED");
        public override string StatusActive => L.T("COOKING");
        public override string StatusIdle => L.T("IDLE · LOW FIRE");
        public override string StatusBack => L.T("COOLING DOWN");
        public override string RarePhrase => L.T("Order up");
        public override string NextLine(long index, EnvItem item) => L.F("Next order: {0}", L.Lower(L.T(item.Type.Name)));
        public override string CollectionShort => L.T("MENU");
        public override string LogNoun => L.T("dishes");
        public override string MetDetail(ItemRecord r) => L.F("Served ×{0}", r.count) + "\n<alpha=#88>" + L.F("First served {0}", r.firstSeen);
        public override string UnmetDetail => "<alpha=#88>" + L.T("Not served yet");

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce(L.F("New on the menu: {0}", L.T(type.Name)), RareNotes.For(EnvironmentId.Kebab, type.Id) ?? L.T("Stamped in your menu"), L.F("{0} · on the menu", L.T(type.Name)));

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
