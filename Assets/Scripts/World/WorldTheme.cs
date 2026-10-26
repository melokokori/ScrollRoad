using System;
using UnityEngine;

namespace ScrollRoad
{
    // Dünya teması (garajdan seçilir): RoadSceneView'in ürettiği renkleri son adımda dönüştürür.
    // Biyom döngüsü, şehir gün batımı ve gizli bölge morunun mantığı değişmez — yalnız tonlar.
    // Seçim kozmetik ve cihaza özel (yerel PlayerPrefs).
    public static class WorldTheme
    {
        public enum Kind { Classic = 0, Night = 1, Sepia = 2 }

        public static readonly string[] DisplayNames = { L.N("Classic"), L.N("Night"), L.N("Sepia") };

        static readonly Color NightSky = new Color(0.07f, 0.09f, 0.18f);
        static readonly Color NightCity = new Color(0.36f, 0.34f, 0.48f); // koyu gökte siluet okunur kalsın (ışıklı şehir)

        static Kind? current;

        public static Kind Current
        {
            get
            {
                if (current == null)
                {
                    int saved = SaveService.LoadWorldTheme();
                    current = Enum.IsDefined(typeof(Kind), saved) ? (Kind)saved : Kind.Classic;
                }
                return current.Value;
            }
        }

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => current = null;

        public static void Set(Kind kind)
        {
            current = kind;
            SaveService.SaveWorldTheme((int)kind);
            Changed?.Invoke();
        }

        // Gökyüzü (tepeler ve zemin bundan türetilir).
        public static Color Sky(Color c)
        {
            switch (Current)
            {
                case Kind.Night:
                    // Biyom tonu hafifçe sızsın: geceler de birbirinden ayırt edilsin.
                    return Color.Lerp(NightSky, c * 0.35f, 0.3f);
                case Kind.Sepia:
                    return ToSepia(c);
                default:
                    return c;
            }
        }

        // Bölgenin tepe/zemin rengi. Gecede gökyüzünden biraz koyu, bölge tonu sızarak.
        public static Color Land(Color c)
        {
            switch (Current)
            {
                case Kind.Night:
                    var night = Color.Lerp(NightSky, c * 0.35f, 0.35f) * 0.8f;
                    night.a = 1f;
                    return night;
                case Kind.Sepia:
                    return ToSepia(c);
                default:
                    return c;
            }
        }

        // Şehir silueti ve yol gibi gökyüzü dışındaki yüzeyler.
        public static Color Surface(Color c)
        {
            return Current == Kind.Sepia ? ToSepia(c) : c;
        }

        public static Color CitySilhouette(Color c)
        {
            switch (Current)
            {
                case Kind.Night: return NightCity;
                case Kind.Sepia: return ToSepia(c);
                default: return c;
            }
        }

        static Color ToSepia(Color c)
        {
            float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            return new Color(Mathf.Clamp01(l * 1.07f), Mathf.Clamp01(l * 0.95f), Mathf.Clamp01(l * 0.78f), c.a);
        }
    }
}
