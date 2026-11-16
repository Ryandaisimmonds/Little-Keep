using System;
using System.Collections.Generic;
using System.Linq;
using LittleKeep.Rules;
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
            TopRight = new Vector2(1f, 1f), Bottom = new Vector2(0.5f, 0f), BottomLeft = new Vector2(0f, 0f), BottomRight = new Vector2(1f, 0f);

        public void Init(CompiledContent content)
        {
            this.content = content;
            Canvas(gameObject, 5, out safe);
            var bg = Img("Backdrop", (RectTransform)transform, Palette.Sky, Center, Vector2.zero, Vector2.zero);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.transform.SetAsFirstSibling();
        }

        public void Hide() => gameObject.SetActive(false);

        RectTransform NewPage()
        {
            gameObject.SetActive(true);
            if (page != null) Destroy(page.gameObject);
            page = Stretch("Page", safe);
            return page;
        }

        // --- Title and start -----------------------------------------------------

        public void ShowTitle(bool canContinue, Action onContinue, Action onNewRun)
        {
            var p = NewPage();
            Label("Title", p, "Little Keep", 120, TextAnchor.MiddleCenter, Center, new Vector2(0f, 240f), new Vector2(1400f, 180f), Palette.TextDark);
            Label("Tag", p, "Hold the Keep. Grow the squad.", 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, 120f), new Vector2(1200f, 60f), Palette.TextDark);
            if (canContinue) Button("Continue", p, "Continue Run", 40, Center, new Vector2(0f, -40f), new Vector2(520f, 120f), onContinue);
            Button("New", p, "New Run", 40, Center, new Vector2(0f, canContinue ? -190f : -40f), new Vector2(520f, 120f), onNewRun);
            if (canContinue) Label("Warn", p, "Starting a new Run replaces the one in progress.", 24, TextAnchor.MiddleCenter, Center, new Vector2(0f, -290f), new Vector2(1000f, 40f), Palette.TextDark);
        }

        public void ShowStarterChoice(Action<string> onPick, Action onBack)
        {
            var p = NewPage();
            Label("Title", p, "Who comes with you?", 60, TextAnchor.MiddleCenter, Top, new Vector2(0f, -90f), new Vector2(1400f, 100f), Palette.TextDark);
            var starters = content.Source.starters.Where(s => s != null).ToList();
            float w = 460f, gap = 40f, total = starters.Count * w + (starters.Count - 1) * gap;
            for (int i = 0; i < starters.Count; i++)
            {
                var s = starters[i];
                var card = FighterCard(p, s, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 20f), new Vector2(w, 520f));
                Button("Pick", card, "Choose " + s.def.Name, 30, Bottom, new Vector2(0f, 30f), new Vector2(w - 60f, 90f), () => onPick(s.def.Id), Palette.Rally.WithAlpha(1f));
            }
            Button("Back", p, "Back", 30, BottomLeft, new Vector2(150f, 70f), new Vector2(220f, 90f), onBack);
        }

        // --- Trail ---------------------------------------------------------------------

        public void ShowTrail(Run run, Action<int> onChoose, Action onSquad, Action onQuit)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, onQuit);

            // The path: you on the left, the choices, the step after, then fog and the boss far off.
            var ground = Img("Path", p, Palette.Lane, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(2400f, 160f));
            ground.transform.SetAsFirstSibling();
            var you = Img("You", p, new Color(1f, 0.93f, 0.75f), Center, new Vector2(-720f, -40f), new Vector2(110f, 110f), Shapes.Circle);
            Label("YouLabel", p, "You", 26, TextAnchor.MiddleCenter, Center, new Vector2(-720f, -130f), new Vector2(200f, 40f), Palette.TextDark);

            var next = run.NextChoices;
            float spacing = 210f;
            for (int i = 0; i < next.Count; i++)
            {
                var y = -40f + (next.Count - 1) * spacing * 0.5f - i * spacing;
                Line(p, new Vector2(-660f, -40f), new Vector2(-380f, y));
                int index = i;
                StopCard(p, next[i], new Vector2(-150f, y), new Vector2(440f, 180f), true, () => onChoose(index));
            }
            var beyond = run.Beyond;
            for (int i = 0; i < beyond.Count; i++)
            {
                var y = -40f + (beyond.Count - 1) * 130f * 0.5f - i * 130f;
                StopCard(p, beyond[i], new Vector2(330f, y), new Vector2(300f, 110f), false, null);
            }
            if (beyond.Count > 0) Label("Then", p, "then…", 26, TextAnchor.MiddleCenter, Center, new Vector2(330f, 220f), new Vector2(300f, 40f), Palette.TextDark);

            for (int i = 0; i < 6; i++)
            {
                var fog = Img("Fog", p, Palette.Sky.WithAlpha(0.18f + i * 0.13f), Center, new Vector2(520f + i * 70f, -40f), new Vector2(80f, 700f));
                fog.raycastTarget = false;
            }
            var boss = Img("Boss", p, Palette.KeepRoof.WithAlpha(0.85f), Center, new Vector2(800f, 20f), new Vector2(180f, 260f), Shapes.Diamond);
            string bossText = run.StepsToBoss <= 1 ? "The boss awaits!" : $"Boss in {run.StepsToBoss} steps";
            Label("BossLabel", p, bossText, 30, TextAnchor.MiddleCenter, Center, new Vector2(800f, -170f), new Vector2(320f, 80f), Palette.TextDark);

            Toast(p, run.Message);
        }

        void TopBar(RectTransform p, Run run, Action onSquad, Action onQuit)
        {
            var bar = Panel("Bar", p, TopLeft, new Vector2(30f, -24f), new Vector2(1000f, 86f));
            Label("Chapter", bar, $"{run.ChapterName}  ({run.Chapter + 1}/{run.ChapterCount})", 30, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(420f, 60f));
            Label("KeepLabel", bar, "Keep", 26, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(450f, 0f), new Vector2(90f, 60f));
            var hp = Bar("Hp", bar, run.KeepHp / run.KeepMaxHp < 0.3f ? Palette.HealthLow : Palette.Health, new Vector2(0f, 0.5f), new Vector2(530f, 0f), new Vector2(260f, 30f));
            hp.fillAmount = run.KeepHp / run.KeepMaxHp;
            Label("HpText", hp.rectTransform.parent as RectTransform, $"{Mathf.CeilToInt(run.KeepHp)}/{Mathf.CeilToInt(run.KeepMaxHp)}", 22, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(260f, 30f));
            Label("Gold", bar, $"{run.Gold} gold", 28, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(170f, 60f), new Color(1f, 0.86f, 0.4f));
            if (onSquad != null) Button("Squad", p, "Squad", 30, TopRight, new Vector2(-200f, -24f), new Vector2(200f, 86f), onSquad);
            if (onQuit != null) Button("Quit", p, "Title", 26, TopRight, new Vector2(-30f, -24f), new Vector2(150f, 86f), onQuit);
        }

        void StopCard(RectTransform p, Stop stop, Vector2 pos, Vector2 size, bool live, Action onClick)
        {
            var (title, body, color) = Describe(stop);
            RectTransform card;
            if (live)
            {
                card = (RectTransform)Button(title, p, "", 1, Center, pos, size, onClick, color).transform;
            }
            else card = Panel(title, p, Center, pos, size, color.WithAlpha(0.45f));
            Label("Title", card, title, live ? 36 : 26, TextAnchor.UpperLeft, TopLeft, new Vector2(22f, -14f), new Vector2(size.x - 44f, 50f));
            if (live) Label("Body", card, body, 24, TextAnchor.UpperLeft, TopLeft, new Vector2(22f, -66f), new Vector2(size.x - 44f, size.y - 76f));
        }

        static (string title, string body, Color color) Describe(Stop s)
        {
            switch (s.Kind)
            {
                case StopKind.Battle: return ("Battle", "Hold off a band of Heroes, then pick a Skill Upgrade.", new Color(0.45f, 0.38f, 0.42f));
                case StopKind.ToughBattle: return ("Tough Battle", "Stronger Heroes. Two Skill Upgrades and more gold.", new Color(0.62f, 0.33f, 0.36f));
                case StopKind.MidBoss: return ("Mid-boss", "A champion blocks the Trail. Two Skill Upgrades.", new Color(0.70f, 0.30f, 0.30f));
                case StopKind.Boss: return ("Boss", "Win to finish the Chapter.", new Color(0.55f, 0.20f, 0.25f));
                case StopKind.Rest: return ("Rest", "Patch up the Keep.", new Color(0.40f, 0.58f, 0.42f));
                case StopKind.Shop: return ("Shop", "Spend gold on Skill Upgrades and repairs.", new Color(0.62f, 0.52f, 0.30f));
                case StopKind.Event: return ("Something curious", "Who knows? Choices await.", new Color(0.42f, 0.45f, 0.62f));
                case StopKind.Wild: return ("Wild creature", "Someone might want to join the squad.", new Color(0.35f, 0.55f, 0.55f));
            }
            return (s.Kind.ToString(), "", Palette.Panel);
        }

        // --- After a Battle ----------------------------------------------------------------

        public void ShowUpgrade(Run run, Action<int> onPick, Action onSkip, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            Label("Title", p, "Choose a Skill Upgrade", 56, TextAnchor.MiddleCenter, Top, new Vector2(0f, -170f), new Vector2(1400f, 80f), Palette.TextDark);
            var offer = run.UpgradeOffer;
            float w = 500f, gap = 40f, total = offer.Count * w + (offer.Count - 1) * gap;
            for (int i = 0; i < offer.Count; i++)
            {
                var o = offer[i];
                int index = i;
                var asset = content.FighterAsset(o.Member.Def.Id);
                var card = (RectTransform)Button("Upgrade", p, "", 1, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), -40f), new Vector2(w, 520f), () => onPick(index),
                    o.MakesMaster ? new Color(0.62f, 0.45f, 0.20f) : Palette.Panel).transform;
                Img("Dot", card, asset != null ? asset.color : Color.white, TopLeft, new Vector2(24f, -24f), new Vector2(56f, 56f), Shapes.Circle);
                Label("Who", card, $"{o.Member.Def.Name} · {o.Skill.Base.Name}", 26, TextAnchor.MiddleLeft, TopLeft, new Vector2(96f, -24f), new Vector2(w - 120f, 56f));
                Label("Name", card, o.Upgrade.Name, 40, TextAnchor.UpperLeft, TopLeft, new Vector2(24f, -110f), new Vector2(w - 48f, 60f));
                Label("Desc", card, o.Upgrade.Description, 26, TextAnchor.UpperLeft, TopLeft, new Vector2(24f, -180f), new Vector2(w - 48f, 140f));
                Label("Count", card, $"Upgrade {o.Skill.Taken.Count + 1} of {SkillProgress.UpgradesForMaster} towards its Master Form", 22, TextAnchor.UpperLeft, TopLeft, new Vector2(24f, -330f), new Vector2(w - 48f, 60f), Palette.Text.WithAlpha(0.8f));
                if (o.MakesMaster && o.Skill.Base.MasterForm != null)
                {
                    var skillAsset = content.SkillAsset(o.Skill.Base.Id);
                    Label("Master", card, $"★ Becomes {o.Skill.Base.MasterForm.Name}!\n{skillAsset?.masterDescription}", 26, TextAnchor.UpperLeft, TopLeft, new Vector2(24f, -390f), new Vector2(w - 48f, 120f), new Color(1f, 0.9f, 0.55f));
                }
            }
            Button("Skip", p, "Skip", 28, Bottom, new Vector2(0f, 40f), new Vector2(240f, 80f), onSkip);
            Toast(p, run.Message);
        }

        public void ShowBefriend(Run run, Action<int> onBefriend, Action onPass, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            var asset = content.FighterAsset(run.CreatureOffer.Id);
            Label("Title", p, $"{run.CreatureOffer.Name} wants to join!", 56, TextAnchor.MiddleCenter, Top, new Vector2(0f, -170f), new Vector2(1400f, 80f), Palette.TextDark);
            FighterCard(p, asset, new Vector2(-380f, -60f), new Vector2(560f, 560f));
            if (!run.SquadFull)
            {
                Button("Befriend", p, "Befriend", 40, Center, new Vector2(320f, 40f), new Vector2(460f, 130f), () => onBefriend(-1), Palette.Rally.WithAlpha(1f));
            }
            else
            {
                Label("Full", p, "The squad is full. Who heads home to make room?", 30, TextAnchor.MiddleCenter, Center, new Vector2(320f, 200f), new Vector2(700f, 80f), Palette.TextDark);
                for (int i = 0; i < run.Familiars.Count; i++)
                {
                    int index = i;
                    Button("Swap", p, "Swap with " + run.Familiars[i].Def.Name, 28, Center, new Vector2(320f, 100f - i * 100f), new Vector2(460f, 84f), () => onBefriend(index));
                }
            }
            Button("Pass", p, "Not this time", 30, Center, new Vector2(320f, -330f), new Vector2(460f, 90f), onPass);
        }

        RectTransform FighterCard(RectTransform p, FighterAsset asset, Vector2 pos, Vector2 size)
        {
            var card = Panel("Card", p, Center, pos, size);
            if (asset == null) return card;
            Img("Dot", card, asset.color, Top, new Vector2(0f, -40f), new Vector2(140f, 140f), Shapes.Circle);
            Label("Name", card, asset.def.Name, 42, TextAnchor.MiddleCenter, Top, new Vector2(0f, -200f), new Vector2(size.x - 40f, 60f));
            Label("Desc", card, asset.description, 24, TextAnchor.UpperCenter, Top, new Vector2(0f, -260f), new Vector2(size.x - 60f, 90f));
            string skills = string.Join("\n", asset.skills.Where(s => s != null).Select(s => $"• {s.def.Name}: {s.description}"));
            Label("Skills", card, skills, 22, TextAnchor.UpperLeft, Top, new Vector2(0f, -350f), new Vector2(size.x - 60f, 140f), Palette.Text.WithAlpha(0.85f));
            return card;
        }

        // --- Shop and events --------------------------------------------------------------

        public void ShowShop(Run run, Action<int> onBuy, Action onLeave, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            Label("Title", p, "The travelling shop", 56, TextAnchor.MiddleCenter, Top, new Vector2(0f, -170f), new Vector2(1400f, 80f), Palette.TextDark);
            var items = run.Shop;
            float w = 400f, gap = 30f, total = items.Count * w + (items.Count - 1) * gap;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int index = i;
                var x = -total * 0.5f + w * 0.5f + i * (w + gap);
                var card = Panel("Item", p, Center, new Vector2(x, -10f), new Vector2(w, 440f));
                string title = item.Kind == ShopItemKind.Repair ? "Repairs" : item.Upgrade.Upgrade.Name;
                string who = item.Kind == ShopItemKind.Repair ? "For the Keep" : $"{item.Upgrade.Member.Def.Name} · {item.Upgrade.Skill.Base.Name}";
                string body = item.Kind == ShopItemKind.Repair ? $"Repair the Keep by {run.Tuning.ShopRepairAmount:0}." : item.Upgrade.Upgrade.Description + (item.Upgrade.MakesMaster ? $"\n★ Becomes {item.Upgrade.Skill.Base.MasterForm.Name}!" : "");
                Label("Who", card, who, 24, TextAnchor.UpperLeft, TopLeft, new Vector2(22f, -20f), new Vector2(w - 44f, 40f), Palette.Text.WithAlpha(0.8f));
                Label("Title", card, title, 36, TextAnchor.UpperLeft, TopLeft, new Vector2(22f, -66f), new Vector2(w - 44f, 60f));
                Label("Body", card, body, 24, TextAnchor.UpperLeft, TopLeft, new Vector2(22f, -130f), new Vector2(w - 44f, 180f));
                var buy = Button("Buy", card, item.Sold ? "Sold" : $"Buy · {item.Price} gold", 28, Bottom, new Vector2(0f, 24f), new Vector2(w - 50f, 84f), () => onBuy(index), new Color(0.62f, 0.52f, 0.30f));
                SetEnabled(buy, run.CanBuy(index));
            }
            Button("Leave", p, "Leave the shop", 30, Bottom, new Vector2(0f, 40f), new Vector2(360f, 84f), onLeave);
            Toast(p, run.Message);
        }

        public void ShowEvent(Run run, Action<int> onChoose, Action onSquad)
        {
            var p = NewPage();
            TopBar(p, run, onSquad, null);
            var e = run.CurrentEvent;
            var card = Panel("Event", p, Center, new Vector2(0f, -30f), new Vector2(1200f, 720f), Palette.PanelLight);
            Label("Title", card, e.Title, 54, TextAnchor.MiddleCenter, Top, new Vector2(0f, -50f), new Vector2(1100f, 80f), Palette.TextDark);
            Label("Text", card, e.Text, 30, TextAnchor.UpperCenter, Top, new Vector2(0f, -150f), new Vector2(1060f, 220f), Palette.TextDark);
            for (int i = 0; i < e.Options.Count; i++)
            {
                int index = i;
                var o = e.Options[i];
                string label = o.GoldCost > 0 ? $"{o.Text}  ({o.GoldCost} gold)" : o.Text;
                var b = Button("Option", card, label, 30, Bottom, new Vector2(0f, 60f + (e.Options.Count - 1 - i) * 110f), new Vector2(900f, 90f), () => onChoose(index));
                SetEnabled(b, run.CanChoose(i));
            }
        }

        // --- Squad and summary ---------------------------------------------------------------

        public void ShowSquad(Run run, Action onClose)
        {
            var overlay = Img("Squad", (RectTransform)transform, new Color(0.15f, 0.1f, 0.12f, 0.75f), Center, Vector2.zero, Vector2.zero);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.raycastTarget = true;
            var members = new List<SquadMember> { run.Keeper };
            members.AddRange(run.Familiars);
            float w = 340f, gap = 20f, total = members.Count * w + (members.Count - 1) * gap;
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var asset = content.FighterAsset(m.Def.Id);
                var card = Panel("Member", overlay.rectTransform, Center, new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 40f), new Vector2(w, 640f));
                Img("Dot", card, asset != null ? asset.color : Color.white, Top, new Vector2(0f, -30f), new Vector2(90f, 90f), Shapes.Circle);
                Label("Name", card, m.Def.Name, 36, TextAnchor.MiddleCenter, Top, new Vector2(0f, -140f), new Vector2(w - 30f, 50f));
                var lines = m.Skills.Select(s =>
                {
                    string name = s.IsMaster ? $"★ {s.Base.MasterForm.Name}" : s.Base.Name;
                    string ups = s.Taken.Count == 0 ? "  no upgrades yet" : string.Join("\n", s.Taken.Select(u => "  + " + u.Name));
                    return $"{name}\n{ups}";
                });
                Label("Skills", card, string.Join("\n\n", lines), 22, TextAnchor.UpperLeft, Top, new Vector2(0f, -200f), new Vector2(w - 40f, 420f));
            }
            Button("Close", overlay.rectTransform, "Close", 32, Bottom, new Vector2(0f, 60f), new Vector2(280f, 90f), () => { Destroy(overlay.gameObject); onClose?.Invoke(); });
        }

        public void ShowSummary(Run run, Action onDone)
        {
            var p = NewPage();
            var r = run.Result;
            Label("Title", p, r.Won ? "The Keep is safe!" : "The Keep has fallen", 80, TextAnchor.MiddleCenter, Top, new Vector2(0f, -110f), new Vector2(1600f, 120f), Palette.TextDark);
            string body = r.Won
                ? $"You saw off every Hero across all {run.ChapterCount} Chapters."
                : $"Your Run ended in {run.ChapterName} (Chapter {r.ChapterReached} of {run.ChapterCount}).";
            body += $"\nBattles won: {r.BattlesWon}    Gold left: {r.Gold}";
            Label("Body", p, body, 34, TextAnchor.MiddleCenter, Top, new Vector2(0f, -240f), new Vector2(1600f, 120f), Palette.TextDark);
            var members = new List<SquadMember> { run.Keeper };
            members.AddRange(run.Familiars);
            string squad = string.Join("     ", members.Select(m => $"{m.Def.Name}: " + string.Join(", ", m.Skills.Select(s => s.IsMaster ? "★" + s.Base.MasterForm.Name : $"{s.Base.Name} +{s.Taken.Count}"))));
            Label("Squad", p, "Your squad\n" + squad, 28, TextAnchor.UpperCenter, Center, new Vector2(0f, -40f), new Vector2(1700f, 240f), Palette.TextDark);
            Button("Done", p, "Back to title", 36, Bottom, new Vector2(0f, 80f), new Vector2(420f, 110f), onDone);
        }

        // --- Bits --------------------------------------------------------------------------

        void Toast(RectTransform p, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            var t = Panel("Toast", p, Bottom, new Vector2(0f, 150f), new Vector2(1100f, 70f));
            Label("Text", t, message, 28, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(1080f, 70f));
        }

        static void Line(RectTransform p, Vector2 from, Vector2 to)
        {
            var d = to - from;
            var img = Img("Line", p, Palette.LaneEdge, Center, (from + to) * 0.5f, new Vector2(d.magnitude, 10f));
            img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            img.transform.SetSiblingIndex(1);
        }
    }
}
