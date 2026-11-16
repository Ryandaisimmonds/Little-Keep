using System;
using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;
using UnityEngine.UI;
using static LittleKeep.Game.UiKit;

namespace LittleKeep.Game
{
    /// The Battle screen's controls and readouts, laid out for landscape thumbs:
    /// Keep health top left, Chapter and waves top centre, pause and speed top right, squad bottom left, Rally bottom right.
    public sealed class BattleHud : MonoBehaviour
    {
        public event Action RallyPressed, PausePressed, SpeedPressed, ResumePressed, QuitPressed, ContinuePressed;

        sealed class Card
        {
            public Fighter fighter;
            public Image hpFill, dot;
            public Color color;
            public Text koText;
            public readonly List<Image> skillFills = new List<Image>();
        }

        RectTransform safe, squadRow, rallyRoot;
        Image keepFill, rallyFill;
        Text keepText, headerText, speedText, rallyText, endTitle, endBody;
        GameObject pausePanel, endPanel;
        readonly List<Card> cards = new List<Card>();

        public void Build(Battle battle, CompiledContent content)
        {
            if (safe == null) BuildStatic();
            foreach (Transform c in squadRow) Destroy(c.gameObject);
            cards.Clear();
            foreach (var f in battle.Squad) cards.Add(BuildCard(f, content.FighterAsset(f.Def.Id)));
            pausePanel.SetActive(false);
            endPanel.SetActive(false);
        }

        void BuildStatic()
        {
            Canvas(gameObject, 10, out safe);

            var keep = Panel("Keep", safe, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(540f, 86f));
            Label("Title", keep, "Keep", 30, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(110f, 60f));
            keepFill = Bar("Hp", keep, Palette.Health, new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(380f, 34f));
            keepText = Label("Value", keepFill.rectTransform.parent as RectTransform, "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 34f));

            var header = Panel("Header", safe, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(640f, 70f));
            headerText = Label("Text", header, "", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 70f));

            Button("Pause", safe, "II", 36, new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(110f, 90f), () => PausePressed?.Invoke());
            speedText = Button("Speed", safe, "1x", 32, new Vector2(1f, 1f), new Vector2(-170f, -28f), new Vector2(130f, 90f), () => SpeedPressed?.Invoke()).GetComponentInChildren<Text>();

            squadRow = Rect("Squad", safe, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(40f, 28f), new Vector2(1000f, 240f));
            var layout = squadRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerLeft;

            rallyRoot = Rect("Rally", safe, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 40f), new Vector2(250f, 250f));
            var rallyBack = Img("Back", rallyRoot, Palette.Panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f), Shapes.Circle);
            rallyBack.raycastTarget = true;
            rallyFill = Img("Charge", rallyRoot, Palette.Rally, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226f, 226f), Shapes.Circle);
            rallyFill.type = Image.Type.Filled;
            rallyFill.fillMethod = Image.FillMethod.Radial360;
            rallyFill.fillOrigin = (int)Image.Origin360.Top;
            rallyText = Label("Text", rallyRoot, "RALLY", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 250f));
            var rallyButton = rallyRoot.gameObject.AddComponent<Button>();
            rallyButton.targetGraphic = rallyBack;
            rallyButton.onClick.AddListener(() => RallyPressed?.Invoke());

            pausePanel = Overlay("Paused", "Paused", "Your Run is saved. If you quit, this Battle starts over next time, but the Keep keeps any damage it has taken.", out _, out _,
                ("Resume", () => ResumePressed?.Invoke()), ("Quit to title", () => QuitPressed?.Invoke()));
            endPanel = Overlay("End", "", "", out endTitle, out endBody, ("Continue", () => ContinuePressed?.Invoke()));
        }

        Card BuildCard(Fighter f, FighterAsset asset)
        {
            var card = new Card { fighter = f, color = asset != null ? asset.color : Color.white };
            int skills = f.Skills.Count;
            float height = 120f + skills * 30f;
            var root = Panel(f.Def.Name, squadRow, Vector2.zero, Vector2.zero, new Vector2(180f, height));
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 180f;
            le.preferredHeight = height;
            card.dot = Img("Dot", root, card.color, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(44f, 44f), Shapes.Circle);
            Label("Name", root, f.Def.Name, 24, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(66f, -14f), new Vector2(110f, 44f));
            card.hpFill = Bar("Hp", root, Palette.Health, new Vector2(0f, 1f), new Vector2(14f, -70f), new Vector2(152f, 16f));
            var skillColor = asset != null ? asset.skillColor : Color.white;
            for (int i = 0; i < skills; i++)
            {
                var def = f.Skills[i].Def;
                var fill = Bar("Skill", root, def.Tier == 2 ? Color.Lerp(skillColor, Color.white, 0.35f) : skillColor, new Vector2(0f, 1f), new Vector2(14f, -100f - i * 30f), new Vector2(152f, 24f));
                card.skillFills.Add(fill);
                string stars = def.Tier == 2 ? " ★" : def.Tier == 1 ? " +" : "";
                Label("SkillName", fill.rectTransform.parent as RectTransform, def.Name + stars, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(152f, 24f), Palette.TextDark);
            }
            card.koText = Label("KO", root, "Zzz", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 80f));
            return card;
        }

        public void Refresh(Battle battle, float speed, string header)
        {
            float keep = battle.KeepMaxHp > 0f ? battle.KeepHp / battle.KeepMaxHp : 0f;
            keepFill.fillAmount = keep;
            keepFill.color = keep < 0.3f ? Palette.HealthLow : Palette.Health;
            keepText.text = $"{Mathf.CeilToInt(battle.KeepHp)} / {Mathf.CeilToInt(battle.KeepMaxHp)}";
            string waves = battle.WaveNumber == 0 ? "Get ready" : $"Wave {battle.WaveNumber}/{battle.WaveCount}";
            headerText.text = string.IsNullOrEmpty(header) ? waves : $"{header}  ·  {waves}";
            speedText.text = speed > 1f ? $"{speed:0}x" : "1x";

            foreach (var c in cards)
            {
                var f = c.fighter;
                c.hpFill.fillAmount = f.IsKnockedOut ? 0f : f.Hp / f.MaxHp;
                c.koText.enabled = f.IsKnockedOut;
                c.dot.color = f.IsKnockedOut ? KnockedOut : c.color;
                for (int i = 0; i < c.skillFills.Count; i++) c.skillFills[i].fillAmount = f.Skills[i].Charge;
            }

            rallyFill.fillAmount = battle.RallyCharge;
            bool ready = battle.RallyCharge >= 1f && battle.Outcome == BattleOutcome.Ongoing;
            rallyText.text = ready ? "RALLY!" : "RALLY";
            rallyRoot.localScale = Vector3.one * (ready ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f : 1f);
            rallyFill.color = ready ? Palette.Rally : Palette.Rally.WithAlpha(0.55f);
        }

        public void ShowPause(bool show) => pausePanel.SetActive(show);

        public void ShowEnd(Battle battle)
        {
            endPanel.SetActive(true);
            bool won = battle.Outcome == BattleOutcome.Won;
            endTitle.text = won ? "The Keep holds!" : "The Keep has fallen";
            endBody.text = won
                ? $"All {battle.WaveCount} waves held back, with {Mathf.CeilToInt(battle.KeepHp)} health left."
                : $"The Heroes broke through on wave {battle.WaveNumber} of {battle.WaveCount}.";
            if (won && battle.FreedCreatures.Count > 0) endBody.text += $"\nA freed creature is watching you curiously…";
        }

        GameObject Overlay(string name, string title, string body, out Text titleText, out Text bodyText, params (string label, Action onClick)[] buttons)
        {
            var dim = Img(name, (RectTransform)transform, new Color(0.15f, 0.1f, 0.12f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.raycastTarget = true;
            var panel = Img("Panel", dim.rectTransform, Palette.PanelLight, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 440f)).rectTransform;
            titleText = Label("Title", panel, title, 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(740f, 90f), Palette.TextDark);
            bodyText = Label("Body", panel, body, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(720f, 130f), Palette.TextDark);
            float w = 300f, gap = 30f, total = buttons.Length * w + (buttons.Length - 1) * gap;
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, onClick) = buttons[i];
                Button(label, panel, label, 32, new Vector2(0.5f, 0f), new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 40f), new Vector2(w, 100f), onClick);
            }
            return dim.gameObject;
        }
    }
}
