using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.RunTestContent;
using static LittleKeep.Rules.Tests.RunTests;

namespace LittleKeep.Rules.Tests
{
    /// Familiars start a Run with their first Skill and learn the rest along the way.
    public class SkillLearningTests
    {
        /// Pip knows "pip_skill" and can learn "pip_spark" and "pip_frost"; the Keeper has one Skill.
        static RunContent TwoSkillPip()
        {
            var content = Make();
            var pip = content.FindFamiliar("pip");
            pip.Skills.Add(Skill("pip_spark"));
            pip.Skills.Add(Skill("pip_frost"));
            return content;
        }

        static Run AtFirstUpgrade(int seed)
        {
            var run = new Run(TwoSkillPip(), seed, "pip");
            GoToBattle(run);
            run.FinishBattle(Win(run));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Upgrade));
            return run;
        }

        [Test]
        public void A_Familiar_starts_a_Run_knowing_only_its_first_Skill()
        {
            var run = new Run(TwoSkillPip(), 1, "pip");
            GoToBattle(run);

            var pip = run.CreateBattle().Familiars.Single();
            Assert.That(pip.Skills.Select(s => s.Id), Is.EqualTo(new[] { "pip_skill" }));
            Assert.That(run.Familiars[0].Skills.Select(s => s.IsLearned), Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void Upgrade_offers_can_teach_the_next_Skill_which_then_fights()
        {
            Run run = null;
            int index = -1;
            for (int seed = 1; seed < 60 && index < 0; seed++)
            {
                run = AtFirstUpgrade(seed);
                index = run.UpgradeOffer.ToList().FindIndex(o => o.Learns);
            }
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "some seed offers a Skill to learn");
            var learn = run.UpgradeOffer[index];
            Assert.That(learn.Skill.Base.Id, Is.EqualTo("pip_spark"), "Skills are learned in order");

            run.PickUpgrade(index);
            while (run.Phase != RunPhase.Trail) Resolve(run);
            GoToBattle(run);

            Assert.That(run.CreateBattle().Familiars.Single().Skills.Select(s => s.Id), Is.EqualTo(new[] { "pip_skill", "pip_spark" }));
        }

        [Test]
        public void Only_learned_Skills_are_offered_upgrades()
        {
            for (int seed = 1; seed < 30; seed++)
            {
                var run = AtFirstUpgrade(seed);
                foreach (var o in run.UpgradeOffer.Where(o => !o.Learns))
                    Assert.That(o.Skill.IsLearned, Is.True, $"seed {seed} offered {o.Upgrade.Id}");
                foreach (var o in run.UpgradeOffer.Where(o => o.Learns))
                    Assert.That(o.Upgrade, Is.Null);
            }
        }

        [Test]
        public void A_learned_Skill_survives_saving_and_loading()
        {
            Run run = null;
            int index = -1;
            for (int seed = 1; seed < 60 && index < 0; seed++)
            {
                run = AtFirstUpgrade(seed);
                index = run.UpgradeOffer.ToList().FindIndex(o => o.Learns);
            }
            run.PickUpgrade(index);

            var loaded = Run.Load(TwoSkillPip(), run.Save());

            Assert.That(loaded.Familiars[0].Skills.Count(s => s.IsLearned), Is.EqualTo(2));
            Assert.That(loaded.Save(), Is.EqualTo(run.Save()));
        }
    }
}
