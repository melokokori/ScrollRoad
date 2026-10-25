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

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        static ItemType D(string id, string name, float size) => new ItemType { Id = id, Name = name, Size = size };
        static ItemType R(string id, string name, float freq, float size) => new ItemType { Id = id, Name = name, Frequency = freq, Size = size };

        // Ortam Standardı v2 (2026-10-25): dört yeni mutfak. Palette: [0] duvar, [1] ocak, [2] tezgâh, [3] tezgâh üstü.
        static ItemSite Kitchen(string id, string name, ItemType[] commons, ItemType[] specials, params string[] palette) => new ItemSite
        {
            Id = id, Name = name, SpecialChance = 0.048f, SpecialMinIndex = 12,
            Commons = commons, Specials = specials, Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly ItemSite Taverna = Kitchen("taverna", "The Greek Taverna",
            new[] { D("souvlaki_pita", "Souvlaki pita", 0.6f), D("greek_salad", "Greek salad", 0.5f), D("fries_plate", "Fries plate", 0.45f), D("tzatziki", "Tzatziki", 0.45f), D("lemon_potatoes", "Lemon potatoes", 0.45f), D("bread_basket", "Bread basket", 0.5f) },
            new[] { R("kleftiko", "Lamb Kleftiko", 30f, 0.6f), R("octopus", "Grilled Octopus", 24f, 0.6f), R("mezze_board", "The Mezze Board", 18f, 0.7f), R("loukoumades", "Loukoumades", 14f, 0.5f), R("kontosouvli", "Kontosouvli", 9f, 0.7f), R("taverna_feast", "The Taverna Feast", 5f, 0.8f) },
            "#F4F6F8", "#C8B89A", "#3E6E9E", "#F2F4F6");

        public static readonly ItemSite ShawarmaStand = Kitchen("shawarma", "The Shawarma Stand",
            new[] { D("falafel_wrap", "Falafel wrap", 0.6f), D("hummus", "Hummus", 0.45f), D("fattoush", "Fattoush", 0.5f), D("manakish", "Manakish", 0.5f), D("pickles", "Pickles", 0.45f), D("lentil_soup", "Lentil soup", 0.45f) },
            new[] { R("kibbeh", "Kibbeh", 30f, 0.55f), R("mansaf", "Mansaf", 24f, 0.7f), R("knafeh", "Knafeh", 18f, 0.55f), R("mixed_grill", "Mixed Grill", 14f, 0.7f), R("maqluba", "Maqluba", 9f, 0.65f), R("royal_mezze", "The Royal Mezze", 5f, 0.8f) },
            "#F0E2C8", "#A8763E", "#2E6E5E", "#D8D2C4");

        public static readonly ItemSite Taqueria = Kitchen("taqueria", "The Taqueria",
            new[] { D("taco_trio", "Taco trio", 0.55f), D("quesadilla", "Quesadilla", 0.5f), D("nachos", "Nachos", 0.5f), D("elote", "Elote", 0.5f), D("guacamole", "Guacamole", 0.45f), D("burrito", "Burrito", 0.6f) },
            new[] { R("enchiladas", "Enchiladas", 30f, 0.6f), R("mole", "Mole Poblano", 24f, 0.6f), R("pozole", "Pozole", 18f, 0.55f), R("tamales", "Tamales", 14f, 0.55f), R("churros", "Churros", 9f, 0.55f), R("golden_trompo", "The Golden Trompo", 5f, 0.75f) },
            "#F6D27A", "#C2412F", "#2E8F6E", "#F2E6D0");

        public static readonly ItemSite NightGrill = Kitchen("night_grill", "The Night Grill",
            new[] { D("yakitori", "Yakitori", 0.5f), D("gyoza", "Gyoza", 0.45f), D("onigiri", "Onigiri", 0.45f), D("edamame", "Edamame", 0.45f), D("kimchi", "Kimchi", 0.45f), D("ramen", "Ramen", 0.55f) },
            new[] { R("bulgogi", "Bulgogi", 30f, 0.6f), R("char_siu", "Char Siu", 24f, 0.6f), R("peking_duck", "Peking Duck", 18f, 0.7f), R("wagyu_skewer", "Wagyu Skewers", 14f, 0.55f), R("katsu_curry", "Katsu Curry", 9f, 0.6f), R("imperial_banquet", "The Imperial Banquet", 5f, 0.8f) },
            "#2A2433", "#5A4A40", "#3A2A22", "#8A6A4A");

        static readonly ItemSite[] AllSites = { House, Taverna, ShawarmaStand, Taqueria, NightGrill };

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
