using System;
using System.Collections.Generic;

namespace ScrollRoad
{
    // Döner kapının ziyaretçileri (2026-10-14, Docs/ortamlar.md "Döner kapı"). Sıradan yürüyenler sayılır
    // ama deftere girmez; nadir ziyaretçiler ilk gelişlerinde misafir defterine damgalanır. Görseller
    // Assets/Art/Door/<id>.png (Tools/gen_visitors.py), yandan SAĞA yürür. Id'ler bütün mekânlarda benzersiz.
    public sealed class VisitorType
    {
        public string Id;
        public string Name;
        public float Frequency; // nadirler arasında göreli sıklık
        public float Height = 1f; // görselin boyu / sıradan yetişkin boyu (ör. maskotun kafası, poşet kulesi)
    }

    // Mekân (Olta'daki FishingSpot'un karşılığı): kendi nadir ziyaretçi seti; ömür boyu ziyaretçi
    // sayısıyla açılır. AVM → Otel → Havalimanı → Müze → Gökdelen. Sıradan yürüyenler ortak.
    public sealed class DoorVenue
    {
        public string Id;
        public string Name;
        public VisitorType[] Guests;
        public float GuestChance;  // bir ziyaretçinin nadir çıkma olasılığı
        public int GuestMinIndex;  // ayın ilk ziyaretçileri hep sıradan (açılışta kalabalık ortalık)
        // Gizli efsane (yalnız AVM: Koca Ayak; Olta'daki Altın Sazan'ın karşılığı): Guests'te değil, "x / 6"
        // sayımına girmez, defterde gelene kadar görünmez. Ziyaretçi başına LegendaryChance olasılıkla, ayın
        // LegendaryMinIndex. ziyaretçisinden sonra (kimlik ay + sıradan belirlendiği için ömür boyu sayıya bağlanmadı).
        public VisitorType Legendary;
        public float LegendaryChance;
        public int LegendaryMinIndex;

        public bool Contains(string id)
        {
            foreach (var g in Guests) if (g.Id == id) return true;
            return Legendary != null && Legendary.Id == id;
        }
    }

    public static class VisitorCatalog
    {
        public static readonly VisitorType[] Walkers =
        {
            new VisitorType { Id = "walker_office", Name = "Office worker" },
            new VisitorType { Id = "walker_student", Name = "Student" },
            new VisitorType { Id = "walker_coat", Name = "Shopper" },
            new VisitorType { Id = "walker_elder", Name = "Grandpa", Height = 0.95f },
            new VisitorType { Id = "walker_teen", Name = "Teenager" },
            new VisitorType { Id = "walker_dress", Name = "Shopper" },
            new VisitorType { Id = "walker_courier", Name = "Courier" },
            new VisitorType { Id = "walker_tourist", Name = "Tourist" },
        };

        // 2026-10-14 prototip: AVM, 6 nadir ziyaretçi. Ziyaretçi başına %1,2 nadir; en nadiri (astronot)
        // ~1.700 ziyaretçide bir (seyirde ~6 saat).
        public static readonly DoorVenue Mall = new DoorVenue
        {
            Id = "mall",
            Name = "The Mall",
            GuestChance = 0.012f,
            GuestMinIndex = 12,
            Guests = new[]
            {
                new VisitorType { Id = "shopper", Name = "Shopping Spree", Frequency = 30f, Height = 1.15f },
                new VisitorType { Id = "mascot", Name = "Teddy Mascot", Frequency = 24f, Height = 1.1f },
                new VisitorType { Id = "mime", Name = "The Mime", Frequency = 18f },
                new VisitorType { Id = "santa", Name = "Santa Claus", Frequency = 14f, Height = 1.05f },
                new VisitorType { Id = "knight", Name = "A Knight", Frequency = 9f, Height = 1.05f },
                new VisitorType { Id = "astronaut", Name = "An Astronaut", Frequency = 5f, Height = 1.05f },
            },
            // 2026-10-14: Koca Ayak — 5.000 ziyaretçide bir (seyirde ~17 sa), ayın ilk 600 ziyaretçisinde çıkmaz.
            Legendary = new VisitorType { Id = "bigfoot", Name = "Bigfoot", Height = 1.3f },
            LegendaryChance = 0.0002f,
            LegendaryMinIndex = 600,
        };

        // 2026-10-14: kalan 4 mekân. Açılma eşikleri Olta'nın 20/50/100/180 balığının puan karşılığı
        // (200 puan/ziyaretçi): 2.000 / 5.000 / 10.000 / 18.000 ömür boyu ziyaretçi.
        public static readonly DoorVenue Hotel = new DoorVenue
        {
            Id = "hotel",
            Name = "The Hotel",
            GuestChance = 0.012f,
            GuestMinIndex = 12,
            Guests = new[]
            {
                new VisitorType { Id = "bellhop", Name = "The Bellhop", Frequency = 30f },
                new VisitorType { Id = "bride", Name = "A Bride", Frequency = 24f, Height = 1.02f },
                new VisitorType { Id = "rockstar", Name = "A Rock Star", Frequency = 18f },
                new VisitorType { Id = "magician", Name = "The Magician", Frequency = 14f, Height = 1.12f },
                new VisitorType { Id = "filmstar", Name = "A Film Star", Frequency = 9f },
                new VisitorType { Id = "king", Name = "The King", Frequency = 5f, Height = 1.08f },
            },
        };

        public static readonly DoorVenue Airport = new DoorVenue
        {
            Id = "airport",
            Name = "The Airport",
            GuestChance = 0.012f,
            GuestMinIndex = 12,
            Guests = new[]
            {
                new VisitorType { Id = "pilot", Name = "The Pilot", Frequency = 30f },
                new VisitorType { Id = "attendant", Name = "Flight Attendant", Frequency = 24f },
                new VisitorType { Id = "backpacker", Name = "A Backpacker", Frequency = 18f, Height = 1.1f },
                new VisitorType { Id = "surfer", Name = "A Surfer", Frequency = 14f },
                new VisitorType { Id = "mountaineer", Name = "A Mountaineer", Frequency = 9f, Height = 1.05f },
                new VisitorType { Id = "diver", Name = "A Deep-sea Diver", Frequency = 5f, Height = 1.05f },
            },
        };

        public static readonly DoorVenue Museum = new DoorVenue
        {
            Id = "museum",
            Name = "The Museum",
            GuestChance = 0.012f,
            GuestMinIndex = 12,
            Guests = new[]
            {
                new VisitorType { Id = "painter", Name = "The Painter", Frequency = 30f },
                new VisitorType { Id = "guide", Name = "Tour Guide", Frequency = 24f, Height = 1.1f },
                new VisitorType { Id = "archaeologist", Name = "An Archaeologist", Frequency = 18f },
                new VisitorType { Id = "ballerina", Name = "A Ballerina", Frequency = 14f },
                new VisitorType { Id = "centurion", Name = "A Centurion", Frequency = 9f, Height = 1.08f },
                new VisitorType { Id = "mummy", Name = "The Mummy", Frequency = 5f },
            },
        };

        public static readonly DoorVenue Skyscraper = new DoorVenue
        {
            Id = "skyscraper",
            Name = "The Skyscraper",
            GuestChance = 0.012f,
            GuestMinIndex = 12,
            Guests = new[]
            {
                new VisitorType { Id = "window_cleaner", Name = "Window Cleaner", Frequency = 30f, Height = 1.05f },
                new VisitorType { Id = "ceo", Name = "The CEO", Frequency = 24f },
                new VisitorType { Id = "firefighter", Name = "A Firefighter", Frequency = 18f, Height = 1.03f },
                new VisitorType { Id = "superhero", Name = "A Superhero", Frequency = 14f },
                new VisitorType { Id = "robot", Name = "A Robot", Frequency = 9f, Height = 1.02f },
                new VisitorType { Id = "alien", Name = "An Alien", Frequency = 5f, Height = 0.72f },
            },
        };

        public static readonly DoorVenue[] Venues = { Mall, Hotel, Airport, Museum, Skyscraper };

        // Bütün nadirler mekân sırasıyla (görsel dizileri bu sırayla bağlanır).
        public static readonly VisitorType[] AllGuests = Flatten();

        static VisitorType[] Flatten()
        {
            var list = new List<VisitorType>();
            foreach (var v in Venues)
            {
                list.AddRange(v.Guests);
                if (v.Legendary != null) list.Add(v.Legendary);
            }
            return list.ToArray();
        }

        public static DoorVenue GetVenue(string id)
        {
            foreach (var v in Venues) if (v.Id == id) return v;
            return Mall;
        }

        public static int VenueIndex(DoorVenue venue) => Array.IndexOf(Venues, venue);

        public static VisitorType GetGuest(string id)
        {
            foreach (var g in AllGuests) if (g.Id == id) return g;
            return null;
        }

        public static int GuestIndex(string id)
        {
            for (int i = 0; i < AllGuests.Length; i++) if (AllGuests[i].Id == id) return i;
            return -1;
        }

        // Ayın i. ziyaretçisinin kim olduğu ay + mekân + sıradan belirlenir (rastgele değil): geri çıkıp
        // yeniden giren aynı kişidir, sahnede yaklaşırken görünen nadir ziyaretçi sayılınca da odur.
        public struct Visitor
        {
            public VisitorType Type;
            public bool IsGuest;
            public bool IsLegendary;
            public float Jitter; // -1..1 kaldırımdaki aralık oynaması
            public float Scale;  // 0,94..1,06 boy çeşitliliği
        }

        public static Visitor Identify(string month, DoorVenue venue, long index, string focus = null, RareGuarantee guarantee = default)
        {
            ulong seed = Fnv(month) ^ (Fnv(venue.Id) * 31UL);
            var v = new Visitor
            {
                Jitter = (float)(Hash01(seed, index, 4) * 2.0 - 1.0),
                Scale = (float)(0.94 + Hash01(seed, index, 5) * 0.12),
            };
            bool forced = index == guarantee.Index;
            if (!forced && venue.Legendary != null && index >= venue.LegendaryMinIndex && Hash01(seed, index, 6) < venue.LegendaryChance)
            {
                v.Type = venue.Legendary;
                v.IsGuest = v.IsLegendary = true;
                return v;
            }
            if (forced || (index >= venue.GuestMinIndex && Hash01(seed, index, 1) < venue.GuestChance))
            {
                float total = 0f;
                foreach (var g in venue.Guests) total += EnvironmentContent.Weight(g.Id, g.Frequency, focus);
                double pick = Hash01(seed, index, 2) * total;
                var chosen = venue.Guests[venue.Guests.Length - 1];
                foreach (var g in venue.Guests)
                {
                    pick -= EnvironmentContent.Weight(g.Id, g.Frequency, focus);
                    if (pick <= 0) { chosen = g; break; }
                }
                chosen = guarantee.Pick(index, chosen, venue.Guests, g => g.Id);
                v.Type = chosen;
                v.IsGuest = true;
                return v;
            }
            v.Type = Walkers[(int)(Hash01(seed, index, 3) * Walkers.Length) % Walkers.Length];
            return v;
        }

        internal static ulong Fnv(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s ?? "")
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return h;
        }

        // splitmix64 → [0,1)
        internal static double Hash01(ulong seed, long index, int salt)
        {
            ulong z = seed + (ulong)index * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xD1B54A32D192ED03UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / (1UL << 53));
        }
    }

    // Misafir defteri kaydı: ilk gelişte damgalanır, kaç kez geldiği tutulur.
    [Serializable]
    public class GuestRecord
    {
        public string id = "";
        public int count;
        public string firstSeen = ""; // yyyy-MM-dd
    }

    // Döner kapı ilerlemesi (yerel PlayerPrefs JSON, SaveService.LoadDoor/SaveDoor).
    [Serializable]
    public class DoorProgress
    {
        // Aylık sayaç (UTC yyyy-MM): ay değişince kapının konumu ve ay rekoru sıfırlanır, defter kalıcı.
        public string month = "";
        public double position;   // bu ay kapının döndüğü puan (≥0); içerideki ziyaretçi = konum / aralık
        public long monthBest;    // bu ay içeri giren en yüksek sıra no: yalnız onu aşan ziyaretçi sayılır
        public string guaranteeMonth = ""; // ilk nadir garantisinin verildiği ay (EnvironmentContent.GuaranteeIndex)

        public long totalVisitors;
        public List<GuestRecord> log = new List<GuestRecord>();
        public string lastGuestId = "";
        // Seçili mekân (DoorVenue.Id; boş = AVM) ve açıldığı duyurulmuş mekânlar.
        public string venue = "";
        public List<string> announcedVenues = new List<string>();

        public GuestRecord Find(string id)
        {
            foreach (var r in log) if (r.id == id) return r;
            return null;
        }
    }
}
