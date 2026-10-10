using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Duraklatma menüsü (Esc veya HUD'daki "Menü"). Alt paneller (Ayarlar, İstatistikler, Skor
    // Tablosu) açıkken Esc önce onları kapatır. Masaüstü arkadaşı modunda duraklatma kullanılmaz.
    public class PauseController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject leaderboardPanel;
        [SerializeField] Text leaderboardTitle;
        [SerializeField] Text leaderboardText;
        [SerializeField] Button hudMenuButton;
        [SerializeField] Button hudCompanionButton;
        [SerializeField] Button resumeButton;
        [SerializeField] Button leaderboardButton;
        [SerializeField] Button revealButton;
        [SerializeField] Button companionButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button leaderboardCloseButton;
        [SerializeField] GameObject[] subPanels; // Ayarlar, İstatistikler (Esc ile kapanacak)

        int fetchVersion;

        public bool IsPaused { get; private set; }

        void Awake()
        {
            hudMenuButton.onClick.AddListener(() => SetPaused(true));
            hudCompanionButton.onClick.AddListener(EnterCompanion);
            resumeButton.onClick.AddListener(() => SetPaused(false));
            leaderboardButton.onClick.AddListener(() => OpenLeaderboard(previousMonth: false));
            revealButton.onClick.AddListener(() => OpenLeaderboard(previousMonth: true));
            companionButton.onClick.AddListener(EnterCompanion);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            quitButton.onClick.AddListener(Application.Quit);
            leaderboardCloseButton.onClick.AddListener(() => leaderboardPanel.SetActive(false));

            pausePanel.SetActive(false);
            leaderboardPanel.SetActive(false);
        }

        void OnDestroy() => Time.timeScale = 1f;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (companionWindow != null && companionWindow.IsCompanionMode) return;

            if (leaderboardPanel.activeSelf) { leaderboardPanel.SetActive(false); return; }
            foreach (var panel in subPanels)
            {
                if (panel != null && panel.activeSelf) { panel.SetActive(false); return; }
            }
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            pausePanel.SetActive(paused);
            if (!paused)
            {
                leaderboardPanel.SetActive(false);
                foreach (var panel in subPanels) if (panel != null) panel.SetActive(false);
            }
            Time.timeScale = paused ? 0f : 1f;
        }

        void EnterCompanion()
        {
            SetPaused(false);
            if (companionWindow != null) companionWindow.SetCompanionMode(true);
        }

        void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        void OpenLeaderboard(bool previousMonth)
        {
            foreach (var panel in subPanels) if (panel != null) panel.SetActive(false);
            leaderboardPanel.SetActive(true);
            leaderboardTitle.text = previousMonth ? "Geçen ayın sonuçları" : "Bu ayın sıralaması";
            if (!SteamManager.IsInitialized)
            {
                leaderboardText.text = "Steam bağlı değil.\nSıralama için Steam açık ve giriş yapılmış olmalı.";
                return;
            }
            leaderboardText.text = "Yükleniyor...";
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
                leaderboardText.text = "Henüz skor yok.";
                return;
            }

            var sb = new StringBuilder();
            float cityKm = drive != null ? drive.CityDistanceKm : RoadDrive.DefaultCityDistanceKm;
            foreach (var entry in entries)
            {
                double km = entry.Score / 1000.0;

                // Bu ayki tablo canlı yarış: kim şehre ulaştı bilgisi ay sonuna kadar rakiplerden gizli.
                // Kendi skorumuz gerçek görünür, başkalarınınki şehir mesafesinde kırpılır.
                // Geçen ayın tablosu (reveal) kırpılmadan gösterilir.
                bool isLocalUser = entry.User.Id == SteamClient.SteamId;
                if (!previousMonth && !isLocalUser && km >= cityKm) km = cityKm;

                sb.AppendLine($"{entry.GlobalRank,3}.  {entry.User.Name}   {Fmt.Km(km)}");
            }
            leaderboardText.text = sb.ToString();
        }
    }
}
