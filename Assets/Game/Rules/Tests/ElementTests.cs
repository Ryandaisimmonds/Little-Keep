using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.TestContent;

namespace LittleKeep.Rules.Tests
{
    public class ElementTests
    {
        /// A Burst big enough to reach the whole lane, so it lands the moment it is cast.
        static SkillDef Burst(string id, Element element, float damage = 1f, float cooldown = 100f) =>
            new SkillDef { Id = id, Name = id, Shape = SkillShape.Burst, Element = element, Cooldown = cooldown, Damage = damage, Radius = 50f };

        static HeroDef Dummy(float speed = 1f) => Walker(hp: 100000f, speed: speed);

        /// Skills are ready at the start, so each casts once as soon as a Hero walks on.
        static Battle Start(FighterDef keeper, List<FighterDef> familiars = null, int heroes = 1, float speed = 1f)
        {
            var setup = Setup(keeper, familiars: familiars, waves: Wave(Dummy(speed), heroes, spawnInterval: 0.05f));
            setup.Tuning.SkillStartCharge = 1f;
            setup.Tuning.FirstWaveDelay = 0f;
            return new Battle(setup);
        }

        [Test]
        public void A_fire_Skill_leaves_burn_which_keeps_hurting_the_Hero()
        {
            var battle = Start(Keeper(damage: 0f, skills: Burst("fire", Element.Fire, damage: 10f)));
            var first = Run(battle, 0.2f);
            var hero = battle.Heroes[0];
            float afterHit = hero.Hp;

            Run(battle, 2f);

            Assert.That(first.Any(e => e.Kind == BattleEventKind.StatusApplied && e.Element == Element.Fire), Is.True);
            Assert.That(hero.Status, Is.EqualTo(Element.Fire));
            Assert.That(hero.Hp, Is.LessThan(afterHit), "burn deals damage over time");
        }

        [Test]
        public void Chill_slows_a_Hero()
        {
            var plain = Start(HarmlessKeeper());
            var chilled = Start(Keeper(damage: 0f, skills: Burst("frost", Element.Frost)));
            Run(plain, 0.2f);
            Run(chilled, 0.2f);
            float plainStart = plain.Heroes[0].Position.X, chilledStart = chilled.Heroes[0].Position.X;

            Run(plain, 1f);
            Run(chilled, 1f);

            Assert.That(chilled.Heroes[0].Status, Is.EqualTo(Element.Frost));
            Assert.That(chilledStart - chilled.Heroes[0].Position.X, Is.LessThan((plainStart - plain.Heroes[0].Position.X) * 0.8f));
        }

        [Test]
        public void Shock_makes_a_Hero_take_more_damage()
        {
            var battle = Start(Keeper(damage: 10f, skills: Burst("spark", Element.Spark)));

            var events = Run(battle, 1.5f);

            var attackHits = events.Where(e => e.Kind == BattleEventKind.Hit && !e.FromSkill).ToList();
            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.Spark));
            Assert.That(attackHits, Is.Not.Empty);
            Assert.That(attackHits, Has.All.Matches<BattleEvent>(h => h.Amount > 10f));
        }

        [Test]
        public void Root_holds_a_Hero_in_place()
        {
            var battle = Start(Keeper(damage: 0f, skills: Burst("bloom", Element.Bloom)));
            Run(battle, 0.2f);
            float x = battle.Heroes[0].Position.X;

            Run(battle, 0.8f);

            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.Bloom));
            Assert.That(battle.Heroes[0].Position.X, Is.EqualTo(x).Within(1e-4f));
        }

        /// Fire (Keeper, cast first) and frost (Familiar) both come off cooldown at 1s, with two Heroes side by side.
        static Battle FireThenFrost(bool singleFrostShot = false, float heroHp = 100000f)
        {
            var keeper = Keeper(damage: 0f, skills: Burst("fire", Element.Fire, damage: 10f, cooldown: 1f));
            var frost = singleFrostShot
                ? new SkillDef { Id = "frost", Shape = SkillShape.Volley, Element = Element.Frost, Cooldown = 1f, Damage = 10f, Count = 1, ProjectileSpeed = 40f }
                : Burst("frost", Element.Frost, damage: 10f, cooldown: 1f);
            var familiar = Familiar("frosty", damage: 0f, skills: frost);
            var setup = Setup(keeper, familiars: new List<FighterDef> { familiar }, waves: Wave(Walker(hp: heroHp, speed: 0.1f), 2, spawnInterval: 0f));
            setup.Tuning.FirstWaveDelay = 0f;
            setup.Tuning.LaneHalfHeight = 0.5f;
            return new Battle(setup);
        }

        [Test]
        public void A_second_Element_on_a_status_triggers_a_Combo_that_bursts_and_consumes_the_status()
        {
            var battle = FireThenFrost();

            var events = Run(battle, 1.05f);

            var combo = events.First(e => e.Kind == BattleEventKind.Combo);
            Assert.That(combo.Amount, Is.GreaterThan(10f), "a Combo bursts harder than the hit that set it off");
            Assert.That(battle.Heroes.First(h => h.Id == combo.TargetId).Status, Is.EqualTo(Element.None));
        }

        [Test]
        public void A_Combo_bursts_onto_Heroes_close_by()
        {
            var battle = FireThenFrost(singleFrostShot: true);

            var events = Run(battle, 1.6f);

            Assert.That(events.Count(e => e.Kind == BattleEventKind.Combo), Is.EqualTo(1), "one frost shot, one Combo");
            var burstHits = events.Where(e => e.Kind == BattleEventKind.Hit && e.Combo).Select(e => e.TargetId).Distinct();
            Assert.That(burstHits.Count(), Is.EqualTo(2), "the burst reaches the second Hero too");
        }

        [Test]
        public void A_killing_hit_still_sets_off_its_Combo()
        {
            // Fire leaves 9 of 19 health; the frost shot's 10 finishes the Hero.
            var battle = FireThenFrost(singleFrostShot: true, heroHp: 19f);

            var events = Run(battle, 1.6f);

            var combo = events.First(e => e.Kind == BattleEventKind.Combo);
            Assert.That(events.Any(e => e.Kind == BattleEventKind.HeroDefeated && e.TargetId == combo.TargetId), Is.True);
            Assert.That(events.Any(e => e.Kind == BattleEventKind.Hit && e.Combo && e.TargetId != combo.TargetId), Is.True, "the burst still reaches the other Hero");
        }

        [Test]
        public void One_Burst_over_several_statused_Heroes_makes_one_Combo_not_a_chain()
        {
            var battle = FireThenFrost();

            var events = Run(battle, 1.05f);

            // The Combo's burst blows the burn off the second Hero, so the rest of the frost Burst just chills it.
            Assert.That(events.Count(e => e.Kind == BattleEventKind.Combo), Is.EqualTo(1));
            Assert.That(battle.Heroes.Select(h => h.Status), Is.EquivalentTo(new[] { Element.None, Element.Frost }));
        }

        [Test]
        public void Repeated_fire_hits_keep_the_burn_ticking()
        {
            // A fire Burst every 0.3s: faster than burn ticks. Burn must still deal damage between hits.
            var rapid = new SkillDef { Id = "embers", Shape = SkillShape.Burst, Element = Element.Fire, Cooldown = 0.3f, Damage = 10f, Radius = 50f };
            var battle = Start(Keeper(damage: 0f, skills: rapid));

            var events = Run(battle, 2f);

            Assert.That(events.Any(e => e.Kind == BattleEventKind.Hit && e.Element == Element.Fire && e.Amount < 10f), Is.True, "burn ticks land between hits");
        }

        [Test]
        public void The_same_Element_again_refreshes_its_status_without_a_Combo()
        {
            var keeper = Keeper(damage: 0f, skills: Burst("fire", Element.Fire));
            var familiar = Familiar("pip", damage: 0f, skills: Burst("fire2", Element.Fire));
            var battle = Start(keeper, new List<FighterDef> { familiar });

            var events = Run(battle, 0.5f);

            Assert.That(events.Any(e => e.Kind == BattleEventKind.Combo), Is.False);
            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.Fire));
        }

        [Test]
        public void Ordinary_attacks_carry_no_Element()
        {
            var battle = Start(Keeper(damage: 1f));

            var events = Run(battle, 2f);

            Assert.That(events.Any(e => e.Kind == BattleEventKind.Hit), Is.True);
            Assert.That(events.Any(e => e.Kind == BattleEventKind.StatusApplied), Is.False);
            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.None));
        }

        [Test]
        public void A_status_wears_off()
        {
            var battle = Start(Keeper(damage: 0f, skills: Burst("frost", Element.Frost)));
            Run(battle, 0.2f);

            var events = Run(battle, battle.Tuning.StatusSeconds + 0.5f);

            Assert.That(battle.Heroes[0].Status, Is.EqualTo(Element.None));
            Assert.That(events.Any(e => e.Kind == BattleEventKind.StatusEnded), Is.True);
        }
    }
}
