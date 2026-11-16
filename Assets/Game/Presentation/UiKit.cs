using System;
using UnityEngine;
using UnityEngine.UI;

namespace LittleKeep.Game
{
    /// Small builders for screen-space UI made in code. Every screen uses these, so the look stays in one place
    /// until the theme asset arrives (ticket 06).
    public static class UiKit
    {
        static Font font;
        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.35f);
        public static readonly Color KnockedOut = new Color(0.55f, 0.55f, 0.6f);

        /// A landscape canvas scaled from 1920x1080, with a child that tracks the device safe area.
        public static RectTransform Canvas(GameObject host, int order, out RectTransform safe)
        {
            var canvas = host.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            host.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)host.transform;
            safe = Stretch("Safe", root);
            host.AddComponent<SafeArea>().Target = safe;
            return root;
        }

        public static RectTransform Rect(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size) =>
            Rect(name, parent, anchor, anchor, anchor, pos, size);

        public static RectTransform Rect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, RectTransform parent) =>
            Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        public static Image Img(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite = null)
        {
            var img = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform Panel(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null) =>
            Img(name, parent, color ?? Palette.Panel, anchor, pos, size).rectTransform;

        public static Text Label(string name, RectTransform parent, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 dims, Color? color = null)
        {
            var t = Rect(name, parent, anchor, pos, dims).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? Palette.Text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(string name, RectTransform parent, string label, int size, Vector2 anchor, Vector2 pos, Vector2 dims, Action onClick, Color? color = null)
        {
            var img = Img(name, parent, color ?? Palette.Panel, anchor, pos, dims);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick?.Invoke());
            Label("Label", img.rectTransform, label, size, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, dims - new Vector2(16f, 8f));
            return b;
        }

        public static void SetEnabled(Button b, bool enabled)
        {
            b.interactable = enabled;
            var c = b.targetGraphic.color;
            c.a = enabled ? 1f : 0.4f;
            b.targetGraphic.color = c;
        }

        /// A horizontal bar; set its fillAmount.
        public static Image Bar(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var back = Img(name + "Back", parent, Shade, anchor, pos, size);
            var fill = Img(name, back.rectTransform, color, new Vector2(0f, 0.5f), Vector2.zero, size, Shapes.Square);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }

    /// Keeps a RectTransform inside the device safe area (notches, rounded corners).
    public sealed class SafeArea : MonoBehaviour
    {
        public RectTransform Target;
        Rect applied;

        void Update()
        {
            var area = Screen.safeArea;
            if (Target == null || area == applied || Screen.width <= 0 || Screen.height <= 0) return;
            applied = area;
            Target.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            Target.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
    }
}
