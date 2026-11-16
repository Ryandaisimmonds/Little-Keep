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
        [Tooltip("Animated look; falls back to a grey-box circle when empty")] public CharacterArt art;
        [Header("Familiars only: how the Player earns it")]
        public UnlockKind unlock = UnlockKind.Start;
        [Tooltip("Bond: Befriendings needed")] public int bondSize = 3;
        [Tooltip("Deed: id of the Deed that grants it")] public string deedId = "";
        [Tooltip("Shown in the Familiar book while it is not owned")] public string unlockHint = "";
    }
}
