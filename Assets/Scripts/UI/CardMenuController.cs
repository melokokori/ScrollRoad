using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Kartın menüsü: sağ üstteki ≡ sağdan yeşil bir "çıkış tabelası" çekmecesi açar (başlıkta ay
    // rekoru, haftalık hedef ve seri; altında Passport, Leaderboard, Garage, Stats, Settings,
    // Taskbar mode; en altta Quit). Alt
    // paneller kartın tamamını kaplar ve × ile kapanır. Oyun DURMAZ: scroll her yerde sayıldığı
    // için duraklatma anlamsızdı. Esc önce açık paneli, sonra çekmeceyi kapatır.
    // Kartın kendi pencere düğmeleri de burada: – simge durumuna küçültür, × kapatır (ayara göre
    // tepsiye gizlenir).
    public class CardMenuController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] GoalTracker goals;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] Button menuButton;
        [SerializeField] Button minimizeButton;
        [SerializeField] Button closeWindowButton;
        [SerializeField] RectTransform drawer;
        [SerializeField] Button drawerBlocker; // çekmece açıkken kartın geri kalanı: tıklayınca kapanır
        [SerializeField] TMP_Text monthBestText;
        [SerializeField] TMP_Text headerSubText;
        [SerializeField] TMP_Text steamText;
        [SerializeField] TMP_Text collectionLabel; // "Passport" (Yol) / "Fish log" (Olta) satırı
        [SerializeField] TMP_Text garageLabel;     // "Garage" (Yol) / ortamın dolabı ("Tackle box"…) satırı
        [SerializeField] Button[] panelButtons; // Passport, Garage, Stats, Settings: panelleri kendi denetleyicileri açar
        [SerializeField] Button leaderboardButton;
        [SerializeField] Button taskbarButton;
        [SerializeField] Button quitButton;
        [SerializeField] GameObject leaderboardPanel;
        [SerializeField] TMP_Text leaderboardText;
        [SerializeField] Button thisMonthTab;
        [SerializeField] Button lastMonthTab;
        [SerializeField] Button leaderboardCloseButton;
        [SerializeField] GameObject[] subPanels; // Esc ile kapanacak paneller
        [SerializeField] float slideSeconds = 0.16f;

        static readonly UnityEngine.Color TabOn = new UnityEngine.Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly UnityEngine.Color TabOff = new UnityEngine.Color(0.165f, 0.18f, 0.215f);

        int fetchVersion;
        bool drawerOpen;
        float drawerT; // 0 kapalı, 1 açık

        public bool IsOpen
        {
            get
            {
                if (drawerOpen || leaderboardPanel.activeSelf) return true;
                foreach (var panel in subPanels) if (panel != null && panel.activeSelf) return true;
                return false;
            }
        }

        void Awake()
        {
            menuButton.onClick.AddListener(() => SetDrawer(!drawerOpen));
            drawerBlocker.onClick.AddListener(() => SetDrawer(false));
            minimizeButton.onClick.AddListener(() => companionWindow.MinimizeCard());
            closeWindowButton.onClick.AddListener(Application.Quit); // BackgroundLifecycle tepsiye gizleyebilir
            foreach (var b in panelButtons) b.onClick.AddListener(() => SetDrawer(false));
            leaderboardButton.onClick.AddListener(() => OpenLeaderboard(previousMonth: false));
            thisMonthTab.onClick.AddListener(() => OpenLeaderboard(previousMonth: false));
            lastMonthTab.onClick.AddListener(() => OpenLeaderboard(previousMonth: true));
            leaderboardCloseButton.onClick.AddListener(() => leaderboardPanel.SetActive(false));
            taskbarButton.onClick.AddListener(() =>
            {
                CloseAll();
                companionWindow.SetCompanionMode(true);
            });
            quitButton.onClick.AddListener(BackgroundLifecycle.QuitApp);
            companionWindow.OnModeChanged += HandleModeChanged;

            leaderboardPanel.SetActive(false);
            drawerBlocker.gameObject.SetActive(false);
            ApplyDrawerPosition();
        }

        void OnDestroy()
        {
            if (companionWindow != null) companionWindow.OnModeChanged -= HandleModeChanged;
        }

        void HandleModeChanged(bool companion)
        {
            if (companion) CloseAll();
        }

        void Update()
        {
            float target = drawerOpen ? 1f : 0f;
            if (!Mathf.Approximately(drawerT, target))
            {
                drawerT = Mathf.MoveTowards(drawerT, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, slideSeconds));
                ApplyDrawerPosition();
            }
            if (drawerOpen) RefreshHeader();

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (companionWindow.IsCompanionMode) return;

            if (leaderboardPanel.activeSelf) { leaderboardPanel.SetActive(false); return; }
            foreach (var panel in subPanels)
            {
                if (panel != null && panel.activeSelf) { panel.SetActive(false); return; }
            }
            SetDrawer(!drawerOpen);
        }

        public void CloseAll()
        {
            SetDrawer(false);
            leaderboardPanel.SetActive(false);
            foreach (var panel in subPanels) if (panel != null) panel.SetActive(false);
        }

        void SetDrawer(bool open)
        {
            drawerOpen = open;
            drawerBlocker.gameObject.SetActive(open);
            if (open) RefreshHeader();
        }

        // Çekmece sağ kenara sabit (pivot sağ): kapalıyken tam genişliği kadar dışarıda.
        void ApplyDrawerPosition()
        {
            float eased = 1f - (1f - drawerT) * (1f - drawerT);
            drawer.anchoredPosition = new Vector2(Mathf.Lerp(drawer.rect.width, 0f, eased), 0f);
            drawer.gameObject.SetActive(drawerT > 0f);
        }

        void RefreshHeader()
        {
            // Ay rekoru = gizli ortak puanın ay rekoru, aktif ortamın biriminde.
            var engine = ScoreEngine.Instance;
            if (engine != null) monthBestText.SetText(EnvironmentManager.Current.Format(engine.MonthBestPoints));
            if (collectionLabel != null) collectionLabel.text = EnvironmentManager.Current.CollectionName;
            var content = EnvironmentContent.Current;
            if (garageLabel != null) garageLabel.text = content != null ? content.SkinBoxName : "Garage";
            if (goals != null)
            {
                string weekKm = goals.AmountLine(weekly: true);
                string weekTime = goals.IsDone(GoalTracker.Goal.WeeklyTime)
                    ? $"{goals.WeeklyHoursGoal:0} h done" : $"{goals.WeekHours:0.0} / {goals.WeeklyHoursGoal:0} h";
                headerSubText.text = $"Week  {weekKm}  ·  {weekTime}\nStreak  {Fmt.Count(goals.DayStreak, "day")}  ·  {Fmt.Count(goals.WeekStreak, "week")}";
            }
            if (steamText != null) steamText.text = SteamManager.StatusText.ToUpperInvariant();
        }

        void OpenLeaderboard(bool previousMonth)
        {
            SetDrawer(false);
            foreach (var panel in subPanels) if (panel != null) panel.SetActive(false);
            leaderboardPanel.SetActive(true);
            thisMonthTab.GetComponent<UnityEngine.UI.Image>().color = previousMonth ? TabOff : TabOn;
            lastMonthTab.GetComponent<UnityEngine.UI.Image>().color = previousMonth ? TabOn : TabOff;
            if (!SteamManager.IsInitialized)
            {
                leaderboardText.text = "Steam is not connected.\nOpen Steam and sign in to see the leaderboard.";
                return;
            }
            leaderboardText.text = "Loading...";
            _ = FetchAndDisplay(previousMonth, ++fetchVersion);
        }

        async Task FetchAndDisplay(bool previousMonth, int version)
        {
            LeaderboardEntry[] entries = null;
            try
            {
                entries = previousMonth
                    ? await ScrollRoadLeaderboard.GetPreviousMonthTopGlobalAsync(10)
                    : await ScrollRoadLeaderboard.GetTopGlobalAsync(10);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Leaderboard okunamadı: {e.Message}");
            }

            if (version != fetchVersion || leaderboardText == null) return; // panel bu arada değişti

            if (entries == null || entries.Length == 0)
            {
                leaderboardText.text = "No scores yet.";
                return;
            }

            var sb = new StringBuilder();
            // Skor gizli puan; bakan oyuncunun aktif ortamının biriminde yazılır (puan görünmez).
            var env = EnvironmentManager.Current;
            double cityPoints = (drive != null ? drive.CityDistanceKm : RoadDrive.DefaultCityDistanceKm) / EnvironmentCatalog.Road.UnitsPerPoint;
            foreach (var entry in entries)
            {
                double points = entry.Score;

                // Bu ayki tablo canlı yarış: kim şehre ulaştı bilgisi ay sonuna kadar rakiplerden gizli.
                // Kendi skorumuz gerçek görünür, başkalarınınki şehir mesafesinde kırpılır.
                // Geçen ayın tablosu (reveal) kırpılmadan gösterilir.
                bool isLocalUser = entry.User.Id == SteamClient.SteamId;
                if (!previousMonth && !isLocalUser && points >= cityPoints) points = cityPoints;

                sb.AppendLine($"{entry.GlobalRank,3}.  {entry.User.Name}   {env.Format(points)}");
            }
            leaderboardText.text = sb.ToString();
        }
    }
}
