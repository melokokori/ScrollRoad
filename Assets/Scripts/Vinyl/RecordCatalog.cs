using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Plak tablası ortamının plakları (2026-10-14, Docs/ortamlar.md "Plak tablası"). Her plak TracksPerRecord
    // parça; plak bitince sandığa girer. Sıradan plaklar sayılır ama deftere girmez (düz renkli kapak, kodla);
    // nadir plaklar ilk bitişlerinde plak sandığına damgalanır. Kapaklar Assets/Art/Vinyl/<id>.png
    // (Tools/gen_vinyl.py). Adlar kurgusal; müzik VinylMusic'te prosedürel (telifsiz).
    public enum MusicStyle { LoFi = 0, Bossa = 1, Synthwave = 2, Jazz = 3, Disco = 4, Funk = 5 }

    public sealed class RecordType
    {
        public string Id;
        public string Name;
        public float Frequency;
        public MusicStyle Style;
        public Color Label;
    }

    // Mekân (FishingSpot / DoorVenue / WindSite karşılığı). Prototipte yalnız Record Shop.
    public sealed class RecordShop
    {
        public string Id;
        public string Name;
        public RecordType[] Specials;
        public float SpecialChance;
        public int SpecialMinIndex;

        public bool Contains(string id)
        {
            foreach (var r in Specials) if (r.Id == id) return true;
            return false;
        }
    }

    public static class RecordCatalog
    {
        public const int TracksPerRecord = 4;

        // Sıradan plakların etiket renkleri (kapak bundan türer).
        static readonly Color[] PlainLabels =
        {
            new Color(0.86f, 0.33f, 0.30f), new Color(0.95f, 0.72f, 0.28f), new Color(0.40f, 0.62f, 0.52f),
            new Color(0.52f, 0.45f, 0.70f), new Color(0.92f, 0.55f, 0.40f), new Color(0.35f, 0.55f, 0.72f),
            new Color(0.80f, 0.78f, 0.70f), new Color(0.62f, 0.40f, 0.30f),
        };

        // Nadir plak plak başına %30 (seyirde ~53 dk'da bir); en nadiri (Golden Groove) seyirde ~18 saatte bir.
        public static readonly RecordShop Shop = new RecordShop
        {
            Id = "recordshop",
            Name = "The Record Shop",
            SpecialChance = 0.30f,
            SpecialMinIndex = 2,
            Specials = new[]
            {
                new RecordType { Id = "cover_rainy", Name = "Rainy Window", Frequency = 30f, Style = MusicStyle.LoFi, Label = new Color(0.36f, 0.58f, 0.60f) },
                new RecordType { Id = "cover_praia", Name = "Praia", Frequency = 24f, Style = MusicStyle.Bossa, Label = new Color(0.30f, 0.70f, 0.68f) },
                new RecordType { Id = "cover_nightdrive", Name = "Night Drive", Frequency = 18f, Style = MusicStyle.Synthwave, Label = new Color(0.85f, 0.30f, 0.62f) },
                new RecordType { Id = "cover_bluesmoke", Name = "Blue Smoke", Frequency = 14f, Style = MusicStyle.Jazz, Label = new Color(0.20f, 0.26f, 0.45f) },
                new RecordType { Id = "cover_mirrorball", Name = "Mirror Ball", Frequency = 9f, Style = MusicStyle.Disco, Label = new Color(0.78f, 0.45f, 0.85f) },
                new RecordType { Id = "cover_goldengroove", Name = "Golden Groove", Frequency = 5f, Style = MusicStyle.Funk, Label = new Color(0.90f, 0.70f, 0.20f) },
            },
        };

        public static readonly RecordShop[] Shops = { Shop };

        public static readonly RecordType[] AllSpecials = Flatten();

        static RecordType[] Flatten()
        {
            var list = new List<RecordType>();
            foreach (var s in Shops) list.AddRange(s.Specials);
            return list.ToArray();
        }

        public static RecordShop GetShop(string id)
        {
            foreach (var s in Shops) if (s.Id == id) return s;
            return Shop;
        }

        public static RecordType GetSpecial(string id)
        {
            foreach (var r in AllSpecials) if (r.Id == id) return r;
            return null;
        }

        public static int SpecialIndex(string id)
        {
            for (int i = 0; i < AllSpecials.Length; i++) if (AllSpecials[i].Id == id) return i;
            return -1;
        }

        public struct Record
        {
            public RecordType Type; // nadir değilse null
            public bool IsSpecial;
            public MusicStyle Style;
            public Color Label;
            public string Name;
        }

        // Ayın r. plağı (1'den) ay + mekân + sıradan belirlenir (kapının Identify'ı gibi): geri sarılıp yeniden
        // çalınan aynı plaktır. Sıradan plakların tarzı çoğunlukla lo-fi, arada bossa/caz.
        public static Record Identify(string month, RecordShop shop, long index, string focus = null)
        {
            ulong seed = Seed(month, shop);
            var r = new Record();
            if (index >= shop.SpecialMinIndex && VisitorCatalog.Hash01(seed, index, 1) < shop.SpecialChance)
            {
                float total = 0f;
                foreach (var s in shop.Specials) total += EnvironmentContent.Weight(s.Id, s.Frequency, focus);
                double pick = VisitorCatalog.Hash01(seed, index, 2) * total;
                var chosen = shop.Specials[shop.Specials.Length - 1];
                foreach (var s in shop.Specials)
                {
                    pick -= EnvironmentContent.Weight(s.Id, s.Frequency, focus);
                    if (pick <= 0) { chosen = s; break; }
                }
                r.Type = chosen;
                r.IsSpecial = true;
                r.Style = chosen.Style;
                r.Label = chosen.Label;
                r.Name = chosen.Name;
                return r;
            }
            double h = VisitorCatalog.Hash01(seed, index, 3);
            r.Style = h < 0.6 ? MusicStyle.LoFi : h < 0.8 ? MusicStyle.Bossa : MusicStyle.Jazz;
            r.Label = PlainLabels[(int)(VisitorCatalog.Hash01(seed, index, 4) * PlainLabels.Length) % PlainLabels.Length];
            r.Name = "Record #" + index;
            return r;
        }

        // Parçanın müzik tohumu (ayın kaçıncı parçası): akor dizisi, ton ve melodi bundan.
        public static ulong TrackSeed(string month, RecordShop shop, long trackIndex)
        {
            ulong seed = Seed(month, shop);
            return (ulong)(VisitorCatalog.Hash01(seed, trackIndex, 9) * ulong.MaxValue) | 1UL;
        }

        static ulong Seed(string month, RecordShop shop) =>
            VisitorCatalog.Fnv(month) ^ (VisitorCatalog.Fnv("vinyl_" + shop.Id) * 31UL);
    }

    [Serializable]
    public class CrateRecord
    {
        public string id = "";
        public int count;
        public string firstSeen = ""; // yyyy-MM-dd
    }

    // Plak tablası ilerlemesi (yerel PlayerPrefs JSON, SaveService.LoadVinyl/SaveVinyl).
    [Serializable]
    public class VinylProgress
    {
        // Aylık sayaç (UTC yyyy-MM): ay değişince konum ve ay rekoru sıfırlanır, sandık kalıcı.
        public string month = "";
        public double position;  // bu ay çalınan puan (≥0); biten parça = konum / aralık
        public long monthBest;   // bu ay biten en yüksek parça sırası: yalnız onu aşan parça sayılır

        public long totalTracks;
        public long totalRecords;
        public List<CrateRecord> log = new List<CrateRecord>();
        public string lastSpecialId = "";
        public string shop = "";

        public CrateRecord Find(string id)
        {
            foreach (var r in log) if (r.id == id) return r;
            return null;
        }
    }
}
