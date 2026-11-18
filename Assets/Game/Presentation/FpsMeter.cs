#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleKeep.Game
{
    /// Development builds only (not compiled into release builds): a small readout of the frame rate and the slowest frame, so 60 frames per second can
    /// be checked on the phone in the busiest Battle. Release builds never create it.
    public sealed class FpsMeter : MonoBehaviour
    {
        const float Window = 1f;

        TextMeshProUGUI label;
        float elapsed, worst;
        int frames;

        public static void CreateIfDevelopment()
        {
            if (!Debug.isDebugBuild) return;
            var go = new GameObject("FpsMeter");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsMeter>();
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var text = new GameObject("Text", typeof(RectTransform));
            text.transform.SetParent(transform, false);
            var rt = (RectTransform)text.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(420f, 30f);
            label = text.AddComponent<TextMeshProUGUI>();
            var theme = UiKit.Theme;
            if (theme != null && theme.font != null) label.font = theme.font;
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.color = new Color(1f, 1f, 1f, 0.75f);
            label.text = "";
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            if (dt > worst) worst = dt;
            if (elapsed < Window) return;
            float fps = frames / elapsed;
            label.color = fps >= 57f ? new Color(1f, 1f, 1f, 0.75f) : new Color(1f, 0.6f, 0.5f, 0.9f);
            label.SetText("{0:0} fps  worst {1:0} ms", fps, worst * 1000f);
            elapsed = 0f;
            frames = 0;
            worst = 0f;
        }
    }
}
#endif
