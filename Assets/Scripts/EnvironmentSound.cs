using UnityEngine;

namespace ScrollRoad
{
    // Ortamların sesleri (2026-10-14; araç seslerinin yaklaşımı: Tools/gen_sound.py --env →
    // Resources/EnvSounds/<ad>.ogg, çalışma zamanında Resources.Load). Oyun saatlerce arka planda açık
    // kaldığı için hepsi sade ve alçak seviyeli; yalnız seçili ortamda çalar. Görev çubuğu/tepside susma ve
    // ana ses ayarı SoundController'da (dinleyici sesi) — buraya ayrıca bir şey gerekmez.
    //  - Döner kapı: her kanat geçişinde yumuşak bir süpürme (4 kanat, ziyaretçi başına yarım tur = 2 geçiş);
    //    sıklığı dönüş hızının kendisi, şiddeti ve perdesi de hıza göre. Geri dönüşte de çalar.
    //  - Olta: makara sarma döngüsü yalnız kullanıcı aktifken (son scroll'dan 3 dk) sarılırken/misina boşalırken
    //    (perde ve şiddet scroll hızına göre, boşalırken biraz kalın) — rölantide saatlerce tıkırdamasın;
    //    atış, balık vurma ve tutma tek seferlik (rölantide de). Makara döngüsü seçili takımın sesi
    //    (FishingGear.ReelSound, 2026-10-15): Klasik spinning, Bambu ahşap merkez makara cırcırı, Karbon
    //    ipeksi rulman, Altın ince metalik tıkırtı.
    //  - İşaret: kapıdan nadir misafir girince ince çan; efsane (Koca Ayak, Altın Sazan) gelince dört notalı çan.
    // Ses dosyası yoksa o ses sessizce atlanır.
    public class EnvironmentSound : MonoBehaviour
    {
        [SerializeField] FishingController fishing;
        [SerializeField] DoorController door;

        [Header("Seviyeler")]
        [SerializeField, Range(0f, 1f)] float sweepVolume = 0.32f;
        [SerializeField, Range(0f, 1f)] float reelVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float castVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] float strikeVolume = 0.32f;
        [SerializeField, Range(0f, 1f)] float catchVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] float chimeVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float legendVolume = 0.4f;

        AudioSource reel;
        AudioSource effects;
        AudioSource sweep;
        AudioClip sweepClip, castClip, strikeClip, catchClip, chimeClip, legendClip;

        string reelSound;
        float reelLevel;
        float smoothedSpeed;
        long lastWing = long.MinValue;
        bool phaseKnown;
        FishingController.Phase lastPhase;

        static AudioClip Load(string name) => Resources.Load<AudioClip>("EnvSounds/" + name);

        void Awake()
        {
            reel = gameObject.AddComponent<AudioSource>();
            reel.playOnAwake = false;
            reel.loop = true;
            reel.volume = 0f;
            reelSound = "reel_loop";
            reel.clip = Load(reelSound);
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            // Süpürmenin perdesi hıza göre oynar; pitch kaynak geneline etki ettiği için ayrı kaynak.
            sweep = gameObject.AddComponent<AudioSource>();
            sweep.playOnAwake = false;

            sweepClip = Load("door_sweep");
            castClip = Load("fish_cast");
            strikeClip = Load("fish_strike");
            catchClip = Load("fish_catch");
            chimeClip = Load("guest_chime");
            legendClip = Load("legend_chime");
        }

        void OnEnable()
        {
            if (door != null) door.GuestArrived += HandleGuest;
            if (fishing != null) fishing.Caught += HandleCaught;
        }

        void OnDisable()
        {
            if (door != null) door.GuestArrived -= HandleGuest;
            if (fishing != null) fishing.Caught -= HandleCaught;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var engine = ScoreEngine.Instance;
            float speed = engine != null ? Mathf.Abs(engine.Pace) : 0f;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-dt / 0.25f));
            float t = Mathf.Sqrt(Mathf.Clamp01(smoothedSpeed / 220f));
            float active = Mathf.Clamp01(smoothedSpeed / 60f); // rölantide (10) kısık, seyirde (60) tam

            var env = EnvironmentManager.Current.Id;
            UpdateDoor(env == EnvironmentId.Door, t, active);
            UpdateFishing(env == EnvironmentId.Fishing, engine != null && engine.IsUserActive, t, active, dt);
        }

        void UpdateDoor(bool on, float t, float active)
        {
            if (!on || door == null)
            {
                lastWing = long.MinValue;
                return;
            }
            // Kanat geçişi: kapının konumu ziyaretçi biriminde; bir ziyaretçi = yarım tur = 2 kanat.
            long wing = (long)System.Math.Floor(door.Turns * 2.0);
            long jump = wing - lastWing;
            bool first = lastWing == long.MinValue;
            lastWing = wing;
            // İlk karede ve büyük sıçramalarda (ay sıfırlanması, yükleme) çalma.
            if (first || jump == 0 || System.Math.Abs(jump) > 3 || sweepClip == null) return;
            sweep.pitch = Mathf.Lerp(0.85f, 1.25f, t) * Random.Range(0.96f, 1.04f);
            sweep.PlayOneShot(sweepClip, sweepVolume * Mathf.Lerp(0.45f, 1f, active));
        }

        void UpdateFishing(bool on, bool userActive, float t, float active, float dt)
        {
            float target = 0f;
            if (on && fishing != null)
            {
                // Takım değişince döngü yeni takımın sesiyle sürer (çalıyorsa kesilmeden).
                var gear = FishingGear.Selected;
                if (gear.ReelSound != reelSound)
                {
                    reelSound = gear.ReelSound;
                    var clip = Load(reelSound);
                    if (clip == null) clip = Load("reel_loop");
                    bool playing = reel.isPlaying;
                    reel.Stop();
                    reel.clip = clip;
                    if (playing && clip != null)
                    {
                        reel.time = Random.Range(0f, clip.length);
                        reel.Play();
                    }
                }
                var phase = fishing.CurrentPhase;
                bool reeling = userActive && fishing.ReelDirection != 0f
                               && (phase == FishingController.Phase.Retrieving || phase == FishingController.Phase.Hooked);
                if (reeling)
                {
                    target = reelVolume * Mathf.Lerp(0.4f, 1f, active);
                    reel.pitch = Mathf.Lerp(0.8f, 1.45f, t) * (fishing.ReelDirection < 0f ? 0.85f : 1f);
                }

                // Aşama geçişleri: atış, vuruş, tutuş (açılıştaki ilk karede çalma).
                if (phaseKnown && phase != lastPhase)
                {
                    if (phase == FishingController.Phase.Casting) Play(castClip, castVolume);
                    else if (phase == FishingController.Phase.Hooked) Play(strikeClip, strikeVolume);
                    else if (phase == FishingController.Phase.Landing) Play(catchClip, catchVolume);
                }
                lastPhase = phase;
                phaseKnown = true;
            }
            else
            {
                phaseKnown = false;
            }

            // Döngü yumuşak açılıp kapanır (sarma durunca kesik kesik olmasın).
            reelLevel = Mathf.MoveTowards(reelLevel, target, dt / 0.25f * Mathf.Max(reelVolume, 0.01f));
            if (reel.clip != null)
            {
                if (reelLevel > 0.001f && !reel.isPlaying)
                {
                    reel.time = Random.Range(0f, reel.clip.length);
                    reel.Play();
                }
                else if (reelLevel <= 0.001f && reel.isPlaying)
                {
                    reel.Stop();
                }
            }
            reel.volume = reelLevel;
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip != null) effects.PlayOneShot(clip, volume);
        }

        void HandleGuest(VisitorType guest, bool isNew)
        {
            bool legendary = door != null && door.Venue.Legendary == guest;
            Play(legendary ? legendClip : chimeClip, legendary ? legendVolume : chimeVolume);
        }

        void HandleCaught(FishSpecies species, float kg, bool isNew)
        {
            if (fishing != null && species == fishing.Spot.Legendary) Play(legendClip, legendVolume);
        }
    }
}
