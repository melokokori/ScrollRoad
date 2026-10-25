using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Albüm (2026-10-25, ≡ → Album): 9 içerikli ortamın 5'er mekânı tek sayfada — her hücre o mekânın 6 nadirinden
    // kaçının defterde olduğunu doluluk çubuğuyla gösterir, tamamlanan mekân yeşil. Ortamlar ayrı mini oyunlar
    // gibi değil tek bir koleksiyon gibi görünsün diye. Hücrenin üzerine gelince mekân adı ve durum; açık bir
    // hücreye tıklayınca o ortam ve mekâna geçilir. Satırlar EnvironmentCatalog.All sırasıyla (Yol hariç).
    public class AlbumController : MonoBehaviour
    {
        public const int Venues = 5;

        [SerializeField] GameObject panel;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text countText;
        [SerializeField] TMP_Text[] rowNames;
        [SerializeField] TMP_Text[] rowTotals;
        [SerializeField] Button[] cellButtons;   // satır × Venues
        [SerializeField] Image[] cellFrames;
        [SerializeField] RectTransform[] cellFills;
        [SerializeField] Image[] cellFillImages;
        [SerializeField] TMP_Text[] cellTexts;

        static readonly Color Complete = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        static readonly Color Amber = new Color(0.949f, 0.702f, 0.239f);
        static readonly Color Idle = new Color(0.133f, 0.145f, 0.172f);
        static readonly Color Here = new Color(0.2f, 0.22f, 0.26f);
        static readonly Color Locked = new Color(0.06f, 0.07f, 0.09f);

        EnvironmentDef[] rows;
        float refreshTimer;

        public GameObject Panel => panel;

        public static EnvironmentDef[] Rows => Array.FindAll(EnvironmentCatalog.All, d => d.Id != EnvironmentId.Road);

        void Awake()
        {
            rows = Rows;
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < cellButtons.Length; i++)
            {
                int index = i;
                cellButtons[i].onClick.AddListener(() => Pick(index));
            }
            panel.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            panel.SetActive(true);
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 1f;
            Refresh();
        }

        void Pick(int cell)
        {
            int row = cell / Venues, v = cell % Venues;
            if (row >= rows.Length) return;
            var c = EnvironmentContent.Get(rows[row].Id);
            if (c == null || v >= c.Venues.Length || !EnvironmentContent.IsVenueUnlocked(c, v)) return;
            EnvironmentManager.Select(rows[row]);
            EnvironmentContent.SelectVenue(c, c.Venues[v].Id);
            panel.SetActive(false);
        }

        void Refresh()
        {
            int found = 0, total = 0, complete = 0;
            var current = EnvironmentManager.Current.Id;
            for (int r = 0; r < rows.Length && r < rowNames.Length; r++)
            {
                var def = rows[r];
                var c = EnvironmentContent.Get(def.Id);
                rowNames[r].text = def.Name;
                int rowFound = 0, rowTotal = 0;
                for (int v = 0; v < Venues; v++)
                {
                    int i = r * Venues + v;
                    if (i >= cellButtons.Length) break;
                    bool exists = c != null && v < c.Venues.Length;
                    cellButtons[i].gameObject.SetActive(exists);
                    if (!exists) continue;

                    var venue = c.Venues[v];
                    var rares = c.RaresAt(venue.Id);
                    int n = 0;
                    foreach (var rare in rares) if (c.IsLogged(rare.Id)) n++;
                    rowFound += n;
                    rowTotal += rares.Length;
                    bool done = rares.Length > 0 && n >= rares.Length;
                    if (done) complete++;
                    bool unlocked = EnvironmentContent.IsVenueUnlocked(c, v);
                    bool here = def.Id == current && venue.Id == c.VenueId;

                    cellFrames[i].color = !unlocked ? Locked : here ? Here : Idle;
                    float t = rares.Length == 0 ? 0f : (float)n / rares.Length;
                    cellFills[i].anchorMax = new Vector2(t, 1f);
                    cellFillImages[i].color = done ? Complete : Amber;
                    cellTexts[i].text = !unlocked ? "<alpha=#55>LOCKED" : done ? "COMPLETE" : $"{n} / {rares.Length}";
                    cellTexts[i].color = n > 0 && t >= 0.5f && !done ? new Color(0.12f, 0.1f, 0.06f) : Color.white;
                    cellButtons[i].interactable = unlocked && !here;

                    string state = !unlocked
                        ? $"opens after {UnlockLadder.Remaining(UnlockLadder.VenuePoints(v) - EnvironmentContent.LadderPoints(c))} more here"
                        : here ? "you are here" : "click to go";
                    TooltipTarget.Attach(cellButtons[i].gameObject, $"{venue.Name}  ·  {n} / {rares.Length}  ·  {state}");
                }
                rowTotals[r].text = $"{rowFound} / {rowTotal}";
                found += rowFound;
                total += rowTotal;
            }
            countText.text = $"{Fmt.Int(found)} / {Fmt.Int(total)} rares  ·  {Fmt.Count(complete, "place")} complete";
        }
    }
}
