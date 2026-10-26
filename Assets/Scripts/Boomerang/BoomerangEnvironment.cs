using UnityEngine;

namespace ScrollRoad
{
    // Bumerang (2026-10-14 prototip; Docs/ortamlar.md "Bumerang"): atıcı bumerangı elinde döndürerek hız
    // biriktirir, fırlatır, bumerang yay çizip hedefi vurur ve geri döner. Birim VURULAN HEDEF (her
    // EnvironmentCatalog.TargetIntervalPoints'te bir atış). Mesafe hedefin özelliği (en uzun atış rekoru).
    // Aşağı scroll: atış geri sarılır — bumerang yolunu tersinden geri gelir, son vurulan hedef yeniden
    // ayağa kalkar, sayaç düşer. Başarısızlık yok: her atış vurur.
    //
    // Bir atışın evreleri (öğenin ilerlemesi f = Phase): 0–0,15 bumerang önceki hedeften ele döner; 0,15–0,70
    // elde dönerek hız biriktirir; 0,70–1 uçar; f = 1'de hedefi vurur (sayılır).
    // loc:catalog — öğe/mekân adları çeviri anahtarı olarak toplanır (Tools/loc/extract.py).
    public class BoomerangEnvironment : IntervalEnvironment
    {
        public const float ReturnEnd = 0.15f;
        public const float ThrowAt = 0.70f;

        public static readonly ItemSite Outback = new ItemSite
        {
            Id = "outback",
            Name = "The Outback",
            // Nadir hedef %1,5: 1 atış = 250 puan; gerçek zamanda diğer ortamların nadir sıklığına yakın
            // (seyirde ~17 dk'da bir). En nadiri (altın kupa) seyirde ~9 saatte bir.
            SpecialChance = 0.015f,
            SpecialMinIndex = 12,
            // Value: taban mesafe (m); hedefin mesafesi Value + Roll × 30.
            Commons = new[]
            {
                new ItemType { Id = "straw_target", Name = "Straw target", Size = 1.15f, Value = 22f },
                new ItemType { Id = "tin_cans", Name = "Tin cans", Size = 1.0f, Value = 22f },
                new ItemType { Id = "bottle_post", Name = "Bottle on a post", Size = 1.25f, Value = 26f },
                new ItemType { Id = "pumpkin_stump", Name = "Pumpkin on a stump", Size = 1.0f, Value = 24f },
                new ItemType { Id = "clay_pot_post", Name = "Clay pot", Size = 1.2f, Value = 26f },
                new ItemType { Id = "wooden_sign", Name = "Wooden sign", Size = 1.2f, Value = 28f },
            },
            Specials = new[]
            {
                new ItemType { Id = "brass_bell", Name = "The Brass Bell", Frequency = 30f, Size = 1.45f, Value = 45f },
                new ItemType { Id = "red_kite", Name = "The Red Kite", Frequency = 24f, Size = 1.9f, Value = 50f },
                new ItemType { Id = "weather_vane", Name = "The Weather Vane", Frequency = 18f, Size = 1.8f, Value = 52f },
                new ItemType { Id = "gong", Name = "The Gong", Frequency = 14f, Size = 1.5f, Value = 55f },
                new ItemType { Id = "paper_lantern", Name = "The Paper Lantern", Frequency = 9f, Size = 1.6f, Value = 58f },
                new ItemType { Id = "golden_trophy", Name = "The Golden Trophy", Frequency = 5f, Size = 1.4f, Value = 60f },
            },
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        static ItemType T(string id, string name, float size, float value) => new ItemType { Id = id, Name = name, Size = size, Value = value };
        static ItemType R(string id, string name, float freq, float size, float value) => new ItemType { Id = id, Name = name, Frequency = freq, Size = size, Value = value };

        // Ortam Standardı v2 (2026-10-25): dört yeni atış alanı. Palette: [0] gök, [1] toprak, [2] toprak kenarı,
        // [3] uzak tepe, [4] çalı.
        static ItemSite Field(string id, string name, ItemType[] commons, ItemType[] specials, params string[] palette) => new ItemSite
        {
            Id = id, Name = name, SpecialChance = 0.015f, SpecialMinIndex = 12,
            Commons = commons, Specials = specials, Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly ItemSite Beach = Field("beach", "The Beach",
            new[] { T("sandcastle", "Sandcastle", 1.0f, 22f), T("coconut_post", "Coconut on a post", 1.2f, 24f), T("beach_ball", "Beach ball", 1.15f, 22f),
                    T("sand_bucket", "Sand bucket", 1.0f, 24f), T("driftwood", "Driftwood target", 1.2f, 26f), T("buoy_post", "Buoy", 1.2f, 28f) },
            new[] { R("ship_bell", "The Ship's Bell", 30f, 1.45f, 45f), R("message_bottle", "The Message in a Bottle", 24f, 1.4f, 50f),
                    R("giant_conch", "The Giant Conch", 18f, 1.4f, 52f), R("surf_trophy", "The Surf Trophy", 14f, 1.6f, 55f),
                    R("pirate_flag", "The Pirate Flag", 9f, 1.9f, 58f), R("golden_pearl", "The Golden Pearl", 5f, 1.4f, 60f) },
            "#BFE6F2", "#E8D3A2", "#D2B57E", "#9CC9C9", "#8FB36A");

        public static readonly ItemSite Prairie = Field("prairie", "The Prairie",
            new[] { T("hay_bale", "Hay bale", 1.0f, 22f), T("scarecrow", "Scarecrow", 1.3f, 24f), T("milk_churn", "Milk churn", 1.0f, 22f),
                    T("watering_can", "Watering can", 1.1f, 24f), T("horseshoe_post", "Horseshoe post", 1.15f, 26f), T("birdhouse", "Birdhouse", 1.25f, 28f) },
            new[] { R("rooster_vane", "The Rooster Vane", 30f, 1.8f, 45f), R("cow_bell", "The Cow Bell", 24f, 1.45f, 50f),
                    R("blue_ribbon", "The Blue Ribbon", 18f, 1.5f, 52f), R("windpump", "The Windpump", 14f, 1.9f, 55f),
                    R("barn_lantern", "The Barn Lantern", 9f, 1.5f, 58f), R("golden_horseshoe", "The Golden Horseshoe", 5f, 1.4f, 60f) },
            "#CFE3F0", "#9BB760", "#7E9A4A", "#8FA86A", "#6E8A3E");

        public static readonly ItemSite Snowfield = Field("snowfield", "The Snowfield",
            new[] { T("snowman", "Snowman", 1.2f, 22f), T("ice_block", "Ice block", 1.0f, 22f), T("sled", "Wooden sled", 1.0f, 24f),
                    T("ski_post", "Skis on a post", 1.25f, 26f), T("pine_cone", "Giant pine cone", 1.1f, 24f), T("snowball_stack", "Snowball stack", 1.0f, 28f) },
            new[] { R("sleigh_bell", "The Sleigh Bell", 30f, 1.45f, 45f), R("ice_crystal", "The Ice Crystal", 24f, 1.6f, 50f),
                    R("aurora_kite", "The Aurora Kite", 18f, 1.9f, 52f), R("igloo_lantern", "The Igloo Lantern", 14f, 1.4f, 55f),
                    R("ice_trophy", "The Ice Trophy", 9f, 1.5f, 58f), R("snow_globe", "The Snow Globe", 5f, 1.4f, 60f) },
            "#DDE9F2", "#F4F7FA", "#D3DEE8", "#B8C8D8", "#5E7F6E");

        public static readonly ItemSite Ruins = Field("ruins", "The Old Ruins",
            new[] { T("stone_urn", "Stone urn", 1.1f, 22f), T("clay_mask", "Clay mask", 1.15f, 24f), T("totem_post", "Totem post", 1.3f, 26f),
                    T("carved_tablet", "Carved tablet", 1.15f, 24f), T("torch_post", "Torch post", 1.3f, 26f), T("monkey_statue", "Monkey statue", 1.1f, 28f) },
            new[] { R("jade_idol", "The Jade Idol", 30f, 1.4f, 45f), R("sun_disc", "The Sun Disc", 24f, 1.6f, 50f),
                    R("temple_bell", "The Temple Bell", 18f, 1.5f, 52f), R("feather_crown", "The Feather Crown", 14f, 1.5f, 55f),
                    R("ruby_eye", "The Ruby Eye", 9f, 1.4f, 58f), R("golden_mask", "The Golden Mask", 5f, 1.45f, 60f) },
            "#CFE8DD", "#8A7A55", "#6E6044", "#5E8A5A", "#3E7A4A");

        static readonly ItemSite[] AllSites = { Outback, Beach, Prairie, Snowfield, Ruins };

        public override EnvironmentDef Def => EnvironmentCatalog.Boomerang;
        protected override string SaveKey => "ScrollRoad_Boomerang";
        protected override string HashKey => "boomerang";
        public override ItemSite[] Sites => AllSites;

        // Hedefin mesafesi (m), tam sayı.
        public static float Distance(EnvItem item) => Mathf.Round(item.Type.Value + item.Roll * 30f);

        public override float ItemValue(EnvItem item) => Distance(item);
        public override string FormatValue(float value) => L.F("{0:0} m", value);
        public override string RecordLabel => L.T("LONGEST HIT");

        public override string TotalLabel => L.T("TARGETS HIT");
        public override string StatusActive => L.T("THROWING");
        public override string StatusIdle => L.T("IDLE · LAZY THROWS");
        public override string StatusBack => L.T("REWINDING");
        public override string RarePhrase => L.T("Aiming at");
        public override string NextLine(long index, EnvItem item) => L.F("Next: target {0} · {1:0} m", Fmt.Int(index), Distance(item));
        public override string CollectionShort => L.T("TARGET LOG");
        public override string LogNoun => L.T("targets");
        public override string MetDetail(ItemRecord r) => L.F("Hit ×{0} · best {1:0} m", r.count, r.best) + "\n<alpha=#88>" + L.F("First hit {0}", r.firstSeen);
        public override string UnmetDetail => "<alpha=#88>" + L.T("Not hit yet");

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce(L.F("New target hit: {0}", L.T(type.Name)), RareNotes.For(EnvironmentId.Boomerang, type.Id) ?? L.T("Stamped in your target log"), L.F("{0} · in the log", L.T(type.Name)));

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = ThrowAt, Clip = "boom_throw", Volume = 0.3f },
            new SoundCue { Cycle = 0, At = 0f, Clip = "boom_hit", Volume = 0.32f },
            new SoundCue { Cycle = 0, At = ReturnEnd, Clip = "boom_catch", Volume = 0.26f },
        };

        public override SoundCue[] Cues => CueList;

        // Ortam ambiyansı (Ortam Standardı v2 · ses paketi; Tools/gen_sound.py sentezi): yalnız kullanıcı aktifken.
        public override string LoopClip => "breeze_loop";
        public override float LoopVolume => 0.2f;
    }
}
