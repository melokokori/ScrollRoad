using UnityEngine;

namespace ScrollRoad
{
    // Çömlekçi çarkı (2026-10-14 prototip; Docs/ortamlar.md "Çömlekçi çarkı"): çark döner, çarktaki çamur
    // topağı yükselip çömleğe dönüşür, biten çömlek pişmiş/sırlı hâliyle rafa konur. Birim BİTEN ÇÖMLEK (her
    // EnvironmentCatalog.PotIntervalPoints'te bir). Aşağı scroll: çamur bastırılır — çömlek raftan çarka
    // geri gelir, yeniden topağa çöker, sayaç düşer. Başarısızlık yok: her çömlek biter.
    //
    // Bir çömleğin evreleri (f = Phase): 0–0,08 çarka yeni topak konur; 0,08–0,80 yükselip biçimlenir;
    // 0,80–0,88 pişmiş/sırlı hâline geçer; 0,88–1 rafa taşınır (f = 1'de sayılır).
    // loc:catalog — öğe/mekân adları çeviri anahtarı olarak toplanır (Tools/loc/extract.py).
    public class PotteryEnvironment : IntervalEnvironment
    {
        public const float ShapeStart = 0.08f;
        public const float ShapeEnd = 0.80f;
        public const float FireEnd = 0.88f;

        // Shape: profil (t, r) çiftleri; t = 0 dip, 1 ağız; r yarıçap, çömleğin boyu (Size) cinsinden.
        public static readonly ItemSite Workshop = new ItemSite
        {
            Id = "workshop",
            Name = "The Village Workshop",
            // Nadir çömlek %12: 1 çömlek = 2.000 puan; gerçek zamanda diğer ortamların nadir sıklığına yakın
            // (seyirde ~17 dk'da bir). En nadiri (altın küp) seyirde ~9 saatte bir.
            SpecialChance = 0.12f,
            SpecialMinIndex = 12,
            Commons = new[]
            {
                new ItemType { Id = "bowl", Name = "Bowl", Size = 0.55f, Shape = new[] { 0f, 0.35f, 0.15f, 0.6f, 0.6f, 0.88f, 1f, 0.95f } },
                new ItemType { Id = "flower_pot", Name = "Flower pot", Size = 0.8f, Shape = new[] { 0f, 0.3f, 0.85f, 0.45f, 0.9f, 0.52f, 1f, 0.52f } },
                new ItemType { Id = "mug", Name = "Mug", Size = 0.7f, Shape = new[] { 0f, 0.32f, 0.1f, 0.35f, 1f, 0.35f } },
                new ItemType { Id = "jar", Name = "Jar", Size = 0.85f, Shape = new[] { 0f, 0.25f, 0.3f, 0.45f, 0.72f, 0.42f, 0.86f, 0.25f, 1f, 0.27f } },
                new ItemType { Id = "cup", Name = "Cup", Size = 0.55f, Shape = new[] { 0f, 0.32f, 0.35f, 0.58f, 1f, 0.6f } },
                new ItemType { Id = "plate", Name = "Plate", Size = 0.14f, Shape = new[] { 0f, 3.0f, 0.6f, 4.2f, 1f, 4.5f } },
            },
            Specials = new[]
            {
                new ItemType { Id = "water_jug", Name = "The Water Jug", Frequency = 30f, Size = 1.15f, Shape = new[] { 0f, 0.18f, 0.15f, 0.3f, 0.45f, 0.36f, 0.75f, 0.22f, 0.88f, 0.12f, 1f, 0.14f } },
                new ItemType { Id = "tall_vase", Name = "The Tall Vase", Frequency = 24f, Size = 1.35f, Shape = new[] { 0f, 0.14f, 0.22f, 0.24f, 0.5f, 0.2f, 0.8f, 0.12f, 1f, 0.18f } },
                new ItemType { Id = "amphora", Name = "The Amphora", Frequency = 18f, Size = 1.4f, Shape = new[] { 0f, 0.04f, 0.15f, 0.15f, 0.5f, 0.3f, 0.75f, 0.24f, 0.86f, 0.1f, 1f, 0.12f } },
                new ItemType { Id = "teapot", Name = "The Teapot", Frequency = 14f, Size = 0.75f, Shape = new[] { 0f, 0.38f, 0.4f, 0.55f, 0.8f, 0.4f, 0.9f, 0.22f, 1f, 0.2f } },
                new ItemType { Id = "tulip_vase", Name = "The Tulip Vase", Frequency = 9f, Size = 1.1f, Shape = new[] { 0f, 0.18f, 0.35f, 0.36f, 0.7f, 0.18f, 1f, 0.3f } },
                new ItemType { Id = "golden_urn", Name = "The Golden Urn", Frequency = 5f, Size = 1.0f, Shape = new[] { 0f, 0.22f, 0.1f, 0.18f, 0.5f, 0.42f, 0.8f, 0.32f, 0.9f, 0.2f, 1f, 0.24f } },
            },
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        static ItemType P(string id, string name, float size, params float[] shape) => new ItemType { Id = id, Name = name, Size = size, Shape = shape };
        static ItemType R(string id, string name, float freq, float size, params float[] shape) => new ItemType { Id = id, Name = name, Frequency = freq, Size = size, Shape = shape };

        // Ortam Standardı v2 (2026-10-25): dört yeni atölye. Palette: [0] duvar, [1] zemin, [2] raf.
        static ItemSite Studio(string id, string name, ItemType[] commons, ItemType[] specials, params string[] palette) => new ItemSite
        {
            Id = id, Name = name, SpecialChance = 0.12f, SpecialMinIndex = 12,
            Commons = commons, Specials = specials, Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly ItemSite Greek = Studio("greek", "The Greek Studio",
            new[] { P("kylix", "Kylix", 0.4f, 0f, 0.15f, 0.25f, 0.2f, 0.4f, 0.9f, 1f, 1.1f),
                    P("lekythos", "Oil flask", 0.95f, 0f, 0.14f, 0.15f, 0.2f, 0.6f, 0.22f, 0.72f, 0.1f, 1f, 0.12f),
                    P("skyphos", "Skyphos", 0.6f, 0f, 0.3f, 0.2f, 0.45f, 1f, 0.55f),
                    P("olive_jar", "Olive jar", 0.85f, 0f, 0.28f, 0.4f, 0.48f, 0.8f, 0.36f, 1f, 0.3f),
                    P("drinking_bowl", "Drinking bowl", 0.45f, 0f, 0.3f, 0.3f, 0.7f, 1f, 0.95f),
                    P("mezze_plate", "Mezze plate", 0.14f, 0f, 3.0f, 0.6f, 4.0f, 1f, 4.3f) },
            new[] { R("hero_vase", "The Hero Vase", 30f, 1.2f, 0f, 0.2f, 0.15f, 0.3f, 0.55f, 0.36f, 0.8f, 0.2f, 0.9f, 0.16f, 1f, 0.22f),
                    R("krater", "The Great Krater", 24f, 1.0f, 0f, 0.2f, 0.2f, 0.25f, 0.4f, 0.45f, 0.85f, 0.55f, 1f, 0.6f),
                    R("hydria", "The Hydria", 18f, 1.15f, 0f, 0.2f, 0.2f, 0.32f, 0.55f, 0.4f, 0.82f, 0.18f, 1f, 0.2f),
                    R("owl_jug", "The Owl Jug", 14f, 0.8f, 0f, 0.3f, 0.45f, 0.46f, 0.85f, 0.34f, 1f, 0.3f),
                    R("dolphin_plate", "The Dolphin Plate", 9f, 0.16f, 0f, 3.0f, 0.6f, 4.3f, 1f, 4.6f),
                    R("olympic_amphora", "The Olympic Amphora", 5f, 1.4f, 0f, 0.06f, 0.15f, 0.16f, 0.5f, 0.32f, 0.75f, 0.25f, 0.86f, 0.1f, 1f, 0.13f) },
            "#F4F1EA", "#C8B89A", "#5E86A8");

        public static readonly ItemSite Japanese = Studio("japanese", "The Japanese Kiln",
            new[] { P("yunomi", "Yunomi cup", 0.65f, 0f, 0.28f, 0.5f, 0.31f, 1f, 0.33f),
                    P("rice_bowl", "Rice bowl", 0.5f, 0f, 0.28f, 0.3f, 0.6f, 1f, 0.82f),
                    P("sake_bottle", "Sake bottle", 0.95f, 0f, 0.24f, 0.35f, 0.4f, 0.65f, 0.3f, 0.85f, 0.1f, 1f, 0.12f),
                    P("soba_cup", "Soba cup", 0.5f, 0f, 0.3f, 1f, 0.42f),
                    P("sushi_plate", "Sushi plate", 0.12f, 0f, 3.6f, 0.7f, 4.2f, 1f, 4.3f),
                    P("incense_pot", "Incense pot", 0.45f, 0f, 0.4f, 0.4f, 0.62f, 0.85f, 0.58f, 1f, 0.5f) },
            new[] { R("raku_bowl", "The Raku Tea Bowl", 30f, 0.6f, 0f, 0.32f, 0.25f, 0.52f, 1f, 0.6f),
                    R("tea_caddy", "The Tea Caddy", 24f, 0.75f, 0f, 0.3f, 0.2f, 0.4f, 0.85f, 0.4f, 0.92f, 0.2f, 1f, 0.22f),
                    R("ikebana_vase", "The Ikebana Vase", 18f, 1.25f, 0f, 0.22f, 0.4f, 0.24f, 0.75f, 0.14f, 1f, 0.26f),
                    R("kintsugi_bowl", "The Kintsugi Bowl", 14f, 0.55f, 0f, 0.3f, 0.3f, 0.65f, 1f, 0.9f),
                    R("dragon_jar", "The Dragon Jar", 9f, 1.2f, 0f, 0.26f, 0.3f, 0.46f, 0.7f, 0.44f, 0.9f, 0.24f, 1f, 0.26f),
                    R("moon_jar", "The Moon Jar", 5f, 1.05f, 0f, 0.2f, 0.25f, 0.45f, 0.55f, 0.52f, 0.85f, 0.4f, 1f, 0.24f) },
            "#E9E1CF", "#7A6650", "#3E3A36");

        public static readonly ItemSite Moroccan = Studio("moroccan", "The Moroccan Souk",
            new[] { P("tagine", "Tagine", 0.85f, 0f, 1.0f, 0.15f, 1.05f, 0.2f, 0.7f, 0.7f, 0.18f, 0.9f, 0.1f, 1f, 0.12f),
                    P("spice_jar", "Spice jar", 0.6f, 0f, 0.3f, 0.6f, 0.38f, 0.85f, 0.26f, 1f, 0.28f),
                    P("couscous_bowl", "Couscous bowl", 0.5f, 0f, 0.4f, 0.25f, 0.75f, 1f, 1.0f),
                    P("clay_cup", "Clay cup", 0.55f, 0f, 0.26f, 1f, 0.36f),
                    P("oil_jar", "Oil jar", 0.9f, 0f, 0.24f, 0.4f, 0.46f, 0.8f, 0.3f, 0.9f, 0.16f, 1f, 0.2f),
                    P("serving_dish", "Serving dish", 0.16f, 0f, 2.6f, 0.6f, 3.9f, 1f, 4.2f) },
            new[] { R("blue_tagine", "The Blue Tagine", 30f, 0.9f, 0f, 1.0f, 0.15f, 1.05f, 0.2f, 0.7f, 0.7f, 0.18f, 0.9f, 0.1f, 1f, 0.12f),
                    R("zellige_plate", "The Zellige Plate", 24f, 0.16f, 0f, 3.0f, 0.6f, 4.3f, 1f, 4.6f),
                    R("lantern_pot", "The Lantern Pot", 18f, 1.05f, 0f, 0.3f, 0.5f, 0.42f, 0.8f, 0.3f, 0.92f, 0.1f, 1f, 0.06f),
                    R("brass_ewer", "The Brass-Rim Ewer", 14f, 1.1f, 0f, 0.24f, 0.35f, 0.4f, 0.65f, 0.22f, 0.85f, 0.12f, 1f, 0.2f),
                    R("peacock_vase", "The Peacock Vase", 9f, 1.25f, 0f, 0.18f, 0.3f, 0.34f, 0.65f, 0.26f, 0.85f, 0.14f, 1f, 0.24f),
                    R("sultans_jar", "The Sultan's Jar", 5f, 1.15f, 0f, 0.22f, 0.15f, 0.3f, 0.5f, 0.46f, 0.82f, 0.3f, 0.92f, 0.18f, 1f, 0.22f) },
            "#E8C9A0", "#B5653E", "#6E3A24");

        public static readonly ItemSite Modern = Studio("modern", "The Modern Studio",
            new[] { P("espresso_cup", "Espresso cup", 0.4f, 0f, 0.3f, 0.4f, 0.4f, 1f, 0.46f),
                    P("planter", "Planter", 0.7f, 0f, 0.44f, 1f, 0.46f),
                    P("bud_vase", "Bud vase", 0.75f, 0f, 0.26f, 0.4f, 0.32f, 0.75f, 0.12f, 1f, 0.1f),
                    P("ramen_bowl", "Ramen bowl", 0.5f, 0f, 0.36f, 0.4f, 0.85f, 1f, 1.0f),
                    P("milk_jug", "Milk jug", 0.75f, 0f, 0.3f, 0.5f, 0.38f, 0.85f, 0.3f, 1f, 0.36f),
                    P("dinner_plate", "Dinner plate", 0.13f, 0f, 3.4f, 0.6f, 4.3f, 1f, 4.5f) },
            new[] { R("speckled_vase", "The Speckled Vase", 30f, 1.15f, 0f, 0.26f, 0.4f, 0.36f, 0.75f, 0.26f, 1f, 0.2f),
                    R("ribbed_lamp", "The Ribbed Lamp Base", 24f, 1.0f, 0f, 0.3f, 0.5f, 0.44f, 0.85f, 0.32f, 1f, 0.14f),
                    R("bubble_vase", "The Bubble Vase", 18f, 0.9f, 0f, 0.22f, 0.3f, 0.5f, 0.6f, 0.5f, 0.88f, 0.16f, 1f, 0.14f),
                    R("neon_jar", "The Neon Glaze Jar", 14f, 0.85f, 0f, 0.3f, 0.4f, 0.44f, 0.8f, 0.4f, 1f, 0.3f),
                    R("marbled_bowl", "The Marbled Bowl", 9f, 0.55f, 0f, 0.3f, 0.3f, 0.7f, 1f, 0.95f),
                    R("platinum_vase", "The Platinum Vase", 5f, 1.3f, 0f, 0.2f, 0.3f, 0.3f, 0.6f, 0.16f, 0.85f, 0.12f, 1f, 0.22f) },
            "#EDEEF0", "#A9ADB3", "#2E3238");

        static readonly ItemSite[] AllSites = { Workshop, Greek, Japanese, Moroccan, Modern };

        public override EnvironmentDef Def => EnvironmentCatalog.Pottery;
        protected override string SaveKey => "ScrollRoad_Pottery";
        protected override string HashKey => "pottery";
        public override ItemSite[] Sites => AllSites;

        public override string TotalLabel => L.T("POTS MADE");
        public override string StatusActive => L.T("SHAPING");
        public override string StatusIdle => L.T("IDLE · SLOW WHEEL");
        public override string StatusBack => L.T("PRESSING DOWN");
        public override string RarePhrase => L.T("Shaping");
        public override string NextLine(long index, EnvItem item) => L.F("On the wheel: {0}", L.Lower(L.T(item.Type.Name)));
        public override string CollectionShort => L.T("POTTERY SHELF");
        public override string LogNoun => L.T("pieces");
        public override string MetDetail(ItemRecord r) => L.F("Made ×{0}", r.count) + "\n<alpha=#88>" + L.F("First fired {0}", r.firstSeen);
        public override string UnmetDetail => "<alpha=#88>" + L.T("Not made yet");

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce(L.F("New on the shelf: {0}", L.T(type.Name)), RareNotes.For(EnvironmentId.Pottery, type.Id) ?? L.T("Stamped on your pottery shelf"), L.F("{0} · on the shelf", L.T(type.Name)));

        // Çömleğin profili: t (0 dip, 1 ağız) → yarıçap (Size cinsinden). Noktalar arası yumuşak geçiş.
        public static float Profile(ItemType type, float t)
        {
            var s = type.Shape;
            if (s == null || s.Length < 4) return 0.3f;
            if (t <= s[0]) return s[1];
            for (int i = 2; i < s.Length; i += 2)
            {
                if (t > s[i]) continue;
                float u = (t - s[i - 2]) / Mathf.Max(1e-4f, s[i] - s[i - 2]);
                u = u * u * (3f - 2f * u);
                return Mathf.Lerp(s[i - 1], s[i + 1], u);
            }
            return s[s.Length - 1];
        }

        public override string LoopClip => "wheel_loop";
        public override float LoopVolume => 0.24f;

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = 0.02f, Clip = "clay_slap", Volume = 0.28f },
            new SoundCue { Cycle = 0, At = 0f, Clip = "pot_shelf", Volume = 0.3f },
        };

        public override SoundCue[] Cues => CueList;
    }
}
