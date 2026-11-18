using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Everything the game is built from, in one place. Create via Assets > Create > Little Keep > Game Content.
    [CreateAssetMenu(menuName = "Little Keep/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        public UiTheme theme;
        [Tooltip("Particle sprites and the glow material Skill effects are drawn with")] public EffectArt effects;
        public FighterAsset keeper;
        [Tooltip("Every Familiar, in Familiar book order. Each one's Unlock says whether it is owned from the start or earned.")]
        public List<FighterAsset> familiars = new List<FighterAsset>();
        public List<HeroAsset> heroes = new List<HeroAsset>();
        public List<ChapterAsset> chapters = new List<ChapterAsset>();
        public List<EventAsset> events = new List<EventAsset>();
        [Header("Permanent progress")]
        public List<DeedDef> deeds = new List<DeedDef>();
        public List<KeeperUpgradeDef> keeperUpgrades = new List<KeeperUpgradeDef>();
        public List<DifficultyDef> tiers = new List<DifficultyDef>();
        public RewardTuning rewards = new RewardTuning();
        public RunTuning runTuning = new RunTuning();
        public BattleTuning battleTuning = new BattleTuning();
    }
}
