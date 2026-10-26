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
        public string result = ""; // tamamlanınca: ödül satırı (eski kayıtlar; yenileri rewardRare/rewardVenue)
        public string rewardRare = "";  // ödül nadirinin (İngilizce) adı
        public string rewardVenue = ""; // gideceği mekânın (İngilizce) adı
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

        // Ortamın sipariş dili: birim fiili ("Catch {0}") ve nadirin adı (sayıya göre çoğul).
        static string Verb(EnvironmentId env) => env switch
        {
            EnvironmentId.Road => L.N("Drive {0}"),
            EnvironmentId.Fishing => L.N("Catch {0}"),
            EnvironmentId.Door => L.N("Welcome {0}"),
            EnvironmentId.Wind => L.N("Generate {0}"),
            EnvironmentId.Vinyl => L.N("Play {0}"),
            EnvironmentId.Globe => L.N("Let {0} roll by"),
            EnvironmentId.Boomerang => L.N("Hit {0}"),
            EnvironmentId.Pottery => L.N("Throw {0}"),
            EnvironmentId.Peeler => L.N("Peel {0}"),
            EnvironmentId.Kebab => L.N("Serve {0}"),
            _ => L.N("Reach {0}"),
        };

        static string RareCount(EnvironmentId env, int n) => env switch
        {
            EnvironmentId.Door => L.P(n, "{0} special guest", "{0} special guests"),
            EnvironmentId.Wind => L.P(n, "{0} landmark", "{0} landmarks"),
            EnvironmentId.Vinyl => L.P(n, "{0} rare record", "{0} rare records"),
            EnvironmentId.Globe => L.P(n, "{0} wonder", "{0} wonders"),
            EnvironmentId.Boomerang => L.P(n, "{0} rare target", "{0} rare targets"),
            EnvironmentId.Pottery => L.P(n, "{0} masterpiece", "{0} masterpieces"),
            EnvironmentId.Peeler => L.P(n, "{0} rare fruit", "{0} rare fruit"),
            EnvironmentId.Kebab => L.P(n, "{0} signature dish", "{0} signature dishes"),
            _ => L.P(n, "{0} rare find", "{0} rare finds"),
        };

        public static string Title(Order o)
        {
            var env = (EnvironmentId)o.env;
            var def = EnvironmentCatalog.Get(env);
            switch (o.kind)
            {
                case OrderKind.Units:
                    return L.F(Verb(env), def.Format(o.target / def.UnitsPerPoint, 0));
                case OrderKind.Rares:
                    return L.F("Find {0} at {1}", RareCount(env, o.target), VenueName(o));
                case OrderKind.Species:
                    return L.F("Catch a {0} at {1}", L.T(FishCatalog.Get(o.rare)?.Name ?? "fish"), VenueName(o));
                default:
                    return L.F("Find {0} in any activity", L.P(o.target, "{0} rare find", "{0} rare finds"));
            }
        }

        public static string Subtitle(Order o) =>
            o.kind == OrderKind.AnyRares ? L.T("Weekly order") : L.T(EnvironmentCatalog.Get((EnvironmentId)o.env).Name);

        static string VenueName(Order o)
        {
            var c = EnvironmentContent.Get((EnvironmentId)o.env);
            if (c == null) return o.venue;
            var v = Array.Find(c.Venues, x => x.Id == o.venue);
            return v != null ? L.T(v.Name) : o.venue;
        }

        // Tamamlanan siparişin ödül satırı, gösterim anında seçili dilde.
        public static string Result(Order o)
        {
            if (!string.IsNullOrEmpty(o.rewardRare)) return L.F("{0} is on the way to {1}", L.T(o.rewardRare), L.T(o.rewardVenue));
            if (string.IsNullOrEmpty(o.result) || o.result == StampedResult) return L.T(StampedResult);
            return o.result; // bu sürümden önce İngilizce kaydedilmiş satır
        }

        public static readonly string StampedResult = L.N("Stamped in your order book");

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
