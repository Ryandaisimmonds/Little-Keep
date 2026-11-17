using LittleKeep.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LittleKeep.Game.UiKit;

namespace LittleKeep.Game
{
    /// Tap-to-explain: attach to any stat, status or Skill and a tap opens a plain-language popup.
    public sealed class Explainer : MonoBehaviour, IPointerClickHandler
    {
        public string Title, Body;
        public Sprite Icon;
        public Color IconTint = Color.white;

        public void OnPointerClick(PointerEventData e)
        {
            Audio.Play(Sound.Tap);
            ExplainPopup.Show(Title, Body, Icon, IconTint);
        }
    }

    public static class Explain
    {
        /// Makes `target` tappable to explain itself. Inside a Button, a tap on `target` explains instead of pressing.
        public static T On<T>(T target, string title, string body, Sprite icon = null, Color? tint = null) where T : Component
        {
            if (target == null) return null;
            var g = target.GetComponent<Graphic>();
            if (g == null)
            {
                var hit = target.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                g = hit;
            }
            g.raycastTarget = true;
            var ex = target.GetComponent<Explainer>();
            if (ex == null) ex = target.gameObject.AddComponent<Explainer>();
            ex.Title = title;
            ex.Body = body;
            ex.Icon = icon;
            ex.IconTint = tint ?? Color.white;
            return target;
        }

        public static T Element<T>(T target, Element e) where T : Component =>
            On(target, e == Rules.Element.None ? "No Element" : $"{ElementLook.Name(e)} · {ElementLook.Status(e)}", ElementLook.Explanation(e), ElementLook.Icon(e), ElementLook.IconTint(e));

        public static T Skill<T>(T target, SkillDef s, string description) where T : Component
        {
            string shape;
            switch (s.Shape)
            {
                case SkillShape.Burst: shape = $"Hits every Hero within {s.Radius:0.#} of its target for {s.Damage:0.#}."; break;
                case SkillShape.Chain: shape = $"Strikes a Hero for {s.Damage:0.#}, then jumps to the next closest within {s.Radius:0.#}: {s.Count} Heroes in all."; break;
                case SkillShape.Nova: shape = $"Hits every Hero within {s.Radius:0.#} of its caster for {s.Damage:0.#}. Waits until a Hero comes that close."; break;
                default: shape = $"Fires {s.Count} shot{(s.Count == 1 ? "" : "s")} of {s.Damage:0.#} damage."; break;
            }
            string element = s.Element == Rules.Element.None ? "" : $"\n\n<color=#FFD15A>{ElementLook.Name(s.Element)}</color>: {ElementLook.Effect(s.Element)}";
            string body = $"{description}\n\n{shape} Casts itself every {s.Cooldown:0.#} seconds while a Hero is on the lane.{element}";
            return On(target, s.Name, body, ElementLook.Icon(s.Element), ElementLook.IconTint(s.Element));
        }

        /// A small tappable chip showing a Skill's Element that explains the whole Skill. For cards that are buttons,
        /// where the rest of the card must stay free to press.
        public static Image SkillChip(RectTransform parent, SkillDef s, string description, Vector2 anchor, Vector2 pos, float size)
        {
            var img = UiKit.Icon("Skill", parent, ElementLook.Icon(s.Element), anchor, pos, size);
            img.color = ElementLook.IconTint(s.Element);
            return Skill(img, s, description);
        }

        /// What a Keeper upgrade gives at a level, in plain words ("+20 Keep health").
        public static string UpgradeEffect(KeeperUpgradeDef u, int level)
        {
            float v = u.PerLevel * level;
            switch (u.Stat)
            {
                case KeeperStat.KeepMaxHp: return $"+{v:0} Keep health";
                case KeeperStat.KeeperDamage: return $"+{v * 100f:0}% Keeper damage";
                case KeeperStat.StartGold: return $"+{v:0} Gold at the start of each Run";
                case KeeperStat.RallyCooldown: return $"Rally recharges {-v * 100f:0}% faster";
            }
            return "";
        }

        public static string Upgrade(KeeperUpgradeDef u, int level, int nextCost)
        {
            string now = level == 0 ? "Not bought yet." : $"Now: {UpgradeEffect(u, level)}.";
            string next = nextCost < 0 ? "Fully upgraded." : $"Next level: {UpgradeEffect(u, level + 1)}, for {nextCost} Gems.";
            return $"{u.Description}\n\n{now}\n{next}\n\nKeeper upgrades last forever and apply from your next Run.";
        }

        /// A small tappable Element chip (icon) that explains its Element.
        public static Image Chip(RectTransform parent, Element e, Vector2 anchor, Vector2 pos, float size)
        {
            var img = UiKit.Icon("Element", parent, ElementLook.Icon(e), anchor, pos, size);
            img.color = ElementLook.IconTint(e);
            return Element(img, e);
        }

        // Stats
        public const string KeepHealth = "The Keep's health. Heroes who reach the Keep damage it; if it falls, the Run ends. Damage carries over between Battles, so rest when you can.";
        public const string Gold = "Gold is spent at shops during this Run. It's gone when the Run ends.";
        public const string Gems = "Gems are kept forever. Every Run pays some, win or lose; spend them in the Workshop on Keeper upgrades.";
        public const string Rally = "When Rally is charged, tap it: the Keeper and every Familiar who isn't knocked out cast every Skill at once.";
        public const string Bond = "Befriend this creature during Runs to fill its Bond. When the Bond is full, it becomes one of your Familiars for good.";
        public const string FighterHealth = "Health. A Keeper or Familiar who drops to zero is knocked out for a few seconds, then gets back up.";
        public const string SkillCharge = "The bar fills as the Skill recharges; it casts itself when full.";
    }

    /// The popup itself: one per scene, built on demand above everything else.
    public sealed class ExplainPopup : MonoBehaviour
    {
        static ExplainPopup instance;

        /// True while an explanation is on screen; the Battle waits meanwhile.
        public static bool IsOpen => instance != null && instance.root.activeSelf;
        GameObject root;
        TMP_Text title, body;
        Image icon;

        public static void Show(string titleText, string bodyText, Sprite iconSprite, Color tint)
        {
            if (instance == null) instance = new GameObject("Explain", typeof(RectTransform)).AddComponent<ExplainPopup>().Build();
            instance.title.text = titleText;
            instance.body.text = bodyText;
            instance.icon.sprite = iconSprite;
            instance.icon.color = tint;
            instance.icon.enabled = iconSprite != null;
            instance.root.SetActive(true);
        }

        ExplainPopup Build()
        {
            Canvas(gameObject, 50, out var safe);
            var dim = Img("Dim", (RectTransform)transform, new Color(0.08f, 0.06f, 0.05f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.raycastTarget = true;
            var close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => root.SetActive(false));
            root = dim.gameObject;
            var panel = Panel("Panel", SafeChild(dim.rectTransform), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 560f));
            icon = UiKit.Icon("Icon", panel, null, new Vector2(0f, 1f), new Vector2(44f, -40f), 100f);
            title = Label("Title", panel, "", 44, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(164f, -50f), new Vector2(780f, 80f), TextGold);
            body = Label("Body", panel, "", 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(60f, -160f), new Vector2(880f, 320f));
            body.enableAutoSizing = true;
            body.fontSizeMax = 30;
            body.fontSizeMin = 18;
            Label("Hint", panel, "Tap anywhere to close", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(600f, 40f), TextColor.WithAlpha(0.6f));
            root.SetActive(false);
            return this;
        }
    }
}
