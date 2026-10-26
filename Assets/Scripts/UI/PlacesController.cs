using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Mekânlar + Odak paneli (Ortam Standardı v2, 2026-10-25; ≡ → Places). Üstte seçili ortamın mekânları
    // (açık olana tıklayınca oraya geçilir; kilitlide kalan seyir süresi), altta bulunulan mekânın nadirleri:
    // birine tıklayınca Odak olur (göreli sıklığı ×3), aynısına yeniden tıklayınca kalkar. Puanı değiştirmez.
    // Yol'da mekân yok: satır pasaportu açar. Nadir görselleri sahne kurucudan "<ortam>:<nadir>" anahtarıyla.
    public class PlacesController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] PassportController passport;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Button[] venueButtons;
        [SerializeField] Image[] venueFrames;
        [SerializeField] TMP_Text[] venueNames;
        [SerializeField] TMP_Text[] venueStatuses;
        [SerializeField] TMP_Text focusHint;
        [SerializeField] Button[] rareButtons;
        [SerializeField] Image[] rareFrames;
        [SerializeField] Image[] rareImages;
        [SerializeField] TMP_Text[] rareNames;
        [SerializeField] TMP_Text[] rareStatuses;
        [SerializeField] string[] spriteKeys;
        [SerializeField] Sprite[] sprites;

        static readonly Color Selected = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Locked = new Color(0.02f, 0.03f, 0.05f, 0.6f);
        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        static readonly Color Amber = new Color(0.949f, 0.702f, 0.239f);

        RareInfo[] rares = System.Array.Empty<RareInfo>();
        float refreshTimer;

        public GameObject Panel => panel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < venueButtons.Length; i++)
            {
                int index = i;
                venueButtons[i].onClick.AddListener(() => PickVenue(index));
            }
            for (int i = 0; i < rareButtons.Length; i++)
            {
                int index = i;
                rareButtons[i].onClick.AddListener(() => PickRare(index));
            }
            panel.SetActive(false);
        }

        public void Open()
        {
            if (EnvironmentContent.Current == null)
            {
                if (passport != null) passport.Open();
                return;
            }
            Refresh();
            panel.SetActive(true);
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 1f; // kilit süreleri ve defter canlı
            if (EnvironmentContent.Current == null) panel.SetActive(false);
            else Refresh();
        }

        void PickVenue(int index)
        {
            var c = EnvironmentContent.Current;
            if (c == null || index >= c.Venues.Length) return;
            EnvironmentContent.SelectVenue(c, c.Venues[index].Id);
            Refresh();
        }

        void PickRare(int index)
        {
            var c = EnvironmentContent.Current;
            if (c == null || index >= rares.Length) return;
            EnvironmentContent.ToggleFocus(c, rares[index].Id);
            Refresh();
        }

        public Sprite SpriteFor(EnvironmentId env, string rareId)
        {
            string key = env + ":" + rareId;
            for (int i = 0; i < spriteKeys.Length && i < sprites.Length; i++) if (spriteKeys[i] == key) return sprites[i];
            return null;
        }

        void Refresh()
        {
            var c = EnvironmentContent.Current;
            if (c == null) return;
            var env = c.Def.Id;
            var venues = c.Venues;
            double points = EnvironmentContent.LadderPoints(c);

            titleText.text = L.T("Places") + " · " + L.T(c.Def.Name);
            int open = 0;
            for (int i = 0; i < venues.Length; i++) if (EnvironmentContent.IsVenueUnlocked(c, i)) open++;
            countText.text = L.F("{0} / {1} open  ·  {2} played here", open, venues.Length, UnlockLadder.Remaining(c.LifetimePoints));

            for (int i = 0; i < venueButtons.Length; i++)
            {
                bool exists = i < venues.Length;
                venueButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool unlocked = EnvironmentContent.IsVenueUnlocked(c, i);
                bool here = venues[i].Id == c.VenueId;
                venueFrames[i].color = here ? Selected : unlocked ? Idle : Locked;
                venueButtons[i].interactable = unlocked && !here;
                venueNames[i].text = L.T(venues[i].Name);
                venueNames[i].color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                venueStatuses[i].text = here ? L.T("YOU ARE HERE")
                    : unlocked ? "<alpha=#99>" + L.T("CLICK TO GO")
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(Amber)}>" + L.F("{0} TO GO", UnlockLadder.Remaining(UnlockLadder.VenuePoints(i) - points)) + "</color>";
            }

            rares = c.RaresAt(c.VenueId);
            string focus = EnvironmentContent.FocusOf(env);
            var focused = System.Array.Find(rares, r => r.Id == focus);
            bool focusAtNight = focused != null && EnvironmentContent.IsNightRare(c, c.VenueId, focused.Id);
            focusHint.text = focused != null
                ? L.T("FOCUS") + "  ·  " + (focusAtNight
                    ? L.F("<color=#FFFFFF>{0}</color> turns up {1:0}× as often after dark. Click it again to clear.", L.T(focused.Name), EnvironmentContent.FocusMultiplier)
                    : L.F("<color=#FFFFFF>{0}</color> turns up {1:0}× as often. Click it again to clear.", L.T(focused.Name), EnvironmentContent.FocusMultiplier))
                : L.T("FOCUS") + "  ·  " + L.F("Pick one to look out for: it turns up {0:0}× as often. Your score stays the same.", EnvironmentContent.FocusMultiplier);

            for (int i = 0; i < rareButtons.Length; i++)
            {
                bool exists = i < rares.Length;
                rareButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var rare = rares[i];
                bool logged = c.IsLogged(rare.Id);
                bool isFocus = rare.Id == focus;
                rareFrames[i].color = isFocus ? Selected : Idle;
                var sprite = SpriteFor(env, rare.Id);
                rareImages[i].sprite = sprite;
                rareImages[i].enabled = sprite != null;
                rareImages[i].color = logged ? Color.white : Silhouette;
                rareNames[i].text = L.T(rare.Name); // Odak seçimi için adı hep görünür; görsel bulunana kadar silüet
                RareNoteTarget.Attach(rareButtons[i].gameObject, env, rare.Id, known: true); // adı zaten görünür
                // Gece nadiri (EnvironmentContent.NightRareIndex): yalnız 20:00-06:00 arası gelir.
                bool night = EnvironmentContent.IsNightRare(env, i);
                string amber = ColorUtility.ToHtmlStringRGB(Amber);
                rareStatuses[i].text = isFocus ? (night ? L.T("IN FOCUS · AFTER DARK") : L.T("IN FOCUS"))
                    : logged ? "<alpha=#88>" + L.T("IN YOUR LOG")
                    : night ? $"<color=#{amber}>" + L.T("AFTER DARK ONLY") + "</color>"
                    : "<alpha=#88>" + L.T("NOT FOUND YET");
            }
        }
    }
}
