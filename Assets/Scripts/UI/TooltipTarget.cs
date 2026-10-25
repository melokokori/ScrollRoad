using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Serbest metinli ipucu (2026-10-25): üzerine gelince RareNoteTooltip plakasında Text görünür (Albüm hücreleri).
    public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Text;

        public static TooltipTarget Attach(GameObject go, string text)
        {
            var graphic = go.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
            var target = go.GetComponent<TooltipTarget>() ?? go.AddComponent<TooltipTarget>();
            target.Text = text;
            return target;
        }

        public void OnPointerEnter(PointerEventData eventData) => RareNoteTooltip.Show(this, Text);

        public void OnPointerExit(PointerEventData eventData) => RareNoteTooltip.Hide(this);

        void OnDisable() => RareNoteTooltip.Hide(this);
    }
}
