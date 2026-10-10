using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Aylık yolculuğun haritası (2026-10-10, kullanıcı kararı): başta sık, sona doğru seyrek
    // 9 bölge + aralarında kasabalar + şehir (adlar İngilizce, oyun İngilizce yayınlanacak). Bölgeye girince manzara değişir; her durak o ayın
    // pasaportuna damga olur (damgalar ayın en uzak noktasından türetilir, ayrıca kaydedilmez).
    // Tahmini tempo: günde ~2 saat aktif ≈ 150 km → ilk gün Wheat Fields, ay sonunda ~Desert.
    public static class RouteMap
    {
        public enum StopKind { Region, Town, City }

        public readonly struct Stop
        {
            public readonly string Name;
            public readonly double Km;
            public readonly StopKind Kind;
            public readonly int RegionIndex;

            public Stop(string name, double km, StopKind kind, int regionIndex)
            {
                Name = name;
                Km = km;
                Kind = kind;
                RegionIndex = regionIndex;
            }
        }

        public sealed class Region
        {
            public string Name;
            public double StartKm;
            public Color Sky;
            public Color FarLand;
            public Color NearLand;
            public float FarHeight = 1f;  // tepe yüksekliği çarpanı (dağ/kanyon yüksek, çöl alçak)
            public float NearHeight = 1f;
            public (string name, double km)[] Towns;
        }

        public const string CityName = "The City";

        public static readonly Region[] Regions =
        {
            new Region
            {
                Name = "Plains", StartKm = 0,
                Sky = new Color(0.84f, 0.93f, 0.95f), FarLand = new Color(0.72f, 0.84f, 0.70f), NearLand = new Color(0.60f, 0.76f, 0.58f),
                Towns = new[] { ("Poplar Hollow", 30.0), ("Millbrook", 70.0), ("Greenfield", 110.0) },
            },
            new Region
            {
                Name = "Wheat Fields", StartKm = 150,
                Sky = new Color(0.95f, 0.93f, 0.84f), FarLand = new Color(0.90f, 0.82f, 0.56f), NearLand = new Color(0.85f, 0.72f, 0.42f),
                FarHeight = 0.7f, NearHeight = 0.6f,
                Towns = new[] { ("Goldenear", 220.0), ("Harvest Hill", 310.0), ("Poppyvale", 400.0) },
            },
            new Region
            {
                Name = "Pine Forest", StartKm = 500,
                Sky = new Color(0.84f, 0.92f, 0.88f), FarLand = new Color(0.47f, 0.62f, 0.52f), NearLand = new Color(0.31f, 0.48f, 0.38f),
                FarHeight = 1.2f, NearHeight = 1.25f,
                Towns = new[] { ("Pinecrest", 650.0), ("Resin Creek", 820.0), ("Cone Hollow", 1000.0) },
            },
            new Region
            {
                Name = "Lakeshore", StartKm = 1200,
                Sky = new Color(0.80f, 0.93f, 0.94f), FarLand = new Color(0.60f, 0.78f, 0.84f), NearLand = new Color(0.54f, 0.72f, 0.60f),
                FarHeight = 0.8f, NearHeight = 0.7f,
                Towns = new[] { ("Reedbrook", 1450.0), ("Gull Harbor", 1800.0), ("Rushmere", 2150.0) },
            },
            new Region
            {
                Name = "Mountain Pass", StartKm = 2500,
                Sky = new Color(0.86f, 0.90f, 0.96f), FarLand = new Color(0.64f, 0.68f, 0.76f), NearLand = new Color(0.50f, 0.56f, 0.58f),
                FarHeight = 1.9f, NearHeight = 1.5f,
                Towns = new[] { ("Eagle Rock", 2900.0), ("Slopeside", 3400.0), ("Misty Ridge", 3950.0) },
            },
            new Region
            {
                Name = "Desert", StartKm = 4500,
                Sky = new Color(0.98f, 0.92f, 0.80f), FarLand = new Color(0.94f, 0.82f, 0.60f), NearLand = new Color(0.89f, 0.73f, 0.49f),
                FarHeight = 0.6f, NearHeight = 0.5f,
                Towns = new[] { ("Sandhead", 5200.0), ("Mirage", 6000.0), ("Palmwell", 6800.0) },
            },
            new Region
            {
                Name = "Canyon", StartKm = 7500,
                Sky = new Color(0.98f, 0.88f, 0.78f), FarLand = new Color(0.84f, 0.56f, 0.44f), NearLand = new Color(0.70f, 0.42f, 0.32f),
                FarHeight = 1.7f, NearHeight = 1.4f,
                Towns = new[] { ("Red Rock", 8400.0), ("Echo Falls", 9300.0), ("Stonefoot", 10200.0) },
            },
            new Region
            {
                Name = "Snowy Plateau", StartKm = 11000,
                Sky = new Color(0.92f, 0.95f, 0.98f), FarLand = new Color(0.86f, 0.90f, 0.95f), NearLand = new Color(0.78f, 0.84f, 0.90f),
                FarHeight = 1.3f, NearHeight = 1.1f,
                Towns = new[] { ("Frostby", 12000.0), ("Icewell", 13000.0), ("Snowdrop", 14000.0) },
            },
            new Region
            {
                Name = "Sunset Road", StartKm = 15000,
                Sky = new Color(0.99f, 0.84f, 0.70f), FarLand = new Color(0.80f, 0.62f, 0.62f), NearLand = new Color(0.62f, 0.46f, 0.52f),
                Towns = new[] { ("Eventide", 16000.0), ("Horizon", 17000.0) },
            },
        };

        // Bölge, kasaba ve şehir duraklarının km'ye göre sıralı listesi (Plains 0 km'de: ilk damga hediye).
        public static readonly IReadOnlyList<Stop> Stops = BuildStops();

        static Stop[] BuildStops()
        {
            var list = new List<Stop>();
            for (int r = 0; r < Regions.Length; r++)
            {
                list.Add(new Stop(Regions[r].Name, Regions[r].StartKm, StopKind.Region, r));
                foreach (var town in Regions[r].Towns) list.Add(new Stop(town.name, town.km, StopKind.Town, r));
            }
            list.Add(new Stop(CityName, RoadDrive.DefaultCityDistanceKm, StopKind.City, Regions.Length - 1));
            list.Sort((a, b) => a.Km.CompareTo(b.Km));
            return list.ToArray();
        }

        // Verilen en uzak noktaya kadar damgalanan durak sayısı (Stops'un ilk N'i).
        public static int CountPassed(double maxKm)
        {
            int n = 0;
            while (n < Stops.Count && Stops[n].Km <= maxKm) n++;
            return n;
        }

        // Konumun ilerisindeki ilk durak; yoksa (şehrin ötesi) -1.
        public static int NextIndexAfter(double km)
        {
            for (int i = 0; i < Stops.Count; i++) if (Stops[i].Km > km) return i;
            return -1;
        }

        public static int RegionIndexAt(double km)
        {
            int index = 0;
            for (int r = 1; r < Regions.Length; r++) if (km >= Regions[r].StartKm) index = r;
            return index;
        }

        // Bölge geçişi tabelanın etrafında yumuşakça olur (sınırın ±7,5 km'si, seyirde ~15 dk):
        // tabelada manzara tam yarı yolda. a = önceki bölge, b = sonraki, t = karışım (0..1).
        const double HalfBlendKm = 7.5;

        public static void Blend(double km, out Region a, out Region b, out float t)
        {
            int i = RegionIndexAt(km);
            a = b = Regions[i];
            t = 0f;

            int from;
            if (i + 1 < Regions.Length && km > Regions[i + 1].StartKm - HalfBlendKm) from = i;      // sınıra yaklaşıyor
            else if (i > 0 && km < Regions[i].StartKm + HalfBlendKm) from = i - 1;                  // sınırı yeni geçti
            else return;

            a = Regions[from];
            b = Regions[from + 1];
            float raw = (float)((km - (b.StartKm - HalfBlendKm)) / (2 * HalfBlendKm));
            t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(raw));
        }
    }
}
