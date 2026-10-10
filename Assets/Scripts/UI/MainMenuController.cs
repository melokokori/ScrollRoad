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
        // Oturumda yalnız bir kez: oyuncu sonradan ana menüye dönerse menüde kalır.
        static bool autoLaunchConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => autoLaunchConsumed = false;

        void Awake()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Game));
            quitButton.onClick.AddListener(Application.Quit);
        }

        void Start()
        {
            if (autoLaunchConsumed) return;
            autoLaunchConsumed = true;
            if (SaveService.LoadStartInCompanion() || CompanionWindowController.LaunchedWithCompanionArg)
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
