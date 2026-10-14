using UnityEngine;

namespace ScrollRoad
{
    // Plak tablasının müziği (2026-10-14): TELİFSİZ, PROSEDÜREL — hiçbir kayıt/örnek kullanılmaz, müzik
    // OnAudioFilterRead ile gerçek zamanlı sentezlenir. Parça = akor dizisi (4 ölçü, döngü) + bas + davul
    // (kick/snare/hi-hat gürültüden) + tuşlu/arpej/melodi; hepsi sinüs/üçgen/yumuşak kare ve gürültüden.
    // Tarz (tempo, swing, akor kümesi, davul ve bas kalıbı, tını) plağın türünden (RecordCatalog.MusicStyle);
    // akorlar, ton ve melodi parçanın tohumundan (ay + parça sırası) — her parça farklı, geri sarılıp yeniden
    // çalınan parça aynısı. Üstüne hafif plak cızırtısı ve alçak geçiren filtre.
    //
    // Scroll'a bağlılık: tempo = tarzın temposu × (tempo / seyir) — seyirde normal, boost'ta en çok %160,
    // rölantide %40 (ve kısık). Gerçek pikap hissi için perde de hafifçe kayar (±%8'e kadar). Geri dönüşte
    // müzik susar, bir kez "scratch" çalar. Parçalar arasında kısa sessizlik. Yalnız Plak tablası seçiliyken.
    // DİKKAT (14.10, build'de ölçüldü): OnAudioFilterRead'in ürettiği sese AudioListener.volume UYGULANMIYOR —
    // şeritte/tepside de tam seste çalıyordu. Bu yüzden dinleyici sesi (ana ses × kart modu, SoundController
    // yönetir) her karede okunup sentezin çıkışına burada elle çarpılır.
    public class VinylMusic : MonoBehaviour
    {
        [SerializeField] VinylController vinyl;
        [SerializeField] AudioClip scratchClip;
        // Ölçüldü (14.10, çevrimdışı çizim): 0,16 + tarz düzeyleriyle her tarz ~-30 LUFS — araç döngülerinin
        // oyundaki etkin düzeyiyle aynı; oyun saatlerce arka planda açık kaldığı için arka plan müziği seviyesi.
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.16f;
        [SerializeField, Range(0f, 1f)] float scratchVolume = 0.3f;

        // ---------------------------------------------------------------- parça tarifi (ana iş parçacığı kurar)

        sealed class Song
        {
            public float Bpm, Swing, Cutoff, Crackle;
            public float Level = 1f; // tarzlar arası ses yüksekliği dengesi
            public MusicStyle Style;
            public int[][] Chords = new int[4][];   // ölçü başına akor sesleri (midi)
            public int[] Roots = new int[4];         // ölçü başına bas kökü (midi)
            public int[] Melody = new int[32];       // 4 ölçü × 8 adım, -1 = yok
            public float[] Kick = new float[8], Snare = new float[8], Hat = new float[8];
            public int[] Bass = new int[8];          // 0 yok, 1 kök, 2 beşli, 3 oktav, 4 üçlü, 5 yaklaşma
            public bool[] Keys = new bool[8];        // akor vuruşu
            public bool Arp, Pad, OpenHat, Rim;
            public float KeysDecay, KeysLevel, MelodyLevel;
            public int KeysWave;                     // 0 elektrik piyano, 1 tel (üçgen), 2 klavinet (yumuşak kare)
        }

        static readonly int[] Maj7 = { 0, 4, 7, 11 }, Min7 = { 0, 3, 7, 10 }, Dom7 = { 0, 4, 7, 10 }, Maj = { 0, 4, 7 }, Min = { 0, 3, 7 };
        static readonly int[] MajorPent = { 0, 2, 4, 7, 9 }, MinorPent = { 0, 3, 5, 7, 10 };

        // (derece, nitelik) dörtlüleri
        static readonly (int, int[])[][] LoFiProg =
        {
            new[] { (2, Min7), (7, Dom7), (0, Maj7), (9, Min7) },
            new[] { (0, Maj7), (9, Min7), (5, Maj7), (7, Dom7) },
            new[] { (9, Min7), (5, Maj7), (0, Maj7), (7, Dom7) },
            new[] { (4, Min7), (9, Min7), (2, Min7), (7, Dom7) },
        };
        static readonly (int, int[])[][] JazzProg =
        {
            new[] { (2, Min7), (7, Dom7), (0, Maj7), (0, Maj7) },
            new[] { (0, Maj7), (9, Dom7), (2, Min7), (7, Dom7) },
            new[] { (4, Min7), (9, Dom7), (2, Min7), (7, Dom7) },
        };
        static readonly (int, int[])[][] MinorProg =
        {
            new[] { (0, Min), (8, Maj), (3, Maj), (10, Maj) },
            new[] { (0, Min), (5, Min), (8, Maj), (10, Maj) },
            new[] { (0, Min), (10, Maj), (8, Maj), (10, Maj) },
        };
        static readonly (int, int[])[][] DiscoProg =
        {
            new[] { (0, Min7), (5, Dom7), (0, Min7), (5, Dom7) },
            new[] { (0, Min7), (10, Maj), (8, Maj7), (10, Maj) },
        };
        static readonly (int, int[])[][] FunkProg =
        {
            new[] { (0, Dom7), (0, Dom7), (5, Dom7), (0, Dom7) },
            new[] { (0, Min7), (5, Dom7), (0, Min7), (10, Dom7) },
        };

        static Song BuildSong(MusicStyle style, ulong seed)
        {
            var rng = new XorShift(seed);
            var s = new Song { Style = style, Cutoff = 4200f, Crackle = 0.35f, KeysDecay = 1.2f, KeysLevel = 1f, MelodyLevel = 0.8f };
            (int, int[])[][] progs;
            int[] scale;
            float melodyDensity;
            switch (style)
            {
                case MusicStyle.Bossa:
                    s.Bpm = 126f; progs = JazzProg; scale = MajorPent; melodyDensity = 0.3f;
                    Set(s.Kick, (0, 0.7f), (3, 0.5f), (4, 0.7f), (7, 0.5f));
                    Set(s.Snare, (0, 0.5f), (3, 0.5f), (6, 0.5f));
                    for (int i = 0; i < 8; i++) s.Hat[i] = 0.22f;
                    SetB(s.Bass, (0, 1), (3, 2), (4, 1), (7, 2));
                    SetK(s.Keys, 1, 3, 4, 6);
                    s.KeysWave = 1; s.KeysDecay = 0.35f; s.Rim = true; s.Level = 0.85f;
                    break;
                case MusicStyle.Synthwave:
                    s.Bpm = 100f; progs = MinorProg; scale = MinorPent; melodyDensity = 0.0f;
                    Set(s.Kick, (0, 1f), (4, 1f));
                    Set(s.Snare, (2, 0.9f), (6, 0.9f));
                    for (int i = 0; i < 8; i++) s.Hat[i] = 0.28f;
                    for (int i = 0; i < 8; i++) s.Bass[i] = 1;
                    s.Arp = s.Pad = true; s.KeysWave = 2; s.KeysDecay = 0.22f; s.KeysLevel = 0.6f; s.Level = 0.9f;
                    break;
                case MusicStyle.Jazz:
                    s.Bpm = 132f; s.Swing = 1f; progs = JazzProg; scale = MajorPent; melodyDensity = 0.35f;
                    Set(s.Kick, (0, 0.35f));
                    Set(s.Snare, (6, 0.25f));
                    Set(s.Hat, (0, 0.45f), (2, 0.45f), (3, 0.3f), (4, 0.45f), (6, 0.45f), (7, 0.3f));
                    SetB(s.Bass, (0, 1), (2, 4), (4, 2), (6, 5));
                    SetK(s.Keys, 3, 7);
                    s.KeysWave = 0; s.KeysDecay = 0.6f; s.Crackle = 0.6f; s.Cutoff = 3600f; s.Level = 0.8f;
                    break;
                case MusicStyle.Disco:
                    s.Bpm = 116f; progs = DiscoProg; scale = MinorPent; melodyDensity = 0.15f;
                    Set(s.Kick, (0, 1f), (2, 1f), (4, 1f), (6, 1f));
                    Set(s.Snare, (2, 0.8f), (6, 0.8f));
                    Set(s.Hat, (1, 0.5f), (3, 0.5f), (5, 0.5f), (7, 0.5f));
                    for (int i = 0; i < 8; i++) s.Bass[i] = i % 2 == 0 ? 1 : 3;
                    SetK(s.Keys, 1, 5);
                    s.OpenHat = true; s.KeysWave = 1; s.KeysDecay = 0.25f; s.Level = 0.6f;
                    break;
                case MusicStyle.Funk:
                    s.Bpm = 100f; s.Swing = 0.25f; progs = FunkProg; scale = MinorPent; melodyDensity = 0.12f;
                    Set(s.Kick, (0, 1f), (3, 0.7f), (5, 0.8f));
                    Set(s.Snare, (2, 1f), (6, 1f));
                    for (int i = 0; i < 8; i++) s.Hat[i] = 0.32f;
                    SetB(s.Bass, (0, 1), (3, 1), (4, 3), (6, 2), (7, 1));
                    SetK(s.Keys, 1, 3, 6);
                    s.KeysWave = 2; s.KeysDecay = 0.12f; s.Level = 0.85f;
                    break;
                default: // Lo-fi
                    s.Bpm = 80f; s.Swing = 0.55f; progs = LoFiProg; scale = MajorPent; melodyDensity = 0.3f;
                    Set(s.Kick, (0, 1f), (5, 0.8f));
                    Set(s.Snare, (2, 0.75f), (6, 0.75f));
                    for (int i = 0; i < 8; i++) s.Hat[i] = i % 2 == 0 ? 0.3f : 0.2f;
                    SetB(s.Bass, (0, 1), (5, 2));
                    SetK(s.Keys, 0);
                    s.KeysWave = 0; s.KeysDecay = 1.6f; s.Cutoff = 2300f; s.Crackle = 1f; s.Level = 1.2f;
                    break;
            }
            // Tempo parçadan parçaya biraz oynasın.
            s.Bpm *= 0.95f + (float)rng.Next01() * 0.1f;

            int key = 55 + (int)(rng.Next01() * 8); // G3..D4
            var prog = progs[(int)(rng.Next01() * progs.Length) % progs.Length];
            for (int bar = 0; bar < 4; bar++)
            {
                var (degree, quality) = prog[bar];
                int root = key + degree;
                if (root > 62) root -= 12;
                s.Roots[bar] = root - 24;
                var tones = new int[quality.Length];
                for (int t = 0; t < quality.Length; t++) tones[t] = root + quality[t];
                s.Chords[bar] = tones;
            }
            // Melodi: pentatonik, akorların bir oktav üstünde; yoğunluk tarzdan.
            int last = 2;
            for (int i = 0; i < 32; i++)
            {
                s.Melody[i] = -1;
                if (rng.Next01() >= melodyDensity) continue;
                last = Mathf.Clamp(last + (int)(rng.Next01() * 5) - 2, 0, scale.Length * 2 - 1);
                s.Melody[i] = key + 12 + scale[last % scale.Length] + 12 * (last / scale.Length);
            }
            return s;
        }

        static void Set(float[] a, params (int, float)[] v) { foreach (var (i, x) in v) a[i] = x; }
        static void SetB(int[] a, params (int, int)[] v) { foreach (var (i, x) in v) a[i] = x; }
        static void SetK(bool[] a, params int[] v) { foreach (var i in v) a[i] = true; }

        struct XorShift
        {
            ulong s;
            public XorShift(ulong seed) { s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }
            public double Next01()
            {
                s ^= s << 13; s ^= s >> 7; s ^= s << 17;
                return (s >> 11) * (1.0 / (1UL << 53));
            }
        }

        // ---------------------------------------------------------------- ana iş parçacığı

        AudioSource source;
        AudioSource fx;
        int sampleRate = 48000;
        ulong songSeed;
        float smoothedSpeed;
        bool wasBacking;
        float lastScratch = -10f;

        // Ana → ses iş parçacığı.
        volatile Song song;
        volatile float tempoFactor = 1f, pitchFactor = 1f, targetGain, listenerVolume;

        void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            // Filtre zinciri yalnız kaynak çalarken işler: sessiz bir döngü klibi çalınır, ses OnAudioFilterRead'de üretilir.
            source.clip = AudioClip.Create("vinyl_silence", sampleRate, 1, sampleRate, false);
            // Scratch ayrı çocuk nesnede: aynı nesnede ikinci AudioSource, OnAudioFilterRead uyarısı verir.
            var fxObject = new GameObject("VinylScratch");
            fxObject.transform.SetParent(transform, false);
            fx = fxObject.AddComponent<AudioSource>();
            fx.playOnAwake = false;
        }

        void Start() => source.Play();

        void Update()
        {
            listenerVolume = AudioListener.volume;
            var engine = ScoreEngine.Instance;
            bool on = vinyl != null && engine != null && EnvironmentManager.Current.Id == EnvironmentId.Vinyl;
            if (!on)
            {
                targetGain = 0f;
                wasBacking = false;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, engine.Pace, 1f - Mathf.Exp(-dt / 0.3f));
            float tf = Mathf.Clamp(Mathf.Abs(smoothedSpeed) / Mathf.Max(1f, engine.CruisePace), 0.4f, 1.6f);
            tempoFactor = tf;
            pitchFactor = Mathf.Clamp(1f + (tf - 1f) * 0.13f, 0.92f, 1.08f);

            // Parça değiştiyse yeni tarif (tohum = ay + parça sırası, tarz = plağın türü).
            ulong seed = vinyl.CurrentTrackSeed;
            if (seed != songSeed || song == null)
            {
                songSeed = seed;
                song = BuildSong(vinyl.Current.Style, seed);
            }

            bool backing = vinyl.SpinDirection < 0f;
            if (backing && !wasBacking && Time.unscaledTime - lastScratch > 1.5f && scratchClip != null)
            {
                fx.PlayOneShot(scratchClip, scratchVolume);
                lastScratch = Time.unscaledTime;
            }
            wasBacking = backing;

            // Parça başı ve sonunda kısa sessizlik (iğne parçalar arası boşlukta).
            float p = vinyl.TrackProgress;
            bool gap = p < 0.012f || p > 0.988f;
            bool playing = !backing && engine.Pace > 0.5f && !gap;
            targetGain = playing ? (engine.IsUserActive ? 1f : 0.55f) : 0f;
        }

        // ---------------------------------------------------------------- ses iş parçacığı

        const int VoiceCount = 24;
        readonly float[] vFreq = new float[VoiceCount], vPhase = new float[VoiceCount], vAmp = new float[VoiceCount];
        readonly float[] vEnv = new float[VoiceCount], vDecay = new float[VoiceCount], vAttack = new float[VoiceCount], vAtk = new float[VoiceCount];
        readonly int[] vWave = new int[VoiceCount];
        int nextVoice;

        Song playingSong;
        double beat;
        long lastStep = -1;
        float gain, lp;
        float kickEnv, kickPitch, kickPhase, kickAmp;
        float snEnv, snTone, snAmp;
        float hatEnv, hatAmp, hatDecay;
        float popEnv, popAmp;
        float prevNoise;
        uint noiseState = 22222u;

        float Noise()
        {
            noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
            return (noiseState & 0xFFFFFF) / (float)0x7FFFFF - 1f;
        }

        static float MidiHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        void Voice(float hz, float amp, float decaySeconds, float attackSeconds, int wave)
        {
            int i = nextVoice;
            nextVoice = (nextVoice + 1) % VoiceCount;
            vFreq[i] = hz;
            vPhase[i] = 0f;
            vAmp[i] = amp;
            vEnv[i] = 1f;
            vDecay[i] = Mathf.Exp(-1f / (Mathf.Max(0.01f, decaySeconds) * sampleRate));
            vAtk[i] = attackSeconds <= 0f ? 1f : 0f;
            vAttack[i] = attackSeconds <= 0f ? 1f : 1f / (attackSeconds * sampleRate);
            vWave[i] = wave;
        }

        void Trigger(Song s, long step, float pitch)
        {
            int bar = (int)((step / 8) % 4);
            int k = (int)(step % 8);
            var chord = s.Chords[bar];

            if (s.Kick[k] > 0f) { kickEnv = 1f; kickPitch = 1f; kickAmp = s.Kick[k]; kickPhase = 0f; }
            if (s.Snare[k] > 0f) { snEnv = 1f; snAmp = s.Snare[k]; snTone = 0f; }
            if (s.Hat[k] > 0f) { hatEnv = 1f; hatAmp = s.Hat[k]; hatDecay = s.OpenHat && k % 2 == 1 ? 0.09f : 0.03f; }

            int bassNote = s.Bass[k] switch
            {
                1 => s.Roots[bar],
                2 => s.Roots[bar] + 7,
                3 => s.Roots[bar] + 12,
                4 => s.Roots[bar] + (chord.Length > 1 ? chord[1] - chord[0] : 4),
                5 => s.Roots[(bar + 1) % 4] - 1,
                _ => -1,
            };
            if (bassNote > 0) Voice(MidiHz(bassNote) * pitch, 0.55f, s.Style == MusicStyle.Jazz ? 0.5f : 0.35f, 0.004f, 3);

            if (s.Pad && k == 0)
                foreach (var n in chord) Voice(MidiHz(n) * pitch, 0.09f, 2.8f, 0.25f, 1);
            if (s.Keys[k])
                foreach (var n in chord) Voice(MidiHz(n) * pitch, 0.13f * s.KeysLevel, s.KeysDecay, 0.003f, s.KeysWave);
            if (s.Arp)
            {
                int n = chord[k % chord.Length] + (k >= 4 ? 12 : 0);
                Voice(MidiHz(n) * pitch, 0.07f, s.KeysDecay, 0.002f, 2);
            }
            int m = s.Melody[bar * 8 + k];
            if (m > 0) Voice(MidiHz(m) * pitch, 0.1f * s.MelodyLevel, 0.5f, 0.004f, s.Style == MusicStyle.Bossa ? 1 : 0);
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            var s = song;
            float target = targetGain * listenerVolume;
            if (s == null || (target <= 0f && gain < 0.0001f))
            {
                System.Array.Clear(data, 0, data.Length);
                gain = 0f;
                return;
            }
            if (s != playingSong)
            {
                playingSong = s;
                beat = 0;
                lastStep = -1;
            }

            float sr = sampleRate;
            float tf = tempoFactor, pitch = pitchFactor;
            double beatInc = s.Bpm * tf / 60.0 / sr;
            double split = 0.5 + s.Swing * 0.1667;
            float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * s.Cutoff / sr);
            float kickDecay = Mathf.Exp(-1f / (0.18f * sr)), kickPitchDecay = Mathf.Exp(-1f / (0.035f * sr));
            float snDecay = Mathf.Exp(-1f / (0.11f * sr)), rimDecay = Mathf.Exp(-1f / (0.02f * sr));
            float hatD = Mathf.Exp(-1f / (Mathf.Max(0.01f, hatDecay) * sr));
            float popD = Mathf.Exp(-1f / (0.0015f * sr));
            float popChance = 2.5f * s.Crackle / sr;
            float twoPi = 2f * Mathf.PI;

            for (int i = 0; i < data.Length; i += channels)
            {
                beat += beatInc;
                long bi = (long)beat;
                double f = beat - bi;
                long step = bi * 2 + (f < split ? 0 : 1);
                if (step != lastStep)
                {
                    lastStep = step;
                    Trigger(s, step, pitch);
                    hatD = Mathf.Exp(-1f / (Mathf.Max(0.01f, hatDecay) * sr));
                }

                float x = 0f;
                for (int v = 0; v < VoiceCount; v++)
                {
                    float env = vEnv[v];
                    if (env < 0.0005f) continue;
                    float a = vAtk[v];
                    if (a < 1f) vAtk[v] = a = Mathf.Min(1f, a + vAttack[v]);
                    float ph = vPhase[v] + vFreq[v] / sr;
                    if (ph >= 1f) ph -= 1f;
                    vPhase[v] = ph;
                    float w;
                    switch (vWave[v])
                    {
                        case 1: w = 4f * Mathf.Abs(ph - 0.5f) - 1f; break;                                     // üçgen (tel/ped)
                        case 2: w = (float)System.Math.Tanh(Mathf.Sin(twoPi * ph) * 3f) * 0.7f; break;          // yumuşak kare
                        case 3: w = Mathf.Sin(twoPi * ph) + 0.25f * Mathf.Sin(2f * twoPi * ph); break;          // bas
                        default: w = Mathf.Sin(twoPi * ph) + 0.3f * Mathf.Sin(2f * twoPi * ph) * env; break;   // elektrik piyano
                    }
                    x += w * vAmp[v] * env * a;
                    vEnv[v] = env * vDecay[v];
                }

                // Davul.
                if (kickEnv > 0.0005f)
                {
                    float hz = (45f + 95f * kickPitch) * pitch;
                    kickPhase += hz / sr;
                    if (kickPhase >= 1f) kickPhase -= 1f;
                    x += Mathf.Sin(twoPi * kickPhase) * kickEnv * kickAmp * 0.7f;
                    kickEnv *= kickDecay;
                    kickPitch *= kickPitchDecay;
                }
                float noise = Noise();
                if (snEnv > 0.0005f)
                {
                    if (s.Rim)
                    {
                        snTone += 1700f * pitch / sr;
                        if (snTone >= 1f) snTone -= 1f;
                        x += Mathf.Sin(twoPi * snTone) * snEnv * snAmp * 0.18f;
                        snEnv *= rimDecay;
                    }
                    else
                    {
                        snTone += 190f * pitch / sr;
                        if (snTone >= 1f) snTone -= 1f;
                        x += (noise * 0.22f + Mathf.Sin(twoPi * snTone) * 0.12f) * snEnv * snAmp;
                        snEnv *= snDecay;
                    }
                }
                if (hatEnv > 0.0005f)
                {
                    x += (noise - prevNoise) * 0.5f * hatEnv * hatAmp * 0.12f;
                    hatEnv *= hatD;
                }
                prevNoise = noise;

                // Plak cızırtısı: seyrek tıkırtılar + çok hafif hışırtı.
                if (Noise() * 0.5f + 0.5f < popChance) { popEnv = 1f; popAmp = 0.04f + (Noise() * 0.5f + 0.5f) * 0.08f; }
                if (popEnv > 0.001f)
                {
                    x += noise * popEnv * popAmp;
                    popEnv *= popD;
                }
                x += noise * 0.0035f * s.Crackle;

                lp += lpA * (x - lp);
                gain += (target - gain) * 0.0006f;
                float y = (float)System.Math.Tanh(lp * 1.2f) * 0.8f * gain * musicVolume * s.Level;
                for (int c = 0; c < channels; c++) data[i + c] = y;
            }
        }
    }
}
