using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleKeep.Game
{
    /// Small builders for screen-space UI made in code. Everything draws through the UiTheme, so the look lives in
    /// one asset; with no theme set, plain colours stand in.
    public static class UiKit
    {
        public static UiTheme Theme;

        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.35f);
        public static readonly Color KnockedOut = new Color(0.55f, 0.55f, 0.6f);
        public static Color TextColor => Theme != null ? Theme.text : Palette.Text;
        public static Color TextDark => Theme != null ? Theme.textDark : Palette.TextDark;
        public static Color TextGold => Theme != null ? Theme.textGold : new Color(1f, 0.86f, 0.4f);

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
            SetSprite(img, sprite);
            img.raycastTarget = false;
            return img;
        }

        /// Uses nine-slicing when the sprite has borders, so frames stay crisp at any size.
        public static void SetSprite(Image img, Sprite sprite)
        {
            img.sprite = sprite;
            img.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = sprite != null && img.type == Image.Type.Simple && sprite != Shapes.Square && sprite != Shapes.Circle;
        }

        public static Image Icon(string name, RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 pos, float size)
        {
            var img = Img(name, parent, Color.white, anchor, pos, new Vector2(size, size), sprite);
            img.preserveAspect = true;
            img.enabled = sprite != null;
            return img;
        }

        /// The full-screen background behind menus.
        public static Image Backdrop(RectTransform parent)
        {
            var img = Img("Backdrop", parent, Theme != null && Theme.background != null ? Color.white : Palette.Sky, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Theme?.background);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.preserveAspect = false;
            return img;
        }

        /// A stone panel. Pass a colour to tint it.
        public static RectTransform Panel(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null) =>
            Img(name, parent, color ?? (Theme?.panel != null ? Color.white : Palette.Panel), anchor, pos, size, Theme?.panel).rectTransform;

        /// A smaller framed card for choices and readouts.
        public static RectTransform Card(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, bool dim = false, Color? color = null)
        {
            var sprite = dim && Theme != null && Theme.cardDim != null ? Theme.cardDim : Theme?.card;
            return Img(name, parent, color ?? (sprite != null ? Color.white : Palette.Panel), anchor, pos, size, sprite).rectTransform;
        }

        public static TMP_Text Label(string name, RectTransform parent, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 dims, Color? color = null)
        {
            var t = Rect(name, parent, anchor, pos, dims).gameObject.AddComponent<TextMeshProUGUI>();
            var c = color ?? TextColor;
            bool light = c.r + c.g + c.b > 1.5f;
            if (Theme != null) t.font = light && Theme.fontOutlined != null ? Theme.fontOutlined : Theme.font != null ? Theme.font : t.font;
            t.text = text;
            t.fontSize = size;
            t.alignment = Align(align);
            t.color = c;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static TextAlignmentOptions Align(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        public static Button Button(string name, RectTransform parent, string label, int size, Vector2 anchor, Vector2 pos, Vector2 dims, Action onClick,
            ButtonStyle style = ButtonStyle.Brown, Sprite icon = null)
        {
            var sprite = Theme?.Button(style);
            var img = Img(name, parent, sprite != null ? Color.white : Palette.Panel, anchor, pos, dims, sprite);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick?.Invoke());
            float iconSize = Mathf.Min(dims.y * 0.6f, 64f);
            if (icon != null)
            {
                float x = string.IsNullOrEmpty(label) ? 0f : -dims.x * 0.5f + iconSize * 0.5f + 24f;
                Icon("Icon", img.rectTransform, icon, new Vector2(0.5f, 0.5f), new Vector2(x, 3f), iconSize);
            }
            if (!string.IsNullOrEmpty(label))
            {
                float pad = icon != null ? iconSize + 30f : 16f;
                var t = Label("Label", img.rectTransform, label, size, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(icon != null ? pad * 0.5f : 0f, 3f), dims - new Vector2(pad + 16f, 8f));
                t.enableAutoSizing = true;
                t.fontSizeMax = size;
                t.fontSizeMin = Mathf.Max(14, size / 2);
            }
            return b;
        }

        /// A round icon button (pause, speed and the like).
        public static Button RoundButton(string name, RectTransform parent, Sprite icon, string fallback, Vector2 anchor, Vector2 pos, float size, Action onClick)
        {
            var sprite = Theme?.buttonRound;
            var img = Img(name, parent, sprite != null ? Color.white : Palette.Panel, anchor, pos, new Vector2(size, size), sprite != null ? sprite : Shapes.Circle);
            img.preserveAspect = true;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick?.Invoke());
            if (icon != null) Icon("Icon", img.rectTransform, icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), size * 0.5f);
            else Label("Label", img.rectTransform, fallback, (int)(size * 0.35f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            return b;
        }

        /// A title on a ribbon.
        public static TMP_Text Title(string name, RectTransform parent, string text, Vector2 anchor, Vector2 pos, float width, int size = 52)
        {
            if (Theme?.ribbon != null)
            {
                var ribbon = Img(name, parent, Color.white, anchor, pos, new Vector2(width, 120f), Theme.ribbon);
                return Label("Text", ribbon.rectTransform, text, size, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(width - 220f, 90f));
            }
            return Label(name, parent, text, size, TextAnchor.MiddleCenter, anchor, pos, new Vector2(width, 100f), TextDark);
        }

        public static void SetEnabled(Button b, bool enabled)
        {
            b.interactable = enabled;
            var c = b.targetGraphic.color;
            c.a = enabled ? 1f : 0.45f;
            b.targetGraphic.color = c;
        }

        public static UiBar Bar(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size) =>
            new UiBar(name, parent, color, anchor, pos, size);

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }

    /// A horizontal bar that keeps its nine-sliced ends at any fill.
    public sealed class UiBar
    {
        public RectTransform Back { get; }
        readonly Image fill;
        readonly RectTransform fillRect;

        public UiBar(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var theme = UiKit.Theme;
            var back = UiKit.Img(name + "Back", parent, theme?.barBack != null ? Color.white : UiKit.Shade, anchor, pos, size, theme?.barBack);
            Back = back.rectTransform;
            fill = UiKit.Img(name, Back, color, new Vector2(0f, 0.5f), Vector2.zero, size, theme != null && theme.barFill != null ? theme.barFill : Shapes.Square);
            fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
        }

        public float Value
        {
            set
            {
                float v = Mathf.Clamp01(value);
                fill.enabled = v > 0.001f;
                fillRect.anchorMax = new Vector2(v, 1f);
            }
        }

        public Color Color { set => fill.color = value; }
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
