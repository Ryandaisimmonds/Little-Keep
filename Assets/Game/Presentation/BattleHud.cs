using System;
using System.Collections.Generic;
using LittleKeep.Rules;
using TMPro;
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
            public UiBar hp;
            public Image portrait;
            public Color color;
            public TMP_Text koText;
            public readonly List<UiBar> skills = new List<UiBar>();
        }

        RectTransform safe, squadRow, rallyRoot;
        UiBar keepBar;
        Image rallyFill;
        TMP_Text keepText, headerText, speedText, rallyText, endTitle, endBody;
        GameObject pausePanel, endPanel;
        readonly List<Card> cards = new List<Card>();
        CompiledContent content;

        public void Build(Battle battle, CompiledContent content)
        {
            this.content = content;
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

            var keep = Panel("Keep", safe, new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(520f, 100f));
            Icon("Heart", keep, Theme?.iconHeart, new Vector2(0f, 0.5f), new Vector2(26f, 2f), 60f);
            Label("Title", keep, "Keep", 30, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(96f, 2f), new Vector2(100f, 60f));
            keepBar = Bar("Hp", keep, Palette.Health, new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(300f, 40f));
            keepText = Label("Value", keepBar.Back, "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(300f, 40f));
            Explain.On(keep, "Keep health", Explain.KeepHealth, Theme?.iconHeart);

            var header = Panel("Header", safe, new Vector2(0.5f, 1f), new Vector2(40f, -16f), new Vector2(580f, 90f));
            headerText = Label("Text", header, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(540f, 80f));
            Explain.On(header, "Waves", "Heroes come in waves. Hold back every wave to win the Battle; the next wave comes once the lane is clear.");
            headerText.enableAutoSizing = true;
            headerText.fontSizeMax = 28;
            headerText.fontSizeMin = 18;

            RoundButton("Pause", safe, Theme?.iconPause, "II", new Vector2(1f, 1f), new Vector2(-30f, -16f), 110f, () => PausePressed?.Invoke());
            var speed = RoundButton("Speed", safe, null, "1x", new Vector2(1f, 1f), new Vector2(-150f, -16f), 110f, () => SpeedPressed?.Invoke());
            speedText = speed.GetComponentInChildren<TMP_Text>();

            squadRow = Rect("Squad", safe, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(30f, 20f), new Vector2(1100f, 260f));
            var layout = squadRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerLeft;

            rallyRoot = Rect("Rally", safe, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(260f, 260f));
            var ring = Theme != null && Theme.buttonRound != null ? Theme.buttonRound : Shapes.Circle;
            var rallyBack = Img("Back", rallyRoot, Theme?.buttonRound != null ? Color.white : Palette.Panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 260f), ring);
            rallyBack.preserveAspect = true;
            rallyBack.raycastTarget = true;
            rallyFill = Img("Charge", rallyRoot, Palette.Rally, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(170f, 170f), Shapes.Circle);
            rallyFill.type = Image.Type.Filled;
            rallyFill.fillMethod = Image.FillMethod.Radial360;
            rallyFill.fillOrigin = (int)Image.Origin360.Top;
            rallyText = Label("Text", rallyRoot, "RALLY", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(240f, 100f));
            var rallyButton = rallyRoot.gameObject.AddComponent<Button>();
            rallyButton.targetGraphic = rallyBack;
            rallyButton.onClick.AddListener(() => RallyPressed?.Invoke());
            var rallyInfo = UiKit.Icon("Info", rallyRoot, Theme?.iconInfo, new Vector2(0f, 1f), new Vector2(0f, 10f), 60f);
            Explain.On(rallyInfo, "Rally", Explain.Rally);

            pausePanel = Overlay("Paused", "Paused", "Your Run is saved. If you quit, this Battle starts over next time, but the Keep keeps any damage it has taken.", out _, out var pauseBody,
                ("Resume", ButtonStyle.Green, () => ResumePressed?.Invoke()), ("Quit to the Keep", ButtonStyle.Brown, () => QuitPressed?.Invoke()));
            // Statuses can't be tapped on the lane (holding there aims), so the pause panel explains them.
            var panel = (RectTransform)pauseBody.transform.parent;
            pauseBody.rectTransform.anchoredPosition += new Vector2(0f, 25f);
            Label("Statuses", panel, "Statuses (tap to read):", 26, TextAnchor.MiddleRight, new Vector2(0.5f, 0.5f), new Vector2(-150f, -32f), new Vector2(340f, 60f));
            var elements = new[] { Element.Fire, Element.Frost, Element.Spark, Element.Bloom };
            for (int i = 0; i < elements.Length; i++)
                Explain.Chip(panel, elements[i], new Vector2(0.5f, 0.5f), new Vector2(40f + i * 76f, -32f), 56f);
            endPanel = Overlay("End", "", "", out endTitle, out endBody, ("Continue", ButtonStyle.Green, () => ContinuePressed?.Invoke()));
        }

        Card BuildCard(Fighter f, FighterAsset asset)
        {
            var card = new Card { fighter = f, color = asset != null && asset.art == null ? asset.color : Color.white };
            int skills = f.Skills.Count;
            float height = 136f + skills * 34f;
            var root = UiKit.Card(f.Def.Name, squadRow, Vector2.zero, Vector2.zero, new Vector2(224f, height));
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 224f;
            le.preferredHeight = height;
            var portraitSprite = asset?.art != null && asset.art.idleRight.Length > 0 ? asset.art.idleRight[0] : Shapes.Circle;
            card.portrait = Img("Portrait", root, card.color, new Vector2(0f, 1f), new Vector2(10f, 6f), new Vector2(84f, 84f), portraitSprite);
            card.portrait.preserveAspect = true;
            if (asset?.art != null)
            {
                // Pack frames are mostly empty space around a small character: size by the visible character.
                var a = asset.art;
                float frame = 80f * a.FrameHeight / Mathf.Max(0.05f, a.visibleSize.y);
                var rt = card.portrait.rectTransform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(frame, frame);
                rt.anchoredPosition = new Vector2(50f, -48f) - a.visibleCenter / a.FrameHeight * frame;
            }
            var nameText = Label("Name", root, f.Def.Name, 24, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(96f, -22f), new Vector2(116f, 44f));
            nameText.enableAutoSizing = true;
            nameText.fontSizeMax = 24;
            nameText.fontSizeMin = 14;
            card.hp = Bar("Hp", root, Palette.Health, new Vector2(0f, 1f), new Vector2(16f, -84f), new Vector2(192f, 22f));
            Explain.On(card.hp.Back, $"{f.Def.Name}'s health", Explain.FighterHealth);
            var skillColor = asset != null ? asset.skillColor : Color.white;
            for (int i = 0; i < skills; i++)
            {
                var def = f.Skills[i].Def;
                var barColor = def.Element != Element.None ? ElementLook.Color(def.Element) : skillColor;
                var bar = Bar("Skill", root, def.Growth == SkillGrowth.Master ? Color.Lerp(barColor, Color.white, 0.35f) : barColor, new Vector2(0f, 1f), new Vector2(50f, -116f - i * 34f), new Vector2(158f, 28f));
                card.skills.Add(bar);
                Explain.Chip(root, def.Element, new Vector2(0f, 1f), new Vector2(14f, -114f - i * 34f), 30f);
                var skillAsset = content.SkillAsset(def.Id);
                string desc = (def.Growth == SkillGrowth.Master ? skillAsset?.masterDescription : skillAsset?.description) ?? "";
                Explain.Skill(bar.Back, def, desc + "\n" + Explain.SkillCharge);
                string stars = def.Growth == SkillGrowth.Master ? " ++" : def.Growth == SkillGrowth.Upgraded ? " +" : "";
                var skillName = Label("SkillName", bar.Back, def.Name + stars, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(154f, 28f));
                skillName.enableAutoSizing = true;
                skillName.fontSizeMax = 18;
                skillName.fontSizeMin = 12;
            }
            card.koText = Label("KO", root, "Zzz", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 80f));
            return card;
        }

        public void Refresh(Battle battle, float speed, string header)
        {
            float keep = battle.KeepMaxHp > 0f ? battle.KeepHp / battle.KeepMaxHp : 0f;
            keepBar.Value = keep;
            keepBar.Color = keep < 0.3f ? Palette.HealthLow : Palette.Health;
            keepText.text = $"{Mathf.CeilToInt(battle.KeepHp)} / {Mathf.CeilToInt(battle.KeepMaxHp)}";
            string waves = battle.WaveNumber == 0 ? "Get ready" : $"Wave {battle.WaveNumber}/{battle.WaveCount}";
            headerText.text = string.IsNullOrEmpty(header) ? waves : $"{header}  ·  {waves}";
            speedText.text = speed > 1f ? $"{speed:0}x" : "1x";

            foreach (var c in cards)
            {
                var f = c.fighter;
                c.hp.Value = f.IsKnockedOut ? 0f : f.Hp / f.MaxHp;
                c.koText.enabled = f.IsKnockedOut;
                c.portrait.color = f.IsKnockedOut ? KnockedOut.WithAlpha(0.7f) : c.color;
                for (int i = 0; i < c.skills.Count; i++) c.skills[i].Value = f.Skills[i].Charge;
            }

            rallyFill.fillAmount = battle.RallyCharge;
            bool ready = battle.RallyCharge >= 1f && battle.Outcome == BattleOutcome.Ongoing;
            rallyText.text = ready ? "RALLY!" : "RALLY";
            rallyRoot.localScale = Vector3.one * (ready ? 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f : 1f);
            var rally = Palette.Rally;
            rallyFill.color = ready ? rally : rally.WithAlpha(0.5f);
        }

        public void ShowPause(bool show) => pausePanel.SetActive(show);

        public void ShowEnd(Battle battle)
        {
            endPanel.SetActive(true);
            bool won = battle.Outcome == BattleOutcome.Won;
            Audio.Play(won ? Sound.Won : Sound.Lost);
            endTitle.text = won ? "The Keep holds!" : "The Keep has fallen";
            endBody.text = won
                ? $"All {battle.WaveCount} waves held back, with {Mathf.CeilToInt(battle.KeepHp)} health left."
                : $"The Heroes broke through on wave {battle.WaveNumber} of {battle.WaveCount}.";
            if (won && battle.FreedCreatures.Count > 0) endBody.text += "\nA freed creature is watching you curiously…";
        }

        GameObject Overlay(string name, string title, string body, out TMP_Text titleText, out TMP_Text bodyText, params (string label, ButtonStyle style, Action onClick)[] buttons)
        {
            var dim = Img(name, (RectTransform)transform, new Color(0.08f, 0.06f, 0.05f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.raycastTarget = true;
            var panel = Panel("Panel", SafeChild(dim.rectTransform), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 520f));
            titleText = Title("Title", panel, title, new Vector2(0.5f, 1f), new Vector2(0f, 40f), 760f);
            bodyText = Label("Body", panel, body, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 160f));
            float w = 320f, gap = 30f, total = buttons.Length * w + (buttons.Length - 1) * gap;
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, style, onClick) = buttons[i];
                Button(label, panel, label, 34, new Vector2(0.5f, 0f), new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 60f), new Vector2(w, 110f), onClick, style);
            }
            return dim.gameObject;
        }
    }
}
