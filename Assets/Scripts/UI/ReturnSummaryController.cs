using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Dönüş anı (Ortam Standardı v2 · Dayanak 1, 2026-10-25): kart en az awayMinutes görülmeden kaldıktan sonra
    // yeniden görülünce "While you were away" özeti — geçen süre, kazanılan birim, gelen nadirler (yeni olanlar
    // ayrı), merdivende sıradaki açılma. "Görülüyor" = kart modu (şerit değil), tepside gizli değil ve pencere
    // odakta (simge durumunda / arkada kalan kart görülmüyor sayılır). Kart görülürken her saniye anlık durum
    // alınır; özet o anla bugünü karşılaştırır. Oyun kapalıyken ilerleme olmadığından açılışta özet yok.
    // Özet açıkken ortam değişirse ya da kart şerit moduna geçerse kapanır; tıklayınca kapanır.
    public class ReturnSummaryController : MonoBehaviour
    {
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] CarCatalog carCatalog;
        [SerializeField] PlacesController places; // nadir görselleri
        [SerializeField] GameObject panel;
        [SerializeField] Button dismissButton; // panelin tamamı
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Image[] rareImages;
        [SerializeField] float awayMinutes = 15f;

        sealed class Snapshot
        {
            public EnvironmentId Env;
            public double Points; // ortamın ömür boyu puanı (Yol'da gizli puanın ömür boyu toplamı)
            public readonly Dictionary<string, int> Found = new Dictionary<string, int>();
            public double SeenAt; // Time.realtimeSinceStartupAsDouble
        }

        Snapshot last;
        EnvironmentId shownEnv;
        float snapshotTimer;
        bool wasSeen;

        public GameObject Panel => panel;

        void Awake()
        {
            dismissButton.onClick.AddListener(() => panel.SetActive(false));
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        bool IsSeen =>
            Application.isFocused && !BackgroundLifecycle.IsHiddenToTray
            && (companionWindow == null || !companionWindow.IsCompanionMode);

        void Update()
        {
            bool seen = IsSeen;
            if (panel.activeSelf && ((companionWindow != null && companionWindow.IsCompanionMode) || shownEnv != EnvironmentManager.Current.Id))
                panel.SetActive(false);

            if (seen && !wasSeen && last != null && Time.realtimeSinceStartupAsDouble - last.SeenAt >= awayMinutes * 60.0)
                Show(last, Take());
            wasSeen = seen;

            if (!seen) return;
            snapshotTimer -= Time.unscaledDeltaTime;
            if (snapshotTimer > 0f && last != null && last.Env == EnvironmentManager.Current.Id) return;
            snapshotTimer = 1f;
            last = Take();
        }

        static Snapshot Take()
        {
            var snap = new Snapshot { Env = EnvironmentManager.Current.Id, SeenAt = Time.realtimeSinceStartupAsDouble };
            var c = EnvironmentContent.Current;
            if (c == null)
            {
                snap.Points = ScoreEngine.Instance != null ? ScoreEngine.Instance.LifetimePoints : 0.0;
                return snap;
            }
            snap.Points = c.LifetimePoints;
            foreach (var venue in c.Venues)
                foreach (var rare in c.RaresAt(venue.Id))
                    snap.Found[rare.Id] = c.TimesFound(rare.Id);
            return snap;
        }

        void Show(Snapshot before, Snapshot now)
        {
            if (before.Env != now.Env) return;
            double gained = now.Points - before.Points;
            if (gained <= 0) return;

            var env = EnvironmentManager.Current;
            titleText.text = "While you were away  ·  " + UnlockLadder.Remaining((now.SeenAt - before.SeenAt) / 3600.0 * UnlockLadder.PointsPerHour);
            amountText.text = "+" + env.Format(gained);

            var body = new StringBuilder();
            var newOnes = new List<RareInfo>();
            var again = new List<string>();
            var c = EnvironmentContent.Current;
            if (c != null)
            {
                foreach (var venue in c.Venues)
                {
                    foreach (var rare in c.RaresAt(venue.Id))
                    {
                        before.Found.TryGetValue(rare.Id, out int was);
                        int delta = c.TimesFound(rare.Id) - was;
                        if (delta <= 0) continue;
                        if (was == 0) newOnes.Add(rare);
                        else again.Add(delta > 1 ? $"{rare.Name} ×{delta}" : rare.Name);
                    }
                }
                if (newOnes.Count > 0)
                    body.AppendLine($"<color=#F2B33D>New in your {env.CollectionName.ToLowerInvariant()}:</color>  {string.Join(", ", newOnes.ConvertAll(r => r.Name))}");
                if (again.Count > 0) body.AppendLine($"<alpha=#AA>Turned up again:  {string.Join(", ", again)}");
                if (newOnes.Count == 0 && again.Count == 0) body.AppendLine("<alpha=#AA>No rare finds this time. They're out there.");
                if (EnvironmentContent.NextUnlock(c, out string next, out double left))
                    body.AppendLine($"<alpha=#AA>Next unlock:  <color=#FFFFFF>{next}</color> in {UnlockLadder.Remaining(left)}");
            }
            else if (carCatalog != null)
            {
                double km = CarCatalog.CurrentProgress.lifetimeKm;
                CarCatalog.Model nextCar = null;
                foreach (var m in carCatalog.models)
                    if (m.unlock == CarCatalog.Unlock.LifetimeKm && m.unlockValue > km && (nextCar == null || m.unlockValue < nextCar.unlockValue))
                        nextCar = m;
                if (nextCar != null) body.AppendLine($"<alpha=#AA>Next car:  <color=#FFFFFF>{nextCar.displayName}</color> in {Fmt.Km(nextCar.unlockValue - km)}");
            }
            bodyText.text = body.ToString().TrimEnd();

            // Yeni nadirlerin görselleri sağ alta yaslı.
            int shown = Mathf.Min(newOnes.Count, rareImages.Length);
            for (int i = 0; i < rareImages.Length; i++)
            {
                var sprite = i < shown && places != null ? places.SpriteFor(now.Env, newOnes[i].Id) : null;
                rareImages[i].sprite = sprite;
                rareImages[i].gameObject.SetActive(sprite != null);
                var rect = rareImages[i].rectTransform;
                rect.anchoredPosition = new Vector2(-26f - (shown - 1 - i) * (rect.sizeDelta.x + 8f), rect.anchoredPosition.y);
            }
            shownEnv = now.Env;
            panel.SetActive(true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: son anlık durumu geçmişe it (kart o kadar süredir görülmüyormuş gibi) ve özeti hemen göster.
        public void DebugShowAfter(double minutes)
        {
            if (last == null) last = Take();
            last.SeenAt -= minutes * 60.0;
            Show(last, Take());
        }
#endif
    }
}
