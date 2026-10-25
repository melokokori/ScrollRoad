using UnityEngine;

namespace ScrollRoad
{
    // Yan etkinlik yuvası (2026-10-25): toplam 5 saat seyirden sonra açılır. Oyuncu Activities panelinden ikinci
    // bir ortam seçer; o ortam, oyuncu başka bir ortamdayken ilerlemenin Rate kadarıyla kendi açılma merdiveninde
    // (mekân + skin) ilerler — öğe toplamaz, aylık skoru ve siparişleri değiştirmez (yalnız ContentState.sidePoints,
    // EnvironmentContent.LadderPoints). "Hangisini arkada çalıştırayım" kararı. Yol yan yuvaya konamaz (merdiveni
    // araçlar). Seçim yerel PlayerPrefs.
    public class SideActivity : MonoBehaviour
    {
        public const float Rate = 0.25f;
        public const double UnlockPoints = 5 * UnlockLadder.PointsPerHour;

        [SerializeField] NoticeController notice;
        [SerializeField] float saveIntervalSeconds = 10f;

        const string SelectedKey = "ScrollRoad_SideActivity";
        const string AnnouncedKey = "ScrollRoad_SideActivityAnnounced";

        static int selected = -2; // -2 = yüklenmedi, -1 = yok
        float saveTimer;
        bool dirty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => selected = -2;

        public static bool IsUnlocked =>
            CarCatalog.ReviewUnlockAll || (ScoreEngine.Instance != null && ScoreEngine.Instance.LifetimePoints >= UnlockPoints);

        // Kalan seyir süresi (açıksa 0).
        public static double PointsToUnlock =>
            ScoreEngine.Instance == null ? UnlockPoints : System.Math.Max(0.0, UnlockPoints - ScoreEngine.Instance.LifetimePoints);

        static int Selected
        {
            get
            {
                if (selected == -2) selected = PlayerPrefs.GetInt(SelectedKey, -1);
                return selected;
            }
        }

        public static bool Has(EnvironmentId id) => IsUnlocked && Selected == (int)id;

        // Yan yuvadaki ortamın içeriği (yoksa ya da kilitliyse null).
        public static IEnvironmentContent Current => IsUnlocked && Selected >= 0 ? EnvironmentContent.Get((EnvironmentId)Selected) : null;

        // Aynı ortama yeniden basınca yuva boşalır. Yol konamaz.
        public static void Toggle(EnvironmentId id)
        {
            if (!IsUnlocked || id == EnvironmentId.Road) return;
            if (Selected >= 0) EnvironmentContent.Save((EnvironmentId)Selected); // eski yuvanın birikeni kaybolmasın
            selected = Selected == (int)id ? -1 : (int)id;
            PlayerPrefs.SetInt(SelectedKey, selected);
            PlayerPrefs.Save();
        }

        void Update()
        {
            var engine = ScoreEngine.Instance;
            if (engine == null) return;

            if (notice != null && engine.LifetimePoints >= UnlockPoints && PlayerPrefs.GetInt(AnnouncedKey, 0) == 0)
            {
                PlayerPrefs.SetInt(AnnouncedKey, 1);
                PlayerPrefs.Save();
                notice.Announce("Side activity unlocked", $"≡ → Activities: pick a second one. It moves toward its unlocks at {Rate * 100:0}% speed while you're elsewhere.",
                    "Side activity unlocked");
            }

            var side = Current;
            if (side != null && side.Def.Id != EnvironmentManager.Current.Id && engine.FrameGainPoints > 0)
            {
                EnvironmentContent.State(side.Def.Id).sidePoints += engine.FrameGainPoints * Rate;
                dirty = true;
            }

            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= saveIntervalSeconds && dirty)
            {
                saveTimer = 0f;
                Save();
            }
        }

        void Save()
        {
            dirty = false;
            var side = Current;
            if (side != null) EnvironmentContent.Save(side.Def.Id);
        }

        void OnApplicationQuit()
        {
            if (dirty) Save();
        }

        void OnDestroy()
        {
            if (dirty) Save();
        }
    }
}
