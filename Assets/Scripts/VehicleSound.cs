using UnityEngine;

namespace ScrollRoad
{
    // Seçili aracın kendi sesi (Tools/gen_sound.py → Resources/VehicleSounds/<model>_loop|_start):
    //  - Döngü: perdesi ve şiddeti hıza göre kayar; rölantide kısılır, geri giderken de çalar.
    //  - Araç değişince eski döngü yeni döngüye yumuşakça geçer; garajda seçilen aracın kısa
    //    "start" sesi (marş, korna, düdük) çalar. Açılıştaki ilk araçta start sesi çalmaz.
    // Sesi olmayan araç sessiz kalır. Görev çubuğu/tepside susma SoundController'da (dinleyici sesi).
    public class VehicleSound : MonoBehaviour
    {
        [SerializeField] RoadDrive drive;
        [SerializeField] CarCatalog catalog;
        [SerializeField, Range(0f, 1f)] float loopVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] float startVolume = 0.6f;
        [SerializeField] float crossfadeSeconds = 0.6f;
        [SerializeField] Vector2 pitchRange = new Vector2(0.85f, 1.45f); // rölanti → en yüksek hız
        [SerializeField] float idleVolumeScale = 0.4f;

        AudioSource current;
        AudioSource previous;
        AudioSource oneShot;
        string modelId;
        float fade = 1f;
        float smoothedSpeed;

        void Awake()
        {
            current = CreateLoopSource();
            previous = CreateLoopSource();
            oneShot = gameObject.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
        }

        AudioSource CreateLoopSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.volume = 0f;
            return s;
        }

        void OnEnable()
        {
            CarCatalog.SelectionChanged += HandleSelectionChanged;
            Switch(playStart: false);
        }

        void OnDisable() => CarCatalog.SelectionChanged -= HandleSelectionChanged;

        void HandleSelectionChanged() => Switch(playStart: true);

        void Switch(bool playStart)
        {
            var model = catalog != null ? catalog.SelectedModel : null;
            string id = model != null ? model.id : null;
            if (id == modelId) return;
            modelId = id;

            // Eski döngü "previous"a geçer ve söner; yenisi baştan başlar.
            (current, previous) = (previous, current);
            current.Stop();
            current.clip = id != null ? Resources.Load<AudioClip>($"VehicleSounds/{id}_loop") : null;
            if (current.clip != null)
            {
                current.time = Random.Range(0f, current.clip.length); // her seçimde aynı yerden başlamasın
                current.Play();
            }
            fade = 0f;

            if (playStart && id != null)
            {
                var start = Resources.Load<AudioClip>($"VehicleSounds/{id}_start");
                if (start != null) oneShot.PlayOneShot(start, startVolume);
            }
        }

        float envGain = 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fade = Mathf.MoveTowards(fade, 1f, dt / Mathf.Max(0.01f, crossfadeSeconds));

            // Perde gerçek hızı biraz gecikmeli izler (scroll çentiğinde motor "devirlenir").
            float speed = Mathf.Abs(drive.CurrentSpeedKmh);
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-dt / 0.25f));
            float t = Mathf.Clamp01(smoothedSpeed / 220f);
            float pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, Mathf.Sqrt(t));
            float level = Mathf.Lerp(idleVolumeScale, 1f, Mathf.Clamp01(smoothedSpeed / 60f));

            // Araç sesi yalnız Yol ortamında (Olta'nın sesleri sonraki aşama): diğer ortamda susar.
            envGain = Mathf.MoveTowards(envGain, RoadDrive.IsActiveEnvironment ? 1f : 0f, dt * 2f);
            current.pitch = pitch;
            current.volume = loopVolume * level * fade * envGain;
            previous.pitch = pitch;
            previous.volume = loopVolume * level * (1f - fade) * envGain;
            if (fade >= 1f && previous.isPlaying) previous.Stop();
        }
    }
}
