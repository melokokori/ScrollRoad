using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollRoad
{
    // Nadir hücresi: üzerine gelince notu gösterir. known=false (defterde bulunmamış) ise not gizli kalır.
    public class RareNoteTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        EnvironmentId env;
        string rareId;
        bool known;

        public static void Attach(GameObject cell, EnvironmentId env, string rareId, bool known)
        {
            if (cell == null) return;
            var graphic = cell.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
            var target = cell.GetComponent<RareNoteTarget>() ?? cell.AddComponent<RareNoteTarget>();
            target.env = env;
            target.rareId = rareId;
            target.known = known;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (known) RareNoteTooltip.Show(this, RareNotes.For(env, rareId));
        }

        public void OnPointerExit(PointerEventData eventData) => RareNoteTooltip.Hide(this);

        void OnDisable() => RareNoteTooltip.Hide(this);
    }
}
