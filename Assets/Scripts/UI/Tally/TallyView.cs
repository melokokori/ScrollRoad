using UnityEngine;

namespace ScrollRoad
{
    // Ortam nesnesi (2026-10-25 kullanıcı kararı: kadran yalnız Yol'da kalır). Diğer 9 ortamın kartında sol üstte,
    // eski kadranın 128x128'lik yuvasında o ortama özgü bir nesne durur: kova, el sayacı, elektrik sayacı, parça
    // listesi, yaprak takvim, çetele, kil çuvalı, yazar kasa, sıra numaratörü. Her biri iki iş görür: bu ayın
    // birimini gösterir ve scroll'a ya da yeni birime tepki verir (ibrenin "sayılıyorum" geri bildirimi).
    // Görseller Tools/make_tallies.py → Resources/Tallies/<takım>_<parça>; takım seçili skinin (SkinInfo.Tally),
    // skin değişince kendiliğinden yenilenir. Katmanlar ve yazılar sahne kurucusunda (CreateTally) kurulur;
    // koordinatlar 200 birimlik yuva uzayında (TallyLayout).
    public abstract class TallyView : MonoBehaviour
    {
        [SerializeField] protected EnvironmentId environment;

        string shownSet;
        double lastUnits = double.NaN;
        AudioSource unitAudio;
        AudioClip unitClip;

        // Ortamın ortak temposu (ScoreEngine.Pace; seyirde ~60, rölantide ~10, boost'ta 220'ye kadar, geri sararken eksi).
        protected float Pace { get; private set; }
        protected float Pace01 => Mathf.Clamp01(Mathf.Abs(Pace) / 220f);

        // Yeni birimde çalan kısa ses (Resources/EnvSounds); null = yok (ortamın kendi sesi yetiyor).
        protected virtual string UnitSound => null;
        protected virtual float UnitVolume => 0.18f;

        protected abstract void ApplySet(string set, SkinInfo skin);
        protected abstract void Tick(double units, float dt);
        protected virtual void OnUnit(long whole) { }

        protected static Sprite Load(string set, string part)
        {
            var sprite = Resources.Load<Sprite>($"Tallies/{set}_{part}");
            if (sprite == null) Debug.LogWarning($"[TallyView] Görsel yok: Tallies/{set}_{part}");
            return sprite;
        }

        protected static Sprite Shared(string name) => Resources.Load<Sprite>($"Tallies/{name}");

        // Yuvanın bir biriminin piksel karşılığı (animasyon kaydırmaları birimle yazılır).
        protected float UnitPx => ((RectTransform)transform).rect.height / 200f;

        void Awake()
        {
            if (UnitSound == null) return;
            unitClip = Resources.Load<AudioClip>("EnvSounds/" + UnitSound);
            unitAudio = gameObject.AddComponent<AudioSource>();
            unitAudio.playOnAwake = false;
        }

        // Kart HUD'u yalnız seçili ortamda açık; geri gelince aradaki fark birim olayı sayılmasın.
        void OnEnable() => lastUnits = double.NaN;

        void Update()
        {
            var c = EnvironmentContent.Get(environment);
            if (c == null) return;
            var skin = c.Skins[Mathf.Max(0, EnvironmentContent.SkinIndex(c, c.SkinId))];
            if (skin.Tally != shownSet)
            {
                shownSet = skin.Tally;
                if (!string.IsNullOrEmpty(shownSet)) ApplySet(shownSet, skin);
            }

            var engine = ScoreEngine.Instance;
            Pace = engine != null && EnvironmentManager.Current.Id == environment ? engine.Pace : 0f;

            double units = System.Math.Max(0.0, c.MonthUnits);
            if (!double.IsNaN(lastUnits))
            {
                long before = (long)System.Math.Floor(lastUnits), now = (long)System.Math.Floor(units);
                // Ay sıfırlanması ya da kayıt yüklemesi gibi büyük sıçramalar birim olayı değil.
                if (now > before && now - before <= 3)
                {
                    OnUnit(now);
                    if (unitClip != null) unitAudio.PlayOneShot(unitClip, UnitVolume);
                }
            }
            lastUnits = units;
            Tick(units, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
        }
    }

    // 200 birimlik yuva uzayı (Tools/make_tallies.py ile aynı sayılar): y aşağı doğru.
    public static class TallyLayout
    {
        public static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0 / 200f, 1f - y1 / 200f);
            rt.anchorMax = new Vector2(x1 / 200f, 1f - y0 / 200f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Yuvayı kaplayan bir katmanın dönme/ölçek merkezi (yuva uzayında bir nokta).
        public static void Pivot(RectTransform rt, float x, float y) => rt.pivot = new Vector2(x / 200f, 1f - y / 200f);
    }
}
