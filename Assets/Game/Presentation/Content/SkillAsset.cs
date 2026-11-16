using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// A Skill with its own pool of Skill Upgrades and its Master Form. Create via Assets > Create > Little Keep > Skill.
    [CreateAssetMenu(menuName = "Little Keep/Skill", fileName = "Skill")]
    public sealed class SkillAsset : ScriptableObject
    {
        [Tooltip("Id must be stable: saves and upgrades refer to it")] public SkillDef def = new SkillDef();
        [TextArea(2, 4)] public string description;
        public List<SkillUpgradeDef> upgrades = new List<SkillUpgradeDef>();
        public bool hasMasterForm = true;
        [Tooltip("What this Skill becomes after three Skill Upgrades")] public SkillDef masterForm = new SkillDef();
        [TextArea(2, 4)] public string masterDescription;
    }
}
