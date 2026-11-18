using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.TestContent;

namespace LittleKeep.Rules.Tests
{
    /// Chain and Nova Skills; healing and status-proof Heroes.
    public class ShapeAndHeroTests
    {
        static HeroDef Dummy(float speed = 0.01f, float hp = 100000f) => Walker(hp: hp, speed: speed);

        /// A bunch of Heroes entering together, close side by side; the Keeper's Skill first casts at 1 second.
        static Battle Bunch(FighterDef keeper, HeroDef hero, int count)
        {
            var setup = Setup(keeper, waves: Wave(hero, count, spawnInterval: 0f));
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.LaneHalfHeight = 0.5f;
            return new Battle(setup);
        }

        static SkillDef Chain(int count, float reach, float damage = 10f) =>
            new SkillDef { Id = "zap", Name = "Zap", Shape = SkillShape.Chain, Element = Element.Spark, Cooldown = 1f, Damage = damage, Count = count, Radius = reach };

        static SkillDef Nova(float radius, float damage = 10f) =>
            new SkillDef { Id = "nova", Name = "Nova", Shape = SkillShape.Nova, Cooldown = 1f, Damage = damage, Radius = radius };

        [Test]
        public void Hits_and_zaps_from_a_grown_Skill_say_how_grown_it_is()
        {
            // So the effects can be bigger for an Upgraded or Master Form Skill.
            var zap = Chain(count: 2, reach: 5f);
            zap.Growth = SkillGrowth.Upgraded;
            var battle = Bunch(Keeper(damage: 0f, skills: zap), Dummy(), 3);

            var events = Run(battle, 1.05f);

            var zaps = events.Where(e => e.Kind == BattleEventKind.Zap).ToList();
            var hits = events.Where(e => e.Kind == BattleEventKind.Hit && e.FromSkill).ToList();
            Assert.That(zaps.Count, Is.EqualTo(2));
            Assert.That(hits.Count, Is.EqualTo(2));
            Assert.That(zaps.All(e => e.Growth == SkillGrowth.Upgraded), Is.True);
            Assert.That(hits.All(e => e.Growth == SkillGrowth.Upgraded), Is.True);
        }

        [Test]
        public void A_Chain_hits_a_Hero_then_jumps_to_the_next_closest_each_once()
        {
            // Four Heroes bunched together: a 3-link chain hits three different Heroes once each.
            var battle = Bunch(Keeper(damage: 0f, skills: Chain(count: 3, reach: 5f)), Dummy(), 4);

            var events = Run(battle, 1.05f);

            var hits = events.Where(e => e.Kind == BattleEventKind.Hit && e.FromSkill).ToList();
            Assert.That(hits.Count, Is.EqualTo(3));
            Assert.That(hits.Select(h => h.TargetId).Distinct().Count(), Is.EqualTo(3));
            Assert.That(events.Count(e => e.Kind == BattleEventKind.Zap), Is.EqualTo(3), "one zap per link, for the effect");
        }

        [Test]
        public void A_Chain_only_jumps_as_far_as_its_reach()
        {
            // Heroes enter 1.5 apart in time and walk at 1/s: they stand 1.5 apart. A reach of 1 can't jump.
            var setup = Setup(Keeper(damage: 0f, skills: Chain(count: 5, reach: 1f)), waves: Wave(Dummy(speed: 1f), 3, spawnInterval: 1.5f));
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.LaneHalfHeight = 0.4f;
            setup.Tuning.SkillStartCharge = 0f;
            var battle = new Battle(setup);
            // Cooldown 1s: first cast at 1s (one Hero on the lane), let all three arrive before looking at later casts.
            Run(battle, 3.2f);

            var events = Run(battle, 1f);

            var hits = events.Where(e => e.Kind == BattleEventKind.Hit && e.FromSkill).ToList();
            Assert.That(hits.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_Nova_hits_Heroes_close_to_its_caster_and_waits_while_none_are()
        {
            var keeper = Keeper(damage: 0f, skills: Nova(radius: 2f));
            var setup = Setup(keeper, waves: Wave(Dummy(speed: 4f), 1));
            setup.Tuning.FirstWaveDelay = 0f;
            var battle = new Battle(setup);

            var far = Run(battle, 2f);   // the Hero is still far up the lane
            var near = Run(battle, 2f);  // it reaches the Keeper (at x=3, lane starts at 16)

            Assert.That(far.Any(e => e.Kind == BattleEventKind.SkillCast), Is.False, "nothing in reach: the Nova waits");
            Assert.That(near.Any(e => e.Kind == BattleEventKind.SkillCast && e.Tag == "nova"), Is.True);
            Assert.That(near.Any(e => e.Kind == BattleEventKind.Hit && e.FromSkill), Is.True);
        }

        [Test]
        public void A_healer_mends_Heroes_near_it_but_not_past_their_health()
        {
            var healer = new HeroDef { Id = "cleric", Name = "Cleric", Hp = 50f, Speed = 0.01f, Size = 0.4f, HealAmount = 5f, HealInterval = 1f, HealRadius = 3f };
            var hurt = Walker(hp: 100f, speed: 0.01f);
            var burst = new SkillDef { Id = "b", Shape = SkillShape.Burst, Cooldown = 100f, Damage = 30f, Radius = 50f };
            var setup = Setup(Keeper(damage: 0f, skills: burst), waves: new WaveDef { SpawnInterval = 0f, Entries = { new WaveEntry { Hero = healer, Count = 1 }, new WaveEntry { Hero = hurt, Count = 1 } } });
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.LaneHalfHeight = 0.4f;
            setup.Tuning.RallyStartCharge = 1f; // both Heroes are on the lane by 0.1s; Rally fires the Burst once
            var battle = new Battle(setup);
            Run(battle, 0.1f);
            Assert.That(battle.TryRally(), Is.True);
            var walker = battle.Heroes.First(h => h.Def.Id == "walker");
            Assert.That(walker.Hp, Is.EqualTo(70f).Within(0.01f));

            var events = Run(battle, 3.05f);

            Assert.That(walker.Hp, Is.EqualTo(85f).Within(0.01f), "three heals of 5");
            Assert.That(events.Count(e => e.Kind == BattleEventKind.HeroHealed && e.TargetId == walker.Id), Is.EqualTo(3));
            Run(battle, 10f);
            Assert.That(walker.Hp, Is.EqualTo(100f).Within(0.01f), "never past full health");
        }

        [Test]
        public void A_steadfast_Hero_takes_the_hit_but_shrugs_off_its_status()
        {
            var steadfast = Walker(hp: 1000f, speed: 0.01f);
            steadfast.Steadfast = true;
            var frost = new SkillDef { Id = "f", Shape = SkillShape.Burst, Element = Element.Frost, Cooldown = 100f, Damage = 10f, Radius = 50f };
            var setup = Setup(Keeper(damage: 0f, skills: frost), waves: Wave(steadfast, 1));
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.SkillStartCharge = 1f;
            var battle = new Battle(setup);

            var events = Run(battle, 0.5f);

            Assert.That(battle.Heroes[0].Hp, Is.EqualTo(990f).Within(0.01f));
            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.None));
            Assert.That(events.Any(e => e.Kind == BattleEventKind.StatusApplied), Is.False);
        }

        [Test]
        public void Rally_holds_back_a_Nova_with_no_Hero_close_enough()
        {
            var keeper = Keeper(damage: 0f, skills: Nova(radius: 2f));
            var setup = Setup(keeper, waves: Wave(Dummy(speed: 4f), 1));
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.RallyStartCharge = 1f;
            setup.Tuning.SkillStartCharge = 1f;
            var battle = new Battle(setup);
            Run(battle, 0.2f); // the Hero has just entered, far up the lane
            battle.TakeEvents();

            Assert.That(battle.TryRally(), Is.True);
            var rallied = battle.TakeEvents();
            var near = Run(battle, 3.5f);

            Assert.That(rallied.Any(e => e.Kind == BattleEventKind.SkillCast), Is.False, "nothing in reach: the Nova is not wasted");
            Assert.That(near.Any(e => e.Kind == BattleEventKind.SkillCast && e.Tag == "nova"), Is.True, "still ready when the Hero arrives");
        }
    }
}
