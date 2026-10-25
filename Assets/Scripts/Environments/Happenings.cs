using System;
using TMPro;
using UnityEngine;

namespace ScrollRoad
{
    // Kısa etkinlikler (2026-10-25, "dönüş anları"): seçili ortamda, kullanıcı aktifken arada bir 5 dakikalık bir
    // olay başlar (Olta'da "Feeding frenzy", Kapı'da tur otobüsü...). Sürerken o ortamda nadir çıkma olasılığı
    // Boost katı (Olta'da iri türler; EnvironmentContent.ChanceBoost). Puanı değiştirmez. Başlangıçta duyuru
    // (şeritte kısa satır) ve kartta vitrinin üstünde kalan süreyle küçük kehribar rozet. Olaylar arası ortalama
    // aktif süre hafta içi 40, hafta sonu 25 dakika (Poisson); iki olay arası en az 15 dakika. Yol'da yok (nadir
    // yok). Kayıt yok: oyun kapanınca süren olay biter.
    public class Happenings : MonoBehaviour
    {
        [SerializeField] NoticeController notice;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject badgeRoot;
        [SerializeField] TMP_Text badgeText;
        [SerializeField] float durationSeconds = 300f;
        [SerializeField] float meanMinutes = 40f;
        [SerializeField] float weekendMeanMinutes = 25f;
        [SerializeField] float minGapMinutes = 15f;
        [SerializeField] float boost = 2.5f;

        struct Kind
        {
            public string Name;   // "Feeding frenzy"
            public string Story;  // "The big fish are on the move."
            public string Rares;  // "Rare species bite"
        }

        static Kind KindOf(EnvironmentId env) => env switch
        {
            EnvironmentId.Fishing => new Kind { Name = "Feeding frenzy", Story = "The big fish are on the move.", Rares = "Big species bite" },
            EnvironmentId.Door => new Kind { Name = "Tour bus", Story = "A tour bus just pulled up outside.", Rares = "Special guests walk in" },
            EnvironmentId.Wind => new Kind { Name = "Gale", Story = "Gale-force wind over the valley.", Rares = "Landmarks join the grid" },
            EnvironmentId.Vinyl => new Kind { Name = "Crate delivery", Story = "A fresh crate of records arrived.", Rares = "Rare records turn up" },
            EnvironmentId.Globe => new Kind { Name = "Golden age", Story = "History is in a generous mood.", Rares = "Wonders rise" },
            EnvironmentId.Boomerang => new Kind { Name = "Perfect wind", Story = "The wind is just right for long throws.", Rares = "Rare targets get hit" },
            EnvironmentId.Pottery => new Kind { Name = "Kiln day", Story = "The big kiln is fired up today.", Rares = "Masterpieces come off the wheel" },
            EnvironmentId.Peeler => new Kind { Name = "Market day", Story = "The good crates came in this morning.", Rares = "Rare fruit turns up" },
            EnvironmentId.Kebab => new Kind { Name = "Lunch rush", Story = "Hungry crowd at the counter.", Rares = "House specials go out" },
            _ => new Kind { Name = "Lucky streak", Story = "Something is in the air.", Rares = "Rare finds turn up" },
        };

        static EnvironmentId activeEnv;
        static float activeLeft;   // saniye; ≤ 0 = olay yok
        static float activeBoost = 1f;

        float quietSeconds; // son olaydan beri aktif geçen süre
        float rollTimer;
        System.Random rng;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            activeLeft = 0f;
            activeBoost = 1f;
        }

        public static bool IsActive(EnvironmentId env) => activeLeft > 0f && activeEnv == env;
        public static float BoostFor(EnvironmentId env) => IsActive(env) ? activeBoost : 1f;
        public static float SecondsLeft => Mathf.Max(0f, activeLeft);
        public static string NameOf(EnvironmentId env) => KindOf(env).Name;

        void Awake()
        {
            rng = new System.Random(Environment.TickCount);
            if (badgeRoot != null) badgeRoot.SetActive(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (activeLeft > 0f)
            {
                activeLeft -= dt;
                if (activeLeft <= 0f) activeBoost = 1f;
            }
            else
            {
                TryStart(dt);
            }
            UpdateBadge();
        }

        void TryStart(float dt)
        {
            var engine = ScoreEngine.Instance;
            var env = EnvironmentManager.Current.Id;
            if (engine == null || !engine.IsUserActive || env == EnvironmentId.Road) return;
            quietSeconds += dt;
            if (quietSeconds < minGapMinutes * 60f) return;

            // Saniyede bir zar: ortalama bekleme meanMinutes (aradaki en az süre düşülerek) olsun.
            rollTimer += dt;
            if (rollTimer < 1f) return;
            rollTimer -= 1f;
            var day = DateTime.Now.DayOfWeek;
            bool weekend = day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
            float mean = Mathf.Max(1f, (weekend ? weekendMeanMinutes : meanMinutes) - minGapMinutes) * 60f;
            if (rng.NextDouble() >= 1.0 / mean) return;
            Begin(env, boost, durationSeconds);
        }

        void Begin(EnvironmentId env, float multiplier, float seconds)
        {
            activeEnv = env;
            activeLeft = seconds;
            activeBoost = multiplier;
            quietSeconds = 0f;
            var kind = KindOf(env);
            int minutes = Mathf.RoundToInt(seconds / 60f);
            if (notice != null)
                notice.Announce(kind.Name, $"{kind.Story} {kind.Rares} {multiplier:0.#}× as often for {minutes} minutes.",
                    $"{kind.Name}! {minutes} min");
        }

        // Kartta, vitrinin üstünde: "FEEDING FRENZY · 4:12" (yalnız olayın ortamı seçiliyken).
        void UpdateBadge()
        {
            if (badgeRoot == null) return;
            bool card = companionWindow == null || !companionWindow.IsCompanionMode;
            bool show = card && IsActive(EnvironmentManager.Current.Id);
            if (badgeRoot.activeSelf != show) badgeRoot.SetActive(show);
            if (!show) return;
            int left = Mathf.CeilToInt(activeLeft);
            badgeText.SetText($"{KindOf(activeEnv).Name.ToUpperInvariant()}  ·  {left / 60}:{left % 60:00}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: seçili ortamda hemen bir olay başlat.
        public void DebugStart(float seconds = 300f) => Begin(EnvironmentManager.Current.Id, boost, seconds);
#endif
    }
}
