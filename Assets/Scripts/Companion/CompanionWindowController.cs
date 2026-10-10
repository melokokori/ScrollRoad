using System;
using System.Collections;
using Kirurobo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrollRoad
{
    // Masaüstü arkadaşı modu: küçük, saydam, her zaman üstte pencere. Native layered-window işini
    // UniWindowController (MIT, kirurobo) yapıyor. F9 veya duraklatma menüsü/☰ ile geçiş yapılır.
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
        [SerializeField] Vector2Int companionSize = new Vector2Int(320, 180);
        [SerializeField] Vector2Int defaultWindowedSize = new Vector2Int(1280, 720);
        [SerializeField] float applyTimeoutSeconds = 3f;
        [SerializeField] int screenMargin = 16;
        [SerializeField] int taskbarAllowance = 56;

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

        public bool IsCompanionMode { get; private set; }

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
        }

        public void ToggleCompanionMode() => SetCompanionMode(!IsCompanionMode);

        public void SetCompanionMode(bool companion)
        {
            if (companion == IsCompanionMode) return;
            IsCompanionMode = companion;
            OnModeChanged?.Invoke(companion);

            if (applyRoutine != null) StopCoroutine(applyRoutine);
#if !UNITY_EDITOR
            if (uniWindow != null)
            {
                applyRoutine = StartCoroutine(companion ? EnterCompanionRoutine() : ExitCompanionRoutine());
            }
#endif
        }

        // windowSize/windowPosition anlık native çağrılardır: UniWinC pencereye kendi Update'inde
        // bağlandığı ve Unity tam ekrandayken boyut tutmadığı için önce Windowed'a geçilir, sonra
        // boyut gerçekten uygulanana kadar her kare yeniden denenir (eski projede doğrulanan yöntem).
        IEnumerator EnterCompanionRoutine()
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

            // UniWinC konumu SOL-ALT orijinli verir (Y yukarı): sağ alt köşe, görev çubuğunun üstü.
            var monitor = Screen.currentResolution;
            var targetPosition = new Vector2(
                monitor.width - companionSize.x - screenMargin,
                taskbarAllowance);

            float deadline = Time.realtimeSinceStartup + applyTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                uniWindow.windowSize = companionSize;
                uniWindow.windowPosition = targetPosition;
                yield return null;

                if (Vector2.Distance(uniWindow.windowSize, companionSize) < 2f) yield break;
            }
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
