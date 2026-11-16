using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// The Keeper or a Familiar. Create via Assets > Create > Little Keep > Fighter.
    [CreateAssetMenu(menuName = "Little Keep/Fighter", fileName = "Fighter")]
    public sealed class FighterAsset : ScriptableObject
    {
        public FighterDef def = new FighterDef();
        [TextArea(2, 4)] public string description;
        [Tooltip("Grey-box colour until real art arrives")] public Color color = Color.white;
        [Tooltip("Colour of this fighter's Skill effects")] public Color skillColor = new Color(1f, 0.85f, 0.3f);
    }
}
