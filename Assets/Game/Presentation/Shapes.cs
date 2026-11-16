using UnityEngine;

namespace LittleKeep.Game
{
    /// Grey-box sprites generated at runtime, so nothing needs importing before real art arrives.
    public static class Shapes
    {
        static Sprite square, circle, ring, diamond;

        public static Sprite Square => square ??= Make(64, (x, y) => 1f);
        public static Sprite Circle => circle ??= Make(128, (x, y) => Edge(1f - Mathf.Sqrt(x * x + y * y)));
        public static Sprite Ring => ring ??= Make(128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Edge(1f - d) * Edge(d - 0.82f);
        });
        public static Sprite Diamond => diamond ??= Make(128, (x, y) => Edge(1f - (Mathf.Abs(x) + Mathf.Abs(y))));

        /// Soft edge: 1 inside, fading to 0 across the last few pixels.
        static float Edge(float distInside) => Mathf.Clamp01(distInside * 40f);

        static Sprite Make(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size * 2f - 1f, y = (j + 0.5f) / size * 2f - 1f;
                    px[j * size + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y)) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            // pixelsPerUnit = size, so every shape is 1 world unit across before scaling.
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
