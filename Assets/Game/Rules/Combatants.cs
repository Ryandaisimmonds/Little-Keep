using System.Collections.Generic;

namespace LittleKeep.Rules
{
    public sealed class SkillState
    {
        public SkillDef Def { get; }
        public float CooldownRemaining { get; internal set; }
        /// 0 = just cast, 1 = ready.
        public float Charge => Def.Cooldown <= 0f ? 1f : 1f - CooldownRemaining / Def.Cooldown;

        internal SkillState(SkillDef def, float startCharge)
        {
            Def = def;
            CooldownRemaining = def.Cooldown * (1f - startCharge);
        }
    }

    /// The Keeper or a Familiar in a Battle.
    public sealed class Fighter
    {
        public int Id { get; }
        public FighterDef Def { get; }
        public bool IsKeeper { get; }
        public Vec2 Position { get; }
        public float Hp { get; internal set; }
        public float MaxHp => Def.Hp;
        public bool IsKnockedOut => KnockoutRemaining > 0f;
        public float KnockoutRemaining { get; internal set; }
        public IReadOnlyList<SkillState> Skills => skills;
        internal readonly List<SkillState> skills = new List<SkillState>();
        internal float attackTimer;

        internal Fighter(int id, FighterDef def, bool isKeeper, Vec2 position, float skillStartCharge)
        {
            Id = id;
            Def = def;
            IsKeeper = isKeeper;
            Position = position;
            Hp = def.Hp;
            foreach (var s in def.Skills) skills.Add(new SkillState(s, skillStartCharge));
        }
    }

    public sealed class Hero
    {
        public int Id { get; }
        public HeroDef Def { get; }
        public Vec2 Position { get; internal set; }
        public float Hp { get; internal set; }
        public float MaxHp => Def.Hp;
        /// True while stopped to fight a Keeper or Familiar.
        public bool IsEngaged { get; internal set; }
        /// The status this Hero carries (None if none), and how long it has left.
        public Element Status { get; internal set; }
        public float StatusRemaining { get; internal set; }
        internal float burnPerTick, burnTimer;
        internal int statusSource;
        internal float attackTimer;
        internal float summonTimer;

        internal Hero(int id, HeroDef def, Vec2 position)
        {
            Id = id;
            Def = def;
            Position = position;
            Hp = def.Hp;
            summonTimer = def.SummonInterval;
        }
    }

    public sealed class Projectile
    {
        public int Id { get; }
        public Vec2 Position { get; internal set; }
        public Vec2 Velocity { get; internal set; }
        /// True if fired by the Keeper or a Familiar; false if fired by a Hero.
        public bool FromSquad { get; }
        public bool FromSkill { get; }
        public int SourceId { get; }
        /// How far the Skill that fired it has grown (Base for ordinary attacks).
        public SkillGrowth Growth { get; }
        public Element Element { get; }
        internal readonly float damage;

        internal Projectile(int id, int sourceId, Vec2 position, Vec2 velocity, float damage, bool fromSquad, bool fromSkill, SkillGrowth growth = SkillGrowth.Base, Element element = Element.None)
        {
            Element = element;
            Growth = growth;
            Id = id;
            SourceId = sourceId;
            Position = position;
            Velocity = velocity;
            this.damage = damage;
            FromSquad = fromSquad;
            FromSkill = fromSkill;
        }
    }
}
