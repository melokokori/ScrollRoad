using UnityEngine;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Garaj paneli (ana menü ve duraklatma menüsünden açılır): üstte seçili aracın büyük
    // önizlemesi + adı, altta katalogdaki her araç için bir yuva. Seçim anında uygulanır
    // (CarCatalog.SelectionChanged → sahnedeki araba, menü önizlemesi). Gizli aracın yuvası
    // kilit açılana kadar hiç gösterilmez — satırlar (HorizontalLayoutGroup) boşluk bırakmadan ortalanır.
    public class GarageController : MonoBehaviour
    {
        [SerializeField] CarCatalog catalog;
        [SerializeField] GameObject garagePanel;
        [SerializeField] Image previewImage;
        [SerializeField] Text selectedNameText;
        [SerializeField] Button[] slotButtons; // katalog sırasıyla
        [SerializeField] Image[] slotFrames;   // seçili yuvanın çerçevesi
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] Color selectedFrameColor = new Color(0.93f, 0.45f, 0.36f);
        [SerializeField] Color idleFrameColor = new Color(1f, 1f, 1f, 0.06f);

        public GameObject Panel => garagePanel;

        void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => garagePanel.SetActive(false));
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                slotButtons[i].onClick.AddListener(() => Choose(index));
            }
            garagePanel.SetActive(false);
        }

        void Open()
        {
            Refresh();
            garagePanel.SetActive(true);
        }

        void Choose(int index)
        {
            if (index >= catalog.entries.Length) return;
            catalog.Select(catalog.entries[index]);
            Refresh();
        }

        void Refresh()
        {
            var selected = catalog.Selected;
            for (int i = 0; i < slotButtons.Length; i++)
            {
                var entry = i < catalog.entries.Length ? catalog.entries[i] : null;
                slotButtons[i].gameObject.SetActive(catalog.IsAvailable(entry));
                slotFrames[i].color = entry == selected ? selectedFrameColor : idleFrameColor;
            }
            if (selected == null) return;
            previewImage.sprite = selected.sprite;
            selectedNameText.text = selected.displayName;
        }
    }
}
