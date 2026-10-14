using System;

namespace ScrollRoad
{
    public enum EnvironmentId { Road = 0, Fishing = 1, Door = 2, Wind = 3, Vinyl = 4, Globe = 5, Boomerang = 6, Pottery = 7, Peeler = 8 }

    // Bir ortam (2026-10-13, Docs/ortamlar.md): kendi birimi, gizli puandan birime oranı, açılma
    // eşiği (gizli puanın ömür boyu toplamı) ve oyuncunun gördüğü metinler. Puan hiç görünmez:
    // her sayı aktif ortamın birimine çevrilerek yazılır.
    public sealed class EnvironmentDef
    {
        public EnvironmentId Id;
        public string Name;            // çekmece / seçici başlığı
        public string Tagline;         // seçicide tek satır tarif
        public string Unit;            // "km", "kg"
        public int Decimals;           // birimin olağan ondalık sayısı
        public double UnitsPerPoint;   // birim = puan × oran
        public double UnlockLifetimePoints;
        public string AmountNoun;      // hedef duyurularında: "distance" / "catch"
        public string CollectionName;  // çekmecedeki koleksiyon satırı: "Passport" / "Fish log"

        public double ToUnits(double points) => points * UnitsPerPoint;

        public string Format(double points) => Format(points, Decimals);

        // Ondalıksız birim aşağı yuvarlanır: 1,6 balık "1 fish" (tutulmamış balık sayılmasın).
        public string Format(double points, int decimals) =>
            Fmt.Amount(decimals == 0 ? Math.Floor(ToUnits(points) + 1e-9) : ToUnits(points), decimals, Unit);
    }

    public static class EnvironmentCatalog
    {
        // 1 puan = Yol'da 1 metre. Puan katmanından önceki km kaydı birebir taşınır (1 km = 1000 puan)
        // ve aylık leaderboard skoru (eskiden metre) aynı sayıda kalır.
        public static readonly EnvironmentDef Road = new EnvironmentDef
        {
            Id = EnvironmentId.Road,
            Name = "Road trip",
            Tagline = "Drive from the plains to the city.",
            Unit = "km",
            Decimals = 1,
            UnitsPerPoint = 0.001,
            UnlockLifetimePoints = 0,
            AmountNoun = "distance",
            CollectionName = "Passport",
        };

        // Olta (2026-10-14 kullanıcı kararı): birim BALIK SAYISI. Kendi ilerlemesiyle, eşit aralıkla
        // her FishIntervalPoints'te bir balık (Yol'un "durağa varış = damga" mantığı); kg yalnız balığın
        // özelliği, skor değil. 20.000 puan = seyirde ~20 dk, rölantide ~2 sa; günlük (80.000) ve haftalık
        // (500.000) hedefler tam sayı çıkar (4 / 25 balık). Açılma: ömür boyu 250.000 puan (Yol'da 250 km).
        public const double FishIntervalPoints = 20000;

        public static readonly EnvironmentDef Fishing = new EnvironmentDef
        {
            Id = EnvironmentId.Fishing,
            Name = "Fishing",
            Tagline = "Cast, reel in and fill your fish log.",
            Unit = "fish",
            Decimals = 0,
            UnitsPerPoint = 1.0 / FishIntervalPoints,
            UnlockLifetimePoints = 250000,
            AmountNoun = "catch",
            CollectionName = "Fish log",
        };

        // Döner kapı (2026-10-14 kullanıcı kararı): birim ZİYARETÇİ SAYISI, her VisitorIntervalPoints'te bir
        // ziyaretçi (seyirde ~12 sn'de bir; günlük hedef 400, haftalık 2.500). Olta gibi kendi ilerlemesiyle,
        // aylık sıfırlanır. Açılma: ömür boyu 500.000 puan (Yol'da 500 km).
        public const double VisitorIntervalPoints = 200;

        public static readonly EnvironmentDef Door = new EnvironmentDef
        {
            Id = EnvironmentId.Door,
            Name = "Revolving door",
            Tagline = "Spin the door, welcome the crowd.",
            Unit = "visitors",
            Decimals = 0,
            UnitsPerPoint = 1.0 / VisitorIntervalPoints,
            UnlockLifetimePoints = 500000,
            AmountNoun = "visitor",
            CollectionName = "Guest book",
        };

        // Rüzgârgülü (2026-10-14, ajan kararı — onay bekliyor): birim kWh, her KwhIntervalPoints'te bir kWh
        // (seyirde ~30 sn'de bir; günlük hedef 160, haftalık 1.000). Aylık sıfırlanır. Açılma: ömür boyu
        // 1.000.000 puan (Yol'da 1.000 km; Kapı'nın iki katı).
        public const double KwhIntervalPoints = 500;

        public static readonly EnvironmentDef Wind = new EnvironmentDef
        {
            Id = EnvironmentId.Wind,
            Name = "Wind turbine",
            Tagline = "Spin the blades, power the valley.",
            Unit = "kWh",
            Decimals = 0,
            UnitsPerPoint = 1.0 / KwhIntervalPoints,
            UnlockLifetimePoints = 1000000,
            AmountNoun = "power",
            CollectionName = "Grid log",
        };

        // Plak tablası (2026-10-14, ajan kararı — onay bekliyor): birim PARÇA, her TrackIntervalPoints'te bir
        // parça (seyirde ~4 dk, gerçek bir şarkı uzunluğu; günlük hedef 20, haftalık 125). Aylık sıfırlanır.
        // Açılma: ömür boyu 1.500.000 puan (Yol'da 1.500 km).
        public const double TrackIntervalPoints = 4000;

        public static readonly EnvironmentDef Vinyl = new EnvironmentDef
        {
            Id = EnvironmentId.Vinyl,
            Name = "Record player",
            Tagline = "Spin the record, the music follows your scroll.",
            Unit = "tracks",
            Decimals = 0,
            UnitsPerPoint = 1.0 / TrackIntervalPoints,
            UnlockLifetimePoints = 1500000,
            AmountNoun = "music",
            CollectionName = "Record crate",
        };

        // Dünyayı döndüren el (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim YIL,
        // her YearIntervalPoints'te bir yıl (seyirde dakikada bir; günlük hedef 80, haftalık 500). Aylık
        // sıfırlanır. Açılma: ömür boyu 2.000.000 puan (Plak'tan sonra, merdiven 500.000'er artar).
        public const double YearIntervalPoints = 1000;

        public static readonly EnvironmentDef Globe = new EnvironmentDef
        {
            Id = EnvironmentId.Globe,
            Name = "Turn the world",
            Tagline = "Spin the globe, let the years roll by.",
            Unit = "years",
            Decimals = 0,
            UnitsPerPoint = 1.0 / YearIntervalPoints,
            UnlockLifetimePoints = 2000000,
            AmountNoun = "history",
            CollectionName = "Chronicle",
        };

        // Bumerang (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim VURULAN HEDEF,
        // her TargetIntervalPoints'te bir atış = bir hedef (seyirde ~15 sn'de bir; günlük hedef 320, haftalık
        // 2.000). Mesafe yalnız hedefin özelliği (rekor). Aylık sıfırlanır. Açılma: ömür boyu 2.500.000 puan.
        public const double TargetIntervalPoints = 250;

        public static readonly EnvironmentDef Boomerang = new EnvironmentDef
        {
            Id = EnvironmentId.Boomerang,
            Name = "Boomerang",
            Tagline = "Spin it up, let it fly, hit the target.",
            Unit = "targets",
            Decimals = 0,
            UnitsPerPoint = 1.0 / TargetIntervalPoints,
            UnlockLifetimePoints = 2500000,
            AmountNoun = "target",
            CollectionName = "Target log",
        };

        // Çömlekçi çarkı (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim BİTEN ÇÖMLEK,
        // her PotIntervalPoints'te bir çömlek (seyirde ~2 dk'da bir; günlük hedef 40, haftalık 250). Aylık
        // sıfırlanır. Açılma: ömür boyu 3.000.000 puan.
        public const double PotIntervalPoints = 2000;

        public static readonly EnvironmentDef Pottery = new EnvironmentDef
        {
            Id = EnvironmentId.Pottery,
            Name = "Pottery wheel",
            Tagline = "Spin the wheel, shape the clay.",
            Unit = "pots",
            Decimals = 0,
            UnitsPerPoint = 1.0 / PotIntervalPoints,
            UnlockLifetimePoints = 3000000,
            AmountNoun = "pottery",
            CollectionName = "Pottery shelf",
        };

        // Meyve soyacağı (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim SOYULAN MEYVE,
        // her FruitIntervalPoints'te bir meyve (seyirde ~75 sn'de bir; günlük hedef 64, haftalık 400). Kabuk
        // uzunluğu meyvenin özelliği ("kopmayan kabuk" rekoru). Aylık sıfırlanır. Açılma: ömür boyu 3.500.000 puan.
        public const double FruitIntervalPoints = 1250;

        public static readonly EnvironmentDef Peeler = new EnvironmentDef
        {
            Id = EnvironmentId.Peeler,
            Name = "Fruit peeler",
            Tagline = "Turn the crank, keep the peel in one piece.",
            Unit = "fruit",
            Decimals = 0,
            UnitsPerPoint = 1.0 / FruitIntervalPoints,
            UnlockLifetimePoints = 3500000,
            AmountNoun = "peeling",
            CollectionName = "Fruit bowl",
        };

        public static readonly EnvironmentDef[] All = { Road, Fishing, Door, Wind, Vinyl, Globe, Boomerang, Pottery, Peeler };

        public static EnvironmentDef Get(EnvironmentId id)
        {
            foreach (var def in All) if (def.Id == id) return def;
            return Road;
        }
    }
}
