using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Bir ortam sesinin ipucu (IntervalSound): konum her `Cycle` puanlık çevrimin `At` noktasını geçince çalar.
    // Cycle ≤ 0 → ortamın birim aralığı (öğe çevrimi).
    public struct SoundCue
    {
        public double Cycle;
        public float At;
        public string Clip;
        public float Volume;
        public bool OnRewind; // geri sararken de çal
    }

    // Eşit aralıklı ortamların ortak döngüsü (2026-10-14; Rüzgârgülü'nün WindController'ının genellemesi):
    // yukarı scroll konumu ilerletir, her EnvironmentDef aralığında bir öğe biter (yıl, hedef, çömlek, meyve,
    // porsiyon); aşağı scroll konumu geri alır, son biten öğeler geri gider ve sayaç düşer (ay rekoru
    // aşılmadan aynı öğe iki kez sayılmaz). Rölantide ScoreEngine'in yavaş temposu. Başarısızlık yok.
    // Sahne konumun saf fonksiyonu (Units / Phase); ayın i. öğesinin kimliği sabit (IntervalCatalog.Identify).
    // Sayaç aylık, defter kalıcı. Yalnız kendi ortamı seçiliyken ilerler.
    //
    // Alt sınıf yalnız verisini (EnvironmentDef, mekânlar, kayıt anahtarı) ve oyuncu metinlerini verir;
    // HUD (IntervalHudController), defter (IntervalLogController), şerit, tepsi ipucu ve ses
    // (IntervalSound) ortaktır.
    public abstract class IntervalEnvironment : MonoBehaviour, IEnvironmentContent
    {
        [SerializeField] NoticeController notice;
        [SerializeField] float autosaveIntervalSeconds = 10f;
        [SerializeField] int maxPerFrame = 50; // yalnız hata/test sıçramalarına karşı

        protected IntervalProgress state;
        float autosaveTimer;

        static readonly Dictionary<EnvironmentId, IntervalEnvironment> Registry = new Dictionary<EnvironmentId, IntervalEnvironment>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Registry.Clear();
            AnySpecial = null;
        }

        // Seçili ortam bu türdense onun denetleyicisi, değilse null.
        public static IntervalEnvironment Current =>
            Registry.TryGetValue(EnvironmentManager.Current.Id, out var env) && env != null ? env : null;

        public static IntervalEnvironment Get(EnvironmentId id) => Registry.TryGetValue(id, out var env) ? env : null;

        // Herhangi bir ortamda nadir öğe geldi (ses için): ortam, öğe, yeni mi.
        public static event Action<IntervalEnvironment, ItemType, bool> AnySpecial;

        // ---- alt sınıfın verisi
        public abstract EnvironmentDef Def { get; }
        protected abstract string SaveKey { get; }   // PlayerPrefs anahtarı
        protected abstract string HashKey { get; }   // kimlik hash'inin tuzu
        public abstract ItemSite[] Sites { get; }

        // ---- oyuncu metinleri (İngilizce)
        public abstract string TotalLabel { get; }   // sol üst plaka: "ELAPSED"
        public abstract string StatusActive { get; } // "TURNING"
        public abstract string StatusIdle { get; }   // "IDLE · SLOW DAYS"
        public abstract string StatusBack { get; }   // "TIME REWINDS"
        public abstract string RarePhrase { get; }   // tabela, sıradaki nadirse: "Raising"
        public abstract string NextLine(long index, EnvItem item); // tabela, yakında nadir yoksa
        public abstract string CollectionShort { get; } // sol alt: "CHRONICLE"
        public abstract string LogNoun { get; }         // defter sayımı: "wonders"
        public abstract string MetDetail(ItemRecord record);
        public abstract string UnmetDetail { get; }
        protected abstract void AnnounceNew(NoticeController notice, ItemType type);

        // Ortama özel değer (mesafe, kabuk uzunluğu); 0 = yok. Rekor ve defterde "best" olarak tutulur.
        public virtual float ItemValue(EnvItem item) => 0f;
        public virtual string FormatValue(float value) => null;
        public virtual string RecordLabel => null; // "LONGEST THROW"

        // ---- ses (IntervalSound)
        public virtual SoundCue[] Cues => Array.Empty<SoundCue>();
        public virtual string LoopClip => null;      // yalnız kullanıcı aktifken çalan döngü
        public virtual float LoopVolume => 0.28f;

        // ---- durum
        public IntervalProgress Progress => state;
        public double Interval => 1.0 / Def.UnitsPerPoint;
        public double Position => state.position;
        public double Units => state.position / Interval;
        public long Done => (long)Math.Floor(Units + 1e-9);
        public float Phase => (float)(Units - Done); // 0..1: sıradaki öğenin ilerlemesi
        // + ilerliyor, - geri, 0 duruyor.
        public float Direction { get; private set; }

        public ItemSite Site
        {
            get
            {
                foreach (var s in Sites) if (s.Id == state.site) return s;
                return Sites[0];
            }
        }

        public ItemType LastSpecial => state == null ? null : Site.GetSpecial(state.lastSpecialId);

        public EnvItem Identify(long index) => IntervalCatalog.Identify(state.month, HashKey, Site, index, EnvironmentContent.FocusOf(Def.Id),
            EnvironmentContent.GuaranteeIndex(Interval, state.log.Count == 0, state.guaranteeMonth, state.month));

        public int LoggedAt(ItemSite site)
        {
            int n = 0;
            foreach (var t in site.Specials) if (state.Find(t.Id) != null) n++;
            return n;
        }

        // ---- IEnvironmentContent (Ortam Standardı v2)
        VenueInfo[] venueInfos;

        public double LifetimePoints => state.total * Interval;
        public virtual string VenueNoun => "place";
        public VenueInfo[] Venues => venueInfos ??= Array.ConvertAll(Sites, s => new VenueInfo { Id = s.Id, Name = s.Name });
        public string VenueId => Site.Id;
        public void SetVenue(string id)
        {
            if (Array.FindIndex(Sites, s => s.Id == id) < 0 || state.site == id) return;
            state.site = id;
            Save();
        }
        public RareInfo[] RaresAt(string venueId)
        {
            var site = Array.Find(Sites, s => s.Id == venueId) ?? Sites[0];
            return Array.ConvertAll(site.Specials, t => new RareInfo { Id = t.Id, Name = t.Name, Frequency = t.Frequency });
        }
        public bool IsLogged(string rareId) => state.Find(rareId) != null;
        public int TimesFound(string rareId) => state.Find(rareId)?.count ?? 0;
        public virtual string SkinNoun => "look";
        public virtual string SkinBoxName => "Looks";
        public virtual SkinInfo[] Skins => EnvironmentContent.ClassicOnly;
        public virtual string SkinId => "classic";
        public virtual void SetSkin(string id) { }
        public IEnumerable<string> LegacyAnnounced => Array.Empty<string>();

        public event Action<long, EnvItem> ItemCompleted;
        public event Action<ItemType, bool> SpecialCompleted;

        protected virtual void Awake()
        {
            Registry[Def.Id] = this;
            state = SaveService.LoadJson<IntervalProgress>(SaveKey);
            if (state.log == null) state.log = new List<ItemRecord>();
            if (CheckMonth()) Save();
            EnvironmentContent.Register(this);
        }

        protected virtual void OnDestroy()
        {
            Save();
            EnvironmentContent.Unregister(this);
            if (Registry.TryGetValue(Def.Id, out var env) && env == this) Registry.Remove(Def.Id);
        }

        void OnApplicationQuit() => Save();

        void Save()
        {
            if (state != null) SaveService.SaveJson(SaveKey, state);
        }

        protected virtual void Update()
        {
            var engine = ScoreEngine.Instance;
            bool active = EnvironmentManager.Current.Id == Def.Id;
            if (engine == null || !active || Time.deltaTime <= 0f)
            {
                Direction = 0f;
                return;
            }

            if (CheckMonth()) Save();

            Direction = engine.Pace > 1f ? 1f : engine.Pace < -1f ? -1f : 0f;
            state.position = Math.Max(0.0, state.position + engine.FrameRawPoints);

            long done = Done;
            for (int n = 0; state.monthBest < done && n < maxPerFrame; n++)
            {
                state.monthBest++;
                Complete(state.monthBest);
            }

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autosaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                Save();
            }
        }

        bool CheckMonth()
        {
            string month = DateTimeOffset.UtcNow.ToString("yyyy-MM");
            if (state.month == month) return false;
            state.month = month;
            state.position = 0;
            state.monthBest = 0;
            return true;
        }

        // Ayın index. öğesi ilk kez bitti.
        void Complete(long index)
        {
            state.total++;
            var item = Identify(index);
            float value = ItemValue(item);
            if (value > state.best)
            {
                state.best = value;
                state.bestId = item.Type.Id;
            }
            ItemCompleted?.Invoke(index, item);
            if (!item.IsSpecial) return;

            var type = item.Type;
            var record = state.Find(type.Id);
            bool isNew = record == null;
            if (isNew)
            {
                record = new ItemRecord { id = type.Id, firstSeen = DateTime.Now.ToString("yyyy-MM-dd") };
                state.log.Add(record);
                if (string.IsNullOrEmpty(state.guaranteeMonth)) state.guaranteeMonth = state.month;
            }
            record.count++;
            if (value > record.best) record.best = value;
            state.lastSpecialId = type.Id;
            Save();

            SpecialCompleted?.Invoke(type, isNew);
            AnySpecial?.Invoke(this, type, isNew);
            if (isNew && notice != null) AnnounceNew(notice, type);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: defteri ve ayın sayacını sıfırla.
        public void DebugReset()
        {
            state = new IntervalProgress();
            CheckMonth();
            Save();
        }

        // YALNIZ test (inceleme sürücüsü): bu ayın konumunu birim cinsinden ayarla (sayım/defter değişmez).
        public void DebugSetUnits(double units)
        {
            state.position = Math.Max(0.0, units * Interval);
            state.monthBest = Math.Max(state.monthBest, Done);
        }
#endif
    }
}
