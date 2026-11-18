using System;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Plays one Battle on screen: feeds Player input into the rules, advances them, and hands state to the view and HUD.
    /// When the Player presses Continue on the end panel, reports the result.
    public sealed class BattleScreen : MonoBehaviour
    {
        static readonly float[] Speeds = { 1f, 2f };

        Battle battle;
        CompiledContent content;
        Camera cam;
        Fx fx;
        BattleView view;
        BattleHud hud;
        AimInput aim;
        string header;
        Action<BattleReport> onEnded;
        Action onContinue;
        Action<float> onQuit, onLeftApp;
        int speedIndex;
        readonly System.Collections.Generic.List<BattleEvent> events = new System.Collections.Generic.List<BattleEvent>();
        bool paused, ended;
        float lastAspect;
        Rect lastSafeArea;

        public bool IsRunning => battle != null;

        public void Init(Camera camera)
        {
            cam = camera;
            fx = new GameObject("Fx").AddComponent<Fx>();
            fx.transform.SetParent(transform, false);
            view = new GameObject("BattleView").AddComponent<BattleView>();
            view.transform.SetParent(transform, false);
            hud = new GameObject("Hud", typeof(RectTransform)).AddComponent<BattleHud>();
            hud.transform.SetParent(transform, false);
            aim = new AimInput(cam);

            hud.RallyPressed += () => { if (!paused && !ended) battle.TryRally(); };
            hud.PausePressed += () => SetPaused(true);
            hud.ResumePressed += () => SetPaused(false);
            hud.SpeedPressed += () => speedIndex = (speedIndex + 1) % Speeds.Length;
            hud.QuitPressed += () =>
            {
                float keep = battle.KeepHp;
                Stop();
                onQuit?.Invoke(keep);
            };
            hud.ContinuePressed += () =>
            {
                Stop();
                onContinue?.Invoke();
            };
            gameObject.SetActive(false);
        }

        /// onEnded gets the result the moment the Battle is decided, before the Player taps Continue, so it can be
        /// saved at once. onQuit (the pause panel's Quit) and onLeftApp (the app going to the background mid-Battle)
        /// get the Keep's health at that moment.
        public void Begin(BattleSetup setup, CompiledContent content, string header, Action<BattleReport> onEnded, Action onContinue,
            Action<float> onQuit, Action<float> onLeftApp)
        {
            gameObject.SetActive(true);
            this.content = content;
            this.header = header;
            this.onEnded = onEnded;
            this.onContinue = onContinue;
            this.onQuit = onQuit;
            this.onLeftApp = onLeftApp;
            battle = new Battle(setup);
            FitCamera(force: true);
            view.Show(battle, content, fx);
            hud.Build(battle, content);
            paused = ended = false;
        }

        void Stop()
        {
            battle = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (battle == null) return;
            FitCamera(force: false);
            if (!paused && !ended && !ExplainPopup.IsOpen)
            {
                var held = aim.Read();
                battle.SetAim(held.HasValue ? ClampToLane(held.Value) : (Vec2?)null);
                // A big hit (a Combo, a Rally) slows the Battle for a moment so it lands.
                battle.Advance(Mathf.Min(Time.deltaTime, 0.1f) * Speeds[speedIndex] * fx.TimeScale);
            }
            else battle.SetAim(null);

            battle.TakeEvents(events);
            view.Handle(events);
            hud.Refresh(battle, Speeds[speedIndex], header);

            if (!ended && battle.Outcome != BattleOutcome.Ongoing)
            {
                ended = true;
                hud.ShowEnd(battle);
                onEnded?.Invoke(BattleReport.From(battle));
            }
        }

        void SetPaused(bool value)
        {
            if (ended || battle == null) return;
            paused = value;
            hud.ShowPause(value);
        }

        /// On a phone, leaving the app pauses the Battle (not in the editor, where clicking another window would)
        /// and records the Keep's health, so closing the app mid-Battle is never a free retry.
        void OnApplicationPause(bool pausing)
        {
            if (!pausing || battle == null || ended) return;
            onLeftApp?.Invoke(battle.KeepHp);
            if (!Application.isEditor) SetPaused(true);
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
            cam.transform.position = new Vector3(left - leftInset * fullWidth + fullWidth * 0.5f, -0.35f, -10f);
            fx.Init(cam);
        }

        Vec2 ClampToLane(Vector2 p)
        {
            var t = battle.Tuning;
            return new Vec2(Mathf.Clamp(p.x, t.KeepX, t.LaneLength), Mathf.Clamp(p.y, -t.LaneHalfHeight, t.LaneHalfHeight));
        }
    }
}
