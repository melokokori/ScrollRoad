using System;
using UnityEngine;

namespace ScrollRoad
{
    // Seçili ortam (2026-10-13). Yalnız seçili ortam ilerler; gizli puan her ortamda birikir.
    // Tüm ortamlar baştan açık (2026-10-15 kullanıcı kararı: oynayarak yalnız skinler açılır).
    // Seçim yerel PlayerPrefs.
    [DefaultExecutionOrder(-150)]
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentDef Current { get; private set; } = EnvironmentCatalog.Road;
        public static event Action<EnvironmentDef> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = EnvironmentCatalog.Road;
            Changed = null;
        }

        public static bool Select(EnvironmentDef def)
        {
            if (def == null) return false;
            if (def == Current) return true;
            Current = def;
            SaveService.SaveEnvironment((int)def.Id);
            Changed?.Invoke(def);
            return true;
        }

        void Awake()
        {
            Current = EnvironmentCatalog.Get((EnvironmentId)SaveService.LoadEnvironment());
        }
    }
}
