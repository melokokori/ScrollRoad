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

        void Awake()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Game));
            quitButton.onClick.AddListener(Application.Quit);
        }

        void Update()
        {
            if (steamStatusText != null) steamStatusText.text = SteamManager.StatusText;
        }
    }
}
