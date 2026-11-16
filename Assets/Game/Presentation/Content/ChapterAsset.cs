using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    /// One Chapter of a Run. Create via Assets > Create > Little Keep > Chapter.
    [CreateAssetMenu(menuName = "Little Keep/Chapter", fileName = "Chapter")]
    public sealed class ChapterAsset : ScriptableObject
    {
        public string displayName = "Chapter";
        public List<EncounterAsset> battles = new List<EncounterAsset>();
        public List<EncounterAsset> toughBattles = new List<EncounterAsset>();
        public EncounterAsset midBoss;
        public EncounterAsset boss;
    }
}
