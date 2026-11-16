using System;
using System.Collections.Generic;

namespace LittleKeep.Rules
{
    public enum SkillShape
    {
        /// Fires Count projectiles at the target, fanned across Spread degrees.
        Volley,
        /// Deals Damage to every Hero within Radius of the target, at once.
        Burst,
    }

    /// A Skill: casts itself whenever its cooldown is ready and a Hero is on the lane.
    [Serializable]
    public sealed class SkillDef
    {
        public string Id = "skill";
        public string Name = "Skill";
        public SkillShape Shape;
        public float Cooldown = 6f;
        public float Damage = 10f;
        public int Count = 1;
        public float Spread = 20f;
        public float Radius = 1.5f;
        public float ProjectileSpeed = 18f;
    }

    /// The Keeper or a Familiar.
    [Serializable]
    public sealed class FighterDef
    {
        public string Id = "fighter";
        public string Name = "Fighter";
        public float Hp = 50f;
        public float AttackDamage = 5f;
        public float AttackInterval = 0.5f;
        public float ProjectileSpeed = 16f;
        public List<SkillDef> Skills = new List<SkillDef>();
    }

    /// An invading Hero type.
    [Serializable]
    public sealed class HeroDef
    {
        public string Id = "hero";
        public string Name = "Hero";
        public float Hp = 20f;
        public float Speed = 1.5f;
        /// Damage dealt to the Keep on arrival.
        public float KeepDamage = 5f;
        /// Collision radius.
        public float Size = 0.4f;

        /// Melee: stops to attack a Keeper or Familiar within MeleeRange. 0 damage = never.
        public float MeleeDamage;
        public float MeleeInterval = 1f;
        public float MeleeRange = 0.9f;

        /// Ranged: stops within ShotRange of a Keeper or Familiar and shoots it. 0 damage = never.
        public float ShotDamage;
        public float ShotInterval = 1.5f;
        public float ShotRange = 6f;
        public float ShotSpeed = 9f;

        /// Fraction of damage from ordinary attacks that is blocked (0 to 1). Skills always hit in full.
        public float AttackBlock;
    }

    [Serializable]
    public sealed class WaveEntry
    {
        public HeroDef Hero;
        public int Count = 1;
    }

    [Serializable]
    public sealed class WaveDef
    {
        public List<WaveEntry> Entries = new List<WaveEntry>();
        /// Seconds between each Hero entering the lane.
        public float SpawnInterval = 0.8f;
    }

    /// Lane geometry and global Battle numbers.
    [Serializable]
    public sealed class BattleTuning
    {
        public float LaneLength = 16f;
        public float LaneHalfHeight = 2.4f;
        /// Heroes reaching this x hit the Keep.
        public float KeepX = 0.6f;
        public Vec2 KeeperPosition = new Vec2(3f, 0f);
        public Vec2[] FamiliarPositions = { new Vec2(2.1f, 1.5f), new Vec2(2.1f, -1.5f), new Vec2(1.5f, 0.6f), new Vec2(1.5f, -0.6f) };
        public float FirstWaveDelay = 1f;
        public float TimeBetweenWaves = 2f;
        public float KnockoutSeconds = 5f;
        public float RallyCooldown = 30f;
        /// How charged Rally is when a Battle starts (0 = empty, 1 = ready).
        public float RallyStartCharge = 0.5f;
        /// Fraction of a Skill's cooldown already charged when a Battle starts.
        public float SkillStartCharge = 0f;
    }

    /// Everything needed to start a Battle.
    public sealed class BattleSetup
    {
        public FighterDef Keeper;
        public List<FighterDef> Familiars = new List<FighterDef>();
        public List<WaveDef> Waves = new List<WaveDef>();
        public float KeepHp = 100f;
        public float KeepMaxHp = 100f;
        public int Seed = 1;
        public BattleTuning Tuning = new BattleTuning();
    }
}
