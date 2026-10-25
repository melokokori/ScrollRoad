using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrollRoad
{
    // Etkinlikleri tanıtma (2026-10-25): 10 ortamın hepsi baştan açık, yeni oyuncu Yol'dan başlar ve diğerlerini
    // kendisi bulmalı. Hiç girilmemiş ortamlar "yeni" sayılır (Activities kartında ve çekmece satırında işaret);
    // ilk rehber bittikten sonra yalnız Yol'da birkaç dakika aktif oynayınca bir kez "Activities" ipucu duyurulur;
    // bir ortama ilk girişte o ortamın ne olduğu tek satırla duyurulur. Kayıt yerel PlayerPrefs JSON.
    // Bu sürümden önce oynanmış ortamlar (içerik puanı > 0) zaten bilinir: duyurusuz "girilmiş" sayılır.
    public class ActivityGuide : MonoBehaviour
    {
        [SerializeField] NoticeController notice;
        [SerializeField] float hintActiveSeconds = 480f; // Yol'da bu kadar aktif oynanınca Activities ipucu

        [Serializable]
        class GuideState
        {
            public bool migrated;
            public bool hinted;
            public List<int> visited = new List<int>();
        }

        const string SaveKey = "ScrollRoad_ActivityGuide";

        static GuideState state;
        float activeSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => state = null;

        static GuideState State
        {
            get
            {
                if (state != null) return state;
                state = SaveService.LoadJson<GuideState>(SaveKey);
                if (state.visited == null) state.visited = new List<int>();
                return state;
            }
        }

        public static bool IsNew(EnvironmentId id) => !State.visited.Contains((int)id);

        public static int NewCount
        {
            get
            {
                int n = 0;
                foreach (var def in EnvironmentCatalog.All) if (IsNew(def.Id)) n++;
                return n;
            }
        }

        void Start()
        {
            // Ortam denetleyicileri Awake'te kaydolur; taşıma onların puanına bakar.
            var s = State;
            if (!s.migrated)
            {
                foreach (var def in EnvironmentCatalog.All)
                {
                    var c = EnvironmentContent.Get(def.Id);
                    if (c != null && c.LifetimePoints > 0 && !s.visited.Contains((int)def.Id)) s.visited.Add((int)def.Id);
                }
                // Activities'i zaten kullanmış oyuncuya ipucu gösterme. Yol herkesin başladığı yer.
                if (s.visited.Count > 0) s.hinted = true;
                if (!s.visited.Contains((int)EnvironmentId.Road)) s.visited.Add((int)EnvironmentId.Road);
                s.migrated = true;
            }
            Visit(EnvironmentManager.Current, announce: false);
            Save();
            EnvironmentManager.Changed += HandleChanged;
        }

        void OnDestroy() => EnvironmentManager.Changed -= HandleChanged;

        void HandleChanged(EnvironmentDef def)
        {
            if (Visit(def, announce: true)) Save();
        }

        bool Visit(EnvironmentDef def, bool announce)
        {
            var s = State;
            if (s.visited.Contains((int)def.Id)) return false;
            s.visited.Add((int)def.Id);
            s.hinted = true; // Activities'i bulmuş
            if (announce && notice != null && def.Id != EnvironmentId.Road)
            {
                notice.Announce(def.Name, $"{def.Tagline} Rare finds fill the shelf below.", $"{def.Name} · new activity");
            }
            return true;
        }

        void Update()
        {
            var s = State;
            if (s.hinted || notice == null) return;
            var engine = ScoreEngine.Instance;
            if (engine == null || !engine.IsUserActive) return;
            activeSeconds += Time.unscaledDeltaTime;
            if (activeSeconds < hintActiveSeconds) return;
            if (!SaveService.LoadOnboardingDone())
            {
                activeSeconds = hintActiveSeconds - 30f; // ilk rehber sürüyor: yarım dakika sonra yeniden bak
                return;
            }
            s.hinted = true;
            Save();
            int others = EnvironmentCatalog.All.Length - 1;
            notice.Announce("Try something new", $"≡ → Activities has {others} more things that move while you scroll.",
                $"≡ Activities: {others} more to try");
        }

        static void Save() => SaveService.SaveJson(SaveKey, State);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // YALNIZ test: hiç girilmemiş gibi başla.
        public static void DebugReset()
        {
            state = new GuideState { migrated = true };
            Save();
        }
#endif
    }
}
