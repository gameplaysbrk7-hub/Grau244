using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjetoMoto
{
    /// <summary>
    /// Controles touchscreen: todos os botões executam ações reais.
    /// Acelerador e freios respondem enquanto o dedo está pressionado.
    /// Opacidade e tamanho vêm das configurações salvas.
    /// </summary>
    public class TouchInputController : MonoBehaviour
    {
        public Action PausePressed;

        MotorcycleController bike;
        MotorcycleInput bikeInput;
        CameraManager cameras;

        HoldButton throttle, brakeFront, brakeRear, left, right, wheelie, trick;
        RectTransform leftGroup, rightGroup, topGroup;
        CanvasGroup leftAlpha, rightAlpha, topAlpha;

        public static TouchInputController Create(MotorcycleController bike, CameraManager cameras)
        {
            Canvas canvas = UIFactory.CreateCanvas("Controles", 10);
            TouchInputController t = canvas.gameObject.AddComponent<TouchInputController>();
            t.bike = bike;
            t.bikeInput = bike.GetComponent<MotorcycleInput>();
            t.cameras = cameras;
            t.Build(canvas.transform);
            return t;
        }

        void Build(Transform parent)
        {
            Vector2 bl = new Vector2(0f, 0f);
            Vector2 br = new Vector2(1f, 0f);
            Vector2 tr = new Vector2(1f, 1f);

            leftGroup = UIFactory.Rect("GrupoEsquerdo", parent, bl, bl, Vector2.zero, new Vector2(700f, 400f));
            leftAlpha = leftGroup.gameObject.AddComponent<CanvasGroup>();
            rightGroup = UIFactory.Rect("GrupoDireito", parent, br, br, Vector2.zero, new Vector2(900f, 650f));
            rightAlpha = rightGroup.gameObject.AddComponent<CanvasGroup>();
            topGroup = UIFactory.Rect("GrupoTopo", parent, tr, tr, Vector2.zero, new Vector2(800f, 160f));
            topAlpha = topGroup.gameObject.AddComponent<CanvasGroup>();

            Color steerColor = new Color(0.2f, 0.2f, 0.25f, 0.75f);
            left = UIFactory.HoldCircle(leftGroup, "Esquerda", bl, new Vector2(170f, 170f), 220f, "<", steerColor, 90);
            right = UIFactory.HoldCircle(leftGroup, "Direita", bl, new Vector2(430f, 170f), 220f, ">", steerColor, 90);

            throttle = UIFactory.HoldCircle(rightGroup, "Acelerar", br, new Vector2(-170f, 190f), 250f, "ACELERAR",
                new Color(0.1f, 0.6f, 0.2f, 0.8f), 34);
            wheelie = UIFactory.HoldCircle(rightGroup, "Grau", br, new Vector2(-170f, 470f), 190f, "GRAU",
                new Color(0.9f, 0.55f, 0.05f, 0.8f), 38);
            brakeFront = UIFactory.HoldCircle(rightGroup, "FreioDianteiro", br, new Vector2(-450f, 150f), 180f, "FREIO\nDIANT.",
                new Color(0.75f, 0.15f, 0.15f, 0.8f), 28);
            brakeRear = UIFactory.HoldCircle(rightGroup, "FreioTraseiro", br, new Vector2(-450f, 350f), 180f, "FREIO\nTRAS.",
                new Color(0.6f, 0.1f, 0.35f, 0.8f), 28);
            trick = UIFactory.HoldCircle(rightGroup, "Derrapar", br, new Vector2(-700f, 150f), 160f, "DERRAPAR",
                new Color(0.25f, 0.35f, 0.8f, 0.8f), 24);

            Color tap = new Color(0.1f, 0.1f, 0.12f, 0.8f);
            UIFactory.TapButton(topGroup, "Pausa", tr, new Vector2(0.5f, 0.5f), new Vector2(-90f, -70f), new Vector2(130f, 90f),
                "II", tap, 42, delegate { if (PausePressed != null) PausePressed(); });
            UIFactory.TapButton(topGroup, "Camera", tr, new Vector2(0.5f, 0.5f), new Vector2(-240f, -70f), new Vector2(150f, 90f),
                "CÂMERA", tap, 28, delegate { cameras.ToggleView(); });
            UIFactory.TapButton(topGroup, "Centralizar", tr, new Vector2(0.5f, 0.5f), new Vector2(-410f, -70f), new Vector2(170f, 90f),
                "CENTRAR", tap, 28, delegate { cameras.Recenter(); });
            UIFactory.TapButton(topGroup, "Recuperar", tr, new Vector2(0.5f, 0.5f), new Vector2(-600f, -70f), new Vector2(190f, 90f),
                "LEVANTAR", tap, 28, delegate { bike.Recover(); });
        }

        void Update()
        {
            if (bikeInput == null) return;

            bikeInput.TouchThrottle = throttle.Held ? 1f : 0f;
            bikeInput.TouchBrakeFront = brakeFront.Held ? 1f : 0f;
            bikeInput.TouchBrakeRear = brakeRear.Held ? 1f : 0f;
            bikeInput.TouchSteer = (right.Held ? 1f : 0f) - (left.Held ? 1f : 0f);
            bikeInput.TouchWheelie = wheelie.Held;
            bikeInput.TouchTrick = trick.Held;

            float opacity = GameSettings.ControlOpacity;
            leftAlpha.alpha = opacity;
            rightAlpha.alpha = opacity;
            topAlpha.alpha = Mathf.Max(opacity, 0.6f);

            Vector3 scale = Vector3.one * GameSettings.ControlScale;
            leftGroup.localScale = scale;
            rightGroup.localScale = scale;
        }
    }
}
