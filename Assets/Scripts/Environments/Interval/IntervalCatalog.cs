using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Eşit aralıklı ortamların ortak veri modeli (2026-10-14, Docs/ortamlar.md "Dünyayı döndüren el" ve
    // sonrası). Rüzgârgülü/Plak ile aynı kural: gizli ortak puandan her N puanda bir birim; ayın i. öğesinin
    // kimliği ay + mekân + sıradan (VisitorCatalog'un hash'i); sıradan öğeler sayılır ama deftere girmez,
    // nadirler ilk gelişte damgalanır; ayın ilk SpecialMinIndex öğesi hep sıradan.
    public sealed class ItemType
    {
        public string Id;
        public string Name;
        public float Frequency;  // nadirler arasında göreli sıklık
        public float Size = 1f;  // dünya birimi (ortama göre yükseklik ya da çap)
        public float Value;      // ortama özel taban değer (mesafe m, kabuk cm...)
        public Color Color = Color.white;   // ortama özel (kabuk, et...)
        public Color Color2 = Color.white;  // ortama özel (iç / meyve eti...)
        public float[] Shape;    // ortama özel biçim (çömlek profili)
    }

    // Mekân (Kapı'daki DoorVenue, Rüzgârgülü'ndeki WindSite karşılığı). Prototiplerde her ortamda tek mekân.
    public sealed class ItemSite
    {
        public string Id;
        public string Name;
        public ItemType[] Commons;
        public ItemType[] Specials;
        public float SpecialChance;
        public int SpecialMinIndex = 12;
        // Mekânın sahne renkleri (sırası ortamın sahnesine özel; IntervalSceneView.Look). Boşsa sahnenin varsayılanı.
        public Color[] Palette = Array.Empty<Color>();

        public bool Contains(string id)
        {
            foreach (var t in Specials) if (t.Id == id) return true;
            return false;
        }

        public ItemType GetSpecial(string id)
        {
            foreach (var t in Specials) if (t.Id == id) return t;
            return null;
        }

        public int SpecialIndex(string id)
        {
            for (int i = 0; i < Specials.Length; i++) if (Specials[i].Id == id) return i;
            return -1;
        }

        public int CommonIndex(ItemType type) => Array.IndexOf(Commons, type);
    }

    public struct EnvItem
    {
        public ItemType Type;
        public bool IsSpecial;
        public float Jitter; // -1..1 (konum/boyut oynaması)
        public float Roll;   // 0..1 (ortama özel değer: mesafe, kabuk uzunluğu...)
    }

    public static class IntervalCatalog
    {
        // Ayın index. öğesi (1'den başlar). Geri sarınca kaybolup yeniden gelen aynı öğedir; tabelada
        // "geliyor" denen nadir öğe gelince de odur. Hash VisitorCatalog'unki (Rüzgârgülü ile aynı kalıp).
        public static EnvItem Identify(EnvironmentId env, string month, string envKey, ItemSite site, long index, string focus = null, RareGuarantee guarantee = default)
        {
            ulong seed = VisitorCatalog.Fnv(month) ^ (VisitorCatalog.Fnv(envKey + "_" + site.Id) * 31UL);
            var item = new EnvItem
            {
                Jitter = (float)(VisitorCatalog.Hash01(seed, index, 4) * 2.0 - 1.0),
                Roll = (float)VisitorCatalog.Hash01(seed, index, 6),
            };
            if (index == guarantee.Index || (index >= site.SpecialMinIndex && VisitorCatalog.Hash01(seed, index, 1) < site.SpecialChance * EnvironmentContent.ChanceBoost(env)))
            {
                var specials = site.Specials;
                float total = 0f;
                for (int i = 0; i < specials.Length; i++) total += EnvironmentContent.Weight(env, i, specials[i].Id, specials[i].Frequency, focus);
                double pick = VisitorCatalog.Hash01(seed, index, 2) * total;
                var chosen = specials[specials.Length - 1];
                for (int i = 0; i < specials.Length; i++)
                {
                    pick -= EnvironmentContent.Weight(env, i, specials[i].Id, specials[i].Frequency, focus);
                    if (pick <= 0) { chosen = specials[i]; break; }
                }
                chosen = guarantee.Pick(index, chosen, site.Specials, s => s.Id);
                item.Type = chosen;
                item.IsSpecial = true;
                return item;
            }
            // Art arda aynı sıradan öğe gelmesin.
            int n = site.Commons.Length;
            int prev = CommonIndex(seed, index - 1, n);
            int c = CommonIndex(seed, index, n);
            if (c == prev && n > 1) c = (c + 1 + (int)(VisitorCatalog.Hash01(seed, index, 5) * (n - 1))) % n;
            item.Type = site.Commons[c];
            return item;
        }

        static int CommonIndex(ulong seed, long index, int n) =>
            (int)(VisitorCatalog.Hash01(seed, index, 3) * n) % n;
    }

    // Defter kaydı: ilk gelişte damgalanır; best = ortama özel en iyi değer (en uzun atış, en uzun kabuk).
    [Serializable]
    public class ItemRecord
    {
        public string id = "";
        public int count;
        public string firstSeen = ""; // yyyy-MM-dd
        public float best;
    }

    // İlerleme (yerel PlayerPrefs JSON, SaveService.LoadJson/SaveJson).
    [Serializable]
    public class IntervalProgress
    {
        // Aylık sayaç (UTC yyyy-MM): ay değişince konum ve ay rekoru sıfırlanır, defter kalıcı.
        public string month = "";
        public double position;  // bu ayın puanı (≥0); biten öğe = konum / aralık
        public long monthBest;   // bu ay biten en yüksek sıra no: yalnız onu aşan öğe sayılır
        public string guaranteeMonth = ""; // ilk nadir garantisinin verildiği ay (EnvironmentContent.GuaranteeIndex)

        public long total;       // ömür boyu biten öğe
        public List<ItemRecord> log = new List<ItemRecord>();
        public string lastSpecialId = "";
        public string site = "";
        public string skin = "";  // seçili skin takımı (boş = Klasik)
        public float best;       // ortama özel ömür boyu rekor (en uzun atış, en uzun kabuk); yoksa 0
        public string bestId = "";

        public ItemRecord Find(string id)
        {
            foreach (var r in log) if (r.id == id) return r;
            return null;
        }
    }
}
