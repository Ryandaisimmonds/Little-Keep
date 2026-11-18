using UnityEngine;

namespace LittleKeep.Game
{
    /// Grey-box sprites generated at runtime, so nothing needs importing before real art arrives.
    public static class Shapes
    {
        static Sprite square, circle, ring, diamond, soft, star, shard, petal;

        // Cached across play sessions, which survive without a domain reload; the textures do not, so test with
        // Unity's own null check (a destroyed Sprite == null) rather than ??=, which would hand back a dead sprite.
        public static Sprite Square => square != null ? square : square = Make(64, (x, y) => 1f);
        public static Sprite Circle => circle != null ? circle : circle = Make(128, (x, y) => Edge(1f - Mathf.Sqrt(x * x + y * y)));
        public static Sprite Ring => ring != null ? ring : ring = Make(128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Edge(1f - d) * Edge(d - 0.82f);
        });
        public static Sprite Diamond => diamond != null ? diamond : diamond = Make(128, (x, y) => Edge(1f - (Mathf.Abs(x) + Mathf.Abs(y))));

        // Particle shapes (Effect Art can replace each one).
        /// A round glow that fades smoothly from the centre.
        public static Sprite Soft => soft != null ? soft : soft = Make(64, (x, y) =>
        {
            float d = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y));
            return (1f - d) * (1f - d);
        });
        /// A four-pointed twinkle with a glowing middle.
        public static Sprite Star => star != null ? star : star = Make(64, (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float arms = Mathf.Clamp01(1f - (ax * ay * 14f + Mathf.Max(ax, ay) * 0.95f));
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2.4f);
            return Mathf.Max(arms, core);
        });
        /// A thin, sharp sliver of ice.
        public static Sprite Shard => shard != null ? shard : shard = Make(64, (x, y) => Edge(1f - (Mathf.Abs(x) * 2.6f + Mathf.Abs(y))) * 0.95f);
        /// A soft oval petal.
        public static Sprite Petal => petal != null ? petal : petal = Make(64, (x, y) => Edge(1f - Mathf.Sqrt(x * x * 3.2f + y * y)));

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
