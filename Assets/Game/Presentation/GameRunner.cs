using System;
using System.Linq;
using LittleKeep.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LittleKeep.Game
{
    /// The whole game loop: title, Runs, Battles. Every Player choice goes to the Run, the Run is saved,
    /// and whichever screen the Run now needs is shown.
    public sealed class GameRunner : MonoBehaviour
    {
        const string SaveKey = "littlekeep.run";

        [SerializeField] GameContent content;
        [SerializeField, Tooltip("0 = a new seed every Run")] int fixedSeed;

        CompiledContent compiled;
        RunScreens screens;
        BattleScreen battle;
        Run run;
        KeepProgress keep;
        RunRewards lastRewards;
        /// Deeds completed during the current Run, shown again on its summary.
        readonly System.Collections.Generic.List<string> runDeeds = new System.Collections.Generic.List<string>();
        string trailNotice = "";

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            UiKit.Theme = content.theme;

            var cam = Camera.main != null ? Camera.main : new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            compiled = new CompiledContent(content);
            screens = new GameObject("Screens", typeof(RectTransform)).AddComponent<RunScreens>();
            screens.Init(compiled);
            battle = new GameObject("Battle").AddComponent<BattleScreen>();
            battle.Init(cam);
            keep = ProgressStore.Load(compiled.Progress);
        }

        void Start() => ShowKeep();

        void ShowKeep() => ShowKeep("");

        void ShowKeep(string notice)
        {
            run = null;
            screens.ShowKeep(keep, HasSave, keep.ChosenTier, ContinueRun, ShowStarters,
                () => screens.ShowBook(keep, ShowKeep),
                ShowWorkshop,
                ShowWatchtower,
                notice);
        }

        void ShowStarters()
        {
            var owned = content.familiars.Where(f => f != null && keep.Owns(f.def.Id)).ToList();
            screens.ShowStarterChoice(owned, StartRun, ShowKeep);
        }

        void ShowWorkshop() => screens.ShowWorkshop(keep, id =>
        {
            if (keep.TryBuy(id)) ProgressStore.Save(keep);
            ShowWorkshop();
        }, ShowKeep);

        void ShowWatchtower() => screens.ShowWatchtower(keep, keep.ChosenTier, i =>
        {
            if (keep.ChooseTier(i)) ProgressStore.Save(keep);
            ShowWatchtower();
        }, ShowKeep);

        static bool HasSave => PlayerPrefs.HasKey(SaveKey);

        void StartRun(string starterId)
        {
            int seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount & 0x7fffffff;
            run = new Run(compiled.Run, seed, starterId, keep.BonusesFor(keep.ChosenTier));
            lastRewards = null;
            runDeeds.Clear();
            SaveAndShow();
        }

        void ContinueRun()
        {
            try
            {
                run = Run.Load(compiled.Run, PlayerPrefs.GetString(SaveKey));
            }
            catch (Exception e)
            {
                // A save from an older build that no longer replays: drop it rather than get stuck.
                Debug.LogWarning("Could not resume the saved Run: " + e.Message);
                PlayerPrefs.DeleteKey(SaveKey);
                ShowKeep(e.Message + " It can't be continued, sorry: start a new Run.");
                return;
            }
            lastRewards = null;
            runDeeds.Clear();
            Show();
        }

        /// Do a Run action, then save and show what comes next.
        void Act(Action action)
        {
            action();
            SaveAndShow();
        }

        void SaveAndShow()
        {
            Save();
            Show();
        }

        /// Saves the Run, and its effect on the Keep: Deeds complete as Chapters are cleared, and a finished Run
        /// is paid for. The Keep pays each Run number once, so a crash before the Run save is dropped can't pay twice.
        void Save()
        {
            if (run.Phase == RunPhase.Ended)
            {
                if (lastRewards == null)
                {
                    lastRewards = keep.RecordRun(run.Result);
                    lastRewards.Deeds.InsertRange(0, runDeeds);
                    ProgressStore.Save(keep);
                }
                PlayerPrefs.DeleteKey(SaveKey);
            }
            else
            {
                var news = keep.NoteChaptersCleared(run.Chapter);
                if (news.Deeds.Count > 0)
                {
                    runDeeds.AddRange(news.Deeds);
                    ProgressStore.Save(keep);
                    trailNotice = string.Join("  ", news.Deeds.Select(id => $"Deed complete: {keep.Content.Deeds.Find(d => d.Id == id)?.Name}!")
                        .Concat(news.NewFamiliars.Select(id => $"{content.familiars.FirstOrDefault(f => f != null && f.def.Id == id)?.def.Name ?? id} joins your Familiars!")));
                }
                PlayerPrefs.SetString(SaveKey, run.Save());
            }
            PlayerPrefs.Save();
        }

        void Show()
        {
            switch (run.Phase)
            {
                case RunPhase.Trail:
                    screens.ShowTrail(run, i => Act(() => run.ChooseStop(i)), ShowSquad, ShowKeep, trailNotice);
                    trailNotice = "";
                    break;
                case RunPhase.Battle:
                    screens.Hide();
                    string header = $"Ch {run.Chapter + 1} · Step {run.Step}/{run.StepsInChapter}";
                    if (!string.IsNullOrEmpty(run.EncounterName)) header += $" · {run.EncounterName}";
                    battle.Begin(run.CreateBattle(), compiled, header,
                        report => Act(() => run.FinishBattle(report)),
                        keepHp => { run.SuspendBattle(keepHp); Save(); ShowKeep(); });
                    break;
                case RunPhase.Upgrade:
                    screens.ShowUpgrade(run, i => Act(() => run.PickUpgrade(i)), () => Act(run.SkipUpgrade), ShowSquad);
                    break;
                case RunPhase.Befriend:
                    screens.ShowBefriend(run, i => Act(() => run.Befriend(i)), () => Act(run.PassCreature), ShowSquad);
                    break;
                case RunPhase.Shop:
                    screens.ShowShop(run, i => Act(() => run.Buy(i)), () => Act(run.LeaveShop), ShowSquad);
                    break;
                case RunPhase.Event:
                    screens.ShowEvent(run, i => Act(() => run.ChooseEventOption(i)), ShowSquad);
                    break;
                case RunPhase.Ended:
                    screens.ShowSummary(run, lastRewards, keep, ShowKeep);
                    break;
            }
        }

        void ShowSquad() => screens.ShowSquad(run, null);
    }
}
