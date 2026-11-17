using System.Linq;
using System.Text;
using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Plays whole Runs through the rules with a simple policy (never aims, Rallies when ready, takes the first
    /// upgrade, befriends when there's room, rests when hurt) and reports how they went. A balance tool, not a test.
    public static class RunBot
    {
        [MenuItem("Little Keep/Run Bot: play 10 Runs")]
        static void Menu() => Debug.Log(Play(10));

        [MenuItem("Little Keep/Run Bot: play 10 Runs on every tier")]
        static void MenuTiers()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Game/Content/GameContent.asset");
            for (int t = 0; t < content.tiers.Count; t++) Debug.Log($"Tier {t}:\n" + Play(10, tier: t));
        }

        /// Rough time a Player spends on each kind of choice, used to estimate a whole Run's length.
        public const float TrailSeconds = 6f, PickSeconds = 10f, ShopSeconds = 15f, EventSeconds = 12f;

        public sealed class Stats
        {
            public float Seconds, Lowest = float.MaxValue, ChoiceSeconds;
            public int Battles, Knockouts, Learned;
            public string LowestAt = "";
            /// Battle time plus the estimated time spent choosing, in minutes.
            public float RunMinutes => (Seconds + ChoiceSeconds) / 60f;
        }

        /// One tier's results, for the balance report.
        public sealed class TierReport
        {
            public string Tier;
            public int UpgradeLevel, Runs, Wins, ChaptersReached, Learned, FullSquads;
            public float RunMinutes, BattleMinutes, WonRunMinutes;
        }

        /// Plays the Run with the bot's policy, to its end or until it reaches the Trail of the given Chapter (0-based).
        public static Stats PlayOut(Run run, int stopAtChapter = int.MaxValue)
        {
            var st = new Stats();
            for (int guard = 0; guard < 500 && run.Phase != RunPhase.Ended && !(run.Phase == RunPhase.Trail && run.Chapter >= stopAtChapter); guard++)
            {
                switch (run.Phase)
                {
                    case RunPhase.Trail: st.ChoiceSeconds += TrailSeconds;
                        int pick = 0;
                        for (int s = 0; s < run.NextChoices.Count; s++)
                            if (run.KeepHp < run.KeepMaxHp * 0.6f && run.NextChoices[s].Kind == StopKind.Rest) pick = s;
                        run.ChooseStop(pick);
                        break;
                    case RunPhase.Battle:
                        var battle = new Battle(run.CreateBattle());
                        while (battle.Outcome == BattleOutcome.Ongoing && battle.Time < 900f)
                        {
                            battle.Advance(0.1f);
                            if (battle.RallyCharge >= 1f) battle.TryRally();
                            st.Knockouts += battle.TakeEvents().Count(e => e.Kind == BattleEventKind.KnockedOut);
                        }
                        st.Seconds += battle.Time;
                        st.Battles++;
                        if (battle.KeepHp < st.Lowest) { st.Lowest = battle.KeepHp; st.LowestAt = $"{run.CurrentStop.Kind} ch{run.Chapter + 1}"; }
                        run.FinishBattle(BattleReport.From(battle));
                        break;
                    case RunPhase.Upgrade: st.ChoiceSeconds += PickSeconds; run.PickUpgrade(0); break;
                    case RunPhase.Befriend: st.ChoiceSeconds += PickSeconds; if (run.SquadFull) run.PassCreature(); else run.Befriend(); break;
                    case RunPhase.Shop: st.ChoiceSeconds += ShopSeconds;
                        for (int s = 0; s < run.Shop.Count; s++) if (run.CanBuy(s)) run.Buy(s);
                        run.LeaveShop();
                        break;
                    case RunPhase.Event: st.ChoiceSeconds += EventSeconds;
                        int o = Enumerable.Range(0, run.CurrentEvent.Options.Count).FirstOrDefault(run.CanChoose);
                        run.ChooseEventOption(o);
                        break;
                }
            }
            // Skills can be learned from upgrades, the shop and events alike: count what the squad knows at the end.
            st.Learned = run.Familiars.Sum(f => f.Skills.Count(x => x.IsLearned) - 1);
            return st;
        }

        /// Plays whole Runs on a Difficulty tier, with every Keeper upgrade at <paramref name="upgradeLevel"/> (capped at its max).
        public static string Play(int runs, int firstSeed = 1, int tier = 0, int upgradeLevel = 0) => PlayTier(runs, firstSeed, tier, upgradeLevel, out _);

        public static string PlayTier(int runs, int firstSeed, int tier, int upgradeLevel, out TierReport report)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Game/Content/GameContent.asset");
            var compiled = new CompiledContent(content);
            var data = new ProgressData { HighestTier = tier };
            foreach (var u in compiled.Progress.Upgrades) data.Upgrades.Add(new CountEntry { Id = u.Id, Count = upgradeLevel });
            var keep = new KeepProgress(compiled.Progress, data);
            var starters = content.familiars.Where(f => f != null && f.unlock == UnlockKind.Start).Select(f => f.def.Id).ToList();
            var sb = new StringBuilder();
            report = new TierReport { Tier = tier < content.tiers.Count ? content.tiers[tier].Name : tier.ToString(), UpgradeLevel = upgradeLevel, Runs = runs };
            for (int i = 0; i < runs; i++)
            {
                var run = new Run(compiled.Run, (firstSeed + i) * 7919, starters[i % starters.Count], keep.BonusesFor(tier));
                var st = PlayOut(run);
                var r = run.Result;
                if (r != null && r.Won) { report.Wins++; report.WonRunMinutes += st.RunMinutes; }
                report.ChaptersReached += r?.ChapterReached ?? 0;
                report.RunMinutes += st.RunMinutes;
                report.BattleMinutes += st.Seconds / 60f;
                report.Learned += st.Learned;
                if (run.Familiars.Count >= compiled.Run.Tuning.MaxFamiliars) report.FullSquads++;
                sb.AppendLine($"Run {i + 1} ({starters[i % starters.Count]}): {(r != null && r.Won ? "WON " : "lost")} ch{r?.ChapterReached} battles={st.Battles} " +
                              $"run={st.RunMinutes:0.0}min battles={st.Seconds / 60f:0.0}min squad={run.Familiars.Count + 1} learned={st.Learned} KOs={st.Knockouts} lowest Keep={st.Lowest:0} ({st.LowestAt})");
            }
            sb.Insert(0, $"Run bot ({report.Tier}, upgrades {upgradeLevel}): {report.Wins}/{runs} won, average Run {report.RunMinutes / runs:0.0} min " +
                         $"(Battles {report.BattleMinutes / runs:0.0} min)\n");
            return sb.ToString();
        }

        const string StartMarker = "<!-- bot results: written by Little Keep/Run Bot: write balance report -->";
        const string EndMarker = "<!-- end of bot results -->";

        /// The settings the balance report runs: each tier at the Keeper upgrade level a Player would likely have by then.
        public static readonly (int tier, int upgrades)[] ReportSettings = { (0, 0), (0, 2), (1, 2), (1, 4), (2, 4), (2, 5) };

        [MenuItem("Little Keep/Run Bot: write balance report")]
        public static string WriteReport() => WriteReport(16);

        /// Plays every report setting and writes docs/balance.md (the latest results; the tuning history lives below them).
        public static string WriteReport(int runsEach)
        {
            var reports = ReportSettings.Select(s => { PlayTier(runsEach, 1, s.tier, s.upgrades, out var rep); return rep; }).ToList();
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Game/Content/GameContent.asset");
            var t = content.runTuning;
            var sb = new StringBuilder();
            sb.AppendLine(StartMarker);
            sb.AppendLine("## Latest bot results");
            sb.AppendLine();
            sb.AppendLine($"{runsEach} Runs per row. The bot never aims, Rallies when ready, takes the first offer, befriends when there is room and rests when hurt.");
            sb.AppendLine($"Run length = Battle time at 1x speed plus about {TrailSeconds:0}s per Trail choice, {PickSeconds:0}s per upgrade or Befriending, {EventSeconds:0}s per event and {ShopSeconds:0}s per shop.");
            sb.AppendLine();
            sb.AppendLine("| Tier | Keeper upgrades | Won | Run length | Won Run length | Battle time | Avg Chapter reached | Skills learned per Run | Full squads |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var r in reports)
                sb.AppendLine($"| {r.Tier} | {r.UpgradeLevel} | {r.Wins}/{r.Runs} | {r.RunMinutes / r.Runs:0.0} min | {(r.Wins > 0 ? $"{r.WonRunMinutes / r.Wins:0.0} min" : "-")} | {r.BattleMinutes / r.Runs:0.0} min | {(float)r.ChaptersReached / r.Runs:0.0} | {(float)r.Learned / r.Runs:0.0} | {r.FullSquads}/{r.Runs} |");
            sb.AppendLine();
            sb.AppendLine($"Tuning: Hero health x{t.HeroHpScale}, Hero numbers x{t.HeroCountScale}, +{t.HeroHpPerChapter * 100f:0}% health per Chapter, +{t.HeroHpPerStep * 100f:0}% per step, {t.StepsPerChapter} steps per Chapter.");
            sb.AppendLine(EndMarker);
            string table = sb.ToString();

            // Only the text between the markers is replaced; everything else in the file is kept as written.
            string path = System.IO.Path.Combine(Application.dataPath, "../docs/balance.md");
            string doc = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "# Balance\n\n" + StartMarker + "\n" + EndMarker + "\n\n## Tuning log\n\nNewest first. What changed, and why.\n";
            int start = doc.IndexOf(StartMarker, System.StringComparison.Ordinal);
            int end = doc.IndexOf(EndMarker, System.StringComparison.Ordinal);
            if (start < 0 || end < start)
            {
                Debug.LogError("docs/balance.md has lost its bot-results markers; not overwriting it. Results:\n" + table);
                return table;
            }
            System.IO.File.WriteAllText(path, doc.Substring(0, start) + table + doc.Substring(end + EndMarker.Length + 1));
            Debug.Log(table);
            return table;
        }
    }
}
