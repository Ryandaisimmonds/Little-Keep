using LittleKeep.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LittleKeep.Game
{
    /// Runs one Battle on screen: feeds Player input into the rules, advances them, and hands state to the view and HUD.
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField] BattleAsset content;
        [SerializeField, Tooltip("0 = new seed every Battle")] int fixedSeed;

        static readonly float[] Speeds = { 1f, 2f };

        Battle battle;
        Camera cam;
        Fx fx;
        BattleView view;
        BattleHud hud;
        AimInput aim;
        int speedIndex;
        bool paused, ended;
        float lastAspect;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            cam = Camera.main != null ? Camera.main : new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;

            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            fx = new GameObject("Fx").AddComponent<Fx>();
            view = new GameObject("BattleView").AddComponent<BattleView>();
            hud = new GameObject("Hud", typeof(RectTransform)).AddComponent<BattleHud>();
            aim = new AimInput(cam);

            hud.RallyPressed += () => { if (!paused) battle.TryRally(); };
            hud.PausePressed += () => SetPaused(true);
            hud.ResumePressed += () => SetPaused(false);
            hud.SpeedPressed += () => speedIndex = (speedIndex + 1) % Speeds.Length;
            hud.RestartPressed += Begin;
        }

        void Start() => Begin();

        void Begin()
        {
            int seed = fixedSeed != 0 ? fixedSeed : System.Environment.TickCount;
            battle = new Battle(content.ToSetup(seed));
            FitCamera(force: true);
            view.Show(battle, content, fx);
            hud.Build(battle, content);
            paused = ended = false;
        }

        void Update()
        {
            FitCamera(force: false);
            if (!paused && !ended)
            {
                var held = aim.Read();
                battle.SetAim(held.HasValue ? new Vec2(held.Value.x, held.Value.y) : (Vec2?)null);
                battle.Advance(Mathf.Min(Time.deltaTime, 0.1f) * Speeds[speedIndex]);
            }
            else battle.SetAim(null);

            view.Handle(battle.TakeEvents());
            hud.Refresh(battle, Speeds[speedIndex]);

            if (!ended && battle.Outcome != BattleOutcome.Ongoing)
            {
                ended = true;
                hud.ShowEnd(battle.Outcome, battle);
            }
        }

        void SetPaused(bool value)
        {
            if (ended || battle == null) return;
            paused = value;
            hud.ShowPause(value);
        }

        void OnApplicationPause(bool pausing)
        {
            if (pausing) SetPaused(true);
        }

        /// Fit the whole lane across the screen width, leaving room above and below for the HUD.
        void FitCamera(bool force)
        {
            float aspect = cam.aspect;
            if (!force && Mathf.Approximately(aspect, lastAspect)) return;
            lastAspect = aspect;
            var t = battle.Tuning;
            float width = t.LaneLength + 3.2f;
            cam.orthographicSize = Mathf.Max(width / aspect * 0.5f, t.LaneHalfHeight + 1.6f);
            cam.transform.position = new Vector3(t.LaneLength * 0.5f - 0.9f, -0.35f, -10f);
            fx.Init(cam);
        }
    }
}
