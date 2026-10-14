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

        static readonly ItemSite[] AllSites = { Outback };

        public override EnvironmentDef Def => EnvironmentCatalog.Boomerang;
        protected override string SaveKey => "ScrollRoad_Boomerang";
        protected override string HashKey => "boomerang";
        public override ItemSite[] Sites => AllSites;

        // Hedefin mesafesi (m), tam sayı.
        public static float Distance(EnvItem item) => Mathf.Round(item.Type.Value + item.Roll * 30f);

        public override float ItemValue(EnvItem item) => Distance(item);
        public override string FormatValue(float value) => $"{value:0} m";
        public override string RecordLabel => "LONGEST HIT";

        public override string TotalLabel => "TARGETS HIT";
        public override string StatusActive => "THROWING";
        public override string StatusIdle => "IDLE · LAZY THROWS";
        public override string StatusBack => "REWINDING";
        public override string RarePhrase => "Aiming at";
        public override string NextLine(long index, EnvItem item) => $"Next: target {Fmt.Int(index)} · {Distance(item):0} m";
        public override string CollectionShort => "TARGET LOG";
        public override string LogNoun => "targets";
        public override string MetDetail(ItemRecord r) => $"Hit ×{r.count} · best {r.best:0} m\n<alpha=#88>First hit {r.firstSeen}";
        public override string UnmetDetail => "<alpha=#88>Not hit yet";

        protected override void AnnounceNew(NoticeController notice, ItemType type) =>
            notice.Announce($"New target hit: {type.Name}", "Stamped in your target log", $"{type.Name} · in the log");

        static readonly SoundCue[] CueList =
        {
            new SoundCue { Cycle = 0, At = ThrowAt, Clip = "boom_throw", Volume = 0.3f },
            new SoundCue { Cycle = 0, At = 0f, Clip = "boom_hit", Volume = 0.32f },
            new SoundCue { Cycle = 0, At = ReturnEnd, Clip = "boom_catch", Volume = 0.26f },
        };

        public override SoundCue[] Cues => CueList;
    }
}
