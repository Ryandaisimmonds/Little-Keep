using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.RunTestContent;

namespace LittleKeep.Rules.Tests
{
    public class RunTests
    {
        [Test]
        public void A_Run_is_three_Chapters_each_with_a_mid_boss_and_a_boss_and_beating_the_last_boss_wins_it()
        {
            var run = new Run(Make(), seed: 5, starterId: "pip");

            var trace = Autoplay(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended));
            Assert.That(run.Result.Won, Is.True);
            Assert.That(trace.Count(t => t.StartsWith("battle Boss")), Is.EqualTo(3));
            Assert.That(trace.Count(t => t.StartsWith("battle MidBoss")), Is.EqualTo(3));
            Assert.That(run.Result.ChaptersCleared, Is.EqualTo(3));
        }

        [Test]
        public void The_Trail_shows_the_next_step_and_the_one_after_and_counts_down_to_the_boss()
        {
            var run = new Run(Make(), seed: 5, starterId: "pip");
            int start = run.StepsToBoss;

            Assert.That(run.NextChoices.Count, Is.InRange(2, 3), "the Trail forks");
            Assert.That(run.Beyond, Is.Not.Empty, "the step after is visible");

            run.ChooseStop(0);
            while (run.Phase != RunPhase.Trail) Resolve(run);
            Assert.That(run.StepsToBoss, Is.EqualTo(start - 1));

            while (run.StepsToBoss > 1)
            {
                run.ChooseStop(0);
                while (run.Phase != RunPhase.Trail) Resolve(run);
            }
            Assert.That(run.NextChoices.Select(s => s.Kind), Is.EqualTo(new[] { StopKind.Boss }));
            Assert.That(run.Beyond, Is.Empty, "fog hides the next Chapter");
        }

        [Test]
        public void Keep_health_carries_between_Battles_and_the_Run_ends_when_the_Keep_falls()
        {
            var run = new Run(Make(), seed: 3, starterId: "pip");
            GoToBattle(run);
            run.FinishBattle(Win(run, keepHp: 60f));
            Assert.That(run.KeepHp, Is.EqualTo(60f));
            while (run.Phase != RunPhase.Trail) Resolve(run);
            GoToBattle(run);

            Assert.That(run.CreateBattle().KeepHp, Is.EqualTo(run.KeepHp), "the next Battle starts with the Keep as it was left (rests and events aside)");
            Assert.That(run.KeepHp, Is.LessThan(run.KeepMaxHp));

            run.FinishBattle(new BattleReport { Won = false, KeepHp = 0f });
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Ended));
            Assert.That(run.Result.Won, Is.False);
            Assert.That(run.Result.BattlesWon, Is.EqualTo(1));
        }

        [Test]
        public void The_same_seed_and_choices_give_the_same_Run()
        {
            Assert.That(Autoplay(new Run(Make(), 11, "pip")), Is.EqualTo(Autoplay(new Run(Make(), 11, "pip"))));
            Assert.That(Autoplay(new Run(Make(), 11, "pip")), Is.Not.EqualTo(Autoplay(new Run(Make(), 12, "pip"))));
        }

        internal static void GoToBattle(Run run)
        {
            for (int guard = 0; guard < 50 && run.Phase != RunPhase.Battle; guard++)
            {
                if (run.Phase == RunPhase.Trail)
                {
                    int i = run.NextChoices.ToList().FindIndex(s => s.Kind == StopKind.Battle || s.Kind == StopKind.ToughBattle || s.Kind == StopKind.MidBoss || s.Kind == StopKind.Boss);
                    run.ChooseStop(i < 0 ? 0 : i);
                }
                else Resolve(run);
            }
        }

        /// Gets past any non-Trail phase with a neutral choice.
        internal static void Resolve(Run run)
        {
            switch (run.Phase)
            {
                case RunPhase.Ended: throw new System.InvalidOperationException("The Run ended unexpectedly.");
                case RunPhase.Trail: throw new System.InvalidOperationException("Nothing to resolve on the Trail.");
                case RunPhase.Battle: run.FinishBattle(Win(run)); break;
                case RunPhase.Upgrade: run.SkipUpgrade(); break;
                case RunPhase.Befriend: run.PassCreature(); break;
                case RunPhase.Shop: run.LeaveShop(); break;
                case RunPhase.Event: run.ChooseEventOption(Enumerable.Range(0, run.CurrentEvent.Options.Count).First(run.CanChoose)); break;
            }
        }
    }
}
