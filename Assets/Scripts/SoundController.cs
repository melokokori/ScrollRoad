using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Tüm ses geri bildirimi tek yerde. Klipler Kenney.nl CC0 ses paketlerinden (Assets/Audio);
    // araçların kendi sesleri VehicleSound'da.
    // Ses YALNIZ KART MODUNDA (kullanıcı kararı, 11.10): görev çubuğu şeridinde ve tepsideyken
    // dinleyici sesi yumuşakça sıfıra iner (kart arka planda kalsa da kart modundaysa çalar).
    public class SoundController : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CompanionWindowController companionWindow;
        [SerializeField] float muteFadeSeconds = 0.4f;
        [SerializeField] AudioClip[] stepClips;
        [SerializeField] AudioClip turnClip;
        [SerializeField] AudioClip cityReachedClip;
        [SerializeField] AudioClip uiClickClip;

        [SerializeField, Range(0f, 1f)] float stepVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float turnVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] float jingleVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] float uiVolume = 0.5f;
        [SerializeField] float minStepInterval = 0.05f; // hızlı tekerlek/touchpad'de ses çorbası olmasın

        // Adım sesleri perde oynatılarak çalınıyor; pitch kaynak genelinde etkili olduğundan
        // (çalmakta olan one-shot'ları da kaydırır) adımlar için ayrı bir kaynak kullanılıyor.
        AudioSource stepSource;
        AudioSource generalSource;
        float lastStepTime = -1f;

        void Awake()
        {
            // Kaydedilmiş ana ses tercihi her sahnede açılışta uygulanır.
            AudioListener.volume = SaveService.LoadMasterVolume();

            stepSource = gameObject.AddComponent<AudioSource>();
            stepSource.playOnAwake = false;
            generalSource = gameObject.AddComponent<AudioSource>();
            generalSource.playOnAwake = false;
            generalSource.ignoreListenerPause = true;

            if (drive != null)
            {
                drive.OnStep += HandleStep;
                drive.OnDirectionChanged += HandleTurn;
                drive.OnReachedCity += HandleCityReached;
            }
        }

        void Start()
        {
            // Sahnedeki tüm butonlara (kapalı panellerdekiler dahil) klik sesi bağlanır.
            if (uiClickClip == null) return;
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                button.onClick.AddListener(PlayUiClick);
            }
        }

        void Update()
        {
            // Ana ses tercihi (Ayarlar) × kart modu: şeritte ve tepside sessiz.
            bool card = (companionWindow == null || !companionWindow.IsCompanionMode) && !BackgroundLifecycle.IsHiddenToTray;
            float target = card ? SaveService.LoadMasterVolume() : 0f;
            AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, muteFadeSeconds));
        }

        void OnDestroy()
        {
            if (drive == null) return;
            drive.OnStep -= HandleStep;
            drive.OnDirectionChanged -= HandleTurn;
            drive.OnReachedCity -= HandleCityReached;
        }

        void HandleStep()
        {
            if (stepClips == null || stepClips.Length == 0) return;
            if (Time.unscaledTime - lastStepTime < minStepInterval) return;
            lastStepTime = Time.unscaledTime;

            float pitch = Random.Range(0.92f, 1.08f);
            // Şehrin ötesi — belgelenmemiş: derin bölgede tik sesi kalınlaşır.
            if (drive.InSecretZone) pitch *= 0.75f;
            stepSource.pitch = pitch;
            stepSource.PlayOneShot(stepClips[Random.Range(0, stepClips.Length)], stepVolume);
        }

        void HandleTurn()
        {
            if (turnClip != null) generalSource.PlayOneShot(turnClip, turnVolume);
        }

        void HandleCityReached()
        {
            if (cityReachedClip != null) generalSource.PlayOneShot(cityReachedClip, jingleVolume);
        }

        public void PlayUiClick()
        {
            if (uiClickClip != null) generalSource.PlayOneShot(uiClickClip, uiVolume);
        }
    }
}
