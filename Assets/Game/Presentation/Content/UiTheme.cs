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
            iconGem, iconBook, iconWorkshop, iconTower, iconGate, iconMedal;

        [Header("Colours")]
        public Color text = new Color(0.98f, 0.93f, 0.82f);
        public Color textDark = new Color(0.27f, 0.21f, 0.18f);
        public Color textGold = new Color(1f, 0.82f, 0.35f);
        public Color backdrop = new Color(0.20f, 0.17f, 0.14f);
        public Color health = new Color(0.55f, 0.85f, 0.35f);
        public Color healthLow = new Color(0.95f, 0.40f, 0.30f);
        public Color rally = new Color(1f, 0.55f, 0.80f);

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
