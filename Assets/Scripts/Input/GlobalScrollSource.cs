using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScrollRoad.InputCapture
{
    // Oyunun tek scroll kaynağı. Windows'ta sistem genelindeki tekerleği (GlobalScrollHook) okur,
    // başka platformlarda Input System'e (sadece odaktayken) düşer.
    // Birimler "çentik": 1.0 = klasik farede bir tık; hassas touchpad'ler kesirli değer üretir.
    [DefaultExecutionOrder(-100)]
    public class GlobalScrollSource : MonoBehaviour
    {
        public static GlobalScrollSource Instance { get; private set; }

        // Geliştirici paneli: final üründe kapalı. Yalnız Editor/development build'de F3 ile açılır.
        [SerializeField] bool showDebugOverlay = false;

        // Bu karedeki scroll (yukarı +).
        public float FrameNotchesSigned { get; private set; }
        public float FrameNotchesAbs { get; private set; }

        // Oturum toplamları.
        public double SessionNotchesAbs { get; private set; }
        public double SessionInjectedAbs { get; private set; } // makro/SendInput: puana sayılmaz
        public double SessionHorizontalAbs { get; private set; }

        public bool IsGlobal { get; private set; }

        public event Action<float> OnScroll; // işaretli çentik, sadece scroll olan karelerde

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject(nameof(GlobalScrollSource));
            DontDestroyOnLoad(go);
            go.AddComponent<GlobalScrollSource>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.runInBackground = true; // odak dışındayken de Update dönsün

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            GlobalScrollHook.Start();
            IsGlobal = GlobalScrollHook.IsRunning;
            if (!IsGlobal) Debug.LogWarning($"[GlobalScroll] Kanca kurulamadı, odak-içi girişe düşülüyor: {GlobalScrollHook.LastError}");
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
#endif
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            GlobalScrollHook.Stop();
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Editor'de play sırasında derleme olursa kanca eski domain'e asılı kalmasın.
        static void OnDomainUnload(object sender, EventArgs e) => GlobalScrollHook.Stop();
#endif

        void Update()
        {
            float signed = 0f, abs = 0f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (IsGlobal)
            {
                var s = GlobalScrollHook.Drain();
                const float unit = GlobalScrollHook.WheelDelta;
                signed = s.Signed / unit;
                abs = s.Abs / unit;
                SessionInjectedAbs += s.InjectedAbs / unit;
                SessionHorizontalAbs += s.HorizontalAbs / unit;
            }
            else
#endif
            {
                // Input System 1.20 varsayılanı (ScrollDeltaBehavior.UniformAcrossAllPlatforms): tık başına ~1.
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    float y = mouse.scroll.ReadValue().y;
                    signed = y;
                    abs = Mathf.Abs(y);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // YALNIZ test: gerçek tekerlek gibi işlenir. Release'te derlenmez; SendInput ile gelen
            // (enjekte) olaylar hook'ta ayrı sayılmaya ve puana girmemeye devam eder.
            if (pendingTestNotches != 0f)
            {
                signed += pendingTestNotches;
                abs += Mathf.Abs(pendingTestNotches);
                pendingTestNotches = 0f;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame) showDebugOverlay = !showDebugOverlay;
#endif

            FrameNotchesSigned = signed;
            FrameNotchesAbs = abs;
            SessionNotchesAbs += abs;
            if (abs > 0f) OnScroll?.Invoke(signed);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float pendingTestNotches;

        // Otomatik test/doğrulama için: bir sonraki karede bu kadar (işaretli) çentik scroll varmış gibi davranır.
        public void DebugInjectNotches(float signedNotches) => pendingTestNotches += signedNotches;
#endif

        void OnGUI()
        {
            if (!showDebugOverlay) return;
            GUI.Box(new Rect(10, 10, 300, 110), GUIContent.none);
            GUI.Label(new Rect(20, 15, 280, 100),
                $"Kaynak: {(IsGlobal ? "GLOBAL (WH_MOUSE_LL)" : "odak-içi (Input System)")}\n" +
                $"Odak: {(Application.isFocused ? "evet" : "hayır")}\n" +
                $"Toplam çentik: {SessionNotchesAbs:F1}\n" +
                $"Makro/enjekte (sayılmaz): {SessionInjectedAbs:F1}\n" +
                $"Yatay (sayılmaz): {SessionHorizontalAbs:F1}");
        }
    }
}
