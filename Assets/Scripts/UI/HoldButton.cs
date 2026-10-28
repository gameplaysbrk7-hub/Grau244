using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoMoto
{
    /// <summary>Botão que fica ativo enquanto o dedo estiver pressionado (suporta multitoque).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image image;
        public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            Held = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Held = false;
            Refresh();
        }

        void OnDisable()
        {
            Held = false;
            Refresh();
        }

        void Refresh()
        {
            if (image != null) image.color = Held ? Color.Lerp(normalColor, Color.white, 0.55f) : normalColor;
        }
    }
}
