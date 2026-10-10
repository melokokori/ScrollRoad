using UnityEngine;

namespace ScrollRoad
{
    // Şehre İLK ulaşma anının kutlama bandı (jingle SoundController'da, konfeti RoadSceneView'de).
    // RoadDrive.OnReachedCity ayda en fazla bir kez tetiklenir. Masaüstü arkadaşı modunda bant
    // gösterilmez (konfeti yine görünür).
    public class CelebrationController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] CanvasGroup bannerGroup;
        [SerializeField] float fadeInSeconds = 0.4f;
        [SerializeField] float holdSeconds = 4.5f;
        [SerializeField] float fadeOutSeconds = 1.2f;

        float timer = -1f; // eksi = bant kapalı

        public bool IsShowing => timer >= 0f;

        void Awake()
        {
            bannerGroup.alpha = 0f;
            bannerGroup.gameObject.SetActive(false);
            drive.OnReachedCity += HandleReachedCity;
        }

        void OnDestroy()
        {
            if (drive != null) drive.OnReachedCity -= HandleReachedCity;
        }

        void HandleReachedCity()
        {
            if (companionWindow != null && companionWindow.IsCompanionMode) return;
            timer = 0f;
            bannerGroup.gameObject.SetActive(true);
        }

        void Update()
        {
            if (timer < 0f) return;

            timer += Time.unscaledDeltaTime;
            bannerGroup.alpha =
                timer < fadeInSeconds ? timer / fadeInSeconds :
                timer < fadeInSeconds + holdSeconds ? 1f :
                Mathf.Clamp01(1f - (timer - fadeInSeconds - holdSeconds) / fadeOutSeconds);

            if (timer >= fadeInSeconds + holdSeconds + fadeOutSeconds)
            {
                timer = -1f;
                bannerGroup.gameObject.SetActive(false);
            }
        }
    }
}
