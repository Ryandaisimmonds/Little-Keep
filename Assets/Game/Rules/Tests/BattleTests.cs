using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.TestContent;

namespace LittleKeep.Rules.Tests
{
    public class BattleTests
    {
        [Test]
        public void A_hero_that_reaches_the_Keep_damages_it_and_the_battle_is_lost_when_the_Keep_falls()
        {
            var battle = new Battle(Setup(HarmlessKeeper(), keepHp: 25f, waves: Wave(Walker(keepDamage: 10f), 3)));

            var events = RunToEnd(battle);

            Assert.That(events.Count(e => e.Kind == BattleEventKind.HeroReachedKeep), Is.EqualTo(3));
            Assert.That(battle.KeepHp, Is.EqualTo(0f));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Lost));
        }
    
        [Test]
        public void The_Keeper_attacks_on_its_own_and_clearing_every_wave_wins_the_battle()
        {
            var battle = new Battle(Setup(Keeper(damage: 10f, interval: 0.25f), keepHp: 100f,
                waves: new[] { Wave(Walker(hp: 20f, speed: 1f), 2), Wave(Walker(hp: 20f, speed: 1f), 2) }));

            var events = RunToEnd(battle);

            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(battle.KeepHp, Is.EqualTo(100f));
            Assert.That(events.Count(e => e.Kind == BattleEventKind.HeroDefeated), Is.EqualTo(4));
            Assert.That(events.Count(e => e.Kind == BattleEventKind.Attack), Is.GreaterThanOrEqualTo(8));
        }
    
        [Test]
        public void The_Keeper_auto_aims_at_the_Hero_closest_to_the_Keep_until_the_Player_holds_an_aim_point()
        {
            var tough = Walker(hp: 10000f, speed: 0.5f);
            var battle = new Battle(Setup(Keeper(damage: 1f, interval: 0.2f), waves: Wave(tough, 2, spawnInterval: 4f)));
            Run(battle, 6f);
            var front = battle.Heroes.OrderBy(h => h.Position.X).First();
            var held = new Vec2(9f, 2f);

            var auto = KeeperAimsDuring(battle, 1f, aim: null);
            var aimed = KeeperAimsDuring(battle, 1f, aim: held);
            var released = KeeperAimsDuring(battle, 1f, aim: null);

            Assert.That(auto, Is.Not.Empty.And.All.Matches<Vec2>(p => System.Math.Abs(p.Y - front.Position.Y) < 0.01f));
            Assert.That(aimed, Is.Not.Empty.And.All.Matches<Vec2>(p => Vec2.Distance(p, held) < 0.01f));
            Assert.That(released, Is.Not.Empty.And.All.Matches<Vec2>(p => System.Math.Abs(p.Y - front.Position.Y) < 0.01f));
        }

        static System.Collections.Generic.List<Vec2> KeeperAimsDuring(Battle battle, float seconds, Vec2? aim)
        {
            battle.SetAim(aim);
            return Run(battle, seconds)
                .Where(e => e.Kind == BattleEventKind.Attack && e.SourceId == battle.Keeper.Id)
                .Select(e => e.Position).ToList();
        }

        [Test]
        public void Familiars_attack_on_their_own()
        {
            var battle = new Battle(Setup(HarmlessKeeper(), familiars: new System.Collections.Generic.List<FighterDef> { Familiar("pip", damage: 10f, interval: 0.25f) },
                waves: Wave(Walker(hp: 20f, speed: 1f), 3)));

            RunToEnd(battle);

            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
        }

        [Test]
        public void A_beaten_Keeper_is_knocked_out_stops_attacking_and_gets_back_up_with_full_health()
        {
            var brawler = new HeroDef { Id = "brawler", Hp = 10000f, Speed = 2f, KeepDamage = 1f, Size = 0.4f, MeleeDamage = 10f, MeleeInterval = 0.5f, MeleeRange = 5f };
            var rock = Familiar("rock", damage: 0f, hp: 10000f);
            var battle = new Battle(Setup(HarmlessKeeper(hp: 30f), familiars: new System.Collections.Generic.List<FighterDef> { rock }, waves: Wave(brawler, 1)));

            var untilKnockout = new System.Collections.Generic.List<BattleEvent>();
            while (!untilKnockout.Any(e => e.Kind == BattleEventKind.KnockedOut) && battle.Outcome == BattleOutcome.Ongoing && battle.Time < 30f)
                untilKnockout.AddRange(Run(battle, 0.1f));
            var whileOut = Run(battle, battle.Tuning.KnockoutSeconds - 0.2f);
            bool stillOut = battle.Keeper.IsKnockedOut;
            var afterwards = Run(battle, 0.4f);

            Assert.That(untilKnockout.Any(e => e.Kind == BattleEventKind.KnockedOut && e.TargetId == battle.Keeper.Id), "Keeper should be knocked out");
            Assert.That(stillOut, Is.True);
            Assert.That(whileOut.Any(e => e.Kind == BattleEventKind.Attack && e.SourceId == battle.Keeper.Id), Is.False, "a knocked-out Keeper does not attack");
            Assert.That(afterwards.Any(e => e.Kind == BattleEventKind.Recovered && e.TargetId == battle.Keeper.Id), Is.True);
            Assert.That(battle.Keeper.Hp, Is.EqualTo(battle.Keeper.MaxHp));
        }
    }
}
