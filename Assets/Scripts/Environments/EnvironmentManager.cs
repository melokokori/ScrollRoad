using System;
using UnityEngine;

namespace ScrollRoad
{
    // Seçili ortam (2026-10-13). Yalnız seçili ortam ilerler; gizli puan her ortamda birikir.
    // Ortamlar gizli puanın ömür boyu toplamıyla açılır (EnvironmentDef.UnlockLifetimePoints); kilit
    // kaydedilmez, ömür boyu puandan türetilir. Seçim yerel PlayerPrefs. Her ortamın açıldığı bir kez
    // yeşil tabelayla duyurulur — eşiği bu sürümden önce geçmiş oyuncuya da (ilk açılışta) bir kez.
    [DefaultExecutionOrder(-150)]
    public class EnvironmentManager : MonoBehaviour
    {
        [SerializeField] NoticeController notice;

        bool checkedOnce;

        public static EnvironmentDef Current { get; private set; } = EnvironmentCatalog.Road;
        public static event Action<EnvironmentDef> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = EnvironmentCatalog.Road;
            Changed = null;
        }

        static double LifetimePoints => ScoreEngine.Instance != null
            ? ScoreEngine.Instance.LifetimePoints
            : SaveService.LoadLifetimePoints();

        public static bool IsUnlocked(EnvironmentDef def) =>
            def.UnlockLifetimePoints <= 0 || CarCatalog.ReviewUnlockAll || LifetimePoints >= def.UnlockLifetimePoints;

        // Kilide kalan puan (≥0). Oyuncuya aktif ortamın birimiyle yazılır.
        public static double PointsToUnlock(EnvironmentDef def) => Math.Max(0.0, def.UnlockLifetimePoints - LifetimePoints);

        // Kilit açılma ilerlemesi 0..1 (seçicideki çubuk).
        public static float UnlockProgress(EnvironmentDef def) =>
            def.UnlockLifetimePoints <= 0 ? 1f : Mathf.Clamp01((float)(LifetimePoints / def.UnlockLifetimePoints));

        public static bool Select(EnvironmentDef def)
        {
            if (def == null || !IsUnlocked(def)) return false;
            if (def == Current) return true;
            Current = def;
            SaveService.SaveEnvironment((int)def.Id);
            Changed?.Invoke(def);
            return true;
        }

        void Awake()
        {
            var def = EnvironmentCatalog.Get((EnvironmentId)SaveService.LoadEnvironment());
            Current = IsUnlocked(def) ? def : EnvironmentCatalog.Road;
        }

        void Update()
        {
            // Açılıştan 4 sn sonra başla (pencere kurulurken takılan ilk karelerde tabela görünmeden
            // bitiyordu), sonra ~yarım saniyede bir yeter.
            if (Time.realtimeSinceStartup < 4f) return;
            if (checkedOnce && Time.frameCount % 30 != 0) return;
            checkedOnce = true;

            foreach (var def in EnvironmentCatalog.All)
            {
                if (def.UnlockLifetimePoints <= 0 || SaveService.IsEnvironmentAnnounced((int)def.Id)) continue;
                // İnceleme modu (her şey açık) duyuru harcamasın: gerçek eşik geçilmiş olmalı.
                if (LifetimePoints < def.UnlockLifetimePoints) continue;
                SaveService.MarkEnvironmentAnnounced((int)def.Id);
                if (notice != null)
                {
                    notice.Announce($"New activity: {def.Name}", "Open the menu and pick Activities", $"{def.Name} unlocked");
                }
            }
        }
    }
}
