using UnityEngine;

namespace LittleKeep.Game
{
    /// The art Skill effects are drawn with. Any slot left empty uses a simple shape made in code, so replacing a
    /// sprite here (or the whole asset) restyles the effects with no code change.
    [CreateAssetMenu(menuName = "Little Keep/Effect Art", fileName = "EffectArt")]
    public sealed class EffectArt : ScriptableObject
    {
        [Tooltip("Additive material for glowing particles and flashes (LittleKeep/FxGlow)")] public Material glow;
        [Tooltip("Soft round glow: puffs, motes and trails")] public Sprite soft;
        [Tooltip("Drawn stretched along its motion: Spark streaks")] public Sprite spark;
        [Tooltip("Fire embers")] public Sprite ember;
        [Tooltip("Frost ice shards")] public Sprite shard;
        [Tooltip("Bloom petals and Rally confetti")] public Sprite petal;
        [Tooltip("Twinkles: upgraded Skills, Master Forms, Rally")] public Sprite star;
    }
}
