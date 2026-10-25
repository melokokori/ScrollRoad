using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Plak sandığı (balık/misafir/şebeke defterinin Plak tablası karşılığı): mekânın 6 nadir plağı. Henüz
    // çalınmamış plak koyu silüet + "???"; çalınınca renkli kapak, ad, tarz, kaç kez çalındığı ve ilk tarih.
    // Prototipte tek mekân (Record Shop), sekme yok. Çekmecedeki koleksiyon satırı Plak tablasındayken bunu açar.
    public class CrateLogController : MonoBehaviour
    {
        [SerializeField] VinylController vinyl;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Image[] images;        // RecordCatalog.AllSpecials sırasıyla
        [SerializeField] GameObject[] slotRoots;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);
        static readonly string[] StyleNames = { "Lo-fi", "Bossa nova", "Synthwave", "Jazz", "Disco", "Funk" };

        public GameObject Panel => panel;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        public void Open()
        {
            var shop = vinyl != null ? vinyl.Shop : RecordCatalog.Shop;
            var p = vinyl != null ? vinyl.Progress : SaveService.LoadVinyl();

            titleText.text = "Record crate · " + shop.Name;
            int logged = 0;
            foreach (var r in shop.Specials) if (p.Find(r.Id) != null) logged++;
            countText.text = $"{logged} / {shop.Specials.Length} records  ·  {Fmt.Int(p.totalTracks)} tracks in all";

            for (int i = 0; i < RecordCatalog.AllSpecials.Length && i < slotRoots.Length; i++)
            {
                var type = RecordCatalog.AllSpecials[i];
                bool inShop = shop.Contains(type.Id);
                slotRoots[i].SetActive(inShop);
                if (!inShop) continue;
                var record = p.Find(type.Id);
                bool met = record != null;
                RareNoteTarget.Attach(slotRoots[i], EnvironmentId.Vinyl, type.Id, met);
                images[i].color = met ? Color.white : Silhouette;
                nameTexts[i].text = met ? type.Name : "???";
                detailTexts[i].text = met
                    ? $"{StyleNames[(int)type.Style]}  ·  played ×{record.count}\n<alpha=#88>First spin {record.firstSeen}"
                    : $"<alpha=#88>{StyleNames[(int)type.Style]}  ·  not found yet";
            }
            panel.SetActive(true);
        }
    }
}
