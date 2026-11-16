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

        public sealed class Stats
        {
            public float Seconds, Lowest = float.MaxValue;
            public int Battles, Knockouts;
            public string LowestAt = "";
        }

        /// Plays the Run with the bot's policy, to its end or until it reaches the Trail of the given Chapter (0-based).
        public static Stats PlayOut(Run run, int stopAtChapter = int.MaxValue)
        {
            var st = new Stats();
            for (int guard = 0; guard < 500 && run.Phase != RunPhase.Ended && !(run.Phase == RunPhase.Trail && run.Chapter >= stopAtChapter); guard++)
            {
                switch (run.Phase)
                {
                    case RunPhase.Trail:
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
                    case RunPhase.Upgrade: run.PickUpgrade(0); break;
                    case RunPhase.Befriend: if (run.SquadFull) run.PassCreature(); else run.Befriend(); break;
                    case RunPhase.Shop:
                        for (int s = 0; s < run.Shop.Count; s++) if (run.CanBuy(s)) run.Buy(s);
                        run.LeaveShop();
                        break;
                    case RunPhase.Event:
                        int o = Enumerable.Range(0, run.CurrentEvent.Options.Count).FirstOrDefault(run.CanChoose);
                        run.ChooseEventOption(o);
                        break;
                }
            }
            return st;
        }

        /// Plays whole Runs on a Difficulty tier, with every Keeper upgrade at <paramref name="upgradeLevel"/> (capped at its max).
        public static string Play(int runs, int firstSeed = 1, int tier = 0, int upgradeLevel = 0)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Game/Content/GameContent.asset");
            var compiled = new CompiledContent(content);
            var data = new ProgressData { HighestTier = tier };
            foreach (var u in compiled.Progress.Upgrades) data.Upgrades.Add(new CountEntry { Id = u.Id, Count = upgradeLevel });
            var keep = new KeepProgress(compiled.Progress, data);
            var starters = content.familiars.Where(f => f != null && f.unlock == UnlockKind.Start).Select(f => f.def.Id).ToList();
            var sb = new StringBuilder();
            int wins = 0;
            float totalMinutes = 0f;
            for (int i = 0; i < runs; i++)
            {
                var run = new Run(compiled.Run, (firstSeed + i) * 7919, starters[i % starters.Count], keep.BonusesFor(tier));
                var st = PlayOut(run);
                var r = run.Result;
                if (r != null && r.Won) wins++;
                totalMinutes += st.Seconds / 60f;
                sb.AppendLine($"Run {i + 1} ({starters[i % starters.Count]}): {(r != null && r.Won ? "WON " : "lost")} ch{r?.ChapterReached} battles={st.Battles} " +
                              $"battle-minutes={st.Seconds / 60f:0.0} squad={run.Familiars.Count + 1} KOs={st.Knockouts} lowest Keep={st.Lowest:0} ({st.LowestAt})");
            }
            sb.Insert(0, $"Run bot: {wins}/{runs} won, average battle time {totalMinutes / runs:0.0} min\n");
            return sb.ToString();
        }
    }
}
