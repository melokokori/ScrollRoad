using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScrollRoad
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Text steamStatusText;

        // "Oyun masaüstü modunda başlasın" açıksa (ya da -companion argümanıyla başlatıldıysa) uygulama açılışında menü atlanır ve doğrudan
        // masaüstü arkadaşı moduna geçilir (CompanionWindowController ayarı Game'de uygular).
        // Windows açılışında (-autostart) da menü atlanır: oyun tepside gizli başlar ve araç hemen akmalı.
        // Oturumda yalnız bir kez: oyuncu sonradan ana menüye dönerse menüde kalır.
        static bool autoLaunchConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => autoLaunchConsumed = false;

        void Awake()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Game));
            quitButton.onClick.AddListener(BackgroundLifecycle.QuitApp);
        }

        void Start()
        {
            if (autoLaunchConsumed) return;
            autoLaunchConsumed = true;
            if (SaveService.LoadStartInCompanion() || CompanionWindowController.LaunchedWithCompanionArg || Platform.WindowsStartup.LaunchedFromAutostart)
            {
                SceneManager.LoadScene(SceneNames.Game);
            }
        }

        void Update()
        {
            if (steamStatusText != null) steamStatusText.text = SteamManager.StatusText;
        }
    }
}
