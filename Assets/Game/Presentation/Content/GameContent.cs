using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Everything the game is built from, in one place. Create via Assets > Create > Little Keep > Game Content.
    [CreateAssetMenu(menuName = "Little Keep/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        public FighterAsset keeper;
        [Tooltip("Familiars the Player can start a Run with")] public List<FighterAsset> starters = new List<FighterAsset>();
        [Tooltip("Every creature that can become a Familiar, starters included")] public List<FighterAsset> familiars = new List<FighterAsset>();
        public List<HeroAsset> heroes = new List<HeroAsset>();
        public List<ChapterAsset> chapters = new List<ChapterAsset>();
        public List<EventAsset> events = new List<EventAsset>();
        public RunTuning runTuning = new RunTuning();
        public BattleTuning battleTuning = new BattleTuning();
    }
}
