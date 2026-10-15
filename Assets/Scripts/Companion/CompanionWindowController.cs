using System;
using System.Collections;
using ScrollRoad.Platform;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrollRoad
{
    // Oyunun iki pencere hâli:
    //  - KART (varsayılan): masaüstünde duran kenarlıksız, köşeleri yuvarlak küçük pencere
    //    (taban 800x340, monitör ölçeği ve Ayarlar'daki boyut ön ayarıyla büyür). Sürüklenir,
    //    konumu hatırlanır, istenirse her zaman üstte. Tam ekran ya da büyük pencere yok.
    //  - GÖREV ÇUBUĞU ŞERİDİ (F9): görev çubuğunun boş kısmına oturan saydam, her zaman üstte şerit
    //    (yükseklik = görev çubuğu). Saydamlık doğrudan Win32/DWM ile (NativeWindow.SetStripTransparent);
    //    2026-10-15'e kadar UniWindowController yapıyordu, Smart App Control imzasız LibUniWinC.dll'i
    //    engelleyince şerit başlıklı siyah pencere kalıyordu. Görev çubuğu da topmost olduğundan şerit
    //    periyodik olarak öne alınır; ön planda tam ekran bir uygulama varken gizlenir.
    //
    // Saydamlık için ön koşullar (sahne kurucu ve Player Settings bunları zorluyor):
    //  - Kamera arka planı alfa 0 (RoadSceneView şerit modunda yapıyor), HDR ve post-process kapalı.
    //  - Windows build'de yalnız Direct3D11 ve "Use DXGI Flip Model Swapchain" KAPALI (DWM camı alfa görmez).
    //
    // EDITOR'DE native pencere işlemi YAPILMAZ: FindUnityWindow orada Editor penceresinin kendisini bulur.
    // Editor'de mod yalnız görsel olarak simüle edilir (Game görünümünü ScrollRoad → Game görünümü:
    // kart boyutu menüsüyle 800x340'a ayarla); gerçek pencere davranışı yalnız build'de doğrulanabilir.
    public class CompanionWindowController : MonoBehaviour
    {
        public static readonly Vector2Int CardBaseSize = new Vector2Int(800, 340);
        public static readonly float[] CardSizePresets = { 0.85f, 1f, 1.25f };
        public static readonly string[] CardSizeNames = { "Small", "Medium", "Large" };

        [SerializeField] int stripBaseWidth = 440; // 48 px'lik (%100 ölçek) görev çubuğu için
#pragma warning disable CS0414 // aşağıdakiler yalnız Windows build'de okunuyor (Editor'de pencereye dokunulmaz)
        [SerializeField] float applyTimeoutSeconds = 3f;
        [SerializeField] float keepOnTopInterval = 0.25f;
        [SerializeField] int cardScreenMargin = 24;
        [SerializeField] float opacityThreshold = 0.1f; // şeritte bu alfanın altındaki pikseller tıklamayı arkaya geçirir
#pragma warning restore CS0414

        static bool companionArgConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => companionArgConsumed = false;

        // Unity son pencere boyutunu hatırlar (eski sürümlerden 1280x720 ya da şerit boyutu kalmış
        // olabilir): açılışta doğrudan kart boyutuna getirilir. Kenarlık ve konum ilk karelerde
        // uygulanır — pencereye BeforeSceneLoad'da native dokunmak açılışı dondurabiliyordu.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SizeWindowOnLaunch()
        {
#if !UNITY_EDITOR
            if (SaveService.LoadStartInCompanion() || LaunchedWithCompanionArg) return;
            var size = CardPixelSize(NativeWindow.DpiScale(IntPtr.Zero));
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
#endif
        }

        static Vector2Int CardPixelSize(float dpiScale)
        {
            float s = dpiScale * CardSizePresets[SaveService.LoadCardSize()];
            return new Vector2Int(Mathf.RoundToInt(CardBaseSize.x * s), Mathf.RoundToInt(CardBaseSize.y * s));
        }

        Coroutine applyRoutine;
        Vector2Int companionSize = new Vector2Int(440, 48);
        float keepOnTopTimer;
#pragma warning disable CS0414
        bool stripPlaced;
        bool hiddenForFullscreen;
        bool cardPending = true;
#pragma warning restore CS0414
#if !UNITY_EDITOR
        IntPtr hwnd;
        int dragOffsetX, dragOffsetY;
        bool stripTransparent, clickThrough;
        Coroutine hitTestRoutine;
        Texture2D hitTestPixel;
#endif

        public bool IsCompanionMode { get; private set; }

        // Şerit ölçeği (görev çubuğu yüksekliği / 48): HUD yazıları bununla büyür.
        public float StripScale { get; private set; } = 1f;

        // "Taşan kadran" görünümünde pencere görev çubuğunun üstüne bu kadar uzar (UI birimi =
        // %100 ölçekte piksel). Kamera şeridi pencerenin altındaki banda çizer (oran).
        public const float PeekOverflow = 44f;
        public float StripOverflowUnits { get; private set; }
        public float StripOverflowFraction { get; private set; }

        // "-companion" komut satırı argümanı: doğrudan görev çubuğu şeridi modunda aç
        // (Steam başlatma seçeneği ya da Windows başlangıç kısayolu için).
        public static bool LaunchedWithCompanionArg =>
            Array.Exists(Environment.GetCommandLineArgs(), a => string.Equals(a, "-companion", StringComparison.OrdinalIgnoreCase));
        public event Action<bool> OnModeChanged;

        void Start()
        {
            // Argüman oturumda yalnız ilk Game yüklemesinde etkili.
            bool fromArg = LaunchedWithCompanionArg && !companionArgConsumed;
            companionArgConsumed = true;
            if (SaveService.LoadStartInCompanion() || fromArg)
            {
                cardPending = false;
                SetCompanionMode(true);
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) ToggleCompanionMode();

#if !UNITY_EDITOR
            if (hwnd == IntPtr.Zero) hwnd = NativeWindow.FindUnityWindow();

            // Kart: Unity pencereyi açılışta kendi stiliyle gösterir; kenarlık birkaç kare sonra kaldırılır.
            if (cardPending && !IsCompanionMode && Time.frameCount >= 3)
            {
                cardPending = false;
                ApplyCardWindow();
            }

            if (BackgroundLifecycle.IsHiddenToTray) return;
            if (!IsCompanionMode)
            {
                if (dragging)
                {
                    DragStep();
                    var mouse = Mouse.current;
                    if (mouse != null && !mouse.leftButton.isPressed) EndCardDrag(); // bırakma olayı kaçtıysa
                }
                return;
            }
            if (!stripPlaced) return;
            keepOnTopTimer -= Time.unscaledDeltaTime;
            if (keepOnTopTimer > 0f) return;
            keepOnTopTimer = keepOnTopInterval;

            bool fullscreen = NativeWindow.IsTaskbarCovered(hwnd);
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
            dragging = false;
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

                // Taşan kadran: pencere yukarı uzar (Editor'de pencereye dokunulmadığı için yalnız build'de).
                StripOverflowUnits = 0f;
                StripOverflowFraction = 0f;
#if !UNITY_EDITOR
                if (GaugeSkins.Strip == GaugeSkins.StripStyle.Peek)
                {
                    int over = Mathf.RoundToInt(PeekOverflow * StripScale);
                    stripRect.Top -= over;
                    stripRect.Height += over;
                    companionSize = new Vector2Int(stripRect.Width, stripRect.Height);
                    StripOverflowUnits = PeekOverflow;
                    StripOverflowFraction = over / (float)stripRect.Height;
                }
#endif
            }
            OnModeChanged?.Invoke(companion);

            if (applyRoutine != null) StopCoroutine(applyRoutine);
#if !UNITY_EDITOR
            applyRoutine = StartCoroutine(companion ? EnterCompanionRoutine(stripRect) : ExitCompanionRoutine());
#endif
        }

        // --- Kart ---

        // Ayarlar'dan boyut ön ayarı değişti: konum (sol-üst) korunur, ekrana sığacak şekilde kırpılır.
        public void SetCardSize(int preset)
        {
            SaveService.SaveCardSize(Mathf.Clamp(preset, 0, CardSizePresets.Length - 1));
#if !UNITY_EDITOR
            if (!IsCompanionMode) ApplyCardWindow();
#endif
        }

        public void SetCardOnTop(bool onTop)
        {
            SaveService.SaveCardOnTop(onTop);
#if !UNITY_EDITOR
            if (!IsCompanionMode) NativeWindow.SetTopmost(hwnd, onTop);
#endif
        }

        public void MinimizeCard()
        {
#if !UNITY_EDITOR
            NativeWindow.Minimize(hwnd);
#endif
        }

        // Kart, kartın boş bir yerinden (yazı/düğme dışı) sürüklenir: CardDragHandle çağırır.
        bool dragging;

        public void BeginCardDrag()
        {
            if (IsCompanionMode) return;
#if !UNITY_EDITOR
            if (!NativeWindow.TryGetBounds(hwnd, out var b) || !NativeWindow.TryGetCursor(out int cx, out int cy)) return;
            dragOffsetX = cx - b.Left;
            dragOffsetY = cy - b.Top;
            dragging = true;
#endif
        }

        public void EndCardDrag()
        {
            if (!dragging) return;
            dragging = false;
#if !UNITY_EDITOR
            DragStep();
            if (NativeWindow.TryGetBounds(hwnd, out var b))
            {
                var clamped = ClampToWorkArea(b);
                if (clamped.Left != b.Left || clamped.Top != b.Top) NativeWindow.MoveTo(hwnd, clamped.Left, clamped.Top);
                SaveService.SaveCardPosition(clamped.Left, clamped.Top);
            }
#endif
        }

#if !UNITY_EDITOR
        // Sürükleme her kare imleçten okunur (Unity olaylarındaki konum pencereyle birlikte kayıyor).
        void DragStep()
        {
            if (!NativeWindow.TryGetCursor(out int cx, out int cy)) return;
            NativeWindow.MoveTo(hwnd, cx - dragOffsetX, cy - dragOffsetY);
        }

        void ApplyCardWindow()
        {
            if (hwnd == IntPtr.Zero) hwnd = NativeWindow.FindUnityWindow();
            if (hwnd == IntPtr.Zero) return;

            var size = CardPixelSize(NativeWindow.DpiScale(hwnd));
            NativeWindow.ScreenRect bounds;
            if (SaveService.TryLoadCardPosition(out int left, out int top))
            {
                bounds = new NativeWindow.ScreenRect { Left = left, Top = top, Width = size.x, Height = size.y };
            }
            else
            {
                // İlk açılış: birincil monitörün sağ altı, görev çubuğunun üstünde.
                var work = NativeWindow.WorkAreaAt(0, 0);
                int margin = Mathf.RoundToInt(cardScreenMargin * NativeWindow.DpiScale(hwnd));
                bounds = new NativeWindow.ScreenRect
                {
                    Left = work.Left + work.Width - size.x - margin,
                    Top = work.Top + work.Height - size.y - margin,
                    Width = size.x,
                    Height = size.y,
                };
            }

            bounds = ClampToWorkArea(bounds);
            NativeWindow.ApplyBorderless(hwnd, bounds);
            NativeWindow.SetTopmost(hwnd, SaveService.LoadCardOnTop());
        }

        // Monitör çıkarılmış/çözünürlük değişmiş olabilir: kart tamamen görünür kalsın.
        static NativeWindow.ScreenRect ClampToWorkArea(NativeWindow.ScreenRect r)
        {
            var work = NativeWindow.WorkAreaAt(r.Left + r.Width / 2, r.Top + r.Height / 2);
            r.Left = Mathf.Clamp(r.Left, work.Left, Mathf.Max(work.Left, work.Left + work.Width - r.Width));
            r.Top = Mathf.Clamp(r.Top, work.Top, Mathf.Max(work.Top, work.Top + work.Height - r.Height));
            return r;
        }
#endif

        // --- Görev çubuğu şeridi ---

        // Unity pencere stilini kare sonunda oturttuğu için boyut gerçekten uygulanana kadar her kare
        // yeniden denenir.
        IEnumerator EnterCompanionRoutine(NativeWindow.ScreenRect strip)
        {
            Screen.SetResolution(companionSize.x, companionSize.y, FullScreenMode.Windowed);
            yield return null; // mod değişikliği kare sonunda uygulanıyor
            yield return null; // pencere stilinin oturması için bir kare daha

#if !UNITY_EDITOR
            // Saydamlık/topmost SetResolution'dan SONRA: Unity pencereyi Windowed'a çevirirken
            // stilleri sıfırlıyor.
            IntPtr window = NativeWindow.FindUnityWindow();
            if (window != IntPtr.Zero) hwnd = window;
            NativeWindow.SetStripTransparent(window, true);
            stripTransparent = true;
            NativeWindow.SetTopmost(window, true);
            // Tamamen click-through değil: yalnız saydam pikseller tıklamayı arkaya geçirir,
            // araba/≡ üzerinde pencere tıklanabilir kalır.
            if (hitTestRoutine == null) hitTestRoutine = StartCoroutine(StripHitTestRoutine());

            // Boyut ve konum native (sol-üst orijinli fiziksel piksel).
            NativeWindow.ApplyStripFrame(window); // kart modundan kalan köşe/kenarlık/gölge şeritte çerçeve gibi duruyordu
            float deadline = Time.realtimeSinceStartup + applyTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                NativeWindow.SetBounds(window, strip);
                yield return null;

                if (NativeWindow.TryGetBounds(window, out var b) && b.Width == strip.Width && b.Height == strip.Height)
                {
                    NativeWindow.SetBounds(window, strip); // boyut değişince konum kaymış olabilir
                    NativeWindow.SetStripTransparent(window, true); // Unity boyut değişiminde stili sıfırlamış olabilir
                    NativeWindow.ApplyStripFrame(window);
                    stripPlaced = true;
                    yield break;
                }
            }
            stripPlaced = true;
            Debug.LogWarning($"Şerit pencere boyutu {applyTimeoutSeconds}sn içinde uygulanamadı.");
#else
            yield break;
#endif
        }

        IEnumerator ExitCompanionRoutine()
        {
#if !UNITY_EDITOR
            // Cam ve tıklama geçirgenliği kapatılır; kart stili ve üstte kalma ApplyCardWindow'da.
            StopStripHitTest();
            NativeWindow.SetStripTransparent(hwnd, false);
            stripTransparent = false;
            NativeWindow.SetTopmost(hwnd, false);
            yield return null;

            var size = CardPixelSize(NativeWindow.DpiScale(hwnd));
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            yield return null;
            yield return null;
            ApplyCardWindow();
#else
            yield break;
#endif
        }

#if !UNITY_EDITOR
        // Şeritte imlecin altındaki piksel kare sonunda okunur: saydamsa pencere tıklamayı alttaki görev
        // çubuğuna geçirir, değilse (araba, ≡, sayaç) tıklanabilir. İmleç pencere dışındayken okuma yapılmaz.
        IEnumerator StripHitTestRoutine()
        {
            var endOfFrame = new WaitForEndOfFrame();
            if (hitTestPixel == null) hitTestPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            while (true)
            {
                yield return endOfFrame;
                if (!IsCompanionMode || !stripTransparent) continue;
                bool hit = CursorOnOpaquePixel();
                if (hit == clickThrough)
                {
                    clickThrough = !hit;
                    NativeWindow.SetClickThrough(hwnd, clickThrough);
                }
            }
        }

        bool CursorOnOpaquePixel()
        {
            if (!NativeWindow.TryGetClientCursor(hwnd, out int x, out int y)) return false;
            int w = Screen.width, h = Screen.height;
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            try
            {
                hitTestPixel.ReadPixels(new Rect(x, h - 1 - y, 1, 1), 0, 0, false);
                return hitTestPixel.GetPixel(0, 0).a >= opacityThreshold;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Şerit isabet testi okunamadı: {e.Message}");
                return true;
            }
        }

        void StopStripHitTest()
        {
            if (hitTestRoutine != null) StopCoroutine(hitTestRoutine);
            hitTestRoutine = null;
            if (clickThrough) NativeWindow.SetClickThrough(hwnd, false);
            clickThrough = false;
        }

        // Odak gelince tıklama geçirgenliği kapatılır (UniWinC'deki gibi): pencere klavye/fareyle kurtarılabilsin.
        void OnApplicationFocus(bool focus)
        {
            if (!focus || !clickThrough) return;
            clickThrough = false;
            NativeWindow.SetClickThrough(hwnd, false);
        }
#endif
    }
}
