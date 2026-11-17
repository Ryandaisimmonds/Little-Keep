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
        /// What the Run in progress has already earned for the Keep (Deeds and their Familiars), for its summary.
        const string NewsKey = "littlekeep.run.news";

        [SerializeField] GameContent content;
        [SerializeField, Tooltip("0 = a new seed every Run")] int fixedSeed;

        CompiledContent compiled;
        RunScreens screens;
        BattleScreen battle;
        Run run;
        KeepProgress keep;
        RunRewards lastRewards;
        /// Deeds completed and Familiars earned during the current Run, shown again on its summary.
        readonly System.Collections.Generic.List<string> runDeeds = new System.Collections.Generic.List<string>();
        readonly System.Collections.Generic.List<string> runFamiliars = new System.Collections.Generic.List<string>();
        int lastActFrame = -1;
        string trailNotice = "";

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            UiKit.Theme = content.theme;
            ElementLook.Tuning = content.battleTuning;

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
            SetRunNews("");
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
                // A save that no longer replays (an older build, or changed content): drop it rather than get stuck.
                Debug.LogWarning("Could not resume the saved Run: " + e);
                PlayerPrefs.DeleteKey(SaveKey);
                SetRunNews("");
                PlayerPrefs.Save();
                string why = e is FormatException ? e.Message : "The saved Run could not be read.";
                ShowKeep(why + " It can't be continued, sorry: start a new Run.");
                return;
            }
            lastRewards = null;
            SetRunNews(PlayerPrefs.GetString(NewsKey, ""));
            Show();
        }

        /// Run news is kept as "deed,deed;familiar,familiar" beside the Run save.
        void SetRunNews(string saved)
        {
            runDeeds.Clear();
            runFamiliars.Clear();
            var parts = (saved ?? "").Split(';');
            if (parts.Length > 0) runDeeds.AddRange(parts[0].Split(',').Where(x => x.Length > 0));
            if (parts.Length > 1) runFamiliars.AddRange(parts[1].Split(',').Where(x => x.Length > 0));
            if (string.IsNullOrEmpty(saved)) PlayerPrefs.DeleteKey(NewsKey);
        }

        /// Do a Run action, then save and show what comes next.
        void Act(Action action)
        {
            // One choice a frame: a second tap landing before the screen has changed must not act on the next screen.
            if (Time.frameCount == lastActFrame) return;
            lastActFrame = Time.frameCount;
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
                    lastRewards.NewFamiliars.InsertRange(0, runFamiliars);
                    ProgressStore.Save(keep);
                }
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.DeleteKey(NewsKey);
            }
            else
            {
                var news = keep.NoteChaptersCleared(run.Chapter);
                if (news.Deeds.Count > 0)
                {
                    runDeeds.AddRange(news.Deeds);
                    runFamiliars.AddRange(news.NewFamiliars);
                    ProgressStore.Save(keep);
                    PlayerPrefs.SetString(NewsKey, string.Join(",", runDeeds) + ";" + string.Join(",", runFamiliars));
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
                        report => { run.FinishBattle(report); Save(); },
                        Show,
                        keepHp => { run.SuspendBattle(keepHp); Save(); ShowKeep(); },
                        keepHp => { run.SuspendBattle(keepHp); Save(); });
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
