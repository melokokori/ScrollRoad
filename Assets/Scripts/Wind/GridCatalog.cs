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
        // Mekânın sahne renkleri (Ortam Standardı v2): [0] gök, [1] uzak sırt, [2] tepe, [3] çimen, [4] toprak.
        public UnityEngine.Color[] Palette = System.Array.Empty<UnityEngine.Color>();

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

        static UnityEngine.Color Hex(string hex) => UnityEngine.ColorUtility.TryParseHtmlString(hex, out var c) ? c : UnityEngine.Color.magenta;
        static BuildingType B(string id, string name, float freq, float height) => new BuildingType { Id = id, Name = name, Frequency = freq, Height = height };

        // Ortam Standardı v2 (2026-10-25): dört yeni vadi; Rüzgârgülü'nde geçen seyir saatiyle açılır (UnlockLadder).
        static WindSite Valley(string id, string name, BuildingType[] specials, params string[] palette) => new WindSite
        {
            Id = id, Name = name, SpecialChance = 0.03f, SpecialMinIndex = 12, Specials = specials,
            Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly WindSite Coast = Valley("coast", "The Coast", new[]
        {
            B("fish_market", "The Fish Market", 30f, 1.9f), B("lighthouse", "The Lighthouse", 24f, 3.0f), B("boathouse", "The Boathouse", 18f, 1.8f),
            B("seaside_pier", "The Seaside Pier", 14f, 2.0f), B("aquarium", "The Aquarium", 9f, 2.2f), B("lifeboat_station", "The Lifeboat Station", 5f, 2.3f),
        }, "#BFE3F2", "#9CC9C9", "#A8C98A", "#B8D48A", "#C9B48A");

        public static readonly WindSite Desert = Valley("desert", "The Desert", new[]
        {
            B("oasis_inn", "The Oasis Inn", 30f, 1.9f), B("caravanserai", "The Caravanserai", 24f, 2.1f), B("date_farm", "The Date Farm", 18f, 2.3f),
            B("solar_farm", "The Solar Farm", 14f, 1.4f), B("sand_fort", "The Sand Fort", 9f, 2.4f), B("mirage_palace", "The Mirage Palace", 5f, 2.8f),
        }, "#F2E3C4", "#E0B98A", "#D9A86A", "#E2BE84", "#B5874E");

        public static readonly WindSite SnowyPass = Valley("snowy_pass", "The Snowy Pass", new[]
        {
            B("ski_lodge", "The Ski Lodge", 30f, 2.2f), B("mountain_hut", "The Mountain Hut", 24f, 1.7f), B("cable_car", "The Cable Car Station", 18f, 2.4f),
            B("ice_rink", "The Ice Rink", 14f, 1.6f), B("research_station", "The Research Station", 9f, 2.0f), B("ice_hotel", "The Ice Hotel", 5f, 2.2f),
        }, "#DDE9F2", "#B8C8D8", "#EEF3F7", "#F4F7FA", "#9AA8B4");

        public static readonly WindSite City = Valley("city", "The City", new[]
        {
            B("diner", "The Diner", 30f, 1.6f), B("train_station", "The Train Station", 24f, 2.2f), B("theatre", "The Theatre", 18f, 2.5f),
            B("stadium", "The Stadium", 14f, 2.0f), B("art_museum", "The Art Museum", 9f, 2.3f), B("skyscraper", "The Skyscraper", 5f, 3.4f),
        }, "#C9D8E6", "#9AA6B2", "#8FA08A", "#A0B090", "#6A6E72");

        public static readonly WindSite[] Sites = { Hills, Coast, Desert, SnowyPass, City };

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
        public string skin = "";           // seçili skin takımı (boş = Klasik)

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
