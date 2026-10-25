using UnityEngine;
using UnityEngine.EventSystems;

namespace ScrollRoad
{
    // Kartın/şeridin arkasındaki görünmez yüzey: boş bir yerden basılı tutup sürükleyince pencere taşınır.
    // Yazılar tıklamayı geçirir (raycastTarget kapalı), düğmeler kendi tıklamalarını alır.
    public class CardDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] CompanionWindowController window;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) window.BeginDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) window.EndDrag();
        }
    }
}
