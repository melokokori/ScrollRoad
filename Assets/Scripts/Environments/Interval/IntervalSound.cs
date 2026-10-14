using UnityEngine;

namespace ScrollRoad
{
    // Eşit aralıklı ortamların sesleri (2026-10-14; EnvironmentSound'un kalıbı: Tools/gen_sound.py --env →
    // Resources/EnvSounds/<ad>.ogg). Yalnız seçili ortamın sesleri çalar; görev çubuğu/tepside susma ve ana
    // ses ayarı SoundController'da (dinleyici sesi).
    //  - İpuçları (IntervalEnvironment.Cues): konum bir çevrimin belli noktasını geçince tek seferlik ses
    //    (atış, vuruş, çömleğin rafa konması...). Büyük sıçramalarda (ay sıfırlanması, yükleme) çalmaz.
    //  - Döngü (LoopClip): yalnız kullanıcı aktifken (son scroll'dan 3 dk) ve konum değişirken; perde/şiddet
    //    scroll hızına göre — rölantide saatlerce aynı ses dönmesin (Olta'nın makara kuralı).
    //  - Nadir öğe gelince ince çan (guest_chime).
    // Ses dosyası yoksa o ses sessizce atlanır.
    public class IntervalSound : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float chimeVolume = 0.3f;

        AudioSource loop;
        AudioSource effects;
        AudioClip chimeClip;

        IntervalEnvironment tracked;
        long[] lastCycle = new long[0];
        AudioClip[] cueClips = new AudioClip[0];
        string loopName;
        float loopLevel;
        float smoothedSpeed;

        static AudioClip Load(string name) => string.IsNullOrEmpty(name) ? null : Resources.Load<AudioClip>("EnvSounds/" + name);

        void Awake()
        {
            loop = gameObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            loop.volume = 0f;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            chimeClip = Load("guest_chime");
        }

        void OnEnable() => IntervalEnvironment.AnySpecial += HandleSpecial;
        void OnDisable() => IntervalEnvironment.AnySpecial -= HandleSpecial;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var engine = ScoreEngine.Instance;
            float speed = engine != null ? Mathf.Abs(engine.Pace) : 0f;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-dt / 0.25f));
            float t = Mathf.Sqrt(Mathf.Clamp01(smoothedSpeed / 220f));
            float active = Mathf.Clamp01(smoothedSpeed / 60f); // rölantide (10) kısık, seyirde (60) tam

            var env = IntervalEnvironment.Current;
            if (env != tracked) Track(env);

            float target = 0f;
            if (env != null)
            {
                var cues = env.Cues;
                for (int i = 0; i < cues.Length && i < lastCycle.Length; i++)
                {
                    double cycle = cues[i].Cycle > 0 ? cues[i].Cycle : env.Interval;
                    long k = (long)System.Math.Floor(env.Position / cycle - cues[i].At);
                    long jump = k - lastCycle[i];
                    bool first = lastCycle[i] == long.MinValue;
                    lastCycle[i] = k;
                    if (first || jump == 0 || System.Math.Abs(jump) > 3) continue;
                    if (jump < 0 && !cues[i].OnRewind) continue;
                    if (cueClips[i] != null)
                    {
                        effects.pitch = 1f;
                        effects.PlayOneShot(cueClips[i], cues[i].Volume * Mathf.Lerp(0.55f, 1f, active));
                    }
                }

                bool userActive = engine != null && engine.IsUserActive;
                if (loop.clip != null && userActive && env.Direction != 0f)
                {
                    target = env.LoopVolume * Mathf.Lerp(0.4f, 1f, active);
                    loop.pitch = Mathf.Lerp(0.8f, 1.35f, t) * (env.Direction < 0f ? 0.85f : 1f);
                }
            }

            // Döngü yumuşak açılıp kapanır.
            loopLevel = Mathf.MoveTowards(loopLevel, target, dt / 0.25f * 0.3f);
            if (loop.clip != null)
            {
                if (loopLevel > 0.001f && !loop.isPlaying)
                {
                    loop.time = Random.Range(0f, loop.clip.length);
                    loop.Play();
                }
                else if (loopLevel <= 0.001f && loop.isPlaying)
                {
                    loop.Stop();
                }
            }
            loop.volume = loopLevel;
        }

        void Track(IntervalEnvironment env)
        {
            tracked = env;
            var cues = env != null ? env.Cues : new SoundCue[0];
            lastCycle = new long[cues.Length];
            cueClips = new AudioClip[cues.Length];
            for (int i = 0; i < cues.Length; i++)
            {
                lastCycle[i] = long.MinValue;
                cueClips[i] = Load(cues[i].Clip);
            }
            string name = env != null ? env.LoopClip : null;
            if (name != loopName)
            {
                loopName = name;
                if (loop.isPlaying) loop.Stop();
                loopLevel = 0f;
                loop.clip = Load(name);
            }
        }

        void HandleSpecial(IntervalEnvironment env, ItemType type, bool isNew)
        {
            if (env == IntervalEnvironment.Current && chimeClip != null) effects.PlayOneShot(chimeClip, chimeVolume);
        }
    }
}
