using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Hız göstergesi (ibre) skinleri: kozmetik, oynayarak açılır, skora etkisiz. Görseller
    // Tools/make_gauges.py ile üretilir (Assets/Resources/Gauges/<id>_face|needle|lit|mini_*|plate).
    //  - Garajda seçilen 5 skin (Classic başlangıçta açık).
    //  - Harbor (tekne) ve Airspeed (hava aracı): araç türüne özel, knot ölçekli; Ayarlar'daki
    //    "kendi göstergesi" açıkken o araçlarda seçili skinin yerine otomatik gelir.
    // Görev çubuğunda üç görünüm (Ayarlar): sade plaka, kartın aynısı, taşan kadran.
    // Hepsi cihaza özel (yerel PlayerPrefs).
    public static class GaugeSkins
    {
        public enum StripStyle { Plate = 0, CardDial = 1, Peek = 2 }
        public static readonly string[] StripStyleNames = { "Simple plate", "Card dial", "Big dial" };

        public sealed class Skin
        {
            public string id;
            public string name;
            public CarCatalog.Unlock unlock;
            public double unlockValue;
            public string unlockLabel;
            public bool lcd;
            public bool knots;
            public bool vehicleOnly;   // garajda seçilmez, araç türüyle gelir
            public Color odoText;      // sayaç rakamları
            public Color odoLastText;  // ondalık hane (ters renk)
            public Color stripText;    // görev çubuğu plakasındaki hız yazısı
            public Color accent;       // garaj yuvasındaki renk noktası
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static readonly Skin[] All =
        {
            new Skin { id = "classic", name = "Classic", unlock = CarCatalog.Unlock.Free,
                odoText = Hex("#F4F1E6"), odoLastText = Hex("#1A1C22"), stripText = Hex("#F4F1E6"), accent = Hex("#F4F1E6") },
            new Skin { id = "tabela", name = "Highway", unlock = CarCatalog.Unlock.MonthBestKm, unlockValue = 150, unlockLabel = "reach Wheat Fields in one month",
                odoText = Hex("#FFFFFF"), odoLastText = Hex("#0A3A22"), stripText = Hex("#FFFFFF"), accent = Hex("#0E6B3E") },
            new Skin { id = "sport", name = "Sport", unlock = CarCatalog.Unlock.LifetimeKm, unlockValue = 5000,
                odoText = Hex("#E9E6DC"), odoLastText = Hex("#14161B"), stripText = Hex("#E9E6DC"), accent = Hex("#FF4B3E") },
            new Skin { id = "retro", name = "Retro", unlock = CarCatalog.Unlock.MonthBestKm, unlockValue = 11000, unlockLabel = "reach the Snowy Plateau in one month",
                odoText = Hex("#EFE4C8"), odoLastText = Hex("#EFE4C8"), stripText = Hex("#2A2016"), accent = Hex("#C9B98F") },
            new Skin { id = "lcd", name = "Digital", lcd = true, unlock = CarCatalog.Unlock.LifetimeKm, unlockValue = 20000,
                odoText = Hex("#A6F06B"), odoLastText = Hex("#0E1512"), stripText = Hex("#A6F06B"), accent = Hex("#A6F06B") },
            new Skin { id = "harbor", name = "Harbor", knots = true, vehicleOnly = true, unlock = CarCatalog.Unlock.Free,
                odoText = Hex("#E8F1F7"), odoLastText = Hex("#0D2238"), stripText = Hex("#E8F1F7"), accent = Hex("#14304F") },
            new Skin { id = "airspeed", name = "Airspeed", knots = true, vehicleOnly = true, unlock = CarCatalog.Unlock.Free,
                odoText = Hex("#F2F2F0"), odoLastText = Hex("#101113"), stripText = Hex("#F2F2F0"), accent = Hex("#101113") },
        };

        public static Skin ById(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return All[0];
        }

        // Garajda seçilebilenler (araç türüne özel olanlar hariç), sırasıyla.
        public static IEnumerable<Skin> Selectable()
        {
            foreach (var s in All) if (!s.vehicleOnly) yield return s;
        }

        public static event Action Changed;

        const string SkinKey = "ScrollRoad_GaugeSkin";
        const string StripKey = "ScrollRoad_GaugeStrip";
        const string VehicleKey = "ScrollRoad_GaugeVehicle";

        public static bool IsUnlocked(Skin s)
        {
            var p = CarCatalog.CurrentProgress;
            return s.unlock switch
            {
                CarCatalog.Unlock.LifetimeKm => p.lifetimeKm >= s.unlockValue,
                CarCatalog.Unlock.MonthBestKm => p.monthBestKm >= s.unlockValue,
                _ => true,
            };
        }

        public static string LockDescription(Skin s)
        {
            var p = CarCatalog.CurrentProgress;
            return s.unlock switch
            {
                CarCatalog.Unlock.LifetimeKm => $"{s.name} gauge unlocks at {Fmt.Km(s.unlockValue, 0)} total  ·  {Fmt.Km(s.unlockValue - p.lifetimeKm, 0)} to go",
                CarCatalog.Unlock.MonthBestKm => $"{s.name} gauge: {s.unlockLabel}  ·  your best: {Fmt.Km(p.monthBestKm, 0)} of {Fmt.Km(s.unlockValue, 0)}",
                _ => "",
            };
        }

        // Garajda seçili skin (kilitliyse Classic).
        public static Skin Selected
        {
            get
            {
                var s = ById(PlayerPrefs.GetString(SkinKey, "classic"));
                return s.vehicleOnly || !IsUnlocked(s) ? All[0] : s;
            }
        }

        public static void Select(Skin s)
        {
            if (s == null || s.vehicleOnly || !IsUnlocked(s)) return;
            PlayerPrefs.SetString(SkinKey, s.id);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static StripStyle Strip
        {
            get => (StripStyle)Mathf.Clamp(PlayerPrefs.GetInt(StripKey, 0), 0, 2);
            set { PlayerPrefs.SetInt(StripKey, (int)value); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static bool VehicleGauges
        {
            get => PlayerPrefs.GetInt(VehicleKey, 1) == 1;
            set { PlayerPrefs.SetInt(VehicleKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        // Ekrandaki gösterge: tekne/hava aracında (ayar açıksa) kendi göstergesi, değilse seçili skin.
        public static Skin For(CarCatalog.Model model)
        {
            if (VehicleGauges && model != null)
            {
                if (model.Lane == CarCatalog.Lane.Water) return ById("harbor");
                if (model.Lane == CarCatalog.Lane.Air) return ById("airspeed");
            }
            return Selected;
        }

        // Görseller (Resources önbelleğe alır).
        public static Sprite Face(Skin s, bool mini) => Load(s, mini ? "mini_face" : "face");
        public static Sprite Needle(Skin s, bool mini) => s.lcd ? null : Load(s, mini ? "mini_needle" : "needle");
        public static Sprite Lit(Skin s, bool mini) => s.lcd ? Load(s, mini ? "mini_lit" : "lit") : null;
        public static Sprite Plate(Skin s) => Load(s, "plate");

        static Sprite Load(Skin s, string part)
        {
            var sprite = Resources.Load<Sprite>($"Gauges/{s.id}_{part}");
            if (sprite == null) Debug.LogWarning($"[GaugeSkins] Görsel yok: Gauges/{s.id}_{part}");
            return sprite;
        }

        // Kadran geometrisi (Tools/make_gauges.py ile aynı): 200 birimlik uzay, -60..220 km/h,
        // sol-alttan (225°) sağ-alta 270° süpürme. İbre görseli yukarı bakar.
        public const float MinKmh = -60f, MaxKmh = 220f, Sweep = 270f, StartDeg = 225f;

        public static float AngleFor(float kmh) => StartDeg - (Mathf.Clamp(kmh, MinKmh, MaxKmh) - MinKmh) / (MaxKmh - MinKmh) * Sweep;

        // İbrenin Unity z dönüşü (saat yönünün tersi pozitif; görsel 90°'yi gösteriyor).
        public static float NeedleZ(float kmh) => AngleFor(kmh) - 90f;

        // Dijital segment yayı için radial fill: Image.FillOrigin = Bottom, saat yönünde. Yay alttaki
        // 90°'lik boşluğun sol kenarından (225°) başlar; boşluğun yarısı (45°) dolgunun başında boş geçer.
        public static float LcdFill(float kmh)
        {
            float swept = (Mathf.Clamp(kmh, MinKmh, MaxKmh) - MinKmh) / (MaxKmh - MinKmh) * Sweep; // 0..270
            return (45f + swept) / 360f;
        }
    }
}
