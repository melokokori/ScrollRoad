using UnityEngine;
using UnityEngine.EventSystems;

namespace ScrollRoad
{
    // Kartın arkasındaki görünmez yüzey: boş bir yerden basılı tutup sürükleyince pencere taşınır.
    // Yazılar tıklamayı geçirir (raycastTarget kapalı), düğmeler kendi tıklamalarını alır.
    public class CardDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] CompanionWindowController window;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) window.BeginCardDrag();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) window.EndCardDrag();
        }
    }
}
