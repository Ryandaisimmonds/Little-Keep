using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LittleKeep.Rules.Tests.RunTestContent;
using static LittleKeep.Rules.Tests.RunTests;

namespace LittleKeep.Rules.Tests
{
    public class RunGrowthTests
    {
        static Run AfterFirstBattle(int seed = 1, params string[] freed)
        {
            var run = new Run(Make(), seed, "pip");
            GoToBattle(run);
            run.FinishBattle(Win(run, -1f, freed));
            return run;
        }

        [Test]
        public void After_a_Battle_the_Player_picks_1_of_3_Skill_Upgrades_from_the_squads_own_Skills()
        {
            var run = AfterFirstBattle();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Upgrade));
            Assert.That(run.UpgradeOffer.Count, Is.EqualTo(3));
            var squadSkills = new[] { "keeper_skill", "pip_skill" };
            Assert.That(run.UpgradeOffer.Select(o => o.Upgrade.SkillId), Has.All.Matches<string>(id => squadSkills.Contains(id)));
        }

        [Test]
        public void A_picked_Skill_Upgrade_changes_that_Skill_in_the_next_Battle()
        {
            var run = AfterFirstBattle();
            var choice = run.UpgradeOffer[0];
            string skillId = choice.Upgrade.SkillId, fighterId = choice.Member.Def.Id;

            run.PickUpgrade(0);
            while (run.Phase != RunPhase.Trail) Resolve(run);
            GoToBattle(run);
            var setup = run.CreateBattle();

            var fighter = fighterId == "keeper" ? setup.Keeper : setup.Familiars.Single(f => f.Id == fighterId);
            var skill = fighter.Skills.Single(s => s.Id == skillId);
            Assert.That(skill.Damage, Is.EqualTo(15f), "base 10 + 5");
            Assert.That(skill.Tier, Is.EqualTo(1));
        }

        [Test]
        public void A_third_Skill_Upgrade_turns_a_Skill_into_its_Master_Form()
        {
            var run = new Run(Make(), 2, "pip");
            int picks = 0;
            for (int guard = 0; guard < 200 && run.Phase != RunPhase.Ended && run.Keeper.Skills[0].Taken.Count < 3; guard++)
            {
                if (run.Phase == RunPhase.Upgrade)
                {
                    int i = run.UpgradeOffer.ToList().FindIndex(o => o.Member == run.Keeper);
                    if (i >= 0) { if (run.UpgradeOffer[i].MakesMaster) Assert.That(run.Keeper.Skills[0].Taken.Count, Is.EqualTo(2)); run.PickUpgrade(i); picks++; }
                    else run.SkipUpgrade();
                }
                else if (run.Phase == RunPhase.Trail) run.ChooseStop(0);
                else Resolve(run);
            }
            while (run.Phase != RunPhase.Trail && run.Phase != RunPhase.Ended) Resolve(run);
            GoToBattle(run);

            var skill = run.CreateBattle().Keeper.Skills[0];
            Assert.That(run.Keeper.Skills[0].IsMaster, Is.True);
            Assert.That(skill.Id, Is.EqualTo("keeper_skill_master"));
            Assert.That(skill.Shape, Is.EqualTo(SkillShape.Burst));
            Assert.That(skill.Damage, Is.EqualTo(65f), "Master Form 50 + three upgrades of 5");
            Assert.That(skill.Tier, Is.EqualTo(2));
        }

        [Test]
        public void A_creature_freed_in_Battle_can_be_befriended_and_fights_in_the_next_Battle()
        {
            var run = AfterFirstBattle(1, "pony");
            run.SkipUpgrade();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Befriend));
            Assert.That(run.CreatureOffer.Id, Is.EqualTo("pony"));
            run.Befriend();
            while (run.Phase != RunPhase.Trail) Resolve(run);
            GoToBattle(run);

            Assert.That(run.CreateBattle().Familiars.Select(f => f.Id), Is.EqualTo(new[] { "pip", "pony" }));
        }

        [Test]
        public void Freed_creatures_already_in_the_squad_and_unknown_ids_are_not_offered()
        {
            var run = AfterFirstBattle(1, "pip", "a_hero_not_a_creature");
            run.SkipUpgrade();

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Trail));
        }

        [Test]
        public void With_a_full_squad_a_new_creature_takes_the_place_of_a_chosen_Familiar()
        {
            var run = new Run(Make(), 1, "pip");
            foreach (var id in new[] { "bramble", "pony", "owl", "moth" })
            {
                GoToBattle(run);
                run.FinishBattle(Win(run, -1f, id));
                run.SkipUpgrade();
                if (run.Phase == RunPhase.Upgrade) run.SkipUpgrade();
                if (run.SquadFull) { Assert.That(id, Is.EqualTo("moth")); run.Befriend(replaceIndex: 1); }
                else run.Befriend();
                while (run.Phase != RunPhase.Trail) Resolve(run);
            }

            Assert.That(run.Familiars.Select(f => f.Def.Id), Is.EqualTo(new[] { "pip", "moth", "pony", "owl" }));
            Assert.That(run.Familiars.Count, Is.EqualTo(4));
        }

        [Test]
        public void Rest_repairs_the_Keep_and_the_shop_spends_Run_gold()
        {
            var run = new Run(Make(), 4, "pip");
            GoToBattle(run);
            run.FinishBattle(Win(run, keepHp: 40f));
            while (run.Phase != RunPhase.Trail) Resolve(run);

            bool rested = false, shopped = false;
            for (int guard = 0; guard < 60 && !(rested && shopped) && run.Phase != RunPhase.Ended; guard++)
            {
                if (run.Phase != RunPhase.Trail) { Resolve(run); continue; }
                int rest = IndexOf(run, StopKind.Rest), shop = IndexOf(run, StopKind.Shop);
                if (!rested && rest >= 0)
                {
                    float before = run.KeepHp;
                    run.ChooseStop(rest);
                    Assert.That(run.KeepHp, Is.EqualTo(System.Math.Min(run.KeepMaxHp, before + 30f)));
                    rested = true;
                }
                else if (!shopped && shop >= 0)
                {
                    run.ChooseStop(shop);
                    Assert.That(run.Phase, Is.EqualTo(RunPhase.Shop));
                    int gold = run.Gold;
                    int item = Enumerable.Range(0, run.Shop.Count).FirstOrDefault(run.CanBuy);
                    if (run.CanBuy(item))
                    {
                        int price = run.Shop[item].Price;
                        Assert.That(run.Buy(item), Is.True);
                        Assert.That(run.Gold, Is.EqualTo(gold - price));
                        Assert.That(run.Buy(item), Is.False, "sold out");
                    }
                    run.LeaveShop();
                    shopped = true;
                }
                else run.ChooseStop(0);
            }
            Assert.That(rested && shopped, "found a rest and a shop on the Trail");
        }

        [Test]
        public void An_event_option_that_costs_more_gold_than_the_Run_has_cannot_be_chosen()
        {
            var run = new Run(Make(), 9, "pip");
            for (int guard = 0; guard < 80 && run.Phase != RunPhase.Ended; guard++)
            {
                if (run.Phase == RunPhase.Event && run.CurrentEvent.Id == "well")
                {
                    Assert.That(run.CanChoose(0), Is.EqualTo(run.Gold >= 10));
                    Assert.That(run.CanChoose(1), Is.True);
                    int gold = run.Gold;
                    if (run.CanChoose(0)) { run.ChooseEventOption(0); Assert.That(run.Gold, Is.EqualTo(gold - 10)); }
                    else run.ChooseEventOption(1);
                    Assert.Pass();
                }
                if (run.Phase == RunPhase.Trail) { int e = IndexOf(run, StopKind.Event); run.ChooseStop(e < 0 ? 0 : e); }
                else if (run.Phase == RunPhase.Event) run.ChooseEventOption(0);
                else Resolve(run);
            }
            Assert.Inconclusive("no wishing well came up for this seed");
        }

        [Test]
        public void A_saved_Run_resumes_exactly_where_it_was()
        {
            var content = Make();
            var run = new Run(content, 21, "pip");
            for (int i = 0; i < 9; i++)
            {
                if (run.Phase == RunPhase.Trail) run.ChooseStop(run.NextChoices.Count - 1);
                else if (run.Phase == RunPhase.Upgrade) run.PickUpgrade(run.UpgradeOffer.Count - 1);
                else if (run.Phase == RunPhase.Battle) run.FinishBattle(Win(run, 70f - i, "owl"));
                else if (run.Phase == RunPhase.Befriend) run.Befriend();
                else Resolve(run);
            }

            var resumed = Run.Load(content, run.Save());

            Assert.That(resumed.Save(), Is.EqualTo(run.Save()));
            Assert.That(Autoplay(resumed), Is.EqualTo(Autoplay(run)));
        }

        [Test]
        public void Leaving_mid_Battle_keeps_the_damage_the_Keep_has_taken_when_the_Run_resumes()
        {
            var content = Make();
            var run = new Run(content, 6, "pip");
            GoToBattle(run);

            run.SuspendBattle(35f);
            var resumed = Run.Load(content, run.Save());

            Assert.That(resumed.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(resumed.KeepHp, Is.EqualTo(35f));
            Assert.That(resumed.CreateBattle().KeepHp, Is.EqualTo(35f));
        }

        [Test]
        public void Every_creature_freed_in_one_Battle_is_offered_in_turn()
        {
            var run = AfterFirstBattle(1, "pony", "owl");
            while (run.Phase == RunPhase.Upgrade) run.SkipUpgrade();

            Assert.That(run.CreatureOffer.Id, Is.EqualTo("pony"));
            run.PassCreature();
            Assert.That(run.CreatureOffer.Id, Is.EqualTo("owl"));
        }

        [Test]
        public void A_save_made_with_different_content_is_refused_rather_than_replayed_wrongly()
        {
            var run = new Run(Make(), 3, "pip");
            run.ChooseStop(0);
            var changed = Make();
            changed.Chapters[0].Battles.Add(Encounter("new_battle"));

            Assert.Throws<System.FormatException>(() => Run.Load(changed, run.Save()));
            Assert.DoesNotThrow(() => Run.Load(Make(), run.Save()));
        }

        [Test]
        public void Content_that_could_leave_a_Run_stuck_is_refused_up_front()
        {
            var noBoss = Make();
            noBoss.Chapters[1].Boss = null;
            var paidOnly = Make();
            paidOnly.Events.Add(new EventDef { Id = "toll", Options = { new EventOption { Text = "Pay", GoldCost = 5 } } });

            Assert.Throws<System.ArgumentException>(() => new Run(noBoss, 1, "pip"));
            Assert.Throws<System.ArgumentException>(() => new Run(paidOnly, 1, "pip"));
        }

        static int IndexOf(Run run, StopKind kind) => run.NextChoices.ToList().FindIndex(s => s.Kind == kind);
    }
}
