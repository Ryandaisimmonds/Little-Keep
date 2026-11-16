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
        /// Damage landed. TargetId is the unit hit, Amount the damage after blocking.
        Hit,
        KnockedOut,
        Recovered,
        HeroDefeated,
        HeroReachedKeep,
        HeroAttack,
        Won,
        Lost,
    }

    /// Something that happened in a Battle, for presentation (visuals, sound) and for tests.
    public struct BattleEvent
    {
        public BattleEventKind Kind;
        /// Unit that caused it (0 = none).
        public int SourceId;
        /// Unit it happened to (0 = none).
        public int TargetId;
        public Vec2 Position;
        public float Amount;
        /// Skill id for SkillCast; Hero id for HeroSpawned.
        public string Tag;
        /// For Hit: true if the damage came from a Skill.
        public bool FromSkill;

        public override string ToString() => $"{Kind} src={SourceId} tgt={TargetId} amt={Amount:0.#} {Tag}";
    }
}
