using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// The Keeper or a Familiar. Create via Assets > Create > Little Keep > Fighter.
    [CreateAssetMenu(menuName = "Little Keep/Fighter", fileName = "Fighter")]
    public sealed class FighterAsset : ScriptableObject
    {
        [Tooltip("Id must be stable: saves refer to it. The Skills list inside is ignored; use Skills below.")]
        public FighterDef def = new FighterDef();
        public List<SkillAsset> skills = new List<SkillAsset>();
        [TextArea(2, 4)] public string description;
        [Tooltip("Grey-box colour until real art arrives")] public Color color = Color.white;
        [Tooltip("Colour of this fighter's Skill effects")] public Color skillColor = new Color(1f, 0.85f, 0.3f);
    }
}
