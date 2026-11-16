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
        /// Win a Run on difficulty tier Target (0-based) or harder.
        WinOnTier,
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
        /// Acorns for each level, in order. Its length is the number of levels.
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
        /// Multiplies the Acorns a Run pays.
        public float Reward = 1f;
    }

    [Serializable]
    public sealed class RewardTuning
    {
        public int PerBattle = 3;
        public int PerChapter = 10;
        public int ForWin = 25;
    }

    /// Everything permanent progress is built from.
    public sealed class ProgressContent
    {
        public List<FamiliarUnlock> Familiars = new List<FamiliarUnlock>();
        public List<DeedDef> Deeds = new List<DeedDef>();
        public List<KeeperUpgradeDef> Upgrades = new List<KeeperUpgradeDef>();
        public List<DifficultyDef> Tiers = new List<DifficultyDef>();
        public RewardTuning Rewards = new RewardTuning();
    }

    /// What one finished Run gave the Keep.
    public sealed class RunRewards
    {
        public int Acorns;
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
        public int Acorns;
        public int RunsPlayed, RunsWon, BattlesWon, Befriended, BestChapters;
        public int HighestTier;
        public List<string> Owned = new List<string>();
        public List<CountEntry> Bonds = new List<CountEntry>();
        public List<CountEntry> Upgrades = new List<CountEntry>();
        public List<string> Deeds = new List<string>();
    }

    /// Permanent progress: Acorns, Keeper upgrades, owned Familiars, Bonds, Deeds and difficulty tiers.
    /// Feed it finished Runs and purchases; ask it what is owned and unlocked.
    public sealed class KeepProgress
    {
        readonly ProgressContent content;
        readonly HashSet<string> owned = new HashSet<string>();
        readonly Dictionary<string, int> bonds = new Dictionary<string, int>();
        readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        readonly HashSet<string> deeds = new HashSet<string>();
        int highestTier;

        public int Acorns { get; private set; }
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
                case DeedKind.WinOnTier: return deeds.Contains(d.Id) ? d.Target : 0;
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
            if (cost < 0 || Acorns < cost) return false;
            Acorns -= cost;
            levels[upgradeId] = UpgradeLevel(upgradeId) + 1;
            return true;
        }

        /// What permanent progress adds to a Run started on this tier.
        public RunBonuses BonusesFor(int tier)
        {
            var b = new RunBonuses { Tier = tier };
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
        public RunRewards RecordRun(RunResult result)
        {
            var rewards = new RunRewards();
            var r = content.Rewards;
            float reward = result.Tier >= 0 && result.Tier < content.Tiers.Count ? content.Tiers[result.Tier].Reward : 1f;
            rewards.Acorns = (int)Math.Round((result.BattlesWon * r.PerBattle + result.ChaptersCleared * r.PerChapter + (result.Won ? r.ForWin : 0)) * reward);
            Acorns += rewards.Acorns;
            RunsPlayed++;
            if (result.Won) RunsWon++;
            BattlesWon += result.BattlesWon;
            BestChapters = Math.Max(BestChapters, result.ChaptersCleared);

            foreach (var id in result.Befriended ?? new List<string>())
            {
                TotalBefriended++;
                var unlock = content.Familiars.Find(f => f.Id == id);
                if (unlock == null || unlock.Kind != UnlockKind.Bond || owned.Contains(id)) continue;
                bonds[id] = BondProgress(id) + 1;
                if (bonds[id] >= unlock.BondSize) Grant(id, rewards);
            }

            foreach (var d in content.Deeds)
            {
                if (deeds.Contains(d.Id)) continue;
                bool done = d.Kind == DeedKind.WinOnTier ? result.Won && result.Tier >= d.Target : DeedProgress(d) >= d.Target;
                if (!done) continue;
                deeds.Add(d.Id);
                rewards.Deeds.Add(d.Id);
                foreach (var f in content.Familiars)
                    if (f.Kind == UnlockKind.Deed && f.DeedId == d.Id) Grant(f.Id, rewards);
            }

            if (result.Won && result.Tier == highestTier && highestTier + 1 < content.Tiers.Count)
            {
                highestTier++;
                rewards.TierUnlocked = highestTier;
            }
            return rewards;
        }

        void Grant(string id, RunRewards rewards)
        {
            if (owned.Add(id)) rewards.NewFamiliars.Add(id);
        }

        // --- Saving ---------------------------------------------------------------------

        public ProgressData ToData() => new ProgressData
        {
            Version = ProgressData.CurrentVersion,
            Acorns = Acorns,
            RunsPlayed = RunsPlayed,
            RunsWon = RunsWon,
            BattlesWon = BattlesWon,
            Befriended = TotalBefriended,
            BestChapters = BestChapters,
            HighestTier = highestTier,
            Owned = owned.ToList(),
            Bonds = bonds.Select(kv => new CountEntry { Id = kv.Key, Count = kv.Value }).ToList(),
            Upgrades = levels.Select(kv => new CountEntry { Id = kv.Key, Count = kv.Value }).ToList(),
            Deeds = deeds.ToList(),
        };

        /// Reads any saved version. Ids for content that no longer exists are dropped; numbers are clamped.
        void Read(ProgressData d)
        {
            // Version 0 (before versioning) had the same fields, possibly missing; every list may be null.
            Acorns = Math.Max(0, d.Acorns);
            RunsPlayed = Math.Max(0, d.RunsPlayed);
            RunsWon = Math.Max(0, d.RunsWon);
            BattlesWon = Math.Max(0, d.BattlesWon);
            TotalBefriended = Math.Max(0, d.Befriended);
            BestChapters = Math.Max(0, d.BestChapters);
            highestTier = Math.Max(0, Math.Min(d.HighestTier, Math.Max(0, content.Tiers.Count - 1)));
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

        internal string Encode() => string.Join(",", new[] { KeepMaxHpBonus, StartGold, KeeperDamage, RallyCooldown, HeroHp, HeroCount, Tier }
            .Select(v => Convert.ToSingle(v).ToString("G9", System.Globalization.CultureInfo.InvariantCulture)));

        internal static RunBonuses Decode(string s)
        {
            var p = s.Split(',').Select(x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return new RunBonuses { KeepMaxHpBonus = p[0], StartGold = (int)p[1], KeeperDamage = p[2], RallyCooldown = p[3], HeroHp = p[4], HeroCount = p[5], Tier = (int)p[6] };
        }
    }
}
