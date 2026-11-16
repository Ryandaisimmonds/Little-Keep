using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.TestContent;

namespace LittleKeep.Rules.Tests
{
    public class SkillTests
    {
        static SkillDef Volley(string id, float cooldown, float damage = 1f) =>
            new SkillDef { Id = id, Name = id, Shape = SkillShape.Volley, Cooldown = cooldown, Damage = damage, Count = 3, Spread = 10f, ProjectileSpeed = 30f };

        static HeroDef Dummy() => Walker(hp: 100000f, speed: 0.2f);

        [Test]
        public void A_Skill_casts_itself_each_time_its_cooldown_is_ready()
        {
            var keeper = Keeper(damage: 0f, skills: Volley("volley", cooldown: 2f));
            var battle = new Battle(Setup(keeper, waves: Wave(Dummy(), 1)));

            var events = Run(battle, 7.5f);

            Assert.That(events.Count(e => e.Kind == BattleEventKind.SkillCast && e.Tag == "volley"), Is.EqualTo(3));
            Assert.That(events.Any(e => e.Kind == BattleEventKind.Hit && e.FromSkill), Is.True, "Skill shots land");
        }

        [Test]
        public void A_Burst_Skill_hits_every_Hero_near_its_target_at_once()
        {
            var burst = new SkillDef { Id = "burst", Shape = SkillShape.Burst, Cooldown = 3f, Damage = 7f, Radius = 50f };
            var battle = new Battle(Setup(Keeper(damage: 0f, skills: burst), waves: Wave(Dummy(), 3, spawnInterval: 0.2f)));

            var events = Run(battle, 3.2f);

            var hits = events.Where(e => e.Kind == BattleEventKind.Hit && e.FromSkill).ToList();
            Assert.That(hits.Select(h => h.TargetId).Distinct().Count(), Is.EqualTo(3));
            Assert.That(hits, Has.All.Matches<BattleEvent>(h => h.Amount == 7f));
        }

        [Test]
        public void Rally_casts_every_Skill_in_the_squad_at_once_then_waits_for_its_cooldown()
        {
            var keeper = Keeper(damage: 0f, skills: Volley("keeper_skill", cooldown: 100f));
            var familiar = Familiar("pip", damage: 0f, skills: Volley("pip_skill", cooldown: 100f));
            var setup = Setup(keeper, familiars: new List<FighterDef> { familiar }, waves: Wave(Dummy(), 1));
            setup.Tuning.RallyCooldown = 10f;
            setup.Tuning.RallyStartCharge = 1f;
            var battle = new Battle(setup);
            Run(battle, 2f);

            bool first = battle.TryRally();
            var rallied = battle.TakeEvents();
            bool again = battle.TryRally();
            Run(battle, 10.1f);
            bool later = battle.TryRally();

            Assert.That(first, Is.True);
            Assert.That(rallied.Any(e => e.Kind == BattleEventKind.Rally), Is.True);
            Assert.That(rallied.Where(e => e.Kind == BattleEventKind.SkillCast).Select(e => e.Tag), Is.EquivalentTo(new[] { "keeper_skill", "pip_skill" }));
            Assert.That(again, Is.False, "Rally is cooling down");
            Assert.That(later, Is.True, "Rally is ready again after its cooldown");
        }

        [Test]
        public void Rally_does_nothing_before_any_Hero_has_arrived()
        {
            var setup = Setup(Keeper(skills: Volley("v", 100f)), waves: Wave(Dummy(), 1));
            setup.Tuning.RallyStartCharge = 1f;
            setup.Tuning.FirstWaveDelay = 5f;
            var battle = new Battle(setup);

            Assert.That(battle.TryRally(), Is.False);
            Assert.That(battle.RallyCharge, Is.EqualTo(1f));
        }
    }
}
