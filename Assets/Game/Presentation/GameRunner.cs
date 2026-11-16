using System;
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
        }

        void Start() => ShowTitle();

        void ShowTitle() => ShowTitle("");

        void ShowTitle(string notice)
        {
            run = null;
            screens.ShowTitle(HasSave, ContinueRun, () => screens.ShowStarterChoice(StartRun, ShowTitle), notice);
        }

        static bool HasSave => PlayerPrefs.HasKey(SaveKey);

        void StartRun(string starterId)
        {
            int seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount & 0x7fffffff;
            run = new Run(compiled.Run, seed, starterId);
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
                ShowTitle(e.Message + " It can't be continued, sorry: start a new Run.");
                return;
            }
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

        void Save()
        {
            if (run.Phase == RunPhase.Ended) PlayerPrefs.DeleteKey(SaveKey);
            else PlayerPrefs.SetString(SaveKey, run.Save());
            PlayerPrefs.Save();
        }

        void Show()
        {
            switch (run.Phase)
            {
                case RunPhase.Trail:
                    screens.ShowTrail(run, i => Act(() => run.ChooseStop(i)), ShowSquad, ShowTitle);
                    break;
                case RunPhase.Battle:
                    screens.Hide();
                    string header = $"Ch {run.Chapter + 1} · Step {run.Step}/{run.StepsInChapter}";
                    if (!string.IsNullOrEmpty(run.EncounterName)) header += $" · {run.EncounterName}";
                    battle.Begin(run.CreateBattle(), compiled, header,
                        report => Act(() => run.FinishBattle(report)),
                        keep => { run.SuspendBattle(keep); Save(); ShowTitle(); });
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
                    screens.ShowSummary(run, ShowTitle);
                    break;
            }
        }

        void ShowSquad() => screens.ShowSquad(run, null);
    }
}
