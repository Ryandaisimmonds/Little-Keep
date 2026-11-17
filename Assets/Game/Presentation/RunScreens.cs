using System;
using System.Collections.Generic;
using System.Linq;
using LittleKeep.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LittleKeep.Game.UiKit;

namespace LittleKeep.Game
{
    /// Every screen outside a Battle. Each Show call clears the page and builds the one asked for,
    /// reading the Run and calling back with the Player's choice; the screens hold no game state of their own.
    public sealed class RunScreens : MonoBehaviour
    {
        RectTransform safe, page, scene;
        /// Where the Trail was last drawn (in steps), so the land pans on from there.
        static float lastTrailProgress = -1f;
        CompiledContent content;
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f), Top = new Vector2(0.5f, 1f), TopLeft = new Vector2(0f, 1f),
            TopRight = new Vector2(1f, 1f), Bottom = new Vector2(0.5f, 0f), BottomLeft = new Vector2(0f, 0f);

        public void Init(CompiledContent content)
        {
            this.content = content;
            Canvas(gameObject, 5, out safe);
            Backdrop((RectTransform)transform).transform.SetAsFirstSibling();
            // Full-screen scenery (under the notch too) sits between the backdrop and the safe-area content.
            scene = Stretch("Scene", (RectTransform)transform);
            scene.SetSiblingIndex(1);
        }

        public void Hide() => gameObject.SetActive(false);

        RectTransform NewPage()
        {
            gameObject.SetActive(true);
            if (page != null) Destroy(page.gameObject);
            foreach (Transform c in scene) Destroy(c.gameObject);
            page = Stretch("Page", safe);
            return page;
        }

        // --- Scenery ----------------------------------------------------------------------------------

        /// Sky, two layers of hills that scroll at different speeds (so the land has depth), and the ground.
        /// `progress` is in Trail steps; the hills pan to it from `from`. Returns the ground layer.
        void Landscape(int seed, float trees, float bumpiness, Color far, Color near, float progress, float from, float horizon = 0.12f)
        {
            var sky = Img("Sky", scene, Color.white, Center, Vector2.zero, Vector2.zero, Scenery.Sky());
            Fill(sky.rectTransform);
            sky.preserveAspect = false;
            Strip("FarHills", Scenery.Hills(seed, 0.45f, bumpiness * 0.8f, trees * 0.6f), far, 0.42f, horizon + 0.18f, progress * 0.05f, from * 0.05f);
            Strip("NearHills", Scenery.Hills(seed + 7, 0.5f, bumpiness, trees), near, 0.32f, horizon, progress * 0.12f, from * 0.12f);
            var ground = Img("Ground", scene, Palette.LaneEdge, Center, Vector2.zero, Vector2.zero);
            ground.rectTransform.anchorMin = Vector2.zero;
            ground.rectTransform.anchorMax = new Vector2(1f, 0.14f);
            ground.rectTransform.sizeDelta = Vector2.zero;
        }

        /// A repeating strip anchored to the bottom of the screen: `height` and `bottom` are fractions of the screen.
        void Strip(string name, Sprite sprite, Color tint, float height, float bottom, float offset, float from)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage), typeof(ScrollingStrip));
            var rt = (RectTransform)go.transform;
            rt.SetParent(scene, false);
            rt.anchorMin = new Vector2(0f, bottom);
            rt.anchorMax = new Vector2(1f, bottom + height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.texture = sprite.texture;
            img.color = tint;
            img.raycastTarget = false;
            var strip = go.GetComponent<ScrollingStrip>();
            strip.TileAspect = sprite.rect.width / sprite.rect.height;
            strip.PanTo(offset, from);
            // Ground under the strip, so its bottom edge never shows the sky.
            var under = Img(name + "Ground", scene, tint, Center, Vector2.zero, Vector2.zero);
            under.rectTransform.anchorMin = Vector2.zero;
            under.rectTransform.anchorMax = new Vector2(1f, bottom + 0.005f);
            under.rectTransform.sizeDelta = Vector2.zero;
        }

        static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        /// A pixel-art building standing with its base at (x, groundY), drawn `scale` UI pixels per art pixel.
        Image Building(RectTransform p, string name, Sprite sprite, float x, float groundY, float scale)
        {
            var rt = Rect(name, p, Center, Center, new Vector2(0.5f, 0f), new Vector2(x, groundY), new Vector2(sprite.rect.width, sprite.rect.height) * scale);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        // --- The Keep (home) --------------------------------------------------------

        /// Home: the Keep, drawn as a little castle. A room's building and its signboard appear once it is unlocked.
        public void ShowKeep(KeepProgress keep, bool canContinue, int tier, Action onContinue, Action onNewRun,
            Action onBook, Action onWorkshop, Action onWatchtower, string notice = "")
        {
            var p = NewPage();
            Title("Logo", p, "Little Keep", Top, new Vector2(0f, -40f), Mathf.Min(900f, SafeWidth - 680f), 70);
            SoundToggle(p);
            GemCounter(p, keep);
            var page = p;
            // The castle is laid out for a 16:9 screen; on narrower ones (4:3 tablets) it is squeezed to fit.
            p = Stretch("Keep", page);
            p.localScale = new Vector3(Fit, Fit, 1f);

            // The Keep itself: buildings on a hill. A room's building appears once the room is unlocked.
            Landscape(3, 0.5f, 0.6f, Palette.HillsFar, Palette.HillsNear, 0f, 0f);
            const float ground = -250f, px = 5f;
            var wallStrip = Rect("Wall", p, Center, Center, new Vector2(0.5f, 0f), new Vector2(40f, ground), new Vector2(1240f, 24f * px));
            var wallImg = wallStrip.gameObject.AddComponent<RawImage>();
            var wallSprite = Scenery.Wall();
            wallImg.texture = wallSprite.texture;
            wallImg.uvRect = new Rect(0f, 0f, 1240f / (wallSprite.rect.width * px), 1f);
            wallImg.raycastTarget = false;
            var floor = Img("Floor", p, Palette.Lane, Center, new Vector2(0f, ground - 200f), new Vector2(3200f, 400f));
            floor.raycastTarget = false;

            Building(p, "Library", Scenery.Library(), -520f, ground, px);
            Building(p, "Gatehouse", Scenery.Gatehouse(), 0f, ground, px);
            if (keep.HasWorkshop) Building(p, "Workshop", Scenery.Workshop(), 380f, ground, px);
            if (keep.HasWatchtower) Building(p, "Watchtower", Scenery.Watchtower(), 640f, ground, px);

            // Signboards: the room buttons, in front of their buildings.
            if (canContinue)
            {
                Button("Continue", p, "Continue Run", 30, Center, new Vector2(0f, ground - 80f), new Vector2(330f, 96f), onContinue, ButtonStyle.Green, Theme?.iconPlay);
                Button("New", p, "New Run", 26, Center, new Vector2(0f, ground - 180f), new Vector2(260f, 80f), onNewRun, ButtonStyle.Brown);
            }
            else Button("New", p, "New Run", 34, Center, new Vector2(0f, ground - 90f), new Vector2(330f, 110f), onNewRun, ButtonStyle.Green, Theme?.iconBattle);
            if (keep.HasWatchtower && keep.Content.Tiers.Count > tier)
                Label("Tier", p, $"Difficulty tier: {keep.Content.Tiers[tier].Name}", 22, TextAnchor.MiddleCenter, Center, new Vector2(660f, ground - 250f), new Vector2(320f, 36f), TextGold);

            int owned = keep.OwnedFamiliars.Count, all = keep.Content.Familiars.Count;
            Button("Book", p, "Familiar Book", 28, Center, new Vector2(-520f, ground - 80f), new Vector2(300f, 96f), onBook, ButtonStyle.Blue, Theme?.iconBook);
            Label("BookCount", p, $"{owned} of {all} Familiars found", 22, TextAnchor.MiddleCenter, Center, new Vector2(-520f, ground - 150f), new Vector2(320f, 36f));
            if (keep.HasWorkshop)
                Button("Workshop", p, "Workshop", 28, Center, new Vector2(380f, ground - 80f), new Vector2(240f, 96f), onWorkshop, ButtonStyle.Teal, Theme?.iconWorkshop);
            if (keep.HasWatchtower)
                Button("Watchtower", p, "Watchtower", 26, Center, new Vector2(660f, ground - 180f), new Vector2(250f, 90f), onWatchtower, ButtonStyle.Purple, Theme?.iconTower);

            p = page;
            if (canContinue) Label("Warn", p, "Starting a new Run ends the one in progress (it still pays its Gems).", 22, TextAnchor.MiddleCenter, Bottom, new Vector2(0f, 24f), new Vector2(1000f, 36f));
            Toast(p, notice, bottom: 760f); // up in the sky, clear of the signboards
        }

        /// X position of item i in a centred row of n items.
        static float Row(int i, int n, float w, float gap) => -(n * w + (n - 1) * gap) * 0.5f + w * 0.5f + i * (w + gap);

        /// Sound on or off, remembered between sessions. Falls back to a word when the theme has no icons.
        void SoundToggle(RectTransform p)
        {
            var on = Theme != null ? Theme.iconSound : null;
            var off = Theme != null && Theme.iconMute != null ? Theme.iconMute : on;
            Image icon = null;
            TMP_Text label = null;
            void Refresh()
            {
                if (icon != null)
                {
                    icon.sprite = Audio.Muted ? off : on;
                    // With no mute icon, the sound icon greys out instead.
                    icon.color = Audio.Muted && off == on ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                }
                if (label != null) label.text = Audio.Muted ? "Off" : "On";
            }
            var b = RoundButton("Sound", p, on, "On", TopLeft, new Vector2(20f, -20f), 100f, () =>
            {
                Audio.Muted = !Audio.Muted;
                Refresh();
                if (!Audio.Muted) Audio.Play(Sound.Tap);
            });
            var t = b.transform.Find("Icon");
            if (t != null) icon = t.GetComponent<Image>();
            var l = b.transform.Find("Label");
            if (l != null) label = l.GetComponent<TMP_Text>();
            Refresh();
        }

        RectTransform GemCounter(RectTransform p, KeepProgress keep)
        {
            var gems = Panel("Gems", p, TopRight, new Vector2(-20f, -20f), new Vector2(300f, 100f));
            Icon("Icon", gems, Theme?.iconGem, new Vector2(0f, 0.5f), new Vector2(26f, 2f), 64f);
            Label("Value", gems, $"{keep.Gems}", 40, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(180f, 70f), TextGold);
            Explain.On(gems, "Gems", Explain.Gems, Theme?.iconGem);
            return gems;
        }

        static readonly Color LockedTint = new Color(0.55f, 0.55f, 0.6f, 0.8f);

        public void ShowStarterChoice(IList<FighterAsset> owned, Action<string> onPick, Action onBack)
        {
            var p = NewPage();
            Title("Title", p, "Who comes with you?", Top, new Vector2(0f, -90f), 900f);
            float gap = 24f, w = Mathf.Min(480f, (SafeWidth - 140f - (owned.Count - 1) * gap) / Mathf.Max(1, owned.Count));
            bool compact = owned.Count > 3;
            for (int i = 0; i < owned.Count; i++)
            {
                var s = owned[i];
                var card = FighterCard(p, s, new Vector2(Row(i, owned.Count, w, gap), -50f), new Vector2(w, 760f), compact);
                Button("Pick", card, compact ? s.def.Name : "Choose " + s.def.Name, 32, Bottom, new Vector2(0f, 36f), new Vector2(w - 40f, 100f), () => onPick(s.def.Id), ButtonStyle.Green);
            }
            Button("Back", p, "Back", 32, TopLeft, new Vector2(150f, -70f), new Vector2(240f, 96f), onBack, ButtonStyle.Brown, Theme?.iconHome);
        }

        /// Every Familiar: owned ones as they look, the rest as silhouettes with how to earn them.
        public void ShowBook(KeepProgress keep, Action onBack)
        {
            var p = NewPage();
            Title("Title", p, "Familiar Book", Top, new Vector2(0f, -70f), 900f);
            var list = keep.Content.Familiars;
            int columns = 3;
            float w = 520f, h = 340f, gap = 24f;
            for (int i = 0; i < list.Count; i++)
            {
                var unlock = list[i];
                var asset = content.FighterAsset(unlock.Id);
                bool have = keep.Owns(unlock.Id);
                int col = i % columns, row = i / columns;
                var pos = new Vector2(-(columns - 1) * (w + gap) * 0.5f + col * (w + gap), 160f - row * (h + gap));
                var card = Card("Entry", p, Center, pos, new Vector2(w, h), !have);
                var portrait = Portrait(card, asset, new Vector2(0f, 0.5f), new Vector2(110f, 20f), 200f);
                if (!have) portrait.color = new Color(0f, 0f, 0f, 0.85f);
                Label("Name", card, asset?.def.Name ?? unlock.Id, 36, TextAnchor.MiddleLeft, TopLeft, new Vector2(220f, -40f), new Vector2(w - 240f, 50f), have ? TextGold : TextColor.WithAlpha(0.8f));
                string body;
                if (have)
                {
                    body = "";
                    float sy = -90f;
                    var known = asset != null ? asset.skills.Where(x => x != null).ToList() : new List<SkillAsset>();
                    if (known.Count > 0) sy -= SkillRow(card, known[0].def, known[0].description, $"<color=#FFD15A>{known[0].def.Name}</color>: {known[0].description}", sy, w - 240f, 22, 110f) + 6f;
                    LaterSkills(card, known.Skip(1), sy, w - 240f, 110f);
                }
                else
                {
                    body = asset?.unlockHint ?? "";
                    if (unlock.Kind == UnlockKind.Bond)
                        body = $"Befriend {asset?.def.Name} {unlock.BondSize} times. {body}\n<color=#FFD15A>Bond {keep.BondProgress(unlock.Id)}/{unlock.BondSize}</color>";
                }
                Label("Body", card, body, 22, TextAnchor.UpperLeft, TopLeft, new Vector2(220f, -90f), new Vector2(w - 240f, h - 110f));
                if (unlock.Kind == UnlockKind.Bond && !have)
                {
                    var bar = Bar("Bond", card, Palette.Rally, Bottom, new Vector2(110f, 30f), new Vector2(w - 260f, 26f));
                    bar.Value = keep.BondProgress(unlock.Id) / (float)Mathf.Max(1, unlock.BondSize);
                    Explain.On(bar.Back, "Bond", Explain.Bond);
                }
            }
            Button("Back", p, "Back", 32, BottomLeft, new Vector2(170f, 60f), new Vector2(260f, 100f), onBack, ButtonStyle.Brown, Theme?.iconHome);
        }

        public void ShowWorkshop(KeepProgress keep, Action<string> onBuy, Action onBack)
        {
            var p = NewPage();
            Title("Title", p, "Workshop", Top, new Vector2(0f, -70f), 900f);
            GemCounter(p, keep);
            var ups = keep.Content.Upgrades;
            float w = 400f, gap = 28f, total = ups.Count * w + (ups.Count - 1) * gap;
            for (int i = 0; i < ups.Count; i++)
            {
                var u = ups[i];
                int level = keep.UpgradeLevel(u.Id), cost = keep.NextCost(u.Id);
                var card = Panel(u.Name, p, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -40f), new Vector2(w, 600f));
                Icon("Icon", card, Theme?.iconWorkshop, Top, new Vector2(0f, -50f), 110f);
                string why = Explain.Upgrade(u, level, cost);
                Explain.On(Label("Name", card, u.Name, 38, TextAnchor.MiddleCenter, Top, new Vector2(0f, -190f), new Vector2(w - 40f, 56f), TextGold), u.Name, why, Theme?.iconWorkshop);
                Label("Desc", card, u.Description, 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -250f), new Vector2(w - 60f, 90f));
                for (int l = 0; l < u.Costs.Count; l++)
                    Img("Pip", card, l < level ? (Theme != null ? Theme.textGold : Color.yellow) : UiKit.Shade, Top, new Vector2(-(u.Costs.Count - 1) * 22f + l * 44f, -345f), new Vector2(34f, 34f), Shapes.Circle);
                var pips = Rect("Pips", card, Top, Top, new Vector2(0.5f, 0.5f), new Vector2(0f, -370f), new Vector2(w - 40f, 90f));
                Explain.On(pips, u.Name, why, Theme?.iconWorkshop);
                Label("Level", card, $"Level {level} of {u.Costs.Count}", 24, TextAnchor.MiddleCenter, Top, new Vector2(0f, -390f), new Vector2(w - 40f, 40f));
                Label("Now", card, level == 0 ? "" : UpgradeNow(u, level), 20, TextAnchor.MiddleCenter, Top, new Vector2(0f, -422f), new Vector2(w - 40f, 30f), TextGold);
                var buy = Button("Buy", card, cost < 0 ? "Complete" : $"{cost}", 34, Bottom, new Vector2(0f, 40f), new Vector2(w - 80f, 100f), () => onBuy(u.Id), ButtonStyle.Green, cost < 0 ? null : Theme?.iconGem);
                SetEnabled(buy, cost >= 0 && keep.Gems >= cost);
            }
            Button("Back", p, "Back", 32, BottomLeft, new Vector2(170f, 60f), new Vector2(260f, 100f), onBack, ButtonStyle.Brown, Theme?.iconHome);
        }

        public void ShowWatchtower(KeepProgress keep, int selected, Action<int> onSelect, Action onBack)
        {
            var p = NewPage();
            Title("Title", p, "Watchtower", Top, new Vector2(0f, -70f), 900f);
            var tiers = keep.Content.Tiers;
            float w = 480f, gap = 40f, total = tiers.Count * w + (tiers.Count - 1) * gap;
            for (int i = 0; i < tiers.Count; i++)
            {
                var t = tiers[i];
                int index = i;
                bool open = keep.IsTierUnlocked(i);
                var card = Panel(t.Name, p, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -40f), new Vector2(w, 560f));
                if (!open) card.GetComponent<Image>().color = LockedTint;
                Icon("Icon", card, open ? Theme?.iconTower : Theme?.iconLock, Top, new Vector2(0f, -50f), 110f);
                Label("Name", card, t.Name, 44, TextAnchor.MiddleCenter, Top, new Vector2(0f, -190f), new Vector2(w - 40f, 60f), TextGold);
                Label("Desc", card, open ? t.Description : $"Win a Run on {tiers[Mathf.Max(0, i - 1)].Name} to open.", 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -250f), new Vector2(w - 60f, 120f));
                if (open)
                {
                    var b = Button("Choose", card, i == selected ? "Chosen" : "Choose", 32, Bottom, new Vector2(0f, 40f), new Vector2(w - 80f, 100f), () => onSelect(index), i == selected ? ButtonStyle.Green : ButtonStyle.Brown);
                    SetEnabled(b, i != selected);
                }
            }
            Button("Back", p, "Back", 32, BottomLeft, new Vector2(170f, 60f), new Vector2(260f, 100f), onBack, ButtonStyle.Brown, Theme?.iconHome);
        }

        // --- Trail ---------------------------------------------------------------------

        public void ShowTrail(Run run, Action<int> onChoose, Action onSquad, Action onQuit, string notice = "")
        {
            var p = NewPage();
            TopBar(p, run, onSquad, onQuit);

            // The land scrolls by as the Run walks on: each Chapter has its own hills.
            float progress = run.Chapter * run.StepsInChapter + run.Step;
            float from = lastTrailProgress < 0f || lastTrailProgress > progress ? progress : lastTrailProgress;
            lastTrailProgress = progress;
            var (seed, trees, bumps, far, near) = ChapterLook(run.Chapter);
            Landscape(seed, trees, bumps, far, near, progress, from, horizon: 0.42f);
            var page = p;
            p = Stretch("Trail", page);
            p.localScale = new Vector3(Fit, Fit, 1f);

            var path = Img("Path", p, Palette.Path.WithAlpha(0.9f), Center, new Vector2(0f, -40f), new Vector2(2600f, 110f));
            path.transform.SetAsFirstSibling();
            var keeper = content.FighterAsset(run.Keeper.Def.Id);
            Portrait(p, keeper, Center, new Vector2(-730f, -30f), 360f);
            Label("YouLabel", p, "You", 28, TextAnchor.MiddleCenter, Center, new Vector2(-730f, -150f), new Vector2(200f, 40f));

            var next = run.NextChoices;
            float spacing = 230f;
            for (int i = 0; i < next.Count; i++)
            {
                var y = -40f + (next.Count - 1) * spacing * 0.5f - i * spacing;
                Line(p, new Vector2(-650f, -40f), new Vector2(-390f, y));
                int index = i;
                StopCard(p, next[i], new Vector2(-140f, y), new Vector2(480f, 200f), true, () => onChoose(index));
            }
            var beyond = run.Beyond;
            for (int i = 0; i < beyond.Count; i++)
            {
                var y = -40f + (beyond.Count - 1) * 140f * 0.5f - i * 140f;
                StopCard(p, beyond[i], new Vector2(340f, y), new Vector2(300f, 120f), false, null);
            }
            if (beyond.Count > 0) Label("Then", p, "then…", 28, TextAnchor.MiddleCenter, Center, new Vector2(340f, 240f), new Vector2(300f, 40f));

            // Past the two visible steps the Trail is lost in fog; the boss's castle looms out of it, nearer each step.
            float near01 = 1f - Mathf.Clamp01((run.StepsToBoss - 1f) / Mathf.Max(1f, run.StepsInChapter - 1f));
            var castle = Img("Castle", p, Color.Lerp(Palette.HillsFar, Palette.TextDark, 0.35f + near01 * 0.4f), Center, Vector2.zero, Vector2.zero, Scenery.BossCastle());
            float castleSize = Mathf.Lerp(150f, 330f, near01);
            castle.rectTransform.pivot = new Vector2(0.5f, 0f);
            castle.rectTransform.anchoredPosition = new Vector2(690f, -40f + 30f);
            castle.rectTransform.sizeDelta = new Vector2(castleSize * 56f / 48f, castleSize);
            castle.raycastTarget = false;
            var fog = Img("Fog", scene, Palette.Fog.WithAlpha(Mathf.Lerp(0.95f, 0.55f, near01)), Center, Vector2.zero, Vector2.zero, Scenery.FogRamp());
            fog.preserveAspect = false;
            fog.rectTransform.anchorMin = new Vector2(0.62f, 0f);
            fog.rectTransform.anchorMax = new Vector2(1f, 1f);
            fog.rectTransform.sizeDelta = Vector2.zero;
            // Fog over the far end of the path too, behind the cards for the two visible steps.
            var pathFog = Img("PathFog", p, Palette.Fog.WithAlpha(Mathf.Lerp(0.9f, 0.6f, near01)), Center, Vector2.zero, Vector2.zero, Scenery.FogRamp());
            pathFog.preserveAspect = false;
            pathFog.rectTransform.pivot = new Vector2(0f, 0.5f);
            pathFog.rectTransform.anchoredPosition = new Vector2(470f, 0f);
            pathFog.rectTransform.sizeDelta = new Vector2(1000f, 1400f);
            pathFog.raycastTarget = false;
            pathFog.transform.SetSiblingIndex(1);
            // The castle looms out of the fog: faint while far off, clearer as it nears.
            castle.color = Color.Lerp(castle.color, Palette.Fog, Mathf.Lerp(0.55f, 0.1f, near01));
            castle.transform.SetSiblingIndex(2);
            string bossText = run.StepsToBoss <= 1 ? "The boss awaits!" : $"Boss in {run.StepsToBoss} steps";
            Icon("Boss", p, Theme?.iconCrown, Center, new Vector2(690f, -10f + castleSize + 50f), 90f);
            var bossTag = Panel("BossTag", p, Center, new Vector2(690f, -110f), new Vector2(300f, 76f));
            var bossLabel = Label("BossLabel", bossTag, bossText, 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(280f, 64f), TextGold);
            bossLabel.enableAutoSizing = true;
            bossLabel.fontSizeMax = 30;
            bossLabel.fontSizeMin = 20;

            // Nothing sits along the bottom of the Trail, so its messages share one panel there.
            p = page;
            Toast(p, string.Join("\n", new[] { run.Message, notice }.Where(m => !string.IsNullOrEmpty(m))), bottom: 24f);
        }

        /// Each Chapter's land: meadows, then deep woods, then a bare, bumpy hill.
        static (int seed, float trees, float bumps, Color far, Color near) ChapterLook(int chapter)
        {
            switch (chapter)
            {
                case 0: return (11, 0.35f, 0.7f, Palette.HillsFar, Palette.HillsNear);
                case 1: return (23, 0.95f, 0.8f, Color.Lerp(Palette.HillsFar, Palette.Trees, 0.35f), Palette.Trees);
                default:
                    var grey = new Color(0.55f, 0.55f, 0.52f);
                    return (37, 0.2f, 1.3f, Color.Lerp(Palette.HillsFar, grey, 0.45f), Color.Lerp(Palette.HillsNear, grey, 0.35f));
            }
        }

        void TopBar(RectTransform p, Run run, Action onSquad, Action onQuit)
        {
            var bar = Panel("Bar", p, TopLeft, new Vector2(20f, -14f), new Vector2(1180f, 104f));
            bar.localScale = new Vector3(Fit, Fit, 1f); // leaves room for the Squad and Title buttons on 4:3
            Label("Chapter", bar, $"{run.ChapterName}  ({run.Chapter + 1}/{run.ChapterCount})", 32, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(34f, 2f), new Vector2(430f, 60f));
            Icon("Heart", bar, Theme?.iconHeart, new Vector2(0f, 0.5f), new Vector2(470f, 2f), 56f);
            var hp = Bar("Hp", bar, run.KeepHp / run.KeepMaxHp < 0.3f ? Palette.HealthLow : Palette.Health, new Vector2(0f, 0.5f), new Vector2(536f, 0f), new Vector2(300f, 38f));
            hp.Value = run.KeepHp / run.KeepMaxHp;
            Explain.On(hp.Back, "Keep health", Explain.KeepHealth, Theme?.iconHeart);
            Label("HpText", hp.Back, $"{Mathf.CeilToInt(run.KeepHp)}/{Mathf.CeilToInt(run.KeepMaxHp)}", 24, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(300f, 38f));
            Icon("GoldIcon", bar, Theme?.iconGold, new Vector2(0f, 0.5f), new Vector2(870f, 2f), 60f);
            Explain.On(Label("Gold", bar, $"{run.Gold} Gold", 32, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(940f, 2f), new Vector2(220f, 60f), TextGold), "Gold", Explain.Gold, Theme?.iconGold);
            if (onSquad != null) Button("Squad", p, "Squad", 32, TopRight, new Vector2(onQuit != null ? -170f : -20f, -14f), new Vector2(240f, 104f), onSquad, ButtonStyle.Blue, Theme?.iconSquad);
            if (onQuit != null) RoundButton("Quit", p, Theme?.iconHome, "Title", TopRight, new Vector2(-20f, -10f), 110f, onQuit);
        }

        void StopCard(RectTransform p, Stop stop, Vector2 pos, Vector2 size, bool live, Action onClick)
        {
            var (title, body, style, icon) = Describe(stop);
            RectTransform card;
            if (live) card = (RectTransform)Button(title, p, "", 1, Center, pos, size, onClick, style).transform;
            else
            {
                card = Card(title, p, Center, pos, size, true);
                card.GetComponent<Image>().color = Color.white.WithAlpha(0.55f);
            }
            float iconSize = live ? 96f : 60f;
            Icon("Icon", card, icon, new Vector2(0f, 0.5f), new Vector2(26f, 4f), iconSize);
            float textX = iconSize + 44f;
            var titleText = Label("Title", card, title, live ? 38 : 26, TextAnchor.MiddleLeft, TopLeft, new Vector2(textX, live ? -26f : -30f), new Vector2(size.x - textX - 24f, live ? 54f : 60f));
            titleText.enableAutoSizing = true;
            titleText.fontSizeMax = live ? 38 : 26;
            titleText.fontSizeMin = 18;
            if (live) Label("Body", card, body, 24, TextAnchor.UpperLeft, TopLeft, new Vector2(textX, -84f), new Vector2(size.x - textX - 30f, size.y - 96f));
        }

        static (string title, string body, ButtonStyle style, Sprite icon) Describe(Stop s)
        {
            var t = Theme;
            switch (s.Kind)
            {
                case StopKind.Battle: return ("Battle", "Hold off a band of Heroes, then pick a Skill Upgrade.", ButtonStyle.Brown, t?.iconBattle);
                case StopKind.ToughBattle: return ("Tough Battle", $"Stronger Heroes. {Upgrades(s.Kind)} and more Gold.", ButtonStyle.Red, t?.iconSkull);
                case StopKind.MidBoss: return ("Mid-boss", $"A champion blocks the Trail. {Upgrades(s.Kind)}.", ButtonStyle.Red, t?.iconCrown);
                case StopKind.Boss: return ("Boss", "Win to finish the Chapter.", ButtonStyle.Purple, t?.iconCrown);
                case StopKind.Rest: return ("Rest", "Patch up the Keep.", ButtonStyle.Green, t?.iconRest);
                case StopKind.Shop: return ("Shop", "Spend Gold on Skill Upgrades and repairs.", ButtonStyle.Teal, t?.iconShop);
                case StopKind.Event: return ("Something curious", "Who knows? Choices await.", ButtonStyle.Blue, t?.iconEvent);
                case StopKind.Wild: return ("Wild creature", "Someone might want to join the squad.", ButtonStyle.Green, t?.iconWild);
            }
            return (s.Kind.ToString(), "", ButtonStyle.Brown, null);
        }

        /// The description of a Skill as it stands: its Master Form's once earned.
        string SkillText(SkillProgress s)
        {
            var a = content.SkillAsset(s.Base.Id);
            if (a == null) return "";
            return s.IsMaster ? a.masterDescription : a.description;
        }

        static string UpgradeNow(KeeperUpgradeDef u, int level) => "Now: " + Explain.UpgradeEffect(u, level);

        static string Upgrades(StopKind kind)
        {
            int n = Run.UpgradesFor(kind);
            return n == 1 ? "One Skill Upgrade" : n == 2 ? "Two Skill Upgrades" : $"{n} Skill Upgrades";
        }

        // --- After a Battle ----------------------------------------------------------------

        public void ShowUpgrade(Run run, Action<int> onPick, Action onSkip, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            Title("Title", p, "Choose a Skill Upgrade", Top, new Vector2(0f, -150f), 1000f);
            var offer = run.UpgradeOffer;
            float w = 520f, gap = 40f, total = offer.Count * w + (offer.Count - 1) * gap;
            for (int i = 0; i < offer.Count; i++)
            {
                var o = offer[i];
                int index = i;
                var asset = content.FighterAsset(o.Member.Def.Id);
                var card = (RectTransform)Button("Upgrade", p, "", 1, Center, new Vector2(Row(i, offer.Count, w, gap), -40f), new Vector2(w, 500f), () => onPick(index),
                    o.Learns ? ButtonStyle.Teal : o.MakesMaster ? ButtonStyle.Purple : ButtonStyle.Brown).transform;
                Portrait(card, asset, TopLeft, new Vector2(80f, -72f), 230f);
                var who = Label("Who", card, $"{o.Member.Def.Name}\n<size=22>{o.Skill.Base.Name}</size>", 30, TextAnchor.MiddleLeft, TopLeft, new Vector2(140f, -70f), new Vector2(w - 230f, 90f));
                Explain.SkillChip(card, o.Skill.Build(), SkillText(o.Skill), TopRight, new Vector2(-30f, -40f), 60f);
                Label("Name", card, o.Learns ? $"Learn {o.Skill.Base.Name}" : o.Upgrade.Name, 42, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -150f), new Vector2(w - 80f, 60f), TextGold);
                Label("Desc", card, o.Learns ? SkillText(o.Skill) : o.Upgrade.Description, o.Learns ? 24 : 28, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -215f), new Vector2(w - 80f, 130f));
                Label("Count", card, o.Learns ? $"A new Skill for {o.Member.Def.Name}, ready from the next Battle."
                    : $"Upgrade {o.Skill.Taken.Count + 1} of {SkillProgress.UpgradesForMaster} towards its Master Form", 22, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, o.Learns ? -370f : -350f), new Vector2(w - 80f, 60f), TextColor.WithAlpha(0.8f));
                if (o.MakesMaster && o.Skill.Base.MasterForm != null)
                {
                    var skillAsset = content.SkillAsset(o.Skill.Base.Id);
                    Label("Master", card, $"Becomes {o.Skill.Base.MasterForm.Name}!\n<size=22>{skillAsset?.masterDescription}</size>", 28, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -410f), new Vector2(w - 80f, 120f), TextGold);
                }
            }
            Button("Skip", p, "Skip", 30, Bottom, new Vector2(0f, 30f), new Vector2(260f, 90f), onSkip, ButtonStyle.Brown);
            Toast(p, run.Message);
        }

        public void ShowBefriend(Run run, Action<int> onBefriend, Action onPass, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            var asset = content.FighterAsset(run.CreatureOffer.Id);
            Title("Title", p, $"{run.CreatureOffer.Name} wants to join!", Top, new Vector2(0f, -150f), 1100f);
            FighterCard(p, asset, new Vector2(-380f, -90f), new Vector2(580f, 600f));
            if (!run.SquadFull)
                Button("Befriend", p, "Befriend", 44, Center, new Vector2(330f, 20f), new Vector2(500f, 150f), () => onBefriend(-1), ButtonStyle.Green, Theme?.iconHeart);
            else
            {
                Label("Full", p, "The squad is full. Who heads home to make room?", 32, TextAnchor.MiddleCenter, Center, new Vector2(330f, 200f), new Vector2(720f, 90f));
                for (int i = 0; i < run.Familiars.Count; i++)
                {
                    int index = i;
                    Button("Swap", p, "Swap with " + run.Familiars[i].Def.Name, 30, Center, new Vector2(330f, 100f - i * 104f), new Vector2(500f, 92f), () => onBefriend(index), ButtonStyle.Blue);
                }
            }
            Button("Pass", p, "Not this time", 32, Center, new Vector2(330f, -340f), new Vector2(500f, 100f), onPass, ButtonStyle.Brown);
        }

        /// A Familiar's card. Compact cards (for narrow columns) drop the description and list every Skill as a chip.
        RectTransform FighterCard(RectTransform p, FighterAsset asset, Vector2 pos, Vector2 size, bool compact = false)
        {
            var card = Panel("Card", p, Center, pos, size);
            if (asset == null) return card;
            Portrait(card, asset, Top, new Vector2(0f, -120f), compact ? 320f : 400f);
            if (compact)
            {
                Label("Name", card, asset.def.Name, 38, TextAnchor.MiddleCenter, Top, new Vector2(0f, -230f), new Vector2(size.x - 30f, 56f), TextGold);
                float cy = -290f;
                foreach (var sk in asset.skills.Where(x => x != null))
                    cy -= SkillRow(card, sk.def, sk.description, sk == asset.skills[0] ? $"<color=#FFD15A>{sk.def.Name}</color>" : $"<alpha=#AA>{sk.def.Name}", cy, size.x - 40f, 20) + 8f;
                Label("Hint", card, "First Skill known; the rest are learned on the way.", 18, TextAnchor.UpperCenter, Top, new Vector2(0f, cy - 6f), new Vector2(size.x - 40f, 60f), TextColor.WithAlpha(0.7f));
                return card;
            }
            Label("Name", card, asset.def.Name, 46, TextAnchor.MiddleCenter, Top, new Vector2(0f, -230f), new Vector2(size.x - 60f, 60f), TextGold);
            Label("Desc", card, asset.description, 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -272f), new Vector2(size.x - 90f, 90f));
            var skills = asset.skills.Where(s => s != null).ToList();
            float y = -350f;
            if (skills.Count > 0) y -= SkillRow(card, skills[0].def, skills[0].description, $"<color=#FFD15A>{skills[0].def.Name}</color>: {skills[0].description}", y, size.x - 70f, 22) + 8f;
            LaterSkills(card, skills.Skip(1), y, size.x - 70f);
            return card;
        }

        /// The Skills a Familiar learns during a Run: a row of tappable chips with their names beneath.
        float LaterSkills(RectTransform parent, IEnumerable<SkillAsset> later, float y, float width, float x = 0f)
        {
            var list = later.ToList();
            if (list.Count == 0) return 0f;
            var row = Rect("Later", parent, Top, Top, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(width, 64f));
            Label("Label", row, "Learns on the way:", 20, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(200f, 36f), TextColor.WithAlpha(0.85f));
            for (int i = 0; i < list.Count; i++)
                Explain.SkillChip(row, list[i].def, list[i].description, new Vector2(0f, 1f), new Vector2(200f + i * 44f, 0f), 36f);
            Label("Names", row, string.Join(", ", list.Select(s => s.def.Name)), 18, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, -38f), new Vector2(width, 26f), TextColor.WithAlpha(0.75f));
            return 64f;
        }

        /// One Skill line: a tappable Element chip, then text that explains the Skill when tapped. Returns its height.
        float SkillRow(RectTransform parent, SkillDef def, string description, string text, float y, float width, int fontSize, float x = 0f)
        {
            float chip = fontSize * 1.5f;
            var row = Rect("Skill", parent, Top, Top, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(width, chip));
            Explain.Chip(row, def.Element, new Vector2(0f, 1f), new Vector2(0f, 0f), chip);
            var label = Label("Text", row, text, fontSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(chip + 10f, -2f), new Vector2(width - chip - 10f, chip));
            label.ForceMeshUpdate();
            float h = Mathf.Max(chip, label.preferredHeight);
            label.rectTransform.sizeDelta = new Vector2(width - chip - 10f, h);
            row.sizeDelta = new Vector2(width, h);
            Explain.Skill(label, def, description);
            return h;
        }

        /// A character's idle animation with the visible character centred on pos and about `size` pixels tall,
        /// or a coloured circle when it has no art yet.
        Image Portrait(RectTransform parent, FighterAsset asset, Vector2 anchor, Vector2 pos, float size)
        {
            bool art = asset?.art != null && asset.art.idleRight.Length > 0;
            Vector2 dims = new Vector2(size, size) * 0.45f;
            if (art)
            {
                var a = asset.art;
                float frame = size * 0.45f * a.FrameHeight / Mathf.Max(0.05f, a.visibleSize.y);
                dims = new Vector2(frame, frame);
                pos -= a.visibleCenter / a.FrameHeight * frame;
            }
            var rect = Rect("Portrait", parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, dims);
            var img = rect.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = art ? Color.white : asset != null ? asset.color : Color.white;
            SetSprite(img, art ? asset.art.idleRight[0] : Shapes.Circle);
            img.preserveAspect = true;
            if (art) img.gameObject.AddComponent<SpriteFlipbook>().Frames = asset.art.idleRight;
            return img;
        }

        // --- Shop and events --------------------------------------------------------------

        public void ShowShop(Run run, Action<int> onBuy, Action onLeave, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            Title("Title", p, "The travelling shop", Top, new Vector2(0f, -150f), 1000f);
            var items = run.Shop;
            float w = 420f, gap = 30f, total = items.Count * w + (items.Count - 1) * gap;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int index = i;
                var x = -total * 0.5f + w * 0.5f + i * (w + gap);
                var card = Panel("Item", p, Center, new Vector2(x, -60f), new Vector2(w, 500f));
                bool repair = item.Kind == ShopItemKind.Repair;
                bool learn = !repair && item.Upgrade.Learns;
                string title = repair ? "Repairs" : learn ? $"Learn {item.Upgrade.Skill.Base.Name}" : item.Upgrade.Upgrade.Name;
                string who = repair ? "For the Keep" : $"{item.Upgrade.Member.Def.Name} · {item.Upgrade.Skill.Base.Name}";
                string body = repair ? $"Repair the Keep by {run.Tuning.ShopRepairAmount:0}."
                    : learn ? $"A new Skill: {SkillText(item.Upgrade.Skill)}"
                    : item.Upgrade.Upgrade.Description + (item.Upgrade.Skill.NextMakesMaster && !item.Sold ? $"\n<color=#FFD15A>Becomes {item.Upgrade.Skill.Base.MasterForm.Name}!</color>" : "");
                if (repair) Icon("Icon", card, Theme?.iconRest, Top, new Vector2(0f, -40f), 100f);
                else
                {
                    Portrait(card, content.FighterAsset(item.Upgrade.Member.Def.Id), Top, new Vector2(0f, -75f), 260f);
                    Explain.SkillChip(card, item.Upgrade.Skill.Build(), SkillText(item.Upgrade.Skill), TopRight, new Vector2(-24f, -24f), 54f);
                }
                Label("Who", card, who, 24, TextAnchor.UpperCenter, Top, new Vector2(0f, -150f), new Vector2(w - 60f, 40f), TextColor.WithAlpha(0.8f));
                Label("Title", card, title, 36, TextAnchor.UpperCenter, Top, new Vector2(0f, -190f), new Vector2(w - 60f, 50f), TextGold);
                Label("Body", card, body, 24, TextAnchor.UpperCenter, Top, new Vector2(0f, -248f), new Vector2(w - 70f, 130f));
                var buy = Button("Buy", card, item.Sold ? "Sold" : $"{item.Price}", 32, Bottom, new Vector2(0f, 34f), new Vector2(w - 90f, 96f), () => onBuy(index), ButtonStyle.Green, item.Sold ? null : Theme?.iconGold);
                SetEnabled(buy, run.CanBuy(index));
            }
            Button("Leave", p, "Leave the shop", 32, Bottom, new Vector2(0f, 26f), new Vector2(400f, 96f), onLeave, ButtonStyle.Brown);
            Toast(p, run.Message);
        }

        public void ShowEvent(Run run, Action<int> onChoose, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            var e = run.CurrentEvent;
            var card = Panel("Event", p, Center, new Vector2(0f, -70f), new Vector2(1240f, 760f));
            Title("Title", card, e.Title, Top, new Vector2(0f, 50f), 900f);
            Icon("Icon", card, Theme?.iconEvent, Top, new Vector2(0f, -150f), 110f);
            Label("Text", card, e.Text, 32, TextAnchor.UpperCenter, Top, new Vector2(0f, -230f), new Vector2(1060f, 180f));
            for (int i = 0; i < e.Options.Count; i++)
            {
                int index = i;
                var o = e.Options[i];
                string label = o.GoldCost > 0 ? $"{o.Text}  ({o.GoldCost} Gold)" : o.Text;
                var b = Button("Option", card, label, 32, Bottom, new Vector2(0f, 70f + (e.Options.Count - 1 - i) * 120f), new Vector2(940f, 104f), () => onChoose(index),
                    i == 0 ? ButtonStyle.Green : ButtonStyle.Brown);
                SetEnabled(b, run.CanChoose(i));
            }
        }

        // --- Squad and summary ---------------------------------------------------------------

        public void ShowSquad(Run run, Action onClose)
        {
            var overlay = Img("Squad", (RectTransform)transform, new Color(0.08f, 0.06f, 0.05f, 0.82f), Center, Vector2.zero, Vector2.zero);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.raycastTarget = true;
            // The dim covers the whole screen; everything on it stays inside the safe area.
            var area = SafeChild(overlay.rectTransform);
            var members = new List<SquadMember> { run.Keeper };
            members.AddRange(run.Familiars);
            Title("Title", area, "Your squad", Top, new Vector2(0f, -90f), 800f);
            float gap = 20f, w = Mathf.Min(340f, (SafeWidth - 140f - (members.Count - 1) * gap) / members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var asset = content.FighterAsset(m.Def.Id);
                var card = Panel("Member", area, Center, new Vector2(Row(i, members.Count, w, gap), -30f), new Vector2(w, 640f));
                Portrait(card, asset, Top, new Vector2(0f, -85f), 320f);
                Label("Name", card, m.Def.Name, 38, TextAnchor.MiddleCenter, Top, new Vector2(0f, -170f), new Vector2(w - 30f, 50f), TextGold);
                float sy = -230f;
                foreach (var s in m.Skills)
                {
                    string name = s.IsMaster ? $"<color=#FFD15A>{s.Base.MasterForm.Name} (Master)</color>" : s.IsLearned ? $"<b>{s.Base.Name}</b>" : $"<alpha=#88>{s.Base.Name}";
                    string ups = !s.IsLearned ? "<alpha=#88>not learned yet" : s.Taken.Count == 0 ? "no upgrades yet" : string.Join("\n", s.Taken.Select(u => "+ " + u.Name));
                    var skillAsset = content.SkillAsset(s.Base.Id);
                    string desc = s.IsMaster ? skillAsset?.masterDescription : skillAsset?.description;
                    sy -= SkillRow(card, s.Build(), desc ?? "", $"{name}\n<size=20>{ups}</size>", sy, w - 50f, 22) + 10f;
                }
            }
            Button("Close", area, "Close", 34, Bottom, new Vector2(0f, 50f), new Vector2(300f, 100f), () => { Destroy(overlay.gameObject); onClose?.Invoke(); }, ButtonStyle.Green);
        }

        public void ShowSummary(Run run, RunRewards rewards, KeepProgress keep, Action onDone)
        {
            var p = NewPage();
            var r = run.Result;
            Title("Title", p, r.Won ? "The Keep is safe!" : "The Keep has fallen", Top, new Vector2(0f, -90f), 1200f, 64);
            string body = r.Won
                ? $"You saw off every Hero across all {run.ChapterCount} Chapters."
                : $"Your Run ended in {run.ChapterName} (Chapter {r.ChapterReached} of {run.ChapterCount}).";
            body += $"   Battles won: {r.BattlesWon}";
            Label("Body", p, body, 32, TextAnchor.MiddleCenter, Top, new Vector2(0f, -230f), new Vector2(1600f, 60f));

            var lines = new List<string> { $"<color=#FFD15A>+{rewards?.Gems ?? 0} Gems</color>  (you have {keep.Gems})" };
            if (rewards != null)
            {
                foreach (var id in rewards.NewFamiliars) lines.Add($"{content.FighterAsset(id)?.def.Name ?? id} joins your Familiars for good!");
                foreach (var id in rewards.Deeds) lines.Add($"Deed complete: {keep.Content.Deeds.Find(d => d.Id == id)?.Name}");
                if (rewards.TierUnlocked >= 0) lines.Add($"New Difficulty tier in the Watchtower: {keep.Content.Tiers[rewards.TierUnlocked].Name}");
                var bonds = (r.Befriended ?? new List<string>()).Distinct()
                    .Where(id => !keep.Owns(id) && keep.Content.Familiars.Exists(f => f.Id == id && f.Kind == UnlockKind.Bond))
                    .Select(id => $"{content.FighterAsset(id)?.def.Name} {keep.BondProgress(id)}/{keep.Content.Familiars.Find(f => f.Id == id).BondSize}");
                var bondText = string.Join(",  ", bonds);
                if (bondText.Length > 0) lines.Add("Bonds: " + bondText);
            }
            float earnedH = 60f + lines.Count * 48f;
            var earned = Panel("Earned", p, Top, new Vector2(0f, -275f), new Vector2(1000f, earnedH));
            Icon("Gem", earned, Theme?.iconGem, new Vector2(0f, 0.5f), new Vector2(70f, 0f), 80f);
            Label("Lines", earned, string.Join("\n", lines), 28, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(830f, earnedH - 20f));
            float cardsY = -275f - earnedH - 30f;

            var members = new List<SquadMember> { run.Keeper };
            members.AddRange(run.Familiars);
            float w = 300f, gap = 24f, total = members.Count * w + (members.Count - 1) * gap;
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var card = Card("Member", p, Top, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), cardsY), new Vector2(w, 300f));
                Portrait(card, content.FighterAsset(m.Def.Id), Top, new Vector2(0f, -70f), 280f);
                Label("Name", card, $"<b>{m.Def.Name}</b>", 30, TextAnchor.UpperCenter, Top, new Vector2(0f, -150f), new Vector2(w - 30f, 40f));
                float sy = -192f;
                foreach (var s in m.Skills.Where(x => x.IsLearned))
                {
                    string label = s.IsMaster ? s.Base.MasterForm.Name + " (Master)" : $"{s.Base.Name} +{s.Taken.Count}";
                    sy -= SkillRow(card, s.Build(), SkillText(s), label, sy, w - 40f, 20) + 4f;
                }
            }
            Button("Done", p, "Back to the Keep", 36, Bottom, new Vector2(0f, 50f), new Vector2(500f, 110f), onDone, ButtonStyle.Green, Theme?.iconHome);
        }

        // --- Bits --------------------------------------------------------------------------

        /// A message along the bottom, one line per line of text.
        void Toast(RectTransform p, string message, float bottom = 140f)
        {
            if (string.IsNullOrEmpty(message)) return;
            int lines = message.Split('\n').Length;
            float h = 96f + (lines - 1) * 40f;
            var t = Panel("Toast", p, Bottom, new Vector2(0f, bottom), new Vector2(1200f, h));
            Label("Text", t, message, 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(1140f, h - 16f));
        }

        static void Line(RectTransform p, Vector2 from, Vector2 to)
        {
            var d = to - from;
            var img = Img("Line", p, Palette.LaneEdge, Center, (from + to) * 0.5f, new Vector2(d.magnitude, 12f));
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            img.transform.SetSiblingIndex(1);
        }
    }

    /// Plays a looping set of sprites on a UI Image (character portraits on menus).
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] Frames;
        public float FramesPerSecond = 6f;
        Image image;
        float time;

        void Awake() => image = GetComponent<Image>();

        void Update()
        {
            if (Frames == null || Frames.Length == 0 || image == null) return;
            time += Time.unscaledDeltaTime;
            image.sprite = Frames[(int)(time * FramesPerSecond) % Frames.Length];
        }
    }
}
