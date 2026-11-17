using UnityEngine;
using UnityEngine.UI;

namespace LittleKeep.Game
{
    /// A repeating landscape strip on a RawImage. Tiles keep their shape at any screen width, and the strip pans
    /// smoothly from where it was to Target (in tiles), so walking the Trail scrolls the land past.
    [RequireComponent(typeof(RawImage))]
    public sealed class ScrollingStrip : MonoBehaviour
    {
        /// Width of one tile divided by its height, so tiles are never stretched.
        public float TileAspect = 3.5f;
        public float Offset, Target;
        public float PanSeconds = 0.9f;
        RawImage image;
        RectTransform rect;
        float from, t = 1f;

        public void PanTo(float target, float start)
        {
            Offset = from = start;
            Target = target;
            t = Mathf.Approximately(start, target) ? 1f : 0f;
        }

        void Awake()
        {
            image = GetComponent<RawImage>();
            rect = (RectTransform)transform;
        }

        void Update()
        {
            if (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(0.01f, PanSeconds));
                Offset = Mathf.Lerp(from, Target, 1f - (1f - t) * (1f - t));
            }
            var size = rect.rect.size;
            if (size.y <= 0f) return;
            float tiles = size.x / (size.y * TileAspect);
            image.uvRect = new Rect(Offset, 0f, tiles, 1f);
        }
    }
}
