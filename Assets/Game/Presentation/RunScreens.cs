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
        RectTransform safe, page;
        CompiledContent content;
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f), Top = new Vector2(0.5f, 1f), TopLeft = new Vector2(0f, 1f),
            TopRight = new Vector2(1f, 1f), Bottom = new Vector2(0.5f, 0f), BottomLeft = new Vector2(0f, 0f);

        public void Init(CompiledContent content)
        {
            this.content = content;
            Canvas(gameObject, 5, out safe);
            Backdrop((RectTransform)transform).transform.SetAsFirstSibling();
        }

        public void Hide() => gameObject.SetActive(false);

        RectTransform NewPage()
        {
            gameObject.SetActive(true);
            if (page != null) Destroy(page.gameObject);
            page = Stretch("Page", safe);
            return page;
        }

        // --- The Keep (home) --------------------------------------------------------

        /// Home: the Keep and its rooms. Rooms the Player has not opened yet show as locked with what opens them.
        public void ShowKeep(KeepProgress keep, bool canContinue, int tier, Action onContinue, Action onNewRun,
            Action onBook, Action onWorkshop, Action onWatchtower, string notice = "")
        {
            var p = NewPage();
            Title("Logo", p, "Little Keep", Top, new Vector2(0f, -40f), 900f, 70);
            GemCounter(p, keep);

            int rooms = 2 + (keep.HasWorkshop ? 1 : 0) + (keep.HasWatchtower ? 1 : 0);
            float w = 400f, gap = 30f;
            int slot = 0;
            float X() => Row(slot++, rooms, w, gap);

            var gate = Room(p, X(), "The Gate", "Set out on a Run.", Theme?.iconGate);
            if (canContinue)
            {
                Button("Continue", gate, "Continue Run", 30, Bottom, new Vector2(0f, 150f), new Vector2(w - 70f, 96f), onContinue, ButtonStyle.Green, Theme?.iconPlay);
                Button("New", gate, "New Run", 28, Bottom, new Vector2(0f, 40f), new Vector2(w - 70f, 90f), onNewRun, ButtonStyle.Brown);
            }
            else Button("New", gate, "New Run", 34, Bottom, new Vector2(0f, 60f), new Vector2(w - 70f, 120f), onNewRun, ButtonStyle.Green, Theme?.iconBattle);
            if (keep.HasWatchtower && keep.Content.Tiers.Count > tier)
                Label("Tier", gate, $"Difficulty tier: {keep.Content.Tiers[tier].Name}", 24, TextAnchor.MiddleCenter, Top, new Vector2(0f, -300f), new Vector2(w - 40f, 40f), TextGold);

            int owned = keep.OwnedFamiliars.Count, all = keep.Content.Familiars.Count;
            var book = Room(p, X(), "Familiar Book", $"{owned} of {all} Familiars found.", Theme?.iconBook);
            Button("Open", book, "Open", 32, Bottom, new Vector2(0f, 60f), new Vector2(w - 70f, 110f), onBook, ButtonStyle.Blue);

            if (keep.HasWorkshop)
            {
                var workshop = Room(p, X(), "Workshop", "Spend Gems on Keeper upgrades.", Theme?.iconWorkshop);
                Button("Open", workshop, "Open", 32, Bottom, new Vector2(0f, 60f), new Vector2(w - 70f, 110f), onWorkshop, ButtonStyle.Teal);
            }
            if (keep.HasWatchtower)
            {
                var tower = Room(p, X(), "Watchtower", "Choose the Difficulty tier.", Theme?.iconTower);
                Button("Open", tower, "Open", 32, Bottom, new Vector2(0f, 60f), new Vector2(w - 70f, 110f), onWatchtower, ButtonStyle.Purple);
            }

            if (canContinue) Label("Warn", p, "Starting a new Run replaces the one in progress.", 24, TextAnchor.MiddleCenter, Bottom, new Vector2(0f, 30f), new Vector2(1000f, 40f));
            Toast(p, notice);
        }

        RectTransform Room(RectTransform p, float x, string title, string body, Sprite icon)
        {
            var card = Panel(title, p, Center, new Vector2(x, -60f), new Vector2(400f, 620f));
            Icon("Icon", card, icon, Top, new Vector2(0f, -60f), 150f);
            Label("Title", card, title, 40, TextAnchor.MiddleCenter, Top, new Vector2(0f, -230f), new Vector2(360f, 60f), TextGold);
            Label("Body", card, body, 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -330f), new Vector2(340f, 120f));
            return card;
        }

        /// X position of item i in a centred row of n items.
        static float Row(int i, int n, float w, float gap) => -(n * w + (n - 1) * gap) * 0.5f + w * 0.5f + i * (w + gap);

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
            float w = Mathf.Min(480f, (1700f - (owned.Count - 1) * 30f) / Mathf.Max(1, owned.Count)), gap = 30f, total = owned.Count * w + (owned.Count - 1) * gap;
            for (int i = 0; i < owned.Count; i++)
            {
                var s = owned[i];
                var card = FighterCard(p, s, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -20f), new Vector2(w, 640f));
                Button("Pick", card, "Choose " + s.def.Name, 32, Bottom, new Vector2(0f, 40f), new Vector2(w - 60f, 110f), () => onPick(s.def.Id), ButtonStyle.Green);
            }
            Button("Back", p, "Back", 32, BottomLeft, new Vector2(170f, 80f), new Vector2(260f, 100f), onBack, ButtonStyle.Brown, Theme?.iconHome);
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
                Label("Name", card, have ? asset?.def.Name : "???", 36, TextAnchor.MiddleLeft, TopLeft, new Vector2(220f, -40f), new Vector2(w - 240f, 50f), have ? TextGold : TextColor);
                string body;
                if (have)
                {
                    body = "";
                    float sy = -90f;
                    if (asset != null)
                        foreach (var sk in asset.skills.Where(x => x != null))
                            sy -= SkillRow(card, sk.def, sk.description, $"<color=#FFD15A>{sk.def.Name}</color>: {sk.description}", sy, w - 240f, 22, 110f) + 6f;
                }
                else
                {
                    body = asset?.unlockHint ?? "";
                    if (unlock.Kind == UnlockKind.Bond) body += $"\n<color=#FFD15A>Bond {keep.BondProgress(unlock.Id)}/{unlock.BondSize}</color>";
                }
                Label("Body", card, body, 22, TextAnchor.UpperLeft, TopLeft, new Vector2(220f, -90f), new Vector2(w - 240f, h - 110f));
                if (unlock.Kind == UnlockKind.Bond && !have)
                {
                    var bar = Bar("Bond", card, Theme != null ? Theme.rally : Palette.Rally, Bottom, new Vector2(110f, 30f), new Vector2(w - 260f, 26f));
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
                Label("Name", card, u.Name, 38, TextAnchor.MiddleCenter, Top, new Vector2(0f, -190f), new Vector2(w - 40f, 56f), TextGold);
                Label("Desc", card, u.Description, 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -250f), new Vector2(w - 60f, 90f));
                for (int l = 0; l < u.Costs.Count; l++)
                    Img("Pip", card, l < level ? (Theme != null ? Theme.textGold : Color.yellow) : UiKit.Shade, Top, new Vector2(-(u.Costs.Count - 1) * 22f + l * 44f, -360f), new Vector2(34f, 34f), Shapes.Circle);
                Label("Level", card, $"Level {level} of {u.Costs.Count}", 24, TextAnchor.MiddleCenter, Top, new Vector2(0f, -410f), new Vector2(w - 40f, 40f));
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

            var ground = Img("Path", p, Palette.Lane.WithAlpha(0.85f), Center, new Vector2(0f, -40f), new Vector2(2600f, 150f));
            ground.transform.SetAsFirstSibling();
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

            for (int i = 0; i < 6; i++)
                Img("Fog", p, (Theme != null ? Theme.backdrop : Palette.Sky).WithAlpha(0.15f + i * 0.13f), Center, new Vector2(540f + i * 70f, -40f), new Vector2(80f, 760f));
            Icon("Boss", p, Theme?.iconCrown, Center, new Vector2(800f, 30f), 200f);
            string bossText = run.StepsToBoss <= 1 ? "The boss awaits!" : $"Boss in {run.StepsToBoss} steps";
            Label("BossLabel", p, bossText, 32, TextAnchor.MiddleCenter, Center, new Vector2(800f, -110f), new Vector2(340f, 80f), TextGold);

            Toast(p, run.Message);
            Toast(p, notice, string.IsNullOrEmpty(run.Message) ? 0 : 1);
        }

        void TopBar(RectTransform p, Run run, Action onSquad, Action onQuit)
        {
            var bar = Panel("Bar", p, TopLeft, new Vector2(20f, -14f), new Vector2(1180f, 104f));
            Label("Chapter", bar, $"{run.ChapterName}  ({run.Chapter + 1}/{run.ChapterCount})", 32, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(34f, 2f), new Vector2(430f, 60f));
            Icon("Heart", bar, Theme?.iconHeart, new Vector2(0f, 0.5f), new Vector2(470f, 2f), 56f);
            var hp = Bar("Hp", bar, run.KeepHp / run.KeepMaxHp < 0.3f ? (Theme != null ? Theme.healthLow : Palette.HealthLow) : (Theme != null ? Theme.health : Palette.Health), new Vector2(0f, 0.5f), new Vector2(536f, 0f), new Vector2(300f, 38f));
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
                case StopKind.ToughBattle: return ("Tough Battle", "Stronger Heroes. Two Skill Upgrades and more gold.", ButtonStyle.Red, t?.iconSkull);
                case StopKind.MidBoss: return ("Mid-boss", "A champion blocks the Trail. Two Skill Upgrades.", ButtonStyle.Red, t?.iconCrown);
                case StopKind.Boss: return ("Boss", "Win to finish the Chapter.", ButtonStyle.Purple, t?.iconCrown);
                case StopKind.Rest: return ("Rest", "Patch up the Keep.", ButtonStyle.Green, t?.iconRest);
                case StopKind.Shop: return ("Shop", "Spend gold on Skill Upgrades and repairs.", ButtonStyle.Teal, t?.iconShop);
                case StopKind.Event: return ("Something curious", "Who knows? Choices await.", ButtonStyle.Blue, t?.iconEvent);
                case StopKind.Wild: return ("Wild creature", "Someone might want to join the squad.", ButtonStyle.Green, t?.iconWild);
            }
            return (s.Kind.ToString(), "", ButtonStyle.Brown, null);
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
                var card = (RectTransform)Button("Upgrade", p, "", 1, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -40f), new Vector2(w, 500f), () => onPick(index),
                    o.MakesMaster ? ButtonStyle.Purple : ButtonStyle.Brown).transform;
                Portrait(card, asset, TopLeft, new Vector2(80f, -72f), 230f);
                var who = Label("Who", card, $"{o.Member.Def.Name}\n<size=22>{o.Skill.Base.Name}</size>", 30, TextAnchor.MiddleLeft, TopLeft, new Vector2(140f, -70f), new Vector2(w - 230f, 90f));
                Explain.Chip(card, o.Skill.Base.Element, TopRight, new Vector2(-30f, -40f), 60f);
                Label("Name", card, o.Upgrade.Name, 42, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -150f), new Vector2(w - 80f, 60f), TextGold);
                Label("Desc", card, o.Upgrade.Description, 28, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -215f), new Vector2(w - 80f, 130f));
                Label("Count", card, $"Upgrade {o.Skill.Taken.Count + 1} of {SkillProgress.UpgradesForMaster} towards its Master Form", 22, TextAnchor.UpperLeft, TopLeft, new Vector2(40f, -350f), new Vector2(w - 80f, 60f), TextColor.WithAlpha(0.8f));
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

        RectTransform FighterCard(RectTransform p, FighterAsset asset, Vector2 pos, Vector2 size)
        {
            var card = Panel("Card", p, Center, pos, size);
            if (asset == null) return card;
            Portrait(card, asset, Top, new Vector2(0f, -120f), 400f);
            Label("Name", card, asset.def.Name, 46, TextAnchor.MiddleCenter, Top, new Vector2(0f, -230f), new Vector2(size.x - 60f, 60f), TextGold);
            Label("Desc", card, asset.description, 26, TextAnchor.UpperCenter, Top, new Vector2(0f, -272f), new Vector2(size.x - 90f, 90f));
            float y = -350f;
            foreach (var s in asset.skills.Where(s => s != null))
                y -= SkillRow(card, s.def, s.description, $"<color=#FFD15A>{s.def.Name}</color>: {s.description}", y, size.x - 70f, 22) + 8f;
            return card;
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
                string title = repair ? "Repairs" : item.Upgrade.Upgrade.Name;
                string who = repair ? "For the Keep" : $"{item.Upgrade.Member.Def.Name} · {item.Upgrade.Skill.Base.Name}";
                string body = repair ? $"Repair the Keep by {run.Tuning.ShopRepairAmount:0}." : item.Upgrade.Upgrade.Description + (item.Upgrade.Skill.NextMakesMaster && !item.Sold ? $"\n<color=#FFD15A>Becomes {item.Upgrade.Skill.Base.MasterForm.Name}!</color>" : "");
                if (repair) Icon("Icon", card, Theme?.iconRest, Top, new Vector2(0f, -40f), 100f);
                else
                {
                    Portrait(card, content.FighterAsset(item.Upgrade.Member.Def.Id), Top, new Vector2(0f, -75f), 260f);
                    Explain.Chip(card, item.Upgrade.Skill.Base.Element, TopRight, new Vector2(-24f, -24f), 54f);
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
            var members = new List<SquadMember> { run.Keeper };
            members.AddRange(run.Familiars);
            Title("Title", overlay.rectTransform, "Your squad", Top, new Vector2(0f, -90f), 800f);
            float w = 340f, gap = 20f, total = members.Count * w + (members.Count - 1) * gap;
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var asset = content.FighterAsset(m.Def.Id);
                var card = Panel("Member", overlay.rectTransform, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -30f), new Vector2(w, 640f));
                Portrait(card, asset, Top, new Vector2(0f, -85f), 320f);
                Label("Name", card, m.Def.Name, 38, TextAnchor.MiddleCenter, Top, new Vector2(0f, -170f), new Vector2(w - 30f, 50f), TextGold);
                float sy = -230f;
                foreach (var s in m.Skills)
                {
                    string name = s.IsMaster ? $"<color=#FFD15A>{s.Base.MasterForm.Name} (Master)</color>" : $"<b>{s.Base.Name}</b>";
                    string ups = s.Taken.Count == 0 ? "no upgrades yet" : string.Join("\n", s.Taken.Select(u => "+ " + u.Name));
                    var skillAsset = content.SkillAsset(s.Base.Id);
                    string desc = s.IsMaster ? skillAsset?.masterDescription : skillAsset?.description;
                    sy -= SkillRow(card, s.Build(), desc ?? "", $"{name}\n<size=20>{ups}</size>", sy, w - 50f, 24) + 16f;
                }
            }
            Button("Close", overlay.rectTransform, "Close", 34, Bottom, new Vector2(0f, 50f), new Vector2(300f, 100f), () => { Destroy(overlay.gameObject); onClose?.Invoke(); }, ButtonStyle.Green);
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
            float earnedH = 50f + lines.Count * 42f;
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
                string skills = string.Join("\n", m.Skills.Select(s => s.IsMaster ? s.Base.MasterForm.Name + " (Master)" : $"{s.Base.Name} +{s.Taken.Count}"));
                Label("Name", card, $"<b>{m.Def.Name}</b>\n<size=22>{skills}</size>", 30, TextAnchor.UpperCenter, Top, new Vector2(0f, -150f), new Vector2(w - 30f, 140f));
            }
            Button("Done", p, "Back to the Keep", 36, Bottom, new Vector2(0f, 50f), new Vector2(500f, 110f), onDone, ButtonStyle.Green, Theme?.iconHome);
        }

        // --- Bits --------------------------------------------------------------------------

        /// A message along the bottom. A second message on the same screen goes on row 1, above the first.
        void Toast(RectTransform p, string message, int row = 0)
        {
            if (string.IsNullOrEmpty(message)) return;
            var t = Panel("Toast", p, Bottom, new Vector2(0f, 140f + row * 104f), new Vector2(1200f, 96f));
            Label("Text", t, message, 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(1140f, 80f));
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
