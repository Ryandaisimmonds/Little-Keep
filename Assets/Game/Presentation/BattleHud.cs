using System;
using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace LittleKeep.Game
{
    /// The Battle screen's controls and readouts, laid out for landscape thumbs:
    /// Keep health top left, waves top centre, pause and speed top right, squad bottom left, Rally bottom right.
    public sealed class BattleHud : MonoBehaviour
    {
        public event Action RallyPressed, PausePressed, SpeedPressed, ResumePressed, RestartPressed;

        sealed class Card
        {
            public Fighter fighter;
            public Image hpFill, dot;
            public Color color;
            public Text koText;
            public readonly List<Image> skillFills = new List<Image>();
        }

        Font font;
        RectTransform safe;
        Rect appliedSafeArea;
        Image keepFill;
        Text keepText, waveText, speedText, rallyText, endTitle, endBody;
        Image rallyFill;
        RectTransform rallyRoot;
        GameObject pausePanel, endPanel;
        readonly List<Card> cards = new List<Card>();
        RectTransform squadRow;

        public void Build(Battle battle, BattleAsset content)
        {
            if (safe == null) BuildStatic();
            foreach (Transform c in squadRow) Destroy(c.gameObject);
            cards.Clear();
            foreach (var f in battle.Squad) cards.Add(BuildCard(f, content.FighterFor(f.Def)));
            pausePanel.SetActive(false);
            endPanel.SetActive(false);
        }

        void BuildStatic()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            gameObject.AddComponent<GraphicRaycaster>();

            safe = Rect("Safe", (RectTransform)transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);

            // Top left: Keep health.
            var keep = Panel("Keep", safe, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(540f, 86f));
            Label("Title", keep, "Keep", 30, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(110f, 60f));
            var barBack = Img("Back", keep, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(380f, 34f));
            keepFill = Img("Fill", barBack.rectTransform, Palette.Health, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(380f, 34f));
            keepFill.type = Image.Type.Filled;
            keepFill.fillMethod = Image.FillMethod.Horizontal;
            keepFill.sprite = Shapes.Square;
            keepText = Label("Value", barBack.rectTransform, "100", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 34f));

            // Top centre: waves.
            var wave = Panel("Waves", safe, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(320f, 70f));
            waveText = Label("Text", wave, "Wave 1 / 1", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 70f));

            // Top right: pause and speed.
            Button("Pause", safe, "II", 36, new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(110f, 90f), () => PausePressed?.Invoke());
            var speed = Button("Speed", safe, "1x", 32, new Vector2(1f, 1f), new Vector2(-170f, -28f), new Vector2(130f, 90f), () => SpeedPressed?.Invoke());
            speedText = speed.GetComponentInChildren<Text>();

            // Bottom left: the squad.
            squadRow = Rect("Squad", safe, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 28f), new Vector2(900f, 210f));
            var layout = squadRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerLeft;

            // Bottom right: Rally, under the right thumb.
            rallyRoot = Rect("Rally", safe, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 40f), new Vector2(250f, 250f));
            var rallyBack = Img("Back", rallyRoot, Palette.Panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f));
            rallyBack.sprite = Shapes.Circle;
            rallyBack.raycastTarget = true;
            rallyFill = Img("Charge", rallyRoot, Palette.Rally, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226f, 226f));
            rallyFill.sprite = Shapes.Circle;
            rallyFill.type = Image.Type.Filled;
            rallyFill.fillMethod = Image.FillMethod.Radial360;
            rallyFill.fillOrigin = (int)Image.Origin360.Top;
            rallyFill.fillClockwise = true;
            rallyText = Label("Text", rallyRoot, "RALLY", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f));
            var rallyButton = rallyRoot.gameObject.AddComponent<Button>();
            rallyButton.targetGraphic = rallyBack;
            rallyButton.onClick.AddListener(() => RallyPressed?.Invoke());

            pausePanel = Overlay("Paused", "Paused", "Take a breather.", out _, out _,
                ("Resume", () => ResumePressed?.Invoke()), ("Restart", () => RestartPressed?.Invoke()));
            endPanel = Overlay("End", "", "", out endTitle, out endBody, ("Play again", () => RestartPressed?.Invoke()));
        }

        Card BuildCard(Fighter f, FighterAsset asset)
        {
            var card = new Card { fighter = f, color = asset != null ? asset.color : Color.white };
            int skills = f.Skills.Count;
            var root = Panel(f.Def.Name, squadRow, Vector2.zero, Vector2.zero, new Vector2(180f, 120f + skills * 30f));
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 180f;
            le.preferredHeight = 120f + skills * 30f;
            card.dot = Img("Dot", root, asset != null ? asset.color : Color.white, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(44f, 44f));
            card.dot.sprite = Shapes.Circle;
            Label("Name", root, f.Def.Name, 24, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(66f, -14f), new Vector2(110f, 44f));
            var hpBack = Img("HpBack", root, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, 1f), new Vector2(14f, -70f), new Vector2(152f, 16f));
            card.hpFill = Img("Hp", hpBack.rectTransform, Palette.Health, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(152f, 16f));
            card.hpFill.type = Image.Type.Filled;
            card.hpFill.fillMethod = Image.FillMethod.Horizontal;
            card.hpFill.sprite = Shapes.Square;
            for (int i = 0; i < skills; i++)
            {
                var y = -100f - i * 30f;
                var back = Img("SkillBack", root, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, 1f), new Vector2(14f, y), new Vector2(152f, 24f));
                var fill = Img("Skill", back.rectTransform, asset != null ? asset.skillColor : Color.white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(152f, 24f));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.sprite = Shapes.Square;
                card.skillFills.Add(fill);
                Label("SkillName", back.rectTransform, f.Skills[i].Def.Name, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(152f, 24f)).color = Palette.TextDark;
            }
            card.koText = Label("KO", root, "Zzz", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 80f));
            return card;
        }

        public void Refresh(Battle battle, float speed)
        {
            ApplySafeArea();
            float keep = battle.KeepMaxHp > 0f ? battle.KeepHp / battle.KeepMaxHp : 0f;
            keepFill.fillAmount = keep;
            keepFill.color = keep < 0.3f ? Palette.HealthLow : Palette.Health;
            keepText.text = $"{Mathf.CeilToInt(battle.KeepHp)} / {Mathf.CeilToInt(battle.KeepMaxHp)}";
            waveText.text = battle.WaveNumber == 0 ? "Get ready" : $"Wave {battle.WaveNumber} / {battle.WaveCount}";
            speedText.text = speed > 1f ? $"{speed:0}x" : "1x";

            foreach (var c in cards)
            {
                var f = c.fighter;
                c.hpFill.fillAmount = f.IsKnockedOut ? 0f : f.Hp / f.MaxHp;
                c.koText.enabled = f.IsKnockedOut;
                c.dot.color = f.IsKnockedOut ? new Color(0.5f, 0.5f, 0.55f) : c.color;
                for (int i = 0; i < c.skillFills.Count; i++) c.skillFills[i].fillAmount = f.Skills[i].Charge;
            }

            rallyFill.fillAmount = battle.RallyCharge;
            bool ready = battle.RallyCharge >= 1f && battle.Outcome == BattleOutcome.Ongoing;
            rallyText.text = ready ? "RALLY!" : "RALLY";
            float pulse = ready ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f : 1f;
            rallyRoot.localScale = Vector3.one * pulse;
            rallyFill.color = ready ? Palette.Rally : Palette.Rally.WithAlpha(0.55f);
        }

        public void ShowPause(bool show) => pausePanel.SetActive(show);

        public void ShowEnd(BattleOutcome outcome, Battle battle)
        {
            endPanel.SetActive(true);
            bool won = outcome == BattleOutcome.Won;
            endTitle.text = won ? "The Keep is safe!" : "The Keep has fallen";
            endBody.text = won
                ? $"All {battle.WaveCount} waves held back with {Mathf.CeilToInt(battle.KeepHp)} health to spare."
                : $"The Heroes broke through on wave {battle.WaveNumber} of {battle.WaveCount}.";
        }

        void ApplySafeArea()
        {
            var area = Screen.safeArea;
            if (area == appliedSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
            appliedSafeArea = area;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        // --- Small UI builders -------------------------------------------------

        GameObject Overlay(string name, string title, string body, out Text titleText, out Text bodyText, params (string label, Action onClick)[] buttons)
        {
            var dim = Img(name, (RectTransform)transform, new Color(0.15f, 0.1f, 0.12f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.raycastTarget = true;
            var panel = Img("Panel", dim.rectTransform, Palette.PanelLight, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
            titleText = Label("Title", panel.rectTransform, title, 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 90f));
            titleText.color = Palette.TextDark;
            bodyText = Label("Body", panel.rectTransform, body, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(680f, 110f));
            bodyText.color = Palette.TextDark;
            float w = 300f, gap = 30f, total = buttons.Length * w + (buttons.Length - 1) * gap;
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, onClick) = buttons[i];
                Button(label, panel.rectTransform, label, 34, new Vector2(0.5f, 0f), new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 40f), new Vector2(w, 100f), () => onClick());
            }
            return dim.gameObject;
        }

        RectTransform Button(string name, RectTransform parent, string label, int size, Vector2 anchor, Vector2 pos, Vector2 dims, Action onClick)
        {
            var img = Img(name, parent, Palette.Panel, anchor, pos, dims);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            Label("Label", img.rectTransform, label, size, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, dims);
            return img.rectTransform;
        }

        RectTransform Panel(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size) =>
            Img(name, parent, Palette.Panel, anchor, pos, size).rectTransform;

        static RectTransform Rect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
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

        static Image Img(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text Label(string name, RectTransform parent, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 dims)
        {
            var rt = Rect(name, parent, anchor, anchor, anchor, pos, dims);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Palette.Text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }

    static class ColorExt
    {
        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }
}
