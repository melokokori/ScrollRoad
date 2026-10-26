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

    // loc:catalog — öğe/mekân adları çeviri anahtarı olarak toplanır (Tools/loc/extract.py).
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
        // Mekânın masa renkleri (Ortam Standardı v2): [0] masa, [1] masa damarı.
        public Color[] Palette = System.Array.Empty<Color>();

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

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        static RecordType Rec(string id, string name, float freq, MusicStyle style, string label) =>
            new RecordType { Id = id, Name = name, Frequency = freq, Style = style, Label = Hex(label) };

        // Ortam Standardı v2 (2026-10-25): dört yeni mekân; Plak'ta geçen seyir saatiyle açılır (UnlockLadder).
        static RecordShop Venue(string id, string name, RecordType[] specials, params string[] palette) => new RecordShop
        {
            Id = id, Name = name, SpecialChance = 0.30f, SpecialMinIndex = 2, Specials = specials,
            Palette = System.Array.ConvertAll(palette, Hex),
        };

        public static readonly RecordShop JazzClub = Venue("jazz_club", "The Jazz Club", new[]
        {
            Rec("cover_midnight_sax", "Midnight Sax", 30f, MusicStyle.Jazz, "#2E3A6E"), Rec("cover_smoke_rings", "Smoke Rings", 24f, MusicStyle.Jazz, "#6E4A8A"),
            Rec("cover_rain_city", "Rain City", 18f, MusicStyle.LoFi, "#3E6E7E"), Rec("cover_velvet", "Velvet Room", 14f, MusicStyle.Bossa, "#8A2E3E"),
            Rec("cover_blue_note", "Blue Hour", 9f, MusicStyle.Jazz, "#1E5A9E"), Rec("cover_gold_trumpet", "Gold Trumpet", 5f, MusicStyle.Funk, "#C9A24A"),
        }, "#3A2A22", "#2E2018");

        public static readonly RecordShop Disco = Venue("disco", "The Disco", new[]
        {
            Rec("cover_glitter", "Glitter Night", 30f, MusicStyle.Disco, "#C94A9E"), Rec("cover_roller", "Roller Rink", 24f, MusicStyle.Funk, "#E8644A"),
            Rec("cover_neon_heart", "Neon Heart", 18f, MusicStyle.Synthwave, "#E23B8A"), Rec("cover_dancefloor", "Light-Up Floor", 14f, MusicStyle.Disco, "#4AB8C9"),
            Rec("cover_platform", "Platform Shoes", 9f, MusicStyle.Funk, "#F2B33D"), Rec("cover_starlight", "Starlight Express", 5f, MusicStyle.Disco, "#B8A0F0"),
        }, "#3A2450", "#2E1C40");

        public static readonly RecordShop BeachBar = Venue("beach_bar", "The Beach Bar", new[]
        {
            Rec("cover_sunset_bay", "Sunset Bay", 30f, MusicStyle.Bossa, "#F28A4A"), Rec("cover_hammock", "Hammock", 24f, MusicStyle.LoFi, "#7EB8A0"),
            Rec("cover_surf_wave", "Surf Wave", 18f, MusicStyle.Synthwave, "#3EA8C9"), Rec("cover_coconut", "Coconut Grove", 14f, MusicStyle.Bossa, "#A8C96A"),
            Rec("cover_tiki", "Tiki Torch", 9f, MusicStyle.Funk, "#C9643E"), Rec("cover_pearl_shore", "Pearl Shore", 5f, MusicStyle.Bossa, "#F2E6D0"),
        }, "#C9A87A", "#B8956A");

        public static readonly RecordShop ConcertHall = Venue("concert_hall", "The Concert Hall", new[]
        {
            Rec("cover_grand_piano", "Grand Piano", 30f, MusicStyle.Jazz, "#1E1A17"), Rec("cover_strings", "Strings at Dawn", 24f, MusicStyle.LoFi, "#C99A6A"),
            Rec("cover_opera", "Night at the Opera", 18f, MusicStyle.Bossa, "#8A1E2E"), Rec("cover_chandelier", "Chandelier", 14f, MusicStyle.Disco, "#E6C46E"),
            Rec("cover_orchestra", "Full Orchestra", 9f, MusicStyle.Funk, "#2E4A3E"), Rec("cover_golden_baton", "The Golden Baton", 5f, MusicStyle.Jazz, "#D9B455"),
        }, "#5E1E22", "#4E181C");

        public static readonly RecordShop[] Shops = { Shop, JazzClub, Disco, BeachBar, ConcertHall };

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
            public long Number; // nadir olmayan plağın sırası ("Record #12")

            public string DisplayName => IsSpecial ? L.T(Name) : L.F("Record #{0}", Number);
        }

        // Ayın r. plağı (1'den) ay + mekân + sıradan belirlenir (kapının Identify'ı gibi): geri sarılıp yeniden
        // çalınan aynı plaktır. Sıradan plakların tarzı çoğunlukla lo-fi, arada bossa/caz.
        public static Record Identify(string month, RecordShop shop, long index, string focus = null, RareGuarantee guarantee = default)
        {
            ulong seed = Seed(month, shop);
            var r = new Record();
            if (index == guarantee.Index || (index >= shop.SpecialMinIndex && VisitorCatalog.Hash01(seed, index, 1) < System.Math.Min(0.9f, shop.SpecialChance * EnvironmentContent.ChanceBoost(EnvironmentId.Vinyl))))
            {
                var specials = shop.Specials;
                float total = 0f;
                for (int i = 0; i < specials.Length; i++) total += EnvironmentContent.Weight(EnvironmentId.Vinyl, i, specials[i].Id, specials[i].Frequency, focus);
                double pick = VisitorCatalog.Hash01(seed, index, 2) * total;
                var chosen = specials[specials.Length - 1];
                for (int i = 0; i < specials.Length; i++)
                {
                    pick -= EnvironmentContent.Weight(EnvironmentId.Vinyl, i, specials[i].Id, specials[i].Frequency, focus);
                    if (pick <= 0) { chosen = specials[i]; break; }
                }
                chosen = guarantee.Pick(index, chosen, shop.Specials, s => s.Id);
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
            r.Number = index;
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
        public string guaranteeMonth = ""; // ilk nadir garantisinin verildiği ay (EnvironmentContent.GuaranteeIndex)
        public string skin = "";           // seçili skin takımı (boş = Klasik)

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
