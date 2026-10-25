using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Defter notu ipucu (2026-10-25): defterlerde bulunmuş bir nadirin ya da Mekânlar panelindeki bir nadirin
    // üzerine gelince imlecin yanında küçük koyu plaka, nadirin tek cümlelik notuyla (RareNotes). Hücreler
    // RareNoteTarget taşır; denetleyiciler onu her açılışta günceller (RareNoteTarget.Attach). Plaka en üst
    // canvas'ta, tıklamayı engellemez.
    public class RareNoteTooltip : MonoBehaviour
    {
        [SerializeField] RectTransform canvasRect;
        [SerializeField] RectTransform plate;
        [SerializeField] TMP_Text text;
        [SerializeField] float maxWidth = 240f;
        [SerializeField] Vector2 offset = new Vector2(14f, -10f);

        static RareNoteTooltip instance;
        Object owner;

        void Awake()
        {
            instance = this;
            plate.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Show(Object from, string note)
        {
            if (instance == null || string.IsNullOrEmpty(note)) return;
            instance.owner = from;
            instance.text.text = note;
            // Kısa notta plaka daralır, uzun notta en çok maxWidth ve satır kırılır.
            float width = Mathf.Min(instance.maxWidth, instance.text.GetPreferredValues(note).x + 1f);
            instance.text.rectTransform.sizeDelta = new Vector2(width, 0f);
            float height = instance.text.GetPreferredValues(note, width, 0f).y;
            instance.plate.sizeDelta = new Vector2(width + 16f, height + 10f);
            instance.plate.gameObject.SetActive(true);
            instance.Follow();
        }

        public static void Hide(Object from)
        {
            if (instance == null || instance.owner != from) return;
            instance.owner = null;
            instance.plate.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (plate.gameObject.activeSelf) Follow();
        }

        // İmlecin sağ altında; kartın kenarına taşacaksa sola/yukarı döner.
        void Follow()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse.position.ReadValue(), null, out var local);
            var half = canvasRect.rect.size / 2f;
            var size = plate.sizeDelta;
            float x = local.x + offset.x;
            float y = local.y + offset.y;
            if (x + size.x > half.x - 4f) x = local.x - offset.x - size.x;
            if (y - size.y < -half.y + 4f) y = local.y - offset.y + size.y;
            // Ne olursa olsun kartın içinde kalsın (imleç kartın dışındayken de).
            x = Mathf.Clamp(x, -half.x + 4f, half.x - 4f - size.x);
            y = Mathf.Clamp(y, -half.y + 4f + size.y, half.y - 4f);
            plate.anchoredPosition = new Vector2(x, y);
        }
    }
}
