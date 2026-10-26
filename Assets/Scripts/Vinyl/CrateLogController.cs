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
        static readonly string[] StyleNames = { L.N("Lo-fi"), L.N("Bossa nova"), L.N("Synthwave"), L.N("Jazz"), L.N("Disco"), L.N("Funk") };

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

            titleText.text = L.T("Record crate") + " · " + L.T(shop.Name);
            int logged = 0;
            foreach (var r in shop.Specials) if (p.Find(r.Id) != null) logged++;
            countText.text = L.F("{0} / {1} records  ·  {2} in all", logged, shop.Specials.Length, L.P(p.totalTracks, "{0} tracks", "{0} tracks"));

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
                nameTexts[i].text = met ? L.T(type.Name) : "???";
                detailTexts[i].text = met
                    ? L.T(StyleNames[(int)type.Style]) + "  ·  " + L.F("played ×{0}", record.count) + "\n<alpha=#88>" + L.F("First spin {0}", record.firstSeen)
                    : "<alpha=#88>" + L.T(StyleNames[(int)type.Style]) + "  ·  " + L.T("not found yet");
            }
            panel.SetActive(true);
        }
    }
}
