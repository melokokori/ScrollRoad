using System;
using System.Runtime.InteropServices;
using ScrollRoad.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScrollRoad
{
    // Oyunun arka planda yaşaması: sistem tepsisi simgesi, pencereyi kapatınca (X / Alt+F4)
    // çıkmak yerine tepsiye gizlenme ve "-autostart" ile (Windows açılışı) pencere göstermeden başlama.
    // Gizliyken araç Game sahnesinde akmaya, global scroll sayılmaya devam eder; kare hızı düşürülür.
    //
    // Gerçek çıkış yolları: tepsi menüsü "Çıkış", oyun içi Çıkış butonları (QuitApp), Windows kapanışı.
    // Yalnız Windows build'de etkin — Editor'de gizlenecek pencere Editor'ün kendisi olurdu.
    public class BackgroundLifecycle : MonoBehaviour
    {
        const int HiddenFrameRate = 10;
        const float TooltipIntervalSeconds = 2f;

#pragma warning disable CS0414 // yalnız Windows build'de okunuyor
        static bool allowQuit;
#pragma warning restore CS0414

        public static bool IsHiddenToTray { get; private set; }

        // Oyun içindeki tüm "Çıkış" butonları bunu çağırır: tepsiye gizlenmeden gerçekten kapanır.
        public static void QuitApp()
        {
            allowQuit = true;
            Application.Quit();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            allowQuit = false;
            IsHiddenToTray = false;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        static BackgroundLifecycle instance;

        IntPtr unityWindow;
        RoadDrive drive;
        CompanionWindowController companion;
        float tooltipTimer;
        bool pendingCompanion;
        bool pendingAutostartHide;
        int savedTargetFrameRate;
        int savedVSyncCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject(nameof(BackgroundLifecycle));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BackgroundLifecycle>();

            // Windows açılışında: "masaüstü modunda başlasın" açıksa arkadaş penceresi görünür açılır
            // (CompanionWindowController), değilse tepsiye gizlenir. Gizleme ilk
            // kareler render edildikten SONRA: Unity açılışta pencereyi kendisi gösteriyor/swapchain kuruyor.
            instance.pendingAutostartHide = WindowsStartup.LaunchedFromAutostart && !SaveService.LoadStartInCompanion();
        }

        void Awake()
        {
            unityWindow = NativeWindow.FindUnityWindow();
            TrayIcon.Start(TooltipText());
            if (!TrayIcon.IsRunning) Debug.LogWarning($"[Tray] Tepsi simgesi kurulamadı: {TrayIcon.LastError}");
            Application.wantsToQuit += OnWantsToQuit;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            Application.wantsToQuit -= OnWantsToQuit;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            TrayIcon.Stop();
        }

        void OnApplicationQuit() => TrayIcon.Stop();

        bool OnWantsToQuit()
        {
            if (allowQuit || TrayIcon.SessionEnding || !TrayIcon.IsRunning || !SaveService.LoadCloseToTray()) return true;
            HideToTray(userClosed: true);
            return false;
        }

        void Update()
        {
            if (pendingAutostartHide && Time.frameCount >= 5)
            {
                pendingAutostartHide = false;
                HideToTray(userClosed: false);
            }

            while (TrayIcon.TryDequeue(out var command))
            {
                switch (command)
                {
                    case TrayIcon.Command.Show: Restore(); break;
                    case TrayIcon.Command.ToggleCompanion: ToggleCompanion(); break;
                    case TrayIcon.Command.Quit: QuitApp(); break;
                }
            }

            tooltipTimer -= Time.unscaledDeltaTime;
            if (tooltipTimer <= 0f)
            {
                tooltipTimer = TooltipIntervalSeconds;
                TrayIcon.SetTooltip(TooltipText());
                if (companion == null) companion = FindAnyObjectByType<CompanionWindowController>();
            }
            TrayIcon.CompanionChecked = companion != null && companion.IsCompanionMode;
        }

        string TooltipText()
        {
            if (drive == null) drive = FindAnyObjectByType<RoadDrive>();
            if (drive == null) return "ScrollRoad";
            return $"ScrollRoad — {Fmt.Km(drive.CurrentDistanceKmPrecise)}" + (drive.IsUserActive ? "" : " (idle)");
        }

        // userClosed=false: açılışta (-autostart).
        void HideToTray(bool userClosed)
        {
            if (unityWindow == IntPtr.Zero) unityWindow = NativeWindow.FindUnityWindow();
            if (unityWindow == IntPtr.Zero) { Debug.LogWarning("[Tray] Unity penceresi bulunamadı, gizlenemedi."); return; }

            if (userClosed)
            {
                // Gizli pencerede açık bir menü/panel kalmasın.
                var menu = FindAnyObjectByType<CardMenuController>();
                if (menu != null) menu.CloseAll();
            }

            ShowWindow(unityWindow, SW_HIDE);
            if (!IsHiddenToTray)
            {
                IsHiddenToTray = true;
                savedTargetFrameRate = Application.targetFrameRate;
                savedVSyncCount = QualitySettings.vSyncCount;
                QualitySettings.vSyncCount = 0; // targetFrameRate vSync açıkken yok sayılır
                Application.targetFrameRate = HiddenFrameRate;
            }

            if (userClosed && !SaveService.LoadTrayHintShown())
            {
                SaveService.MarkTrayHintShown();
                TrayIcon.ShowBalloon("ScrollRoad is running in the background", "Your scrolls keep counting. Click the icon to reopen it, or right-click > Quit to close it.");
            }
        }

        void Restore()
        {
            if (unityWindow == IntPtr.Zero) unityWindow = NativeWindow.FindUnityWindow();
            if (unityWindow == IntPtr.Zero) return;

            if (IsHiddenToTray)
            {
                IsHiddenToTray = false;
                QualitySettings.vSyncCount = savedVSyncCount;
                Application.targetFrameRate = savedTargetFrameRate;
            }
            ShowWindow(unityWindow, IsIconic(unityWindow) ? SW_RESTORE : SW_SHOW);
            SetForegroundWindow(unityWindow);
        }

        void ToggleCompanion()
        {
            Restore();
            if (companion == null) companion = FindAnyObjectByType<CompanionWindowController>();
            if (companion != null)
            {
                companion.ToggleCompanionMode();
                return;
            }
            // Sahne henüz yüklenmediyse: Game yüklenince şerit moduna geç.
            pendingCompanion = true;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            drive = null;
            companion = null;
            if (!pendingCompanion || scene.name != SceneNames.Game) return;
            pendingCompanion = false;
            var target = FindAnyObjectByType<CompanionWindowController>();
            if (target != null) target.SetCompanionMode(true);
        }

        // --- Win32 ---

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
#endif
    }
}
