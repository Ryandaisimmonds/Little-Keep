using System;
using System.Collections.Generic;

namespace LittleKeep.Rules
{
    /// A kind of Skill damage. Each leaves its own status on a Hero: Fire burns, Frost chills, Spark shocks, Bloom roots.
    public enum Element { None, Fire, Frost, Spark, Bloom }

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
        public Element Element;
        public float Cooldown = 6f;
        public float Damage = 10f;
        public int Count = 1;
        public float Spread = 20f;
        public float Radius = 1.5f;
        public float ProjectileSpeed = 18f;

        /// The evolved version this Skill becomes after three Skill Upgrades (null = none). Filled from content, not serialized here.
        [NonSerialized] public SkillDef MasterForm;
        /// This Skill's own pool of Skill Upgrades. Filled from content, not serialized here.
        [NonSerialized] public List<SkillUpgradeDef> Upgrades = new List<SkillUpgradeDef>();
        /// Visual loudness: 0 = base, 1 = upgraded, 2 = Master Form. Set by the Run.
        [NonSerialized] public int Tier;

        public SkillDef Clone()
        {
            var c = (SkillDef)MemberwiseClone();
            c.Upgrades = new List<SkillUpgradeDef>(Upgrades);
            return c;
        }
    }

    public enum SkillStat { Damage, Cooldown, Count, Radius, Spread, ProjectileSpeed }
    public enum ModOp { Add, Multiply }

    [Serializable]
    public struct SkillMod
    {
        public SkillStat Stat;
        public ModOp Op;
        public float Value;

        public SkillMod(SkillStat stat, ModOp op, float value) { Stat = stat; Op = op; Value = value; }

        public void ApplyTo(SkillDef s)
        {
            switch (Stat)
            {
                case SkillStat.Damage: s.Damage = Apply(s.Damage); break;
                case SkillStat.Cooldown: s.Cooldown = Math.Max(0.5f, Apply(s.Cooldown)); break;
                case SkillStat.Count: s.Count = Math.Max(1, (int)Math.Round(Apply(s.Count))); break;
                case SkillStat.Radius: s.Radius = Apply(s.Radius); break;
                case SkillStat.Spread: s.Spread = Apply(s.Spread); break;
                case SkillStat.ProjectileSpeed: s.ProjectileSpeed = Apply(s.ProjectileSpeed); break;
            }
        }

        float Apply(float v) => Op == ModOp.Add ? v + Value : v * Value;
    }

    /// A change to one Skill, offered after a Battle. Belongs to that Skill's pool and targets it by id.
    [Serializable]
    public sealed class SkillUpgradeDef
    {
        public string Id = "upgrade";
        public string Name = "Upgrade";
        public string Description = "";
        public List<SkillMod> Mods = new List<SkillMod>();
        /// The Skill this upgrades. Filled from content.
        [NonSerialized] public string SkillId;
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

        /// Id of the creature this Hero brought along, freed when the Hero is defeated ("" = none).
        public string CreatureId = "";

        /// Bosses get a big health bar and may call in more Heroes.
        public bool IsBoss;
        /// Hero type a boss calls in (null = none). Filled from content, not serialized here.
        [NonSerialized] public HeroDef Summon;
        public float SummonInterval = 8f;
        public int SummonCount = 2;

        public HeroDef Clone() => (HeroDef)MemberwiseClone();
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

        /// How long burn, chill and shock last.
        public float StatusSeconds = 3f;
        /// How long root holds a Hero; bosses are held for BossRootFactor of that.
        public float RootSeconds = 1.5f;
        public float BossRootFactor = 0.5f;
        /// Burn deals this fraction of the hit that lit it, spread over StatusSeconds.
        public float BurnFraction = 0.5f;
        public float BurnTickSeconds = 0.5f;
        /// Chill takes this fraction off a Hero's speed.
        public float ChillSlow = 0.4f;
        /// A shocked Hero takes this much more damage.
        public float ShockBonus = 0.2f;
        /// A Combo deals this multiple of the hit that set it off, to every Hero within ComboRadius.
        public float ComboMultiplier = 1.25f;
        public float ComboRadius = 1.5f;

        public BattleTuning Clone() => (BattleTuning)MemberwiseClone();
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
