using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    public enum HeroLook { Square, Tall, Diamond }

    /// An invading Hero type. Create via Assets > Create > Little Keep > Hero.
    [CreateAssetMenu(menuName = "Little Keep/Hero", fileName = "Hero")]
    public sealed class HeroAsset : ScriptableObject
    {
        public HeroDef def = new HeroDef();
        [TextArea(2, 4)] public string description;
        [Tooltip("Grey-box colour until real art arrives")] public Color color = new Color(0.9f, 0.75f, 0.4f);
        public HeroLook look;
    }
}
