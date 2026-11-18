using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>
    /// Opción de un menú navegable con teclado, joystick o mouse.
    /// Muestra el puntero "▶" al estar seleccionada y reenvía Cancelar.
    /// </summary>
    public class OpcionMenu : MonoBehaviour, ISelectHandler, IDeselectHandler, ICancelHandler, IPointerEnterHandler, IMoveHandler
    {
        public Text texto;
        public Text puntero;
        public Action alSeleccionar;
        public Action alCancelar;
        /// <summary>Opciones con valor (volumen, idioma...): izquierda = -1, derecha = +1.</summary>
        public Action<int> alMoverHorizontal;
        public bool silenciosa;

        public void OnSelect(BaseEventData eventData)
        {
            if (puntero != null) puntero.enabled = true;
            if (!silenciosa) Sfx.Play(SfxId.Mover, 0.5f);
            silenciosa = false;
            alSeleccionar?.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (puntero != null) puntero.enabled = false;
        }

        public void OnCancel(BaseEventData eventData)
        {
            alCancelar?.Invoke();
        }

        public void OnMove(AxisEventData eventData)
        {
            if (alMoverHorizontal == null) return;
            if (eventData.moveDir == MoveDirection.Left) alMoverHorizontal(-1);
            else if (eventData.moveDir == MoveDirection.Right) alMoverHorizontal(+1);
        }

        // Con mouse: pasar por encima selecciona (así el teclado y el mouse conviven).
        public void OnPointerEnter(PointerEventData eventData)
        {
            var b = GetComponent<Selectable>();
            if (b != null && b.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
