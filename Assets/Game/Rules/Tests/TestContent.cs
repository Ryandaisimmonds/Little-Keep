using System.Collections.Generic;

namespace LittleKeep.Rules.Tests
{
    /// Small, readable defs for tests. Numbers are chosen so outcomes are easy to reason about.
    static class TestContent
    {
        public static FighterDef Keeper(float damage = 10f, float interval = 0.5f, float hp = 50f, params SkillDef[] skills) =>
            new FighterDef { Id = "keeper", Name = "Keeper", Hp = hp, AttackDamage = damage, AttackInterval = interval, ProjectileSpeed = 30f, Skills = new List<SkillDef>(skills) };

        public static FighterDef HarmlessKeeper(float hp = 50f) => Keeper(damage: 0f, hp: hp);

        public static FighterDef Familiar(string id, float damage = 5f, float interval = 0.5f, float hp = 30f, params SkillDef[] skills) =>
            new FighterDef { Id = id, Name = id, Hp = hp, AttackDamage = damage, AttackInterval = interval, ProjectileSpeed = 30f, Skills = new List<SkillDef>(skills) };

        public static HeroDef Walker(float hp = 20f, float speed = 4f, float keepDamage = 10f) =>
            new HeroDef { Id = "walker", Name = "Walker", Hp = hp, Speed = speed, KeepDamage = keepDamage, Size = 0.4f };

        public static WaveDef Wave(HeroDef hero, int count, float spawnInterval = 0.5f) =>
            new WaveDef { Entries = new List<WaveEntry> { new WaveEntry { Hero = hero, Count = count } }, SpawnInterval = spawnInterval };

        public static BattleSetup Setup(FighterDef keeper, float keepHp = 100f, int seed = 1, List<FighterDef> familiars = null, params WaveDef[] waves) =>
            new BattleSetup
            {
                Keeper = keeper,
                Familiars = familiars ?? new List<FighterDef>(),
                Waves = new List<WaveDef>(waves),
                KeepHp = keepHp,
                KeepMaxHp = keepHp,
                Seed = seed,
            };

        /// Advance until the battle ends or the time limit passes, collecting every event.
        public static List<BattleEvent> RunToEnd(Battle battle, float maxSeconds = 120f)
        {
            var events = new List<BattleEvent>();
            while (battle.Outcome == BattleOutcome.Ongoing && battle.Time < maxSeconds)
            {
                battle.Advance(0.1f);
                events.AddRange(battle.TakeEvents());
            }
            return events;
        }

        public static List<BattleEvent> Run(Battle battle, float seconds)
        {
            var events = new List<BattleEvent>();
            float end = battle.Time + seconds;
            while (battle.Time < end - 1e-4f && battle.Outcome == BattleOutcome.Ongoing)
            {
                battle.Advance(System.Math.Min(0.1f, end - battle.Time));
                events.AddRange(battle.TakeEvents());
            }
            return events;
        }
    }
}
