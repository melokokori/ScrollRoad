using System;
using System.Collections.Generic;

namespace ScrollRoad
{
    // Rüzgârgülü ortamının vadisindeki yapılar (2026-10-14, Docs/ortamlar.md "Rüzgârgülü"). Ayın i. kWh'ı
    // vadideki i. yapıyı şebekeye bağlar. Sıradan evler sayılır ama deftere girmez; nadir yapılar ilk
    // bağlanışlarında şebeke defterine damgalanır. Görseller Assets/Art/Wind/<id>.png (Tools/gen_wind.py).
    public sealed class BuildingType
    {
        public string Id;
        public string Name;
        public float Frequency; // nadirler arasında göreli sıklık
        public float Height;    // dünya birimi (sıradan kulübe ~1,5)
    }

    // Mekân (Olta'daki FishingSpot, Kapı'daki DoorVenue karşılığı). Prototipte yalnız Tepeler.
    public sealed class WindSite
    {
        public string Id;
        public string Name;
        public int UnlockTotalKwh;
        public BuildingType[] Specials;
        public float SpecialChance;
        public int SpecialMinIndex;

        public bool Contains(string id)
        {
            foreach (var b in Specials) if (b.Id == id) return true;
            return false;
        }
    }

    public static class GridCatalog
    {
        public static readonly BuildingType[] Homes =
        {
            new BuildingType { Id = "home_cottage", Name = "Cottage", Height = 1.45f },
            new BuildingType { Id = "home_farmhouse", Name = "Farmhouse", Height = 1.9f },
            new BuildingType { Id = "home_barn", Name = "Barn", Height = 2.0f },
            new BuildingType { Id = "home_townhouse", Name = "Townhouse", Height = 2.45f },
            new BuildingType { Id = "home_bungalow", Name = "Bungalow", Height = 1.1f },
            new BuildingType { Id = "home_shop", Name = "Corner shop", Height = 1.45f },
        };

        // Tepeler: nadir yapı kWh başına %3 (1 kWh = 2,5 ziyaretçilik puan; gerçek zamanda kapıdaki misafir
        // sıklığına yakın, seyirde ~17 dk'da bir). En nadiri (gözlemevi) seyirde ~9 saatte bir.
        public static readonly WindSite Hills = new WindSite
        {
            Id = "hills",
            Name = "The Hills",
            UnlockTotalKwh = 0,
            SpecialChance = 0.03f,
            SpecialMinIndex = 12,
            Specials = new[]
            {
                new BuildingType { Id = "bakery", Name = "The Bakery", Frequency = 30f, Height = 1.9f },
                new BuildingType { Id = "dairy", Name = "The Dairy Farm", Frequency = 24f, Height = 2.1f },
                new BuildingType { Id = "school", Name = "The Village School", Frequency = 18f, Height = 2.3f },
                new BuildingType { Id = "cinema", Name = "The Cinema", Frequency = 14f, Height = 2.2f },
                new BuildingType { Id = "fairground", Name = "The Fairground", Frequency = 9f, Height = 2.9f },
                new BuildingType { Id = "observatory", Name = "The Observatory", Frequency = 5f, Height = 2.6f },
            },
        };

        public static readonly WindSite[] Sites = { Hills };

        public static readonly BuildingType[] AllSpecials = Flatten();

        static BuildingType[] Flatten()
        {
            var list = new List<BuildingType>();
            foreach (var s in Sites) list.AddRange(s.Specials);
            return list.ToArray();
        }

        public static WindSite GetSite(string id)
        {
            foreach (var s in Sites) if (s.Id == id) return s;
            return Hills;
        }

        public static BuildingType GetSpecial(string id)
        {
            foreach (var b in AllSpecials) if (b.Id == id) return b;
            return null;
        }

        public static int SpecialIndex(string id)
        {
            for (int i = 0; i < AllSpecials.Length; i++) if (AllSpecials[i].Id == id) return i;
            return -1;
        }

        public struct Building
        {
            public BuildingType Type;
            public bool IsSpecial;
            public float Jitter; // -1..1 aralık oynaması
        }

        // Ayın i. yapısı ay + mekân + sıradan belirlenir (kapının VisitorCatalog.Identify'ı gibi): geri sarınca
        // kararıp yeniden bağlanan aynı yapıdır, tabelada "geliyor" denen nadir yapı bağlanınca da odur.
        public static Building Identify(string month, WindSite site, long index, string focus = null, RareGuarantee guarantee = default)
        {
            ulong seed = VisitorCatalog.Fnv(month) ^ (VisitorCatalog.Fnv("wind_" + site.Id) * 31UL);
            var b = new Building { Jitter = (float)(VisitorCatalog.Hash01(seed, index, 4) * 2.0 - 1.0) };
            if (index == guarantee.Index || (index >= site.SpecialMinIndex && VisitorCatalog.Hash01(seed, index, 1) < site.SpecialChance))
            {
                float total = 0f;
                foreach (var s in site.Specials) total += EnvironmentContent.Weight(s.Id, s.Frequency, focus);
                double pick = VisitorCatalog.Hash01(seed, index, 2) * total;
                var chosen = site.Specials[site.Specials.Length - 1];
                foreach (var s in site.Specials)
                {
                    pick -= EnvironmentContent.Weight(s.Id, s.Frequency, focus);
                    if (pick <= 0) { chosen = s; break; }
                }
                chosen = guarantee.Pick(index, chosen, site.Specials, s => s.Id);
                b.Type = chosen;
                b.IsSpecial = true;
                return b;
            }
            // Art arda aynı ev gelmesin: önceki sıranın evinden farklı seç.
            int prev = HomeIndex(seed, index - 1);
            int h = HomeIndex(seed, index);
            if (h == prev) h = (h + 1 + (int)(VisitorCatalog.Hash01(seed, index, 5) * (Homes.Length - 1))) % Homes.Length;
            b.Type = Homes[h];
            return b;
        }

        static int HomeIndex(ulong seed, long index) =>
            (int)(VisitorCatalog.Hash01(seed, index, 3) * Homes.Length) % Homes.Length;
    }

    // Şebeke defteri kaydı: ilk bağlanışta damgalanır.
    [Serializable]
    public class GridRecord
    {
        public string id = "";
        public int count;
        public string firstSeen = ""; // yyyy-MM-dd
    }

    // Rüzgârgülü ilerlemesi (yerel PlayerPrefs JSON, SaveService.LoadWind/SaveWind).
    [Serializable]
    public class WindProgress
    {
        // Aylık sayaç (UTC yyyy-MM): ay değişince konum ve ay rekoru sıfırlanır, defter kalıcı.
        public string month = "";
        public double position;  // bu ay üretilen puan (≥0); bağlanan yapı = konum / aralık
        public long monthBest;   // bu ay bağlanan en yüksek sıra no: yalnız onu aşan yapı sayılır
        public string guaranteeMonth = ""; // ilk nadir garantisinin verildiği ay (EnvironmentContent.GuaranteeIndex)

        public long totalKwh;
        public List<GridRecord> log = new List<GridRecord>();
        public string lastSpecialId = "";
        public string site = "";

        public GridRecord Find(string id)
        {
            foreach (var r in log) if (r.id == id) return r;
            return null;
        }
    }
}
