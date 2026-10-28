using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjetoMoto
{
    /// <summary>Criação de elementos de interface por código (sem prefabs).</summary>
    public static class UIFactory
    {
        static Sprite circle;
        static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                return font;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 128;
                Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                Color32[] px = new Color32[n * n];
                float r = n * 0.5f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x + 0.5f - r;
                        float dy = y + 0.5f - r;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(r - d);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor align, Color color)
        {
            GameObject go = new GameObject("Texto", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Text t = go.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Text HudText(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            int fontSize, TextAnchor align, Color color)
        {
            RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
            Text t = Label(rt, "", fontSize, align, color);
            Outline o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        /// <summary>Botão circular de segurar (acelerador, freios, grau...).</summary>
        public static HoldButton HoldCircle(Transform parent, string name, Vector2 anchor, Vector2 pos, float diameter,
            string label, Color color, int fontSize)
        {
            RectTransform rt = Rect(name, parent, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Circle;
            img.color = color;
            Label(rt, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            HoldButton hb = rt.gameObject.AddComponent<HoldButton>();
            hb.image = img;
            hb.normalColor = color;
            return hb;
        }

        /// <summary>Botão retangular de toque (pausa, câmera, menus).</summary>
        public static Button TapButton(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            string label, Color color, int fontSize, UnityAction onClick)
        {
            RectTransform rt = Rect(name, parent, anchor, pivot, pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            Label(rt, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            return b;
        }
    }
}
