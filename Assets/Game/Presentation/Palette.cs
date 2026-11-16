using UnityEngine;

namespace LittleKeep.Game
{
    /// Grey-box colours: warm, slightly muted world so effects stay the loudest thing on screen.
    /// Replaced by the UI theme asset in ticket 06.
    public static class Palette
    {
        public static readonly Color Sky = new Color(0.98f, 0.91f, 0.82f);
        public static readonly Color Lane = new Color(0.72f, 0.80f, 0.60f);
        public static readonly Color LaneEdge = new Color(0.60f, 0.70f, 0.50f);
        public static readonly Color Keep = new Color(0.80f, 0.62f, 0.48f);
        public static readonly Color KeepRoof = new Color(0.66f, 0.42f, 0.38f);
        public static readonly Color Panel = new Color(0.27f, 0.21f, 0.24f, 0.82f);
        public static readonly Color PanelLight = new Color(1f, 0.97f, 0.92f, 0.95f);
        public static readonly Color Text = new Color(1f, 0.97f, 0.92f);
        public static readonly Color TextDark = new Color(0.27f, 0.21f, 0.24f);
        public static readonly Color Health = new Color(0.55f, 0.85f, 0.45f);
        public static readonly Color HealthLow = new Color(0.95f, 0.45f, 0.40f);
        public static readonly Color HeroHealth = new Color(0.95f, 0.45f, 0.40f);
        public static readonly Color Rally = new Color(1f, 0.62f, 0.85f);
        public static readonly Color HeroShot = new Color(0.85f, 0.25f, 0.25f);
    }
}
