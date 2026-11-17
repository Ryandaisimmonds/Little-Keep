using System;
using System.Collections.Generic;
using System.Linq;

namespace LittleKeep.Rules
{
    public enum UnlockKind
    {
        /// Owned from the very first Run.
        Start,
        /// Earned by Befriending this creature enough times across Runs.
        Bond,
        /// Earned by completing a Deed.
        Deed,
    }

    /// How a Familiar becomes the Player's to start Runs with.
    [Serializable]
    public sealed class FamiliarUnlock
    {
        public string Id = "familiar";
        public UnlockKind Kind;
        /// Befriendings needed (Bond only).
        public int BondSize = 3;
        /// The Deed that grants it (Deed only).
        public string DeedId = "";
    }

    public enum DeedKind
    {
        /// Clear at least Target Chapters in one Run.
        ClearChapters,
        /// Win Target Runs in total.
        WinRuns,
        /// Win Target Battles in total.
        WinBattles,
        /// Befriend Target creatures in total.
        Befriend,
    }

    /// A named achievement. Completing it can grant a Familiar.
    [Serializable]
    public sealed class DeedDef
    {
        public string Id = "deed";
        public string Name = "Deed";
        public string Description = "";
        public DeedKind Kind;
        public int Target = 1;
    }

    public enum KeeperStat
    {
        /// Adds PerLevel to the Keep's maximum health.
        KeepMaxHp,
        /// Adds PerLevel (0.1 = +10%) to the Keeper's damage.
        KeeperDamage,
        /// Adds PerLevel gold at the start of each Run.
        StartGold,
        /// Adds PerLevel (-0.1 = 10% faster) to Rally's recharge time.
        RallyCooldown,
    }

    /// One track on the Keeper's permanent upgrade path.
    [Serializable]
    public sealed class KeeperUpgradeDef
    {
        public string Id = "upgrade";
        public string Name = "Upgrade";
        public string Description = "";
        public KeeperStat Stat;
        public float PerLevel = 1f;
        /// Gems for each level, in order. Its length is the number of levels.
        public List<int> Costs = new List<int>();
    }

    /// A difficulty tier: tougher Heroes, bigger rewards.
    [Serializable]
    public sealed class DifficultyDef
    {
        public string Name = "Tier";
        public string Description = "";
        public float HeroHp = 1f;
        public float HeroCount = 1f;
        /// Multiplies the Gems a Run pays.
        public float Reward = 1f;
    }

    [Serializable]
    public sealed class RewardTuning
    {
        public int PerBattle = 3;
        public int PerChapter = 10;
        public int ForWin = 25;
        /// Paid even for a Run lost in its first Battle.
        public int Minimum = 5;
    }

    /// Everything permanent progress is built from.
    public sealed class ProgressContent
    {
        public List<FamiliarUnlock> Familiars = new List<FamiliarUnlock>();
        public List<DeedDef> Deeds = new List<DeedDef>();

        /// Familiars that can turn up on the Trail to be befriended: all but those a Deed must earn first.
        public List<string> WildCreatures => Familiars.Where(f => f.Kind != UnlockKind.Deed).Select(f => f.Id).ToList();
        public List<KeeperUpgradeDef> Upgrades = new List<KeeperUpgradeDef>();
        public List<DifficultyDef> Tiers = new List<DifficultyDef>();
        public RewardTuning Rewards = new RewardTuning();
    }

    /// What one finished Run gave the Keep.
    public sealed class RunRewards
    {
        public int Gems;
        public List<string> NewFamiliars = new List<string>();
        public List<string> Deeds = new List<string>();
        /// The tier this Run unlocked, or -1.
        public int TierUnlocked = -1;
    }

    // --- Saved form ------------------------------------------------------------------------

    [Serializable] public sealed class CountEntry { public string Id; public int Count; }

    /// Permanent progress as saved on the device. Plain fields only, so any JSON serializer can write it.
    /// Bump CurrentVersion when the shape changes, and teach KeepProgress to read the older shape.
    [Serializable]
    public sealed class ProgressData
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public int Gems;
        public int RunsPlayed, RunsWon, BattlesWon, Befriended, BestChapters;
        public int HighestTier;
        public int ChosenTier;
        /// The number of the last Run paid for, so a Run replayed after a crash is never paid twice.
        public int LastRunNumber;
        /// How many of the Run in progress's Befriendings have already filled Bonds.
        public int NotedRun, NotedBefriended;
        public List<string> Owned = new List<string>();
        public List<CountEntry> Bonds = new List<CountEntry>();
        public List<CountEntry> Upgrades = new List<CountEntry>();
        public List<string> Deeds = new List<string>();
    }

    /// Permanent progress: Gems, Keeper upgrades, owned Familiars, Bonds, Deeds and difficulty tiers.
    /// Feed it finished Runs and purchases; ask it what is owned and unlocked.
    public sealed class KeepProgress
    {
        readonly ProgressContent content;
        readonly HashSet<string> owned = new HashSet<string>();
        readonly Dictionary<string, int> bonds = new Dictionary<string, int>();
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        readonly HashSet<string> deeds = new HashSet<string>();
        int highestTier, chosenTier, lastRunNumber, notedRun, notedBefriended;

        public int Gems { get; private set; }
        public int RunsPlayed { get; private set; }
        public int RunsWon { get; private set; }
        public int BattlesWon { get; private set; }
        public int TotalBefriended { get; private set; }
        public int BestChapters { get; private set; }
        public ProgressContent Content => content;

        public KeepProgress(ProgressContent content, ProgressData data = null)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            foreach (var f in content.Familiars)
                if (f.Kind == UnlockKind.Start) owned.Add(f.Id);
            if (data != null) Read(data);
        }

        /// Owned Familiar ids, in content order.
        public IReadOnlyList<string> OwnedFamiliars => content.Familiars.Where(f => owned.Contains(f.Id)).Select(f => f.Id).ToList();
        public bool Owns(string familiarId) => owned.Contains(familiarId);
        public int BondProgress(string familiarId) => bonds.TryGetValue(familiarId, out var n) ? n : 0;
        public bool IsDeedDone(string deedId) => deeds.Contains(deedId);
        public int UpgradeLevel(string upgradeId) => levels.TryGetValue(upgradeId, out var n) ? n : 0;
        public bool IsTierUnlocked(int tier) => tier >= 0 && tier <= highestTier && tier < Math.Max(1, content.Tiers.Count);
        public int HighestTierUnlocked => highestTier;

        /// The Difficulty tier the next Run starts on; falls back to the hardest open one.
        public int ChosenTier => IsTierUnlocked(chosenTier) ? chosenTier : highestTier;

        public bool ChooseTier(int tier)
        {
            if (!IsTierUnlocked(tier)) return false;
            chosenTier = tier;
            return true;
        }

        /// Read from a file written by a newer version of the game: it must not be saved over.
        public bool FromNewerVersion { get; private set; }

        /// The Keeper's workshop opens after the first Run, the watchtower (difficulty) after the first win.
        public bool HasWorkshop => RunsPlayed > 0;
        public bool HasWatchtower => RunsWon > 0;

        public int DeedProgress(DeedDef d)
        {
            switch (d.Kind)
            {
                case DeedKind.ClearChapters: return BestChapters;
                case DeedKind.WinRuns: return RunsWon;
                case DeedKind.WinBattles: return BattlesWon;
                case DeedKind.Befriend: return TotalBefriended;
            }
            return 0;
        }

        /// Cost of the next level, or -1 when the track is complete or unknown.
        public int NextCost(string upgradeId)
        {
            var u = content.Upgrades.Find(x => x.Id == upgradeId);
            if (u == null) return -1;
            int level = UpgradeLevel(upgradeId);
            return level < u.Costs.Count ? u.Costs[level] : -1;
        }

        public bool TryBuy(string upgradeId)
        {
            int cost = NextCost(upgradeId);
            if (cost < 0 || Gems < cost) return false;
            Gems -= cost;
            levels[upgradeId] = UpgradeLevel(upgradeId) + 1;
            return true;
        }

        /// What permanent progress adds to a Run started on this tier.
        public RunBonuses BonusesFor(int tier)
        {
            var b = new RunBonuses { Tier = tier, RunNumber = lastRunNumber + 1 };
            foreach (var u in content.Upgrades)
            {
                float total = u.PerLevel * UpgradeLevel(u.Id);
                switch (u.Stat)
                {
                    case KeeperStat.KeepMaxHp: b.KeepMaxHpBonus += total; break;
                    case KeeperStat.KeeperDamage: b.KeeperDamage += total; break;
                    case KeeperStat.StartGold: b.StartGold += (int)Math.Round(total); break;
                    case KeeperStat.RallyCooldown: b.RallyCooldown = Math.Max(0.3f, b.RallyCooldown + total); break;
                }
            }
            if (tier >= 0 && tier < content.Tiers.Count)
            {
                b.HeroHp = content.Tiers[tier].HeroHp;
                b.HeroCount = content.Tiers[tier].HeroCount;
            }
            return b;
        }

        /// Pays for a finished Run and applies its Bonds, Deeds and tier unlocks.
        /// A Run already paid for (same Run number) pays nothing.
        public RunRewards RecordRun(RunResult result)
        {
            var rewards = new RunRewards();
            if (result.RunNumber > 0 && result.RunNumber <= lastRunNumber) return rewards;
            lastRunNumber = Math.Max(lastRunNumber, result.RunNumber);

            int alreadyNoted = result.RunNumber > 0 && result.RunNumber == notedRun ? notedBefriended : 0;
            var r = content.Rewards;
            float reward = result.Tier >= 0 && result.Tier < content.Tiers.Count ? content.Tiers[result.Tier].Reward : 1f;
            int earned = (int)Math.Round((result.BattlesWon * r.PerBattle + result.ChaptersCleared * r.PerChapter + (result.Won ? r.ForWin : 0)) * reward);
            rewards.Gems = Math.Max(r.Minimum, earned);
            Gems += rewards.Gems;
            RunsPlayed++;
            if (result.Won) RunsWon++;
            BattlesWon += result.BattlesWon;
            BestChapters = Math.Max(BestChapters, result.ChaptersCleared);

            var befriended = result.Befriended ?? new List<string>();
            for (int i = alreadyNoted; i < befriended.Count; i++) Befriend(befriended[i], rewards);
            notedRun = notedBefriended = 0;

            CompleteDeeds(rewards);

            if (result.Won && result.Tier == highestTier && highestTier + 1 < content.Tiers.Count)
            {
                highestTier++;
                rewards.TierUnlocked = highestTier;
            }
            return rewards;
        }

        /// Called as a Run goes, with everything it has befriended so far: fills Bonds for the new Befriendings
        /// (granting a Familiar the moment its Bond is full). Safe to call again with the same list.
        public RunRewards NoteBefriended(int runNumber, IReadOnlyList<string> befriendedSoFar)
        {
            var rewards = new RunRewards();
            if (runNumber != notedRun)
            {
                notedRun = runNumber;
                notedBefriended = 0;
            }
            for (int i = notedBefriended; i < befriendedSoFar.Count; i++) Befriend(befriendedSoFar[i], rewards);
            notedBefriended = Math.Max(notedBefriended, befriendedSoFar.Count);
            CompleteDeeds(rewards);
            return rewards;
        }

        void Befriend(string id, RunRewards rewards)
        {
            TotalBefriended++;
            var unlock = content.Familiars.Find(f => f.Id == id);
            if (unlock == null || unlock.Kind != UnlockKind.Bond || owned.Contains(id)) return;
            bonds[id] = BondProgress(id) + 1;
            if (bonds[id] >= unlock.BondSize) Grant(id, rewards);
        }

        /// Called as a Run goes: completes any Deed the Chapters cleared so far satisfy, granting its Familiar now.
        public RunRewards NoteChaptersCleared(int chaptersCleared)
        {
            var rewards = new RunRewards();
            BestChapters = Math.Max(BestChapters, chaptersCleared);
            CompleteDeeds(rewards);
            return rewards;
        }

        void CompleteDeeds(RunRewards rewards)
        {
            foreach (var d in content.Deeds)
            {
                if (deeds.Contains(d.Id) || DeedProgress(d) < d.Target) continue;
                deeds.Add(d.Id);
                rewards.Deeds.Add(d.Id);
                foreach (var f in content.Familiars)
                    if (f.Kind == UnlockKind.Deed && f.DeedId == d.Id) Grant(f.Id, rewards);
            }
        }

        void Grant(string id, RunRewards rewards)
        {
            if (owned.Add(id)) rewards.NewFamiliars.Add(id);
        }

        // --- Saving ---------------------------------------------------------------------

        public ProgressData ToData() => new ProgressData
        {
            Version = ProgressData.CurrentVersion,
            Gems = Gems,
            RunsPlayed = RunsPlayed,
            RunsWon = RunsWon,
            BattlesWon = BattlesWon,
            Befriended = TotalBefriended,
            BestChapters = BestChapters,
            HighestTier = highestTier,
            ChosenTier = chosenTier,
            LastRunNumber = lastRunNumber,
            NotedRun = notedRun,
            NotedBefriended = notedBefriended,
            Owned = owned.ToList(),
            Bonds = bonds.Select(kv => new CountEntry { Id = kv.Key, Count = kv.Value }).ToList(),
            Upgrades = levels.Select(kv => new CountEntry { Id = kv.Key, Count = kv.Value }).ToList(),
            Deeds = deeds.ToList(),
        };

        /// Reads any saved version. Ids for content that no longer exists are dropped; numbers are clamped.
        void Read(ProgressData d)
        {
            // Version 0 (before versioning) had the same fields, possibly missing; every list may be null.
            Gems = Math.Max(0, d.Gems);
            RunsPlayed = Math.Max(0, d.RunsPlayed);
            RunsWon = Math.Max(0, d.RunsWon);
            BattlesWon = Math.Max(0, d.BattlesWon);
            TotalBefriended = Math.Max(0, d.Befriended);
            BestChapters = Math.Max(0, d.BestChapters);
            FromNewerVersion = d.Version > ProgressData.CurrentVersion;
            highestTier = Math.Max(0, Math.Min(d.HighestTier, Math.Max(0, content.Tiers.Count - 1)));
            chosenTier = Math.Max(0, d.ChosenTier);
            lastRunNumber = Math.Max(0, d.LastRunNumber);
            notedRun = Math.Max(0, d.NotedRun);
            notedBefriended = Math.Max(0, d.NotedBefriended);
            foreach (var id in d.Owned ?? new List<string>())
                if (content.Familiars.Exists(f => f.Id == id)) owned.Add(id);
            foreach (var e in d.Bonds ?? new List<CountEntry>())
                if (e != null && content.Familiars.Exists(f => f.Id == e.Id && f.Kind == UnlockKind.Bond)) bonds[e.Id] = Math.Max(0, e.Count);
            foreach (var e in d.Upgrades ?? new List<CountEntry>())
            {
                var u = e == null ? null : content.Upgrades.Find(x => x.Id == e.Id);
                if (u != null) levels[e.Id] = Math.Max(0, Math.Min(e.Count, u.Costs.Count));
            }
            foreach (var id in d.Deeds ?? new List<string>())
                if (content.Deeds.Exists(x => x.Id == id)) deeds.Add(id);
        }
    }

    /// What permanent progress and the chosen difficulty add to one Run. Saved with the Run.
    public sealed class RunBonuses
    {
        public float KeepMaxHpBonus;
        public int StartGold;
        /// Multiplies the Keeper's damage.
        public float KeeperDamage = 1f;
        /// Multiplies Rally's recharge time.
        public float RallyCooldown = 1f;
        /// Multiplies Hero health (difficulty).
        public float HeroHp = 1f;
        /// Multiplies Hero numbers (difficulty).
        public float HeroCount = 1f;
        public int Tier;
        /// Which Run this is, counted by the Keep; lets a Run be paid for only once.
        public int RunNumber;

        internal string Encode() => string.Join(",", new[] { KeepMaxHpBonus, StartGold, KeeperDamage, RallyCooldown, HeroHp, HeroCount, Tier, RunNumber }
            .Select(v => Convert.ToSingle(v).ToString("G9", System.Globalization.CultureInfo.InvariantCulture)));

        internal static RunBonuses Decode(string s)
        {
            var p = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new RunBonuses { KeepMaxHpBonus = p[0], StartGold = (int)p[1], KeeperDamage = p[2], RallyCooldown = p[3], HeroHp = p[4], HeroCount = p[5], Tier = (int)p[6], RunNumber = p.Length > 7 ? (int)p[7] : 0 };
        }
    }
}
