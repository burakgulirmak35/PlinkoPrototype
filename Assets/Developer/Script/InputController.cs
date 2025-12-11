using UnityEngine;
using UnityEngine.EventSystems;
using PlinkoPrototype;

namespace PlinkoPrototype
{
    public class InputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool isHolding = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isHolding)
            {
                GameEvents.OnTapStart?.Invoke();
                GameEvents.OnHoldStart?.Invoke();
                isHolding = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isHolding)
            {
                isHolding = false;
                GameEvents.OnHoldEnd?.Invoke();
                GameEvents.OnTapEnd?.Invoke();
            }
        }
    }
}
