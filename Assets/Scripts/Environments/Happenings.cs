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
            EnvironmentId.Fishing => new Kind { Name = L.N("Feeding frenzy"), Story = L.N("The big fish are on the move."), Rares = L.N("Big species bite") },
            EnvironmentId.Door => new Kind { Name = L.N("Tour bus"), Story = L.N("A tour bus just pulled up outside."), Rares = L.N("Special guests walk in") },
            EnvironmentId.Wind => new Kind { Name = L.N("Gale"), Story = L.N("Gale-force wind over the valley."), Rares = L.N("Landmarks join the grid") },
            EnvironmentId.Vinyl => new Kind { Name = L.N("Crate delivery"), Story = L.N("A fresh crate of records arrived."), Rares = L.N("Rare records turn up") },
            EnvironmentId.Globe => new Kind { Name = L.N("Golden age"), Story = L.N("History is in a generous mood."), Rares = L.N("Wonders rise") },
            EnvironmentId.Boomerang => new Kind { Name = L.N("Perfect wind"), Story = L.N("The wind is just right for long throws."), Rares = L.N("Rare targets get hit") },
            EnvironmentId.Pottery => new Kind { Name = L.N("Kiln day"), Story = L.N("The big kiln is fired up today."), Rares = L.N("Masterpieces come off the wheel") },
            EnvironmentId.Peeler => new Kind { Name = L.N("Market day"), Story = L.N("The good crates came in this morning."), Rares = L.N("Rare fruit turns up") },
            EnvironmentId.Kebab => new Kind { Name = L.N("Lunch rush"), Story = L.N("Hungry crowd at the counter."), Rares = L.N("House specials go out") },
            _ => new Kind { Name = L.N("Lucky streak"), Story = L.N("Something is in the air."), Rares = L.N("Rare finds turn up") },
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
        public static string NameOf(EnvironmentId env) => L.T(KindOf(env).Name);

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
                notice.Announce(L.T(kind.Name), L.T(kind.Story) + " " + L.F("{0} {1:0.#}× as often for {2} minutes.", L.T(kind.Rares), multiplier, minutes),
                    L.F("{0}! {1} min", L.T(kind.Name), minutes));
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
            badgeText.SetText($"{L.Upper(L.T(KindOf(activeEnv).Name))}  ·  {left / 60}:{left % 60:00}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: seçili ortamda hemen bir olay başlat.
        public void DebugStart(float seconds = 300f) => Begin(EnvironmentManager.Current.Id, boost, seconds);
#endif
    }
}
