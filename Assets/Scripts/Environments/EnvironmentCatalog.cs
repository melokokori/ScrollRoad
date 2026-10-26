using System;

namespace ScrollRoad
{
    public enum EnvironmentId { Road = 0, Fishing = 1, Door = 2, Wind = 3, Vinyl = 4, Globe = 5, Boomerang = 6, Pottery = 7, Peeler = 8, Kebab = 9 }

    // Bir ortam (2026-10-13, Docs/ortamlar.md): kendi birimi, gizli puandan birime oranı ve oyuncunun
    // gördüğü metinler. Puan hiç görünmez: her sayı aktif ortamın birimine çevrilerek yazılır.
    // Tüm ortamlar baştan açık (2026-10-15 kullanıcı kararı); oynayarak yalnız skinler açılır.
    public sealed class EnvironmentDef
    {
        public EnvironmentId Id;
        public string Name;            // çekmece / seçici başlığı
        public string Tagline;         // seçicide tek satır tarif
        public string Unit;            // "km", "kg"
        public int Decimals;           // birimin olağan ondalık sayısı
        public double UnitsPerPoint;   // birim = puan × oran
        public string AmountNoun;      // hedef duyurularında: "distance" / "catch"
        public string CollectionName;  // çekmecedeki koleksiyon satırı: "Passport" / "Fish log"

        public double ToUnits(double points) => points * UnitsPerPoint;

        public string Format(double points) => Format(points, Decimals);

        // Ondalıksız birim aşağı yuvarlanır: 1,6 balık "1 fish" (tutulmamış balık sayılmasın).
        // Sayılabilir birimler dilin çoğul kuralıyla yazılır (anahtar "{0} fish", UnitKeys).
        public string Format(double points, int decimals)
        {
            double units = decimals == 0 ? Math.Floor(ToUnits(points) + 1e-9) : ToUnits(points);
            if (decimals == 0 && Countable) return L.P((long)units, "{0} " + Unit, "{0} " + Unit);
            return Fmt.Amount(units, decimals, Unit);
        }

        public bool Countable => Unit != "km" && Unit != "kWh";

        // Sayılabilir birimlerin çeviri anahtarları (Tools/loc/extract.py bunları toplar).
        static readonly string[] UnitKeys =
        {
            L.N("{0} fish"), L.N("{0} visitors"), L.N("{0} tracks"), L.N("{0} years"), L.N("{0} targets"),
            L.N("{0} pots"), L.N("{0} fruit"), L.N("{0} portions"),
        };
    }

    public static class EnvironmentCatalog
    {
        // 1 puan = Yol'da 1 metre. Puan katmanından önceki km kaydı birebir taşınır (1 km = 1000 puan)
        // ve aylık leaderboard skoru (eskiden metre) aynı sayıda kalır.
        public static readonly EnvironmentDef Road = new EnvironmentDef
        {
            Id = EnvironmentId.Road,
            Name = L.N("Road trip"),
            Tagline = L.N("Drive from the plains to the city."),
            Unit = L.N("km"),
            Decimals = 1,
            UnitsPerPoint = 0.001,
            AmountNoun = "distance",
            CollectionName = L.N("Passport"),
        };

        // Olta (2026-10-14 kullanıcı kararı): birim BALIK SAYISI. Kendi ilerlemesiyle, eşit aralıkla
        // her FishIntervalPoints'te bir balık (Yol'un "durağa varış = damga" mantığı); kg yalnız balığın
        // özelliği, skor değil. 20.000 puan = seyirde ~20 dk, rölantide ~2 sa; günlük (80.000) ve haftalık
        // (500.000) hedefler tam sayı çıkar (4 / 25 balık).
        public const double FishIntervalPoints = 20000;

        public static readonly EnvironmentDef Fishing = new EnvironmentDef
        {
            Id = EnvironmentId.Fishing,
            Name = L.N("Fishing"),
            Tagline = L.N("Cast, reel in and fill your fish log."),
            Unit = L.N("fish"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / FishIntervalPoints,
            AmountNoun = "catch",
            CollectionName = L.N("Fish log"),
        };

        // Döner kapı (2026-10-14 kullanıcı kararı): birim ZİYARETÇİ SAYISI, her VisitorIntervalPoints'te bir
        // ziyaretçi (seyirde ~12 sn'de bir; günlük hedef 400, haftalık 2.500). Olta gibi kendi ilerlemesiyle,
        // aylık sıfırlanır.
        public const double VisitorIntervalPoints = 200;

        public static readonly EnvironmentDef Door = new EnvironmentDef
        {
            Id = EnvironmentId.Door,
            Name = L.N("Revolving door"),
            Tagline = L.N("Spin the door, welcome the crowd."),
            Unit = L.N("visitors"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / VisitorIntervalPoints,
            AmountNoun = "visitor",
            CollectionName = L.N("Guest book"),
        };

        // Rüzgârgülü (2026-10-14, ajan kararı — onay bekliyor): birim kWh, her KwhIntervalPoints'te bir kWh
        // (seyirde ~30 sn'de bir; günlük hedef 160, haftalık 1.000). Aylık sıfırlanır.
        public const double KwhIntervalPoints = 500;

        public static readonly EnvironmentDef Wind = new EnvironmentDef
        {
            Id = EnvironmentId.Wind,
            Name = L.N("Wind turbine"),
            Tagline = L.N("Spin the blades, power the valley."),
            Unit = L.N("kWh"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / KwhIntervalPoints,
            AmountNoun = "power",
            CollectionName = L.N("Grid log"),
        };

        // Plak tablası (2026-10-14, ajan kararı — onay bekliyor): birim PARÇA, her TrackIntervalPoints'te bir
        // parça (seyirde ~4 dk, gerçek bir şarkı uzunluğu; günlük hedef 20, haftalık 125). Aylık sıfırlanır.
        public const double TrackIntervalPoints = 4000;

        public static readonly EnvironmentDef Vinyl = new EnvironmentDef
        {
            Id = EnvironmentId.Vinyl,
            Name = L.N("Record player"),
            Tagline = L.N("Spin the record, the music follows your scroll."),
            Unit = L.N("tracks"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / TrackIntervalPoints,
            AmountNoun = "music",
            CollectionName = L.N("Record crate"),
        };

        // Dünyayı döndüren el (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim YIL,
        // her YearIntervalPoints'te bir yıl (seyirde dakikada bir; günlük hedef 80, haftalık 500). Aylık
        // sıfırlanır.
        public const double YearIntervalPoints = 1000;

        public static readonly EnvironmentDef Globe = new EnvironmentDef
        {
            Id = EnvironmentId.Globe,
            Name = L.N("Turn the world"),
            Tagline = L.N("Spin the globe, let the years roll by."),
            Unit = L.N("years"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / YearIntervalPoints,
            AmountNoun = "history",
            CollectionName = L.N("Chronicle"),
        };

        // Bumerang (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim VURULAN HEDEF,
        // her TargetIntervalPoints'te bir atış = bir hedef (seyirde ~15 sn'de bir; günlük hedef 320, haftalık
        // 2.000). Mesafe yalnız hedefin özelliği (rekor). Aylık sıfırlanır.
        public const double TargetIntervalPoints = 250;

        public static readonly EnvironmentDef Boomerang = new EnvironmentDef
        {
            Id = EnvironmentId.Boomerang,
            Name = L.N("Boomerang"),
            Tagline = L.N("Spin it up, let it fly, hit the target."),
            Unit = L.N("targets"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / TargetIntervalPoints,
            AmountNoun = "target",
            CollectionName = L.N("Target log"),
        };

        // Çömlekçi çarkı (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim BİTEN ÇÖMLEK,
        // her PotIntervalPoints'te bir çömlek (seyirde ~2 dk'da bir; günlük hedef 40, haftalık 250). Aylık
        // sıfırlanır.
        public const double PotIntervalPoints = 2000;

        public static readonly EnvironmentDef Pottery = new EnvironmentDef
        {
            Id = EnvironmentId.Pottery,
            Name = L.N("Pottery wheel"),
            Tagline = L.N("Spin the wheel, shape the clay."),
            Unit = L.N("pots"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / PotIntervalPoints,
            AmountNoun = "pottery",
            CollectionName = L.N("Pottery shelf"),
        };

        // Meyve soyacağı (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim SOYULAN MEYVE,
        // her FruitIntervalPoints'te bir meyve (seyirde ~75 sn'de bir; günlük hedef 64, haftalık 400). Kabuk
        // uzunluğu meyvenin özelliği ("kopmayan kabuk" rekoru). Aylık sıfırlanır.
        public const double FruitIntervalPoints = 1250;

        public static readonly EnvironmentDef Peeler = new EnvironmentDef
        {
            Id = EnvironmentId.Peeler,
            Name = L.N("Fruit peeler"),
            Tagline = L.N("Peel by hand, keep the peel in one piece."),
            Unit = L.N("fruit"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / FruitIntervalPoints,
            AmountNoun = "peeling",
            CollectionName = L.N("Fruit bowl"),
        };

        // Tavuk/et döner, çağ kebabı (2026-10-14, kullanıcının fikri; ayrıntılar ajan kararı — onay bekliyor): birim
        // PORSİYON, her PortionIntervalPoints'te bir porsiyon (seyirde ~48 sn'de bir; günlük hedef 100, haftalık
        // 625). Aylık sıfırlanır.
        public const double PortionIntervalPoints = 800;

        public static readonly EnvironmentDef Kebab = new EnvironmentDef
        {
            Id = EnvironmentId.Kebab,
            Name = L.N("Kebab spit"),
            Tagline = L.N("Turn the spit, slice the portions."),
            Unit = L.N("portions"),
            Decimals = 0,
            UnitsPerPoint = 1.0 / PortionIntervalPoints,
            AmountNoun = "kebab",
            CollectionName = L.N("Menu"),
        };

        public static readonly EnvironmentDef[] All = { Road, Fishing, Door, Wind, Vinyl, Globe, Boomerang, Pottery, Peeler, Kebab };

        public static EnvironmentDef Get(EnvironmentId id)
        {
            foreach (var def in All) if (def.Id == id) return def;
            return Road;
        }
    }
}
