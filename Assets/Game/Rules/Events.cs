namespace LittleKeep.Rules
{
    public enum BattleOutcome { Ongoing, Won, Lost }

    public enum BattleEventKind
    {
        WaveStarted,
        HeroSpawned,
        /// A Keeper or Familiar fired an ordinary attack.
        Attack,
        SkillCast,
        Rally,
        /// Damage landed. TargetId is who was hit, Amount the damage after blocking.
        Hit,
        KnockedOut,
        Recovered,
        HeroDefeated,
        HeroReachedKeep,
        HeroAttack,
        /// A defeated Hero's creature is free. Tag is the creature id.
        CreatureFreed,
        /// A Hero now carries a status. Element is the Element that left it.
        StatusApplied,
        /// A Hero's status wore off. Element is the status that ended.
        StatusEnded,
        /// A Chain Skill jumped: From is where it came from, Position where it struck.
        Zap,
        /// A healer mended a Hero. Amount is the health restored.
        HeroHealed,
        /// A second Element hit a Hero carrying another's status. Element hit, Other was the status; Amount is the burst damage.
        Combo,
        Won,
        Lost,
    }

    /// Something that happened in a Battle, for presentation (visuals, sound) and for tests.
    public struct BattleEvent
    {
        public BattleEventKind Kind;
        /// Id of the Keeper, Familiar or Hero that caused it (0 = none).
        public int SourceId;
        /// Id of the Keeper, Familiar or Hero it happened to (0 = none).
        public int TargetId;
        public Vec2 Position;
        public float Amount;
        /// Skill id for SkillCast; Hero type id for HeroSpawned and HeroDefeated; creature id for CreatureFreed.
        public string Tag;
        /// For Hit: true if the damage came from a Skill.
        public bool FromSkill;
        /// For Hit, StatusApplied, StatusEnded and Combo: the Element involved.
        public Element Element;
        /// For Combo: the status that was consumed.
        public Element Other;
        /// For Zap: where the jump started.
        public Vec2 From;
        /// For Hit: true if the damage is a Combo's burst.
        public bool Combo;

        public override string ToString() => $"{Kind} src={SourceId} tgt={TargetId} amt={Amount:0.#} {Tag}";
    }
}
