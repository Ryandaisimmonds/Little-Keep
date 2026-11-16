using System.Collections.Generic;
using LittleKeep.Rules;
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

    /// A playable Battle: who fights, which waves come, and the lane's numbers.
    /// Create via Assets > Create > Little Keep > Battle.
    [CreateAssetMenu(menuName = "Little Keep/Battle", fileName = "Battle")]
    public sealed class BattleAsset : ScriptableObject
    {
        public FighterAsset keeper;
        public List<FighterAsset> familiars = new List<FighterAsset>();
        public List<WaveAsset> waves = new List<WaveAsset>();
        public float keepMaxHp = 100f;
        public BattleTuning tuning = new BattleTuning();

        public BattleSetup ToSetup(int seed)
        {
            var setup = new BattleSetup
            {
                Keeper = keeper.def,
                KeepHp = keepMaxHp,
                KeepMaxHp = keepMaxHp,
                Seed = seed,
                Tuning = tuning,
            };
            foreach (var f in familiars)
                if (f != null) setup.Familiars.Add(f.def);
            foreach (var w in waves)
            {
                var wave = new WaveDef { SpawnInterval = w.spawnInterval };
                foreach (var e in w.entries)
                    if (e.hero != null) wave.Entries.Add(new WaveEntry { Hero = e.hero.def, Count = e.count });
                setup.Waves.Add(wave);
            }
            return setup;
        }

        public FighterAsset FighterFor(FighterDef def)
        {
            if (keeper != null && keeper.def == def) return keeper;
            return familiars.Find(f => f != null && f.def == def);
        }

        public HeroAsset HeroFor(HeroDef def)
        {
            foreach (var w in waves)
                foreach (var e in w.entries)
                    if (e.hero != null && e.hero.def == def) return e.hero;
            return null;
        }
    }
}
