using System;
using System.Collections.Generic;

namespace ScrollRoad
{
    // Siparişler (Ortam Standardı v2 · Dayanak 4, 2026-10-25): günde 3 + haftada 1, ortamın dilinde somut işler.
    // Para yok, skinler yalnız süreyle açılır; ödül o mekânın eksik bir nadirinin birkaç öğe sonra gelmesi
    // (EnvironmentContent.GrantReward), mekân tamamsa yalnız sipariş defterine damga.
    //  - Units:   bir ortamda N birim ("Welcome 200 visitors") — ~40 seyir dakikası.
    //  - Rares:   bir mekânda N nadir ("Find 2 rare guests at The Mall") — nadir sıklığıyla ~35 dk.
    //  - Species: Olta'da belirli bir tür ("Catch a Zander at The River"; göreli sıklığı ≥ 14 olanlardan).
    //  - AnyRares (haftalık): hangi ortamda olursa olsun N nadir.
    // İlk günlük sipariş, sipariş üretildiğinde seçili olan ortamdan; diğer ikisi başka ortamlardan (geçişi özendirir).
    public enum OrderKind { Units, Rares, Species, AnyRares }

    [Serializable]
    public class Order
    {
        public OrderKind kind;
        public int env;            // EnvironmentId
        public string venue = "";  // Rares / Species
        public string rare = "";   // Species
        public int target;
        public double baseline;    // Units: ortamın ömür boyu puanı; Rares/Species/AnyRares: bulunma sayısı toplamı
        public bool done;
        public string result = ""; // tamamlanınca: ödül satırı
    }

    [Serializable]
    public class OrdersState
    {
        public string day = "";
        public string week = "";
        public List<Order> daily = new List<Order>();
        public Order weekly;
        public int completed; // sipariş defteri: ömür boyu tamamlanan

        // Seri (2026-10-25, GoalTracker'ın yerine): günün 3 siparişinin hepsi / haftalık sipariş tamamlanınca uzar.
        public bool streakMigrated;
        public int dayStreak;
        public string lastStreakDay = "";
        public int weekStreak;
        public string lastStreakWeek = "";
    }

    // Kaldırılan günlük/haftalık hedeflerin (GoalTracker) kaydı: yalnız serisi bir kez siparişlere taşınır.
    [Serializable]
    public class GoalProgress
    {
        public int dayStreak;
        public string lastDoneDay = "";
        public int weekStreak;
        public string lastDoneWeek = "";
    }

    public static class OrderCatalog
    {
        public const double UnitsOrderPoints = 40000; // ~40 seyir dakikası
        public const int RaresPerOrder = 2;
        public const int WeeklyRares = 8;

        // Ortamın sipariş dili: birim fiili ve nadirin adı.
        static string Verb(EnvironmentId env) => env switch
        {
            EnvironmentId.Road => "Drive",
            EnvironmentId.Fishing => "Catch",
            EnvironmentId.Door => "Welcome",
            EnvironmentId.Wind => "Generate",
            EnvironmentId.Vinyl => "Play",
            EnvironmentId.Globe => "Let",
            EnvironmentId.Boomerang => "Hit",
            EnvironmentId.Pottery => "Throw",
            EnvironmentId.Peeler => "Peel",
            EnvironmentId.Kebab => "Serve",
            _ => "Reach",
        };

        static string RareNoun(EnvironmentId env, int n) => env switch
        {
            EnvironmentId.Door => n == 1 ? "special guest" : "special guests",
            EnvironmentId.Wind => n == 1 ? "landmark" : "landmarks",
            EnvironmentId.Vinyl => n == 1 ? "rare record" : "rare records",
            EnvironmentId.Globe => n == 1 ? "wonder" : "wonders",
            EnvironmentId.Boomerang => n == 1 ? "rare target" : "rare targets",
            EnvironmentId.Pottery => n == 1 ? "masterpiece" : "masterpieces",
            EnvironmentId.Peeler => n == 1 ? "rare fruit" : "rare fruit",
            EnvironmentId.Kebab => n == 1 ? "signature dish" : "signature dishes",
            _ => n == 1 ? "rare find" : "rare finds",
        };

        public static string Title(Order o)
        {
            var env = (EnvironmentId)o.env;
            var def = EnvironmentCatalog.Get(env);
            switch (o.kind)
            {
                case OrderKind.Units:
                    string amount = def.Format(o.target / def.UnitsPerPoint, 0);
                    return env == EnvironmentId.Globe ? $"Let {amount} roll by" : $"{Verb(env)} {amount}";
                case OrderKind.Rares:
                    return $"Find {o.target} {RareNoun(env, o.target)} at {VenueName(o)}";
                case OrderKind.Species:
                    return $"Catch a {FishCatalog.Get(o.rare)?.Name ?? "fish"} at {VenueName(o)}";
                default:
                    return $"Find {o.target} rare finds in any activity";
            }
        }

        public static string Subtitle(Order o) =>
            o.kind == OrderKind.AnyRares ? "Weekly order" : EnvironmentCatalog.Get((EnvironmentId)o.env).Name;

        static string VenueName(Order o)
        {
            var c = EnvironmentContent.Get((EnvironmentId)o.env);
            if (c == null) return o.venue;
            var v = Array.Find(c.Venues, x => x.Id == o.venue);
            return v != null ? v.Name : o.venue;
        }

        // Güzel bir hedef: 2 anlamlı basamağa yuvarlanmış (≥1).
        static int Nice(double value)
        {
            if (value <= 1) return 1;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(value)) - 1);
            return (int)Math.Max(1, Math.Round(value / mag) * mag);
        }

        public static int UnitsTarget(EnvironmentId env) => Nice(UnitsOrderPoints * EnvironmentCatalog.Get(env).UnitsPerPoint);

        // Bir günlük sipariş üretir; içerik kaydı yüklenmemiş ortam için yalnız Units.
        public static Order MakeDaily(EnvironmentId env, Random rng)
        {
            var c = EnvironmentContent.Get(env);
            if (c != null)
            {
                // Açık mekânlardan biri (bulunulana ağırlık).
                var open = new List<string>();
                for (int i = 0; i < c.Venues.Length; i++) if (EnvironmentContent.IsVenueUnlocked(c, i)) open.Add(c.Venues[i].Id);
                string venue = rng.NextDouble() < 0.6 || open.Count == 0 ? c.VenueId : open[rng.Next(open.Count)];

                if (env == EnvironmentId.Fishing && rng.NextDouble() < 0.5)
                {
                    var spot = FishCatalog.GetSpot(venue);
                    var pool = Array.FindAll(spot.Species, s => s.Frequency >= 14f);
                    if (pool.Length > 0)
                    {
                        var fish = pool[rng.Next(pool.Length)];
                        return new Order { kind = OrderKind.Species, env = (int)env, venue = venue, rare = fish.Id, target = 1 };
                    }
                }
                if (env != EnvironmentId.Fishing && rng.NextDouble() < 0.5)
                    return new Order { kind = OrderKind.Rares, env = (int)env, venue = venue, target = RaresPerOrder };
            }
            return new Order { kind = OrderKind.Units, env = (int)env, target = UnitsTarget(env) };
        }

        public static Order MakeWeekly() => new Order { kind = OrderKind.AnyRares, env = -1, target = WeeklyRares };
    }
}
