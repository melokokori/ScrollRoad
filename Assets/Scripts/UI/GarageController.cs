using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Garaj paneli (≡ çekmecesinden): solda seçili aracın büyük önizlemesi + "MODEL · RENK" + kilit
    // bilgisi; sağda kategori SEKMELERİ (Cars, Off-road, Two wheels, Sea, Sky), sekmenin araçları
    // 5'erli sayfalarda (‹ › okları — fare tekerleği oyunda gaz olduğu için kaydırma yok), altında
    // RENK yuvarlakları (sabit renkli araçta gizlenir; gizli renk kilidi açılana kadar hiç görünmez),
    // altında dünya teması (WorldTheme; sıra WorldTheme.Kind ile aynı), en altta hız göstergesi
    // skinleri (GaugeSkins; kilitliye tıklayınca nasıl açılacağı yazar).
    // Yuva türleri: açık model (seçili renkte), kilitli model (silüet + "500 KM" / "TRIP"
    // plakası; tıklayınca önizlemenin altında nasıl açılacağı yazar), "coming soon" (silüet + "SOON").
    public class GarageController : MonoBehaviour
    {
        [SerializeField] CarCatalog catalog;
        [SerializeField] GameObject garagePanel;
        [SerializeField] Image previewImage;
        [SerializeField] TMP_Text selectedNameText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] Button[] tabButtons;      // CarCatalog.Tab sırasıyla
        [SerializeField] Button[] slotButtons;     // sayfa başına yuva
        [SerializeField] Image[] slotFrames;
        [SerializeField] Image[] slotImages;
        [SerializeField] Image[] slotPlates;
        [SerializeField] TMP_Text[] slotPlateTexts;
        [SerializeField] Button prevButton;
        [SerializeField] Button nextButton;
        [SerializeField] TMP_Text pageText;
        [SerializeField] GameObject paintRow;
        [SerializeField] TMP_Text paintNote;       // sabit renkli araçta "fixed livery"
        [SerializeField] Button[] paintButtons;    // catalog.paints sırasıyla
        [SerializeField] Image[] paintRings;
        [SerializeField] Button[] themeButtons;    // WorldTheme.Kind sırasıyla
        [SerializeField] Button[] gaugeButtons;    // GaugeSkins.Selectable() sırasıyla
        [SerializeField] Image[] gaugeIcons;
        [SerializeField] TMP_Text[] gaugeLabels;
        [SerializeField] Button openButton;
        [SerializeField] SkinBoxController skinBox; // Yol dışında "Garage" satırı ortamın dolabını açar (Ortam Standardı v2)
        [SerializeField] Button closeButton;
        [SerializeField] Color selectedFrameColor = new Color(0.055f, 0.42f, 0.243f); // tabela yeşili
        [SerializeField] Color idleFrameColor = new Color(1f, 1f, 1f, 0.07f);
        [SerializeField] Color lockedTint = new Color(0.055f, 0.06f, 0.075f);
        [SerializeField] Color idleButtonColor = new Color(0.165f, 0.18f, 0.215f);
        [SerializeField] Color lockPlateColor = new Color(0.082f, 0.09f, 0.11f, 0.86f);
        [SerializeField] Color lockPlateInk = new Color(0.957f, 0.945f, 0.902f);
        [SerializeField] Color soonPlateColor = new Color(0.949f, 0.702f, 0.239f);
        [SerializeField] Color soonPlateInk = new Color(0.082f, 0.09f, 0.11f);

        CarCatalog.Tab tab;
        int page;
        readonly List<object> entries = new List<object>(); // CarCatalog.Model ya da CarCatalog.Teaser

        public GameObject Panel => garagePanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => garagePanel.SetActive(false));
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var t = (CarCatalog.Tab)i;
                tabButtons[i].onClick.AddListener(() => { SetTab(t, 0); detailText.text = ""; Refresh(); });
            }
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                slotButtons[i].onClick.AddListener(() => ChooseSlot(index));
            }
            prevButton.onClick.AddListener(() => { page--; Refresh(); });
            nextButton.onClick.AddListener(() => { page++; Refresh(); });
            for (int i = 0; i < paintButtons.Length; i++)
            {
                int index = i;
                paintButtons[i].onClick.AddListener(() => ChoosePaint(index));
            }
            for (int i = 0; i < themeButtons.Length; i++)
            {
                var kind = (WorldTheme.Kind)i;
                themeButtons[i].onClick.AddListener(() => { WorldTheme.Set(kind); Refresh(); });
            }
            var skins = new List<GaugeSkins.Skin>(GaugeSkins.Selectable());
            for (int i = 0; i < gaugeButtons.Length && i < skins.Count; i++)
            {
                var skin = skins[i];
                gaugeIcons[i].sprite = GaugeSkins.Face(skin, true);
                gaugeButtons[i].onClick.AddListener(() => ChooseGauge(skin));
            }
            garagePanel.SetActive(false);
        }

        // Açılışta seçili aracın sekmesi ve sayfası gösterilir.
        void Open()
        {
            if (skinBox != null && EnvironmentContent.Current != null)
            {
                skinBox.Open();
                return;
            }
            detailText.text = "";
            var selected = catalog.SelectedModel;
            SetTab(selected != null ? CarCatalog.TabOf(selected.kind) : CarCatalog.Tab.Cars, 0);
            int index = entries.IndexOf(selected);
            if (index >= 0) page = index / slotButtons.Length;
            Refresh();
            garagePanel.SetActive(true);
        }

        void SetTab(CarCatalog.Tab newTab, int newPage)
        {
            tab = newTab;
            page = newPage;
            entries.Clear();
            foreach (var model in catalog.models)
            {
                if (CarCatalog.TabOf(model.kind) == tab) entries.Add(model);
            }
            foreach (var teaser in catalog.comingSoon)
            {
                if (CarCatalog.TabOf(teaser.kind) == tab) entries.Add(teaser);
            }
        }

        int PageCount => Mathf.Max(1, (entries.Count + slotButtons.Length - 1) / slotButtons.Length);

        object EntryAt(int slot)
        {
            int index = page * slotButtons.Length + slot;
            return index < entries.Count ? entries[index] : null;
        }

        void ChooseSlot(int slot)
        {
            if (!(EntryAt(slot) is CarCatalog.Model model)) return;
            if (!CarCatalog.IsUnlocked(model))
            {
                detailText.text = CarCatalog.LockDescription(model);
                return;
            }
            detailText.text = "";
            catalog.Select(model);
            Refresh();
        }

        void ChoosePaint(int index)
        {
            catalog.Select(catalog.paints[index]);
            Refresh();
        }

        void ChooseGauge(GaugeSkins.Skin skin)
        {
            if (!GaugeSkins.IsUnlocked(skin))
            {
                detailText.text = GaugeSkins.LockDescription(skin);
                return;
            }
            detailText.text = "";
            GaugeSkins.Select(skin);
            Refresh();
        }

        void Refresh()
        {
            page = Mathf.Clamp(page, 0, PageCount - 1);
            var model = catalog.SelectedModel;
            var paint = catalog.SelectedPaint;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].image.color = (int)tab == i ? selectedFrameColor : idleButtonColor;
            }

            for (int i = 0; i < slotButtons.Length; i++)
            {
                var entry = EntryAt(i);
                slotButtons[i].gameObject.SetActive(entry != null);
                if (entry is CarCatalog.Model m)
                {
                    bool open = CarCatalog.IsUnlocked(m);
                    slotImages[i].sprite = open ? catalog.SpriteFor(m, paint) : CarCatalog.SilhouetteFor(m);
                    slotImages[i].color = open ? Color.white : lockedTint;
                    slotFrames[i].color = m == model ? selectedFrameColor : idleFrameColor;
                    slotPlates[i].gameObject.SetActive(!open);
                    slotPlates[i].color = lockPlateColor;
                    slotPlateTexts[i].color = lockPlateInk;
                    slotPlateTexts[i].text = CarCatalog.LockPlate(m);
                }
                else if (entry is CarCatalog.Teaser t)
                {
                    slotImages[i].sprite = CarCatalog.SilhouetteFor(t);
                    slotImages[i].color = lockedTint;
                    slotFrames[i].color = idleFrameColor;
                    slotPlates[i].gameObject.SetActive(true);
                    slotPlates[i].color = soonPlateColor;
                    slotPlateTexts[i].color = soonPlateInk;
                    slotPlateTexts[i].text = L.T("SOON");
                }
            }

            bool paged = PageCount > 1;
            prevButton.gameObject.SetActive(paged);
            nextButton.gameObject.SetActive(paged);
            prevButton.interactable = page > 0;
            nextButton.interactable = page < PageCount - 1;
            pageText.text = paged ? $"{page + 1} / {PageCount}" : "";

            bool paintable = model != null && model.paintable;
            paintRow.SetActive(paintable);
            paintNote.gameObject.SetActive(!paintable);
            for (int i = 0; i < paintButtons.Length && i < catalog.paints.Length; i++)
            {
                paintButtons[i].gameObject.SetActive(CarCatalog.IsAvailable(catalog.paints[i]));
                paintRings[i].enabled = catalog.paints[i] == paint;
            }

            for (int i = 0; i < themeButtons.Length; i++)
            {
                themeButtons[i].image.color = (int)WorldTheme.Current == i ? selectedFrameColor : idleButtonColor;
            }

            int g = 0;
            var selectedGauge = GaugeSkins.Selected;
            foreach (var skin in GaugeSkins.Selectable())
            {
                if (g >= gaugeButtons.Length) break;
                bool open = GaugeSkins.IsUnlocked(skin);
                gaugeButtons[g].image.color = skin == selectedGauge ? selectedFrameColor : idleButtonColor;
                gaugeIcons[g].color = open ? Color.white : new Color(0.35f, 0.35f, 0.38f, 1f);
                gaugeLabels[g].text = open ? L.T(skin.name) : skin.unlock == CarCatalog.Unlock.MonthBestKm ? L.T("TRIP") : L.Upper(Fmt.Km(skin.unlockValue, 0));
                gaugeLabels[g].alpha = open ? 1f : 0.6f;
                g++;
            }

            previewImage.sprite = catalog.SelectedSprite;
            selectedNameText.text = model == null ? "" : paintable ? $"{model.displayName} · {L.T(paint.displayName)}" : model.displayName;
        }
    }
}
