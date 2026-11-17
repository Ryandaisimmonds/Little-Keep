using UnityEngine;

namespace LittleKeep.Game
{
    /// World and fallback colours, read from the UI theme so one asset restyles the whole game.
    /// The values here are only used before a theme is set.
    public static class Palette
    {
        static UiTheme T => UiKit.Theme;
        static bool Has => T != null;

        public static Color Sky => Has ? T.sky : new Color(0.22f, 0.19f, 0.16f);
        public static Color SkyTop => Has ? T.skyTop : new Color(0.55f, 0.66f, 0.72f);
        public static Color SkyBottom => Has ? T.skyBottom : new Color(0.93f, 0.84f, 0.68f);
        public static Color HillsFar => Has ? T.hillsFar : new Color(0.55f, 0.62f, 0.55f);
        public static Color HillsNear => Has ? T.hillsNear : new Color(0.42f, 0.55f, 0.36f);
        public static Color Trees => Has ? T.trees : new Color(0.30f, 0.43f, 0.28f);
        public static Color Fog => Has ? T.fog : new Color(0.90f, 0.88f, 0.84f);
        public static Color Lane => Has ? T.lane : new Color(0.47f, 0.60f, 0.33f);
        public static Color LaneEdge => Has ? T.laneEdge : new Color(0.36f, 0.47f, 0.26f);
        public static Color Path => Has ? T.path : new Color(0.78f, 0.66f, 0.48f);
        public static Color Keep => Has ? T.keepWall : new Color(0.80f, 0.70f, 0.58f);
        public static Color KeepStone => Has ? T.keepStone : new Color(0.66f, 0.57f, 0.48f);
        public static Color KeepRoof => Has ? T.keepRoof : new Color(0.66f, 0.38f, 0.34f);
        public static Color KeepDoor => Has ? T.keepDoor : new Color(0.42f, 0.28f, 0.20f);
        public static Color Window => Has ? T.window : new Color(1f, 0.85f, 0.50f);
        public static Color Panel => Has ? T.panelPlain : new Color(0.27f, 0.21f, 0.24f, 0.82f);
        public static Color Text => Has ? T.text : new Color(1f, 0.97f, 0.92f);
        public static Color TextDark => Has ? T.textDark : new Color(0.27f, 0.21f, 0.24f);
        public static Color Health => Has ? T.health : new Color(0.55f, 0.85f, 0.45f);
        public static Color HealthLow => Has ? T.healthLow : new Color(0.95f, 0.45f, 0.40f);
        public static Color HeroHealth => Has ? T.heroHealth : new Color(0.95f, 0.45f, 0.40f);
        public static Color Rally => Has ? T.rally : new Color(1f, 0.62f, 0.85f);
        public static Color HeroShot => Has ? T.heroShot : new Color(0.85f, 0.25f, 0.25f);
    }
}
