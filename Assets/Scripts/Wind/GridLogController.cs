using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Şebeke defteri (balık/misafir defterinin Rüzgârgülü karşılığı): seçili mekânın 6 nadir yapısı.
    // Bağlanmamış yapı koyu silüet + "???"; bağlanınca renkli görsel, ad, kaç kez bağlandığı ve ilk tarih.
    // Prototipte tek mekân (Tepeler), sekme yok. Çekmecedeki koleksiyon satırı Rüzgârgülü'ndeyken bunu açar.
    public class GridLogController : MonoBehaviour
    {
        [SerializeField] WindController wind;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Image[] images;        // GridCatalog.AllSpecials sırasıyla
        [SerializeField] GameObject[] slotRoots;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        public void Open()
        {
            var site = wind != null ? wind.Site : GridCatalog.Hills;
            var p = wind != null ? wind.Progress : SaveService.LoadWind();

            titleText.text = "Grid log · " + site.Name;
            int logged = 0;
            foreach (var b in site.Specials) if (p.Find(b.Id) != null) logged++;
            countText.text = $"{logged} / {site.Specials.Length} landmarks  ·  {Fmt.Int(p.totalKwh)} kWh in all";

            for (int i = 0; i < GridCatalog.AllSpecials.Length && i < slotRoots.Length; i++)
            {
                var type = GridCatalog.AllSpecials[i];
                bool inSite = site.Contains(type.Id);
                slotRoots[i].SetActive(inSite);
                if (!inSite) continue;
                var record = p.Find(type.Id);
                bool met = record != null;
                RareNoteTarget.Attach(slotRoots[i], EnvironmentId.Wind, type.Id, met);
                images[i].color = met ? Color.white : Silhouette;
                nameTexts[i].text = met ? type.Name : "???";
                detailTexts[i].text = met
                    ? $"Powered ×{record.count}\n<alpha=#88>First lit {record.firstSeen}"
                    : "<alpha=#88>Not on the grid yet";
            }
            panel.SetActive(true);
        }
    }
}
