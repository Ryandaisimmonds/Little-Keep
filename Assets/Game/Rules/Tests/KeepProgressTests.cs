using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LittleKeep.Rules.Tests
{
    public class KeepProgressTests
    {
        static ProgressContent Make() => new ProgressContent
        {
            Familiars =
            {
                new FamiliarUnlock { Id = "pip", Kind = UnlockKind.Start },
                new FamiliarUnlock { Id = "bramble", Kind = UnlockKind.Start },
                new FamiliarUnlock { Id = "puff", Kind = UnlockKind.Start },
                new FamiliarUnlock { Id = "biscuit", Kind = UnlockKind.Bond, BondSize = 3 },
                new FamiliarUnlock { Id = "bun", Kind = UnlockKind.Bond, BondSize = 3 },
                new FamiliarUnlock { Id = "tide", Kind = UnlockKind.Deed, DeedId = "clear_meadow" },
            },
            Deeds =
            {
                new DeedDef { Id = "clear_meadow", Name = "Clear Meadow Road", Kind = DeedKind.ClearChapters, Target = 1 },
                new DeedDef { Id = "first_win", Name = "Win a Run", Kind = DeedKind.WinRuns, Target = 1 },
            },
            Upgrades =
            {
                new KeeperUpgradeDef { Id = "walls", Stat = KeeperStat.KeepMaxHp, PerLevel = 10f, Costs = { 20, 40, 80 } },
                new KeeperUpgradeDef { Id = "sharp", Stat = KeeperStat.KeeperDamage, PerLevel = 0.1f, Costs = { 30, 60 } },
                new KeeperUpgradeDef { Id = "purse", Stat = KeeperStat.StartGold, PerLevel = 15f, Costs = { 10 } },
                new KeeperUpgradeDef { Id = "horn", Stat = KeeperStat.RallyCooldown, PerLevel = -0.1f, Costs = { 25 } },
            },
            Tiers =
            {
                new DifficultyDef { Name = "Cozy", HeroHp = 1f, HeroCount = 1f, Reward = 1f },
                new DifficultyDef { Name = "Brisk", HeroHp = 1.3f, HeroCount = 1.15f, Reward = 1.5f },
                new DifficultyDef { Name = "Fierce", HeroHp = 1.6f, HeroCount = 1.3f, Reward = 2f },
            },
            Rewards = new RewardTuning { PerBattle = 3, PerChapter = 10, ForWin = 25 },
        };

        static RunResult Result(bool won = false, int battles = 0, int chapters = 0, int tier = 0, params string[] befriended) =>
            new RunResult { Won = won, BattlesWon = battles, ChaptersCleared = chapters, Tier = tier, Befriended = befriended.ToList() };

        [Test]
        public void A_new_Keep_owns_the_three_starting_Familiars_and_nothing_else()
        {
            var keep = new KeepProgress(Make());

            Assert.That(keep.OwnedFamiliars, Is.EquivalentTo(new[] { "pip", "bramble", "puff" }));
            Assert.That(keep.Gems, Is.EqualTo(0));
            Assert.That(keep.IsTierUnlocked(0) && !keep.IsTierUnlocked(1), Is.True);
        }

        [Test]
        public void Every_finished_Run_pays_Gems_win_or_lose_with_no_cap()
        {
            var keep = new KeepProgress(Make());

            var lost = keep.RecordRun(Result(battles: 4));
            var won = keep.RecordRun(Result(won: true, battles: 15, chapters: 3));
            for (int i = 0; i < 100; i++) keep.RecordRun(Result(won: true, battles: 15, chapters: 3));

            Assert.That(lost.Gems, Is.EqualTo(12), "4 Battles x 3");
            Assert.That(won.Gems, Is.EqualTo(45 + 30 + 25));
            Assert.That(keep.Gems, Is.EqualTo(12 + 101 * 100));
        }

        [Test]
        public void Harder_tiers_pay_more()
        {
            var keep = new KeepProgress(Make());
            keep.RecordRun(Result(won: true, battles: 1));
            keep.RecordRun(Result(won: true, battles: 1, tier: 1));

            var brisk = keep.RecordRun(Result(battles: 10, tier: 1));

            Assert.That(brisk.Gems, Is.EqualTo(45), "30 x 1.5");
        }

        [Test]
        public void Keeper_upgrades_are_bought_level_by_level_and_shape_the_next_Run()
        {
            var keep = new KeepProgress(Make());
            Assert.That(keep.TryBuy("walls"), Is.False, "no Gems yet");

            keep.RecordRun(Result(battles: 100));
            Assert.That(keep.TryBuy("walls"), Is.True);
            Assert.That(keep.Gems, Is.EqualTo(280));
            Assert.That(keep.NextCost("walls"), Is.EqualTo(40));
            keep.TryBuy("walls");
            keep.TryBuy("walls");
            keep.TryBuy("sharp");
            keep.TryBuy("purse");
            keep.TryBuy("horn");

            Assert.That(keep.TryBuy("walls"), Is.False, "fully upgraded");
            Assert.That(keep.UpgradeLevel("walls"), Is.EqualTo(3));
            var bonuses = keep.BonusesFor(0);
            Assert.That(bonuses.KeepMaxHpBonus, Is.EqualTo(30f));
            Assert.That(bonuses.KeeperDamage, Is.EqualTo(1.1f).Within(1e-4));
            Assert.That(bonuses.StartGold, Is.EqualTo(15));
            Assert.That(bonuses.RallyCooldown, Is.EqualTo(0.9f).Within(1e-4));
        }

        [Test]
        public void A_Bond_fills_on_each_Befriending_and_grants_the_Familiar_when_full()
        {
            var keep = new KeepProgress(Make());

            var first = keep.RecordRun(Result(befriended: "biscuit"));
            var second = keep.RecordRun(Result(befriended: new[] { "biscuit", "pip" }));
            Assert.That(keep.Owns("biscuit"), Is.False);
            Assert.That(keep.BondProgress("biscuit"), Is.EqualTo(2));
            var third = keep.RecordRun(Result(befriended: "biscuit"));
            var fourth = keep.RecordRun(Result(befriended: "biscuit"));

            Assert.That(first.NewFamiliars.Concat(second.NewFamiliars), Is.Empty);
            Assert.That(third.NewFamiliars, Is.EqualTo(new[] { "biscuit" }));
            Assert.That(fourth.NewFamiliars, Is.Empty, "granted once");
            Assert.That(keep.Owns("biscuit"), Is.True);
        }

        [Test]
        public void Completing_a_Deed_grants_its_Familiar_at_once_even_on_a_lost_Run()
        {
            var keep = new KeepProgress(Make());

            var rewards = keep.RecordRun(Result(won: false, battles: 6, chapters: 1));

            Assert.That(rewards.Deeds, Is.EqualTo(new[] { "clear_meadow" }));
            Assert.That(rewards.NewFamiliars, Is.EqualTo(new[] { "tide" }));
            Assert.That(keep.Owns("tide") && keep.IsDeedDone("clear_meadow") && !keep.IsDeedDone("first_win"), Is.True);
        }

        [Test]
        public void Each_difficulty_tier_unlocks_by_winning_the_one_below()
        {
            var keep = new KeepProgress(Make());
            keep.RecordRun(Result(won: false, tier: 0));
            Assert.That(keep.IsTierUnlocked(1), Is.False, "losing unlocks nothing");

            var first = keep.RecordRun(Result(won: true, tier: 0));
            keep.RecordRun(Result(won: true, tier: 0));
            Assert.That(keep.IsTierUnlocked(1) && !keep.IsTierUnlocked(2), Is.True);
            var second = keep.RecordRun(Result(won: true, tier: 1));

            Assert.That(first.TierUnlocked, Is.EqualTo(1));
            Assert.That(second.TierUnlocked, Is.EqualTo(2));
            Assert.That(keep.IsTierUnlocked(2), Is.True);
            Assert.That(keep.BonusesFor(2).HeroHp, Is.EqualTo(1.6f));
        }

        [Test]
        public void Rooms_of_the_Keep_appear_as_progress_unlocks_them()
        {
            var keep = new KeepProgress(Make());
            Assert.That(keep.HasWorkshop || keep.HasWatchtower, Is.False);

            keep.RecordRun(Result(battles: 2));
            Assert.That(keep.HasWorkshop, Is.True, "after the first Run");
            keep.RecordRun(Result(won: true));
            Assert.That(keep.HasWatchtower, Is.True, "after the first win");
        }

        [Test]
        public void Progress_survives_saving_and_loading()
        {
            var content = Make();
            var keep = new KeepProgress(content);
            keep.RecordRun(Result(won: true, battles: 20, chapters: 3, befriended: new[] { "bun", "bun" }));
            keep.TryBuy("walls");

            var loaded = new KeepProgress(content, keep.ToData());

            Assert.That(loaded.Gems, Is.EqualTo(keep.Gems));
            Assert.That(loaded.OwnedFamiliars, Is.EquivalentTo(keep.OwnedFamiliars));
            Assert.That(loaded.BondProgress("bun"), Is.EqualTo(2));
            Assert.That(loaded.UpgradeLevel("walls"), Is.EqualTo(1));
            Assert.That(loaded.IsTierUnlocked(1) && loaded.IsDeedDone("first_win"), Is.True);
            Assert.That(loaded.RunsPlayed, Is.EqualTo(1));
        }

        [Test]
        public void A_save_from_an_older_version_loads_and_ignores_content_that_no_longer_exists()
        {
            var old = new ProgressData { Version = 0, Gems = 77, Owned = new List<string> { "removed_familiar" }, Bonds = null, Upgrades = null, Deeds = null };

            var keep = new KeepProgress(Make(), old);

            Assert.That(keep.Gems, Is.EqualTo(77));
            Assert.That(keep.OwnedFamiliars, Is.EquivalentTo(new[] { "pip", "bramble", "puff" }));
            Assert.That(keep.ToData().Version, Is.EqualTo(ProgressData.CurrentVersion));
        }

        [Test]
        public void A_Run_lost_at_once_still_pays_the_minimum()
        {
            var content = Make();
            content.Rewards.Minimum = 5;
            var keep = new KeepProgress(content);

            Assert.That(keep.RecordRun(Result()).Gems, Is.EqualTo(5));
        }

        [Test]
        public void The_same_Run_is_never_paid_twice()
        {
            var keep = new KeepProgress(Make());
            var bonuses = keep.BonusesFor(0);
            var result = Result(battles: 4);
            result.RunNumber = bonuses.RunNumber;

            var first = keep.RecordRun(result);
            var reloaded = new KeepProgress(Make(), keep.ToData());
            var again = reloaded.RecordRun(result);

            Assert.That(first.Gems, Is.GreaterThan(0));
            Assert.That(again.Gems, Is.EqualTo(0));
            Assert.That(reloaded.Gems, Is.EqualTo(first.Gems));
            Assert.That(reloaded.RunsPlayed, Is.EqualTo(1));
            Assert.That(reloaded.BonusesFor(0).RunNumber, Is.GreaterThan(bonuses.RunNumber));
        }

        [Test]
        public void Clearing_a_Chapter_mid_Run_completes_its_Deed_at_once()
        {
            var keep = new KeepProgress(Make());

            var now = keep.NoteChaptersCleared(1);
            var atEnd = keep.RecordRun(Result(chapters: 1));

            Assert.That(now.NewFamiliars, Is.EquivalentTo(new[] { "tide" }));
            Assert.That(now.Deeds, Is.EquivalentTo(new[] { "clear_meadow" }));
            Assert.That(keep.Owns("tide"), Is.True);
            Assert.That(atEnd.NewFamiliars, Is.Empty);
        }

        [Test]
        public void The_chosen_tier_is_kept_with_progress_and_must_be_unlocked()
        {
            var keep = new KeepProgress(Make());

            Assert.That(keep.ChooseTier(1), Is.False);
            keep.RecordRun(Result(won: true, chapters: 3));
            Assert.That(keep.ChooseTier(1), Is.True);
            var reloaded = new KeepProgress(Make(), keep.ToData());

            Assert.That(reloaded.ChosenTier, Is.EqualTo(1));
        }

        [Test]
        public void Only_Bond_Familiars_turn_up_in_the_wild()
        {
            Assert.That(Make().WildCreatures, Is.EquivalentTo(new[] { "biscuit", "bun" }));
        }

        [Test]
        public void A_save_from_a_newer_version_is_flagged_so_it_is_not_overwritten()
        {
            var data = new KeepProgress(Make()).ToData();
            data.Version = ProgressData.CurrentVersion + 1;

            Assert.That(new KeepProgress(Make(), data).FromNewerVersion, Is.True);
            Assert.That(new KeepProgress(Make(), new KeepProgress(Make()).ToData()).FromNewerVersion, Is.False);
        }
    }
}
