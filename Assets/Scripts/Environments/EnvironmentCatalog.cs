using System;

namespace ScrollRoad
{
    public enum EnvironmentId { Road = 0, Fishing = 1 }

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

        public static readonly EnvironmentDef[] All = { Road, Fishing };

        public static EnvironmentDef Get(EnvironmentId id)
        {
            foreach (var def in All) if (def.Id == id) return def;
            return Road;
        }
    }
}
