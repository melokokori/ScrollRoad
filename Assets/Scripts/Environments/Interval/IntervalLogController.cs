using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Eşit aralıklı ortamların defteri (şebeke defterinin genellemesi): seçili mekânın 6 nadir öğesi.
    // Gelmemiş öğe koyu silüet + "???"; gelince renkli görsel, ad, kaç kez geldiği ve ilk tarih.
    // Prototiplerde tek mekân, sekme yok. Çekmecedeki koleksiyon satırı bu ortamdayken bunu açar.
    public class IntervalLogController : MonoBehaviour
    {
        [SerializeField] IntervalEnvironment env;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countText;
        [SerializeField] Image[] images;        // Site.Specials sırasıyla
        [SerializeField] GameObject[] slotRoots;
        [SerializeField] TMP_Text[] nameTexts;
        [SerializeField] TMP_Text[] detailTexts;
        [SerializeField] Button closeButton;
        [SerializeField] string[] spriteIds;    // tüm mekânların nadirleri (kimlikle)
        [SerializeField] Sprite[] sprites;

        static readonly Color Silhouette = new Color(0.02f, 0.03f, 0.05f, 0.55f);

        public GameObject Panel => panel;
        public IntervalEnvironment Environment => env;

        void Awake()
        {
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            panel.SetActive(false);
        }

        public void Open()
        {
            if (env == null) return;
            var site = env.Site;
            var p = env.Progress;

            titleText.text = $"{env.Def.CollectionName} · {site.Name}";
            int logged = env.LoggedAt(site);
            string record = env.RecordLabel != null && p.best > 0f
                ? $"  ·  {env.RecordLabel.ToLowerInvariant()} {env.FormatValue(p.best)}" : "";
            countText.text = $"{logged} / {site.Specials.Length} {env.LogNoun}  ·  {Fmt.Int(p.total)} {env.Def.Unit} in all{record}";

            for (int i = 0; i < slotRoots.Length; i++)
            {
                bool inSite = i < site.Specials.Length;
                slotRoots[i].SetActive(inSite);
                if (!inSite) continue;
                var type = site.Specials[i];
                var r = p.Find(type.Id);
                bool met = r != null;
                RareNoteTarget.Attach(slotRoots[i], env.Def.Id, type.Id, met);
                int si = System.Array.IndexOf(spriteIds, type.Id);
                if (si >= 0 && si < sprites.Length) images[i].sprite = sprites[si];
                images[i].color = met ? Color.white : Silhouette;
                nameTexts[i].text = met ? type.Name : "???";
                detailTexts[i].text = met ? env.MetDetail(r) : env.UnmetDetail;
            }
            panel.SetActive(true);
        }
    }
}
