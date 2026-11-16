using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// How each Element looks and reads: colour, icon, names and a plain explanation built from the Battle tuning.
    public static class ElementLook
    {
        /// The tuning explanations quote numbers from. Set once content is loaded.
        public static BattleTuning Tuning = new BattleTuning();

        public static Color Color(Element e)
        {
            switch (e)
            {
                case Element.Fire: return new Color(1f, 0.55f, 0.2f);
                case Element.Frost: return new Color(0.45f, 0.8f, 1f);
                case Element.Spark: return new Color(1f, 0.9f, 0.3f);
                case Element.Bloom: return new Color(0.5f, 0.92f, 0.35f);
                default: return UnityEngine.Color.white;
            }
        }

        public static Sprite Icon(Element e)
        {
            var t = UiKit.Theme;
            if (t == null) return Shapes.Circle;
            switch (e)
            {
                case Element.Fire: return t.iconFire;
                case Element.Frost: return t.iconFrost;
                case Element.Spark: return t.iconSpark;
                case Element.Bloom: return t.iconBloom;
                default: return null;
            }
        }

        /// The bloom icon is a full-colour picture; the others are white glyphs tinted by Element.
        public static Color IconTint(Element e) => e == Element.Bloom && UiKit.Theme?.iconBloom != null ? UnityEngine.Color.white : Color(e);

        public static string Name(Element e) => e.ToString();

        public static string Status(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "Burn";
                case Element.Frost: return "Chill";
                case Element.Spark: return "Shock";
                case Element.Bloom: return "Root";
                default: return "";
            }
        }

        public static string Effect(Element e)
        {
            var t = Tuning;
            switch (e)
            {
                case Element.Fire: return $"Burns the Hero: it takes another {t.BurnFraction * 100f:0}% of the hit over {t.StatusSeconds:0.#} seconds.";
                case Element.Frost: return $"Chills the Hero: it walks {t.ChillSlow * 100f:0}% slower for {t.StatusSeconds:0.#} seconds.";
                case Element.Spark: return $"Shocks the Hero: it takes {t.ShockBonus * 100f:0}% more damage for {t.StatusSeconds:0.#} seconds.";
                case Element.Bloom: return $"Roots the Hero in place for {t.RootSeconds:0.#} seconds (bosses break free in half the time).";
                default: return "";
            }
        }

        public static string ComboRule =>
            $"Hit a Hero carrying one Element's status with a different Element for a Combo: a burst of {Tuning.ComboMultiplier:0.#}x the hit onto every Hero close by. The status is used up.";

        /// A short, sweet name for each pair of Elements.
        public static string ComboName(Element a, Element b)
        {
            bool Has(Element x) => a == x || b == x;
            if (Has(Element.Fire) && Has(Element.Frost)) return "Steam!";
            if (Has(Element.Fire) && Has(Element.Spark)) return "Firework!";
            if (Has(Element.Fire) && Has(Element.Bloom)) return "Wildfire!";
            if (Has(Element.Frost) && Has(Element.Spark)) return "Shatter!";
            if (Has(Element.Frost) && Has(Element.Bloom)) return "Frostbloom!";
            if (Has(Element.Spark) && Has(Element.Bloom)) return "Thunderbloom!";
            return "Combo!";
        }

        public static string Explanation(Element e) => e == Element.None
            ? "No Element: plain damage that leaves no status."
            : $"{Effect(e)}\n\n{ComboRule}";
    }
}
