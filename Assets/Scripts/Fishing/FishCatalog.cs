using System;
using System.Collections.Generic;

namespace ScrollRoad
{
    // Olta türleri: hepsi spinning/LRF'de sahte yeme vuran gerçek balıklar. Ağırlık aralıkları gerçekçi
    // ama cömert (oyun); sıklık küçükten büyüğe azalır. Görseller Assets/Art/Fishing/<id>.png
    // (Tools/gen_fishing.py), sağa bakar. Id'ler bütün mekânlarda benzersiz (balık defteri id ile tutar).
    public sealed class FishSpecies
    {
        public string Id;
        public string Name;
        public float MinKg, MaxKg;
        public float Frequency;    // göreli sıklık
        public float LengthUnits;  // en büyük örneğin kart sahnesindeki boyu (dünya birimi)
    }

    // Mekân (Yol'daki bölgelerin karşılığı, Docs/ortamlar.md): kendi balık seti; Olta'nın ömür boyu
    // tutulan balık sayısıyla açılır (aylık sayaç değil). Göl → Nehir → (Kıyı → Açık deniz → Buz deliği).
    public sealed class FishingSpot
    {
        public string Id;
        public string Name;
        public FishSpecies[] Species;
        // Gizli efsane (yalnız Göl: Altın Sazan): Species'te değil, "x / 6" sayımına girmez; defterde
        // tutulana kadar görünmez. Her atışta LegendaryChance olasılıkla, ömür boyu LegendaryMinFish balıktan sonra.
        public FishSpecies Legendary;
        public float LegendaryChance;
        public int LegendaryMinFish;

        public float HeaviestKg
        {
            get
            {
                float max = 0f;
                foreach (var s in Species) max = Math.Max(max, s.MaxKg);
                return max;
            }
        }

        public bool Contains(string speciesId)
        {
            foreach (var s in Species) if (s.Id == speciesId) return true;
            return Legendary != null && Legendary.Id == speciesId;
        }
    }

    public static class FishCatalog
    {
        // 2026-10-13 prototip: Göl, 6 tür.
        public static readonly FishingSpot Lake = new FishingSpot
        {
            Id = "lake",
            Name = "The Lake",
            Species = new[]
            {
                new FishSpecies { Id = "perch", Name = "Perch", MinKg = 0.1f, MaxKg = 1.2f, Frequency = 30f, LengthUnits = 0.9f },
                new FishSpecies { Id = "chub", Name = "Chub", MinKg = 0.3f, MaxKg = 2.5f, Frequency = 24f, LengthUnits = 1.1f },
                new FishSpecies { Id = "trout", Name = "Rainbow Trout", MinKg = 0.5f, MaxKg = 3.5f, Frequency = 18f, LengthUnits = 1.3f },
                new FishSpecies { Id = "bass", Name = "Largemouth Bass", MinKg = 0.5f, MaxKg = 4f, Frequency = 14f, LengthUnits = 1.2f },
                new FishSpecies { Id = "zander", Name = "Zander", MinKg = 1f, MaxKg = 8f, Frequency = 9f, LengthUnits = 1.7f },
                new FishSpecies { Id = "pike", Name = "Northern Pike", MinKg = 2f, MaxKg = 14f, Frequency = 5f, LengthUnits = 2.4f },
            },
            // 2026-10-14: Altın Sazan — atış başına %0,4 (ortalama ~250 atışta bir), ömür boyu 30 balıktan önce çıkmaz.
            Legendary = new FishSpecies { Id = "golden_carp", Name = "Golden Carp", MinKg = 6f, MaxKg = 20f, Frequency = 0f, LengthUnits = 1.8f },
            LegendaryChance = 0.004f,
            LegendaryMinFish = 30,
        };

        // 2026-10-14: Nehir — akıntılı soğuk su, kayalık kıyı. Ömür boyu 20 balıkta açılır (seyirde ~7 sa).
        public static readonly FishingSpot River = new FishingSpot
        {
            Id = "river",
            Name = "The River",
            Species = new[]
            {
                new FishSpecies { Id = "grayling", Name = "Grayling", MinKg = 0.2f, MaxKg = 1.5f, Frequency = 28f, LengthUnits = 1.0f },
                new FishSpecies { Id = "brown_trout", Name = "Brown Trout", MinKg = 0.3f, MaxKg = 3f, Frequency = 24f, LengthUnits = 1.2f },
                new FishSpecies { Id = "ide", Name = "Ide", MinKg = 0.4f, MaxKg = 3f, Frequency = 18f, LengthUnits = 1.1f },
                new FishSpecies { Id = "asp", Name = "Asp", MinKg = 1f, MaxKg = 6f, Frequency = 14f, LengthUnits = 1.5f },
                new FishSpecies { Id = "barbel", Name = "Barbel", MinKg = 1f, MaxKg = 7f, Frequency = 10f, LengthUnits = 1.5f },
                new FishSpecies { Id = "huchen", Name = "Huchen", MinKg = 3f, MaxKg = 18f, Frequency = 6f, LengthUnits = 2.5f },
            },
        };

        // 2026-10-14: Kıyı — mendirekten LRF, tuzlu su (Ege/Akdeniz kıyı balıkları). Ömür boyu 50 balıkta açılır.
        public static readonly FishingSpot Coast = new FishingSpot
        {
            Id = "coast",
            Name = "The Coast",
            Species = new[]
            {
                new FishSpecies { Id = "horse_mackerel", Name = "Horse Mackerel", MinKg = 0.05f, MaxKg = 0.5f, Frequency = 28f, LengthUnits = 0.8f },
                new FishSpecies { Id = "garfish", Name = "Garfish", MinKg = 0.1f, MaxKg = 1f, Frequency = 22f, LengthUnits = 1.4f },
                new FishSpecies { Id = "gilthead", Name = "Gilthead Bream", MinKg = 0.2f, MaxKg = 2.5f, Frequency = 18f, LengthUnits = 1.0f },
                new FishSpecies { Id = "bluefish", Name = "Bluefish", MinKg = 0.4f, MaxKg = 5f, Frequency = 14f, LengthUnits = 1.4f },
                new FishSpecies { Id = "seabass", Name = "European Seabass", MinKg = 0.5f, MaxKg = 6f, Frequency = 11f, LengthUnits = 1.5f },
                new FishSpecies { Id = "leerfish", Name = "Leerfish", MinKg = 2f, MaxKg = 16f, Frequency = 7f, LengthUnits = 2.3f },
            },
        };

        // 2026-10-14: Açık deniz — kayıktan spinning/jigging, büyük pelajikler. Ömür boyu 100 balıkta açılır.
        public static readonly FishingSpot OpenSea = new FishingSpot
        {
            Id = "open_sea",
            Name = "The Open Sea",
            Species = new[]
            {
                new FishSpecies { Id = "atlantic_mackerel", Name = "Atlantic Mackerel", MinKg = 0.2f, MaxKg = 1.5f, Frequency = 28f, LengthUnits = 0.9f },
                new FishSpecies { Id = "bonito", Name = "Atlantic Bonito", MinKg = 1f, MaxKg = 6f, Frequency = 22f, LengthUnits = 1.3f },
                new FishSpecies { Id = "little_tunny", Name = "Little Tunny", MinKg = 1.5f, MaxKg = 10f, Frequency = 18f, LengthUnits = 1.5f },
                new FishSpecies { Id = "mahi", Name = "Mahi-mahi", MinKg = 2f, MaxKg = 18f, Frequency = 14f, LengthUnits = 1.9f },
                new FishSpecies { Id = "amberjack", Name = "Greater Amberjack", MinKg = 4f, MaxKg = 30f, Frequency = 11f, LengthUnits = 2.2f },
                new FishSpecies { Id = "bluefin", Name = "Bluefin Tuna", MinKg = 20f, MaxKg = 120f, Frequency = 7f, LengthUnits = 3.0f },
            },
        };

        // 2026-10-14: Buz deliği — donmuş göl, delikten dikey avcılık. Ömür boyu 180 balıkta açılır.
        public static readonly FishingSpot IceHole = new FishingSpot
        {
            Id = "ice_hole",
            Name = "The Ice Hole",
            Species = new[]
            {
                new FishSpecies { Id = "yellow_perch", Name = "Yellow Perch", MinKg = 0.1f, MaxKg = 1f, Frequency = 28f, LengthUnits = 0.8f },
                new FishSpecies { Id = "whitefish", Name = "Whitefish", MinKg = 0.3f, MaxKg = 2.5f, Frequency = 22f, LengthUnits = 1.1f },
                new FishSpecies { Id = "arctic_char", Name = "Arctic Char", MinKg = 0.5f, MaxKg = 5f, Frequency = 18f, LengthUnits = 1.3f },
                new FishSpecies { Id = "burbot", Name = "Burbot", MinKg = 0.5f, MaxKg = 6f, Frequency = 14f, LengthUnits = 1.5f },
                new FishSpecies { Id = "lake_trout", Name = "Lake Trout", MinKg = 1f, MaxKg = 15f, Frequency = 11f, LengthUnits = 2.0f },
                new FishSpecies { Id = "taimen", Name = "Taimen", MinKg = 5f, MaxKg = 30f, Frequency = 7f, LengthUnits = 2.6f },
            },
        };

        public static readonly FishingSpot[] Spots = { Lake, River, Coast, OpenSea, IceHole };

        // Bütün türler mekân sırasıyla (görsel dizileri bu sırayla bağlanır).
        public static readonly FishSpecies[] All = Flatten();

        static FishSpecies[] Flatten()
        {
            var list = new List<FishSpecies>();
            foreach (var spot in Spots)
            {
                list.AddRange(spot.Species);
                if (spot.Legendary != null) list.Add(spot.Legendary);
            }
            return list.ToArray();
        }

        public static FishingSpot GetSpot(string id)
        {
            foreach (var s in Spots) if (s.Id == id) return s;
            return Lake;
        }

        public static int SpotIndex(FishingSpot spot) => Array.IndexOf(Spots, spot);

        public static FishSpecies Get(string id)
        {
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return i;
            return -1;
        }

        // Sıklığa göre tür, aralığında küçüğe eğilimli ağırlık (r² → çoğu balık küçük, iri olan nadir).
        public static FishSpecies Roll(FishingSpot spot, Random rng, int totalFish, out float kg, string focus = null)
        {
            if (spot.Legendary != null && totalFish >= spot.LegendaryMinFish && rng.NextDouble() < spot.LegendaryChance)
            {
                var l = spot.Legendary;
                double lr = rng.NextDouble();
                kg = (float)Math.Round(l.MinKg + (l.MaxKg - l.MinKg) * lr * lr, 2);
                return l;
            }
            var species = spot.Species;
            float total = 0f;
            foreach (var s in species) total += EnvironmentContent.Weight(s.Id, s.Frequency, focus);
            double pick = rng.NextDouble() * total;
            var chosen = species[species.Length - 1];
            foreach (var s in species)
            {
                pick -= EnvironmentContent.Weight(s.Id, s.Frequency, focus);
                if (pick <= 0) { chosen = s; break; }
            }
            double r = rng.NextDouble();
            kg = (float)Math.Round(chosen.MinKg + (chosen.MaxKg - chosen.MinKg) * r * r, 2);
            return chosen;
        }
    }

    // Balık defteri kaydı: ilk tutuluşta tür damgalanır, en büyük kg rekoru tutulur.
    [Serializable]
    public class FishRecord
    {
        public string id = "";
        public int count;
        public float bestKg;
        public string firstCaught = ""; // yyyy-MM-dd
    }

    // Olta ilerlemesi (yerel PlayerPrefs JSON, SaveService.LoadFishing/SaveFishing). Yarım kalan atış
    // da saklanır: kapanıp açılınca sarılmış puan kaybolmaz.
    [Serializable]
    public class FishingProgress
    {
        // Aylık sayaç (UTC yyyy-MM, ScoreEngine ile aynı): ay değişince balık sayısı ve süren atış sıfırlanır.
        public string month = "";
        public int monthFish;

        public double totalKg;
        public int totalFish;
        public List<FishRecord> log = new List<FishRecord>();
        public string lastFishId = "";
        // Seçili mekân (FishingSpot.Id; boş = Göl) ve açıldığı duyurulmuş mekânlar.
        public string spot = "";
        public List<string> announcedSpots = new List<string>();
        public float lastKg;
        // Olta takımı skini (FishingGear.Id; boş = Klasik) ve açıldığı duyurulmuş takımlar.
        public string gear = "";
        public List<string> announcedGear = new List<string>();

        // Süren atış
        public string castFishId = "";
        public float castKg;
        public float castDistance;  // 0..1 (1 = en uzak)
        public float strikeAt;      // 0..1 sarılan oranda vurur
        public double castProgress; // sarılan puan (0..EnvironmentCatalog.FishIntervalPoints)

        public FishRecord Find(string id)
        {
            foreach (var r in log) if (r.id == id) return r;
            return null;
        }
    }
}
