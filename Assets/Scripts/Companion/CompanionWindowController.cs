using System;
using System.Collections;
using Kirurobo;
using ScrollRoad.Platform;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrollRoad
{
    // Masaüstü modu: görev çubuğunun boş kısmına oturan saydam, her zaman üstte şerit (yükseklik =
    // görev çubuğu). Native layered-window işini UniWindowController (MIT, kirurobo) yapıyor.
    // F9 veya duraklatma menüsü/≡ ile geçiş yapılır. Görev çubuğu da topmost olduğundan şerit
    // periyodik olarak öne alınır; ön planda tam ekran bir uygulama varken gizlenir.
    //
    // Saydamlık için ön koşullar (sahne kurucu ve Player Settings bunları zorluyor):
    //  - Kamera arka planı alfa 0 (RoadSceneView companion modunda yapıyor), HDR ve post-process kapalı.
    //  - Windows build'de yalnız Direct3D11 ve "Use DXGI Flip Model Swapchain" KAPALI (UniWinC README, Limitations).
    //
    // EDITOR'DE native pencere işlemi YAPILMAZ: UniWinC orada Editor penceresinin kendisine bağlanır
    // (saydam/tıklama-geçirgen bir Editor istemeyiz). Editor'de mod yalnız görsel olarak (kamera
    // yakınlaşması, HUD değişimi) simüle edilir; gerçek pencere davranışı yalnız build'de doğrulanabilir.
    public class CompanionWindowController : MonoBehaviour
    {
        [SerializeField] UniWindowController uniWindow;
        [SerializeField] int stripBaseWidth = 440; // 48 px'lik (%100 ölçek) görev çubuğu için
        [SerializeField] Vector2Int defaultWindowedSize = new Vector2Int(1280, 720);
        [SerializeField] float applyTimeoutSeconds = 3f;
#pragma warning disable CS0414 // aşağıdakiler yalnız Windows build'de okunuyor (Editor'de pencereye dokunulmaz)
        [SerializeField] float keepOnTopInterval = 0.25f;
#pragma warning restore CS0414

        static bool companionArgConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => companionArgConsumed = false;

        // Unity son pencere boyutunu/konumunu hatırlar: masaüstü modunda kapatılan oyun bir sonraki
        // normal açılışta 320x180 ve köşede açılırdı. Normal açılışta pencere makul boyuta ve ortaya alınır.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RestoreNormalWindowOnLaunch()
        {
#if !UNITY_EDITOR
            if (SaveService.LoadStartInCompanion() || LaunchedWithCompanionArg) return;
            if (Screen.fullScreenMode != FullScreenMode.Windowed || Screen.width >= 640) return;
            const int w = 1280, h = 720;
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            var display = Screen.mainWindowDisplayInfo;
            Screen.MoveMainWindowTo(display, new Vector2Int(Mathf.Max(0, (display.width - w) / 2), Mathf.Max(0, (display.height - h) / 2)));
#endif
        }

        Coroutine applyRoutine;
        Vector2Int restoreSize;
        Vector2Int companionSize = new Vector2Int(440, 48);
        float keepOnTopTimer;
#pragma warning disable CS0414
        bool stripPlaced;
        bool hiddenForFullscreen;
#pragma warning restore CS0414
#if !UNITY_EDITOR
        IntPtr hwnd;
#endif

        public bool IsCompanionMode { get; private set; }

        // Şerit ölçeği (görev çubuğu yüksekliği / 48): HUD yazıları bununla büyür.
        public float StripScale { get; private set; } = 1f;

        // "-companion" komut satırı argümanı: doğrudan masaüstü arkadaşı modunda aç
        // (Steam başlatma seçeneği ya da Windows başlangıç kısayolu için).
        public static bool LaunchedWithCompanionArg =>
            Array.Exists(Environment.GetCommandLineArgs(), a => string.Equals(a, "-companion", StringComparison.OrdinalIgnoreCase));
        public event Action<bool> OnModeChanged;

        void Awake()
        {
#if UNITY_EDITOR
            if (uniWindow != null) uniWindow.enabled = false;
#endif
        }

        void Start()
        {
            // Argüman oturumda yalnız ilk Game yüklemesinde etkili (ana menüye dönüp tekrar girince değil).
            bool fromArg = LaunchedWithCompanionArg && !companionArgConsumed;
            companionArgConsumed = true;
            if (SaveService.LoadStartInCompanion() || fromArg) SetCompanionMode(true);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) ToggleCompanionMode();

#if !UNITY_EDITOR
            if (!IsCompanionMode || !stripPlaced || BackgroundLifecycle.IsHiddenToTray) return;
            keepOnTopTimer -= Time.unscaledDeltaTime;
            if (keepOnTopTimer > 0f) return;
            keepOnTopTimer = keepOnTopInterval;

            if (hwnd == IntPtr.Zero) hwnd = NativeWindow.FindUnityWindow();
            bool fullscreen = NativeWindow.IsFullscreenAppActive(hwnd);
            if (fullscreen != hiddenForFullscreen)
            {
                hiddenForFullscreen = fullscreen;
                NativeWindow.Show(hwnd, !fullscreen);
            }
            if (!fullscreen) NativeWindow.KeepOnTop(hwnd);
#endif
        }

        public void ToggleCompanionMode() => SetCompanionMode(!IsCompanionMode);

        public void SetCompanionMode(bool companion)
        {
            if (companion == IsCompanionMode) return;
            IsCompanionMode = companion;
            stripPlaced = false;
#if !UNITY_EDITOR
            if (hiddenForFullscreen) NativeWindow.Show(hwnd, true);
#endif
            hiddenForFullscreen = false;

            // Şerit boyutu ve ölçeği OnModeChanged'den ÖNCE: HUD ölçeği buna göre ayarlanıyor.
            var stripRect = default(NativeWindow.ScreenRect);
            float scale = 1f;
            bool onTaskbar = companion && NativeWindow.TryGetTaskbarStrip(stripBaseWidth, out stripRect, out scale);
            if (companion)
            {
                if (onTaskbar)
                {
                    StripScale = scale;
                    companionSize = new Vector2Int(stripRect.Width, stripRect.Height);
                }
                else
                {
                    // Görev çubuğu bulunamadı (Editor, üst/yan kenarda görev çubuğu): ekranın sol altı.
                    StripScale = 1f;
                    companionSize = new Vector2Int(stripBaseWidth, 48);
                    var monitor = Screen.currentResolution;
                    stripRect = new NativeWindow.ScreenRect { Left = 12, Top = monitor.height - 48, Width = stripBaseWidth, Height = 48 };
                }
            }
            OnModeChanged?.Invoke(companion);

            if (applyRoutine != null) StopCoroutine(applyRoutine);
#if !UNITY_EDITOR
            if (uniWindow != null)
            {
                applyRoutine = StartCoroutine(companion ? EnterCompanionRoutine(stripRect) : ExitCompanionRoutine());
            }
#endif
        }

        // windowSize/windowPosition anlık native çağrılardır: UniWinC pencereye kendi Update'inde
        // bağlandığı ve Unity tam ekrandayken boyut tutmadığı için önce Windowed'a geçilir, sonra
        // boyut gerçekten uygulanana kadar her kare yeniden denenir (eski projede doğrulanan yöntem).
        IEnumerator EnterCompanionRoutine(NativeWindow.ScreenRect strip)
        {
            restoreSize = Screen.fullScreenMode == FullScreenMode.Windowed
                ? new Vector2Int(Screen.width, Screen.height)
                : defaultWindowedSize;

            Screen.SetResolution(companionSize.x, companionSize.y, FullScreenMode.Windowed);
            yield return null; // mod değişikliği kare sonunda uygulanıyor
            yield return null; // pencere stilinin oturması için bir kare daha

            // Saydamlık/topmost SetResolution'dan SONRA: Unity pencereyi Windowed'a çevirirken
            // stilleri sıfırlıyor (eski projede pencere kenarlıklı kalıyordu). SetTransparent aynı
            // değerde bile native stili yeniden uygular.
            uniWindow.isTransparent = true;
            uniWindow.isTopmost = true;
            // Tamamen click-through değil: yalnız saydam pikseller tıklamayı arkaya geçirir,
            // araba/☰ üzerinde pencere tıklanabilir kalır.
            uniWindow.isHitTestEnabled = true;

            // UniWinC konumu SOL-ALT orijinli verir (Y yukarı); şerit sol-üst orijinli ekran piksellerinde.
            var monitor = Screen.currentResolution;
            var targetPosition = new Vector2(strip.Left, monitor.height - strip.Bottom);

            float deadline = Time.realtimeSinceStartup + applyTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                uniWindow.windowSize = companionSize;
                uniWindow.windowPosition = targetPosition;
                yield return null;

                if (Vector2.Distance(uniWindow.windowSize, companionSize) < 2f)
                {
                    uniWindow.windowPosition = targetPosition; // boyut değişince konum kaymış olabilir
                    stripPlaced = true;
                    yield break;
                }
            }
            stripPlaced = true;
            Debug.LogWarning($"Companion pencere boyutu {applyTimeoutSeconds}sn içinde uygulanamadı (şu an: {uniWindow.windowSize}).");
        }

        IEnumerator ExitCompanionRoutine()
        {
            uniWindow.isTransparent = false;
            uniWindow.isTopmost = false;
            uniWindow.isHitTestEnabled = false;
            yield return null;

            var size = restoreSize.x > companionSize.x ? restoreSize : defaultWindowedSize;
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            yield return null;
            yield return null;

            // Ekranın ortasına geri al (sol-alt orijin).
            var monitor = Screen.currentResolution;
            uniWindow.windowPosition = new Vector2((monitor.width - size.x) / 2f, (monitor.height - size.y) / 2f);
        }
    }
}
