using UnityEngine;
using UnityEngine.UI;

namespace ProjetoMoto
{
    /// <summary>HUD (velocidade, grau, avisos) e menu de pausa com configurações salvas.</summary>
    public class UIManager : MonoBehaviour
    {
        MotorcycleController bike;
        WheelieController wheelie;

        Text speedText, statusText, hintText;
        Text opacityLabel, scaleLabel, sensLabel, vibrationLabel;
        GameObject pausePanel;
        bool paused;

        public static UIManager Create(MotorcycleController bike, WheelieController wheelie,
            CameraManager cameras, TouchInputController touch)
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD", 20);
            UIManager ui = canvas.gameObject.AddComponent<UIManager>();
            ui.bike = bike;
            ui.wheelie = wheelie;
            ui.Build(canvas.transform);
            touch.PausePressed = ui.TogglePause;
            bike.Crashed += GameSettings.Vibrate;
            return ui;
        }

        void Build(Transform parent)
        {
            speedText = UIFactory.HudText("Velocidade", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -30f), new Vector2(500f, 120f), 64, TextAnchor.UpperLeft, Color.white);
            statusText = UIFactory.HudText("Status", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(900f, 140f), 60, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.2f));
            hintText = UIFactory.HudText("Dica", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 20f), new Vector2(1200f, 80f), 34, TextAnchor.LowerCenter, Color.white);
            hintText.text = "Segure GRAU e acelere para empinar. Freio traseiro desce a roda.";

            BuildPausePanel(parent);
        }

        void BuildPausePanel(Transform parent)
        {
            GameObject panel = new GameObject("Pausa", typeof(RectTransform));
            RectTransform rt = (RectTransform)panel.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            pausePanel = panel;

            Vector2 c = new Vector2(0.5f, 0.5f);
            Vector2 size = new Vector2(780f, 100f);
            Color col = new Color(0.15f, 0.17f, 0.22f, 0.95f);

            Text title = UIFactory.HudText("Titulo", rt, c, c, new Vector2(0f, 440f), new Vector2(600f, 100f),
                70, TextAnchor.MiddleCenter, Color.white);
            title.text = "PAUSA";

            UIFactory.TapButton(rt, "Continuar", c, c, new Vector2(0f, 320f), size, "CONTINUAR", col, 40,
                delegate { SetPaused(false); });
            UIFactory.TapButton(rt, "Levantar", c, c, new Vector2(0f, 205f), size, "LEVANTAR A MOTO", col, 40,
                delegate { bike.Recover(); SetPaused(false); });
            opacityLabel = MakeSetting(rt, new Vector2(0f, 90f), size, col, delegate { GameSettings.CycleOpacity(); RefreshLabels(); });
            scaleLabel = MakeSetting(rt, new Vector2(0f, -25f), size, col, delegate { GameSettings.CycleScale(); RefreshLabels(); });
            sensLabel = MakeSetting(rt, new Vector2(0f, -140f), size, col, delegate { GameSettings.CycleSensitivity(); RefreshLabels(); });
            vibrationLabel = MakeSetting(rt, new Vector2(0f, -255f), size, col, delegate { GameSettings.ToggleVibration(); RefreshLabels(); });

            RefreshLabels();
            pausePanel.SetActive(false);
        }

        static Text MakeSetting(Transform parent, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            Vector2 c = new Vector2(0.5f, 0.5f);
            Button b = UIFactory.TapButton(parent, "Config", c, c, pos, size, "", color, 36, onClick);
            return b.GetComponentInChildren<Text>();
        }

        void RefreshLabels()
        {
            opacityLabel.text = "Opacidade dos botões: " + Mathf.RoundToInt(GameSettings.ControlOpacity * 100f) + "%";
            scaleLabel.text = "Tamanho dos botões: " + Mathf.RoundToInt(GameSettings.ControlScale * 100f) + "%";
            sensLabel.text = "Sensibilidade da câmera: " + GameSettings.CameraSensitivity.ToString("0.00") + "x";
            vibrationLabel.text = "Vibração: " + (GameSettings.Vibration ? "ligada" : "desligada");
        }

        public void TogglePause() { SetPaused(!paused); }

        void SetPaused(bool value)
        {
            paused = value;
            Time.timeScale = value ? 0f : 1f;
            pausePanel.SetActive(value);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && !paused && pausePanel != null) SetPaused(true);
        }

        void Update()
        {
            speedText.text = Mathf.RoundToInt(bike.Speed * 3.6f) + " km/h";

            if (bike.IsCrashed) statusText.text = "CAIU! Recuperando...";
            else if (wheelie.IsWheelie)
                statusText.text = "GRAU " + Mathf.RoundToInt(bike.NoseUpAngle) + "°  " + wheelie.WheelieTime.ToString("0.0") + "s";
            else statusText.text = "";

            if (hintText.gameObject.activeSelf && Time.timeSinceLevelLoad > 15f) hintText.gameObject.SetActive(false);
        }
    }
}
