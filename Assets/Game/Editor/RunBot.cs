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

        public static string Play(int runs, int firstSeed = 1)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Game/Content/GameContent.asset");
            var compiled = new CompiledContent(content);
            var starters = content.starters.Where(s => s != null).Select(s => s.def.Id).ToList();
            var sb = new StringBuilder();
            int wins = 0;
            float totalMinutes = 0f;
            for (int i = 0; i < runs; i++)
            {
                var run = new Run(compiled.Run, (firstSeed + i) * 7919, starters[i % starters.Count]);
                float seconds = 0f, lowest = float.MaxValue;
                int battles = 0, knockouts = 0;
                string lowestAt = "";
                for (int guard = 0; guard < 500 && run.Phase != RunPhase.Ended; guard++)
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
                                knockouts += battle.TakeEvents().Count(e => e.Kind == BattleEventKind.KnockedOut);
                            }
                            seconds += battle.Time;
                            battles++;
                            if (battle.KeepHp < lowest) { lowest = battle.KeepHp; lowestAt = $"{run.CurrentStop.Kind} ch{run.Chapter + 1}"; }
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
                var r = run.Result;
                if (r != null && r.Won) wins++;
                totalMinutes += seconds / 60f;
                sb.AppendLine($"Run {i + 1} ({starters[i % starters.Count]}): {(r != null && r.Won ? "WON " : "lost")} ch{r?.ChapterReached} battles={battles} " +
                              $"battle-minutes={seconds / 60f:0.0} squad={run.Familiars.Count + 1} KOs={knockouts} lowest Keep={lowest:0} ({lowestAt})");
            }
            sb.Insert(0, $"Run bot: {wins}/{runs} won, average battle time {totalMinutes / runs:0.0} min\n");
            return sb.ToString();
        }
    }
}
