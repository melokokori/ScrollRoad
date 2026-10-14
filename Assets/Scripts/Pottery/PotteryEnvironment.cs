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

        static readonly ItemSite[] AllSites = { Workshop };

        public override EnvironmentDef Def => EnvironmentCatalog.Pottery;
        protected override string SaveKey => "ScrollRoad_Pottery";
        protected override string HashKey => "pottery";
        public override ItemSite[] Sites => AllSites;

        public override string TotalLabel => "POTS MADE";
        public override string StatusActive => "SHAPING";
        public override string StatusIdle => "IDLE · SLOW WHEEL";
        public override string StatusBack => "PRESSING DOWN";
        public override string RarePhrase => "Shaping";
        public override string NextLine(long index, EnvItem item) => "On the wheel: " + item.Type.Name.ToLowerInvariant();
        public override string CollectionShort => "POTTERY SHELF";
        public override string LogNoun => "pieces";
        public override string MetDetail(ItemRecord r) => $"Made ×{r.count}\n<alpha=#88>First fired {r.firstSeen}";
        public override string UnmetDetail => "<alpha=#88>Not made yet";

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce($"New on the shelf: {type.Name}", "Stamped on your pottery shelf", $"{type.Name} · on the shelf");

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
