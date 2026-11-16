using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.TestContent;

namespace LittleKeep.Rules.Tests
{
    public class HeroAndWaveTests
    {
        [Test]
        public void The_next_wave_waits_until_the_current_wave_is_cleared()
        {
            var slow = Walker(hp: 100000f, speed: 1f, keepDamage: 1f);
            var battle = new Battle(Setup(HarmlessKeeper(), waves: new[] { Wave(slow, 1), Wave(slow, 1) }));

            var early = Run(battle, 10f);
            int waveWhileFirstHeroWalks = battle.WaveNumber;
            var late = Run(battle, 12f);

            Assert.That(waveWhileFirstHeroWalks, Is.EqualTo(1));
            Assert.That(early.Count(e => e.Kind == BattleEventKind.HeroSpawned), Is.EqualTo(1));
            Assert.That(late.Any(e => e.Kind == BattleEventKind.WaveStarted && e.Amount == 2f), Is.True);
            Assert.That(battle.WaveNumber, Is.EqualTo(2));
            Assert.That(battle.WaveCount, Is.EqualTo(2));
        }

        [Test]
        public void A_ranged_Hero_stops_at_range_and_shoots_the_squad()
        {
            var archer = new HeroDef { Id = "archer", Hp = 100000f, Speed = 2f, KeepDamage = 5f, Size = 0.35f, ShotDamage = 5f, ShotInterval = 1f, ShotRange = 6f, ShotSpeed = 12f };
            var battle = new Battle(Setup(HarmlessKeeper(hp: 1000f), waves: Wave(archer, 1)));

            Run(battle, 20f);

            var hero = battle.Heroes.Single();
            Assert.That(hero.IsEngaged, Is.True);
            Assert.That(hero.Position.X, Is.GreaterThan(battle.Keeper.Position.X + 4f), "stays at range");
            Assert.That(battle.Keeper.Hp, Is.LessThan(battle.Keeper.MaxHp));
            Assert.That(battle.KeepHp, Is.EqualTo(battle.KeepMaxHp));
        }

        [Test]
        public void A_shielded_Hero_blocks_most_of_an_ordinary_attack_but_none_of_a_Skill()
        {
            var shielded = new HeroDef { Id = "shield", Hp = 100000f, Speed = 0.2f, Size = 0.45f, AttackBlock = 0.75f };
            var burst = new SkillDef { Id = "burst", Shape = SkillShape.Burst, Cooldown = 2f, Damage = 10f, Radius = 2f };
            var battle = new Battle(Setup(Keeper(damage: 10f, interval: 0.5f, skills: burst), waves: Wave(shielded, 1)));

            var hits = Run(battle, 5f).Where(e => e.Kind == BattleEventKind.Hit).ToList();

            Assert.That(hits.Where(h => !h.FromSkill).Select(h => h.Amount), Is.Not.Empty.And.All.EqualTo(2.5f));
            Assert.That(hits.Where(h => h.FromSkill).Select(h => h.Amount), Is.Not.Empty.And.All.EqualTo(10f));
        }

        [Test]
        public void The_same_seed_gives_the_same_Battle()
        {
            string Play(int seed)
            {
                var battle = new Battle(Setup(Keeper(damage: 4f), seed: seed, waves: new[] { Wave(Walker(), 6, 0.3f), Wave(Walker(), 6, 0.3f) }));
                return string.Join("\n", RunToEnd(battle).Select(e => $"{e} {e.Position}"));
            }

            Assert.That(Play(42), Is.EqualTo(Play(42)));
            Assert.That(Play(42), Is.Not.EqualTo(Play(7)));
        }
    }
}
