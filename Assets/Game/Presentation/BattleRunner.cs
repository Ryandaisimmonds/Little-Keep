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
        Rect lastSafeArea;

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
                battle.SetAim(held.HasValue ? ClampToLane(held.Value) : (Vec2?)null);
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

        /// Fit the Keep and the whole lane inside the safe area's width, leaving room above and below for the HUD.
        void FitCamera(bool force)
        {
            float aspect = cam.aspect;
            var safe = Screen.safeArea;
            if (!force && Mathf.Approximately(aspect, lastAspect) && safe == lastSafeArea) return;
            lastAspect = aspect;
            lastSafeArea = safe;
            var t = battle.Tuning;
            float left = t.KeepX - 2.3f, right = t.LaneLength + 0.9f;
            float safeFraction = Screen.width > 0 ? Mathf.Clamp(safe.width / Screen.width, 0.5f, 1f) : 1f;
            float leftInset = Screen.width > 0 ? safe.xMin / Screen.width : 0f;
            float fullWidth = Mathf.Max((right - left) / safeFraction, (t.LaneHalfHeight + 1.6f) * 2f * aspect);
            cam.orthographicSize = fullWidth / aspect * 0.5f;
            float x = left - leftInset * fullWidth + fullWidth * 0.5f;
            cam.transform.position = new Vector3(x, -0.35f, -10f);
            fx.Init(cam);
        }

        Vec2 ClampToLane(Vector2 p)
        {
            var t = battle.Tuning;
            return new Vec2(Mathf.Clamp(p.x, t.KeepX, t.LaneLength), Mathf.Clamp(p.y, -t.LaneHalfHeight, t.LaneHalfHeight));
        }
    }
}
