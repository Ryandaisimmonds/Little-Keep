using TMPro;
using UnityEngine;

namespace LittleKeep.Game
{
    public enum ButtonStyle { Brown, Green, Blue, Red, Purple, Teal }

    /// The game's look in one place: font, panel, button, bar and ribbon sprites, icons and colours.
    /// Every screen draws through this, so changing it restyles the whole game.
    [CreateAssetMenu(menuName = "Little Keep/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Type")]
        public TMP_FontAsset font;
        public TMP_FontAsset fontOutlined;

        [Header("Surfaces")]
        public Sprite background;
        public Sprite panel;
        public Sprite card;
        public Sprite cardDim;
        public Sprite topBar;
        public Sprite ribbon;
        public Sprite tag;
        public Sprite ring;

        [Header("Buttons")]
        public Sprite buttonBrown, buttonGreen, buttonBlue, buttonRed, buttonPurple, buttonTeal;
        public Sprite buttonRound;
        public Sprite buttonSquare;

        [Header("Bars")]
        public Sprite barBack;
        public Sprite barFill;

        [Header("Icons")]
        public Sprite iconPause, iconPlay, iconHome, iconSpeed, iconGold, iconHeart, iconCrown, iconSkull,
            iconBattle, iconShop, iconRest, iconEvent, iconWild, iconStar, iconSquad, iconLock, iconSparkle,
            iconGem, iconBook, iconWorkshop, iconTower, iconGate, iconMedal,
            iconFire, iconFrost, iconSpark, iconBloom, iconInfo, iconSound, iconMute;

        [Header("Colours")]
        public Color text = new Color(0.98f, 0.93f, 0.82f);
        public Color textDark = new Color(0.27f, 0.21f, 0.18f);
        public Color textGold = new Color(1f, 0.82f, 0.35f);
        public Color backdrop = new Color(0.20f, 0.17f, 0.14f);
        public Color health = new Color(0.55f, 0.85f, 0.35f);
        public Color healthLow = new Color(0.95f, 0.40f, 0.30f);
        public Color rally = new Color(1f, 0.55f, 0.80f);

        [Header("World: warm and slightly muted, so Skill effects stay the loudest thing on screen")]
        public Color sky = new Color(0.22f, 0.19f, 0.16f);
        public Color skyTop = new Color(0.55f, 0.66f, 0.72f);
        public Color skyBottom = new Color(0.93f, 0.84f, 0.68f);
        public Color hillsFar = new Color(0.55f, 0.62f, 0.55f);
        public Color hillsNear = new Color(0.42f, 0.55f, 0.36f);
        public Color trees = new Color(0.30f, 0.43f, 0.28f);
        public Color fog = new Color(0.90f, 0.88f, 0.84f);
        public Color lane = new Color(0.47f, 0.60f, 0.33f);
        public Color laneEdge = new Color(0.36f, 0.47f, 0.26f);
        public Color path = new Color(0.78f, 0.66f, 0.48f);
        public Color keepWall = new Color(0.80f, 0.70f, 0.58f);
        public Color keepStone = new Color(0.66f, 0.57f, 0.48f);
        public Color keepRoof = new Color(0.66f, 0.38f, 0.34f);
        public Color keepDoor = new Color(0.42f, 0.28f, 0.20f);
        public Color window = new Color(1f, 0.85f, 0.50f);
        public Color heroHealth = new Color(0.95f, 0.45f, 0.40f);
        public Color heroShot = new Color(0.85f, 0.25f, 0.25f);
        public Color panelPlain = new Color(0.27f, 0.21f, 0.24f, 0.82f);

        public Sprite Button(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Green: return buttonGreen;
                case ButtonStyle.Blue: return buttonBlue;
                case ButtonStyle.Red: return buttonRed;
                case ButtonStyle.Purple: return buttonPurple;
                case ButtonStyle.Teal: return buttonTeal;
                default: return buttonBrown;
            }
        }
    }
}
