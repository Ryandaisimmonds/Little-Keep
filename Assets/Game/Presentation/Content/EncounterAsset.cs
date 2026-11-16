using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    [System.Serializable]
    public sealed class WaveEntryAsset
    {
        public HeroAsset hero;
        [Min(1)] public int count = 1;
    }

    [System.Serializable]
    public sealed class WaveAsset
    {
        public List<WaveEntryAsset> entries = new List<WaveEntryAsset>();
        [Tooltip("Seconds between Heroes entering")] public float spawnInterval = 0.8f;
    }

    /// One fight: its waves and the gold it pays. Create via Assets > Create > Little Keep > Encounter.
    [CreateAssetMenu(menuName = "Little Keep/Encounter", fileName = "Encounter")]
    public sealed class EncounterAsset : ScriptableObject
    {
        public string id = "encounter";
        public string displayName = "";
        public int gold = 15;
        public List<WaveAsset> waves = new List<WaveAsset>();
    }
}
