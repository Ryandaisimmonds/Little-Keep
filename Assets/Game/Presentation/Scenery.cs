using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Stand-in illustrations painted in code as small pixel art (to sit with the pixel-art characters): skies,
    /// rolling hills with trees that tile for scrolling, fog, the boss's castle, and the Keep's buildings.
    /// Colours come from the theme. Replace any of these with drawn art later without touching the layouts.
    public static class Scenery
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// Painting is cached by name and colours; a changed theme repaints. Uses Unity's null check because the
        /// textures don't survive between play sessions when the domain isn't reloaded.
        static Sprite Cached(string key, Func<Sprite> paint)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = paint();
            cache[key] = s;
            return s;
        }

        static string Key(string name, params Color[] colors)
        {
            var k = name;
            foreach (var c in colors) k += "|" + ColorUtility.ToHtmlStringRGBA(c);
            return k;
        }

        // --- Landscape ------------------------------------------------------------------------------

        /// A vertical gradient from the horizon colour up to the sky colour.
        public static Sprite Sky() => Cached(Key("sky", Palette.SkyTop, Palette.SkyBottom), () =>
        {
            var p = new Painter(4, 64);
            for (int y = 0; y < p.H; y++) p.Rect(0, y, p.W, 1, Color.Lerp(Palette.SkyBottom, Palette.SkyTop, Mathf.SmoothStep(0f, 1f, (float)y / (p.H - 1))));
            return p.Sprite(FilterMode.Bilinear);
        });

        /// A strip of hills, white so it can be tinted, that tiles left to right. Trees grow along the ridge.
        public static Sprite Hills(int seed, float height, float bumpiness, float treeDensity) =>
            Cached($"hills{seed}_{height}_{bumpiness}_{treeDensity}", () =>
            {
                const int W = 320, H = 90;
                var p = new Painter(W, H);
                var rng = new System.Random(seed);
                // Whole numbers of waves across the strip, so its two ends meet when it repeats.
                int f1 = 2 + rng.Next(2), f2 = 4 + rng.Next(3), f3 = 9 + rng.Next(4);
                double p1 = rng.NextDouble() * 6.28, p2 = rng.NextDouble() * 6.28, p3 = rng.NextDouble() * 6.28;
                var ridge = new int[W];
                for (int x = 0; x < W; x++)
                {
                    double t = (double)x / W * Math.PI * 2;
                    double wave = 0.55 * Math.Sin(f1 * t + p1) + 0.3 * Math.Sin(f2 * t + p2) + 0.15 * Math.Sin(f3 * t + p3);
                    ridge[x] = Mathf.Clamp((int)(H * height + wave * H * 0.18f * bumpiness), 4, H - 14);
                    p.Rect(x, 0, 1, ridge[x], Color.white);
                }
                // Round trees on the ridge, wrapped so they tile too.
                for (int x = rng.Next(8); x < W; x += 6 + rng.Next(14))
                {
                    if (rng.NextDouble() > treeDensity) continue;
                    int r = 3 + rng.Next(4);
                    int cx = x, cy = ridge[x] + r - 1;
                    p.Rect(cx, ridge[x] - 1, 1, r + 1, Color.white);
                    p.CircleWrapped(cx, cy + 1, r, Color.white);
                    if (rng.NextDouble() < 0.5) p.CircleWrapped(cx + r - 1, cy - 1, r - 1, Color.white);
                }
                return p.Sprite(FilterMode.Point, repeat: true);
            });

        /// Fog that thickens from left to right (white; tint it with the fog colour).
        public static Sprite FogRamp() => Cached("fog", () =>
        {
            var p = new Painter(64, 1);
            for (int x = 0; x < p.W; x++) p.Rect(x, 0, 1, 1, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (float)x / (p.W - 1))));
            return p.Sprite(FilterMode.Bilinear);
        });

        /// The boss's castle on the horizon: a white silhouette with a pennant.
        public static Sprite BossCastle() => Cached("bosscastle", () =>
        {
            var p = new Painter(56, 48);
            var c = Color.white;
            p.Rect(8, 0, 40, 22, c);
            p.Battlements(8, 22, 40, 3, 3, c);
            p.Rect(2, 0, 10, 32, c); p.Battlements(2, 32, 10, 3, 2, c);
            p.Rect(44, 0, 10, 32, c); p.Battlements(44, 32, 10, 3, 2, c);
            p.Rect(22, 0, 12, 36, c); p.Tri(20, 36, 36, 28, 46, c);
            p.Rect(28, 38, 1, 8, c);
            p.Rect(29, 43, 6, 3, c);
            return p.Sprite(FilterMode.Point);
        });

        // --- The Keep -------------------------------------------------------------------------------

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        /// The Keep seen from the side, for the left end of the Battle lane: a stone tower with a door to the lane.
        public static Sprite KeepSide() => Cached(Key("keepside", Palette.Keep, Palette.KeepStone, Palette.KeepRoof, Palette.KeepDoor, Palette.Window), () =>
        {
            var p = new Painter(24, 64);
            var wall = Palette.Keep;
            p.Rect(2, 0, 20, 52, wall);
            p.Bricks(2, 0, 20, 52, Shade(Palette.KeepStone, 0.9f));
            p.Rect(2, 0, 3, 52, Shade(wall, 0.8f));
            p.Battlements(2, 52, 20, 3, 3, wall);
            p.Tri(0, 24, 55, 12, 64, Palette.KeepRoof);
            p.Arch(13, 0, 9, 16, Palette.KeepDoor);
            p.Rect(8, 32, 4, 6, Palette.Window);
            p.Rect(8, 22, 4, 5, Palette.Window);
            return p.Sprite(FilterMode.Point);
        });

        /// The Gate: the Keep's front, with its big door. Always there.
        public static Sprite Gatehouse() => Cached(Key("gate", Palette.Keep, Palette.KeepStone, Palette.KeepRoof, Palette.KeepDoor, Palette.Window), () =>
        {
            var p = new Painter(64, 60);
            var wall = Palette.Keep;
            p.Rect(4, 0, 56, 40, wall);
            p.Bricks(4, 0, 56, 40, Shade(Palette.KeepStone, 0.9f));
            p.Battlements(4, 40, 56, 3, 4, wall);
            p.Rect(0, 0, 12, 50, Shade(wall, 0.92f)); p.Bricks(0, 0, 12, 50, Shade(Palette.KeepStone, 0.85f)); p.Battlements(0, 50, 12, 3, 3, wall);
            p.Rect(52, 0, 12, 50, Shade(wall, 0.92f)); p.Bricks(52, 0, 12, 50, Shade(Palette.KeepStone, 0.85f)); p.Battlements(52, 50, 12, 3, 3, wall);
            p.Arch(24, 0, 16, 26, Palette.KeepDoor);
            p.Rect(31, 0, 2, 24, Shade(Palette.KeepDoor, 0.7f));
            p.Rect(4, 34, 4, 6, Palette.Window); p.Rect(56, 34, 4, 6, Palette.Window);
            p.Rect(14, 28, 5, 6, Palette.Window); p.Rect(45, 28, 5, 6, Palette.Window);
            p.Rect(31, 43, 1, 14, Shade(Palette.KeepDoor, 0.8f));
            p.Rect(32, 51, 7, 5, Palette.KeepRoof);
            return p.Sprite(FilterMode.Point);
        });

        /// The Familiar Book's library: a round tower with a pointed roof and a big lit window.
        public static Sprite Library() => Cached(Key("library", Palette.Keep, Palette.KeepStone, Palette.KeepRoof, Palette.Window), () =>
        {
            var p = new Painter(30, 76);
            var wall = Shade(Palette.Keep, 0.95f);
            p.Rect(4, 0, 22, 54, wall);
            p.Bricks(4, 0, 22, 54, Shade(Palette.KeepStone, 0.9f));
            p.Rect(4, 0, 3, 54, Shade(wall, 0.8f));
            p.Tri(1, 29, 54, 15, 76, Palette.KeepRoof);
            p.Arch(10, 28, 10, 16, Palette.Window);
            p.Rect(15, 28, 1, 14, Shade(Palette.Window, 0.7f));
            p.Arch(12, 0, 6, 10, Palette.KeepDoor);
            return p.Sprite(FilterMode.Point);
        });

        /// The Workshop: a timber hut with a sloped roof and a smoking chimney.
        public static Sprite Workshop() => Cached(Key("workshop", Palette.KeepDoor, Palette.KeepRoof, Palette.Window, Palette.KeepStone), () =>
        {
            var p = new Painter(48, 44);
            var timber = Shade(Palette.KeepDoor, 1.25f);
            p.Rect(4, 0, 40, 24, timber);
            for (int x = 4; x < 44; x += 5) p.Rect(x, 0, 1, 24, Shade(timber, 0.75f));
            p.Tri(0, 48, 24, 24, 38, Palette.KeepRoof);
            p.Rect(34, 26, 5, 14, Palette.KeepStone);
            p.Circle(37, 41, 2, new Color(1f, 1f, 1f, 0.55f));
            p.Circle(40, 43, 1, new Color(1f, 1f, 1f, 0.4f));
            p.Rect(10, 0, 10, 14, Shade(Palette.KeepDoor, 0.7f));
            p.Rect(26, 8, 10, 8, Palette.Window);
            p.Rect(30, 8, 1, 8, Shade(timber, 0.7f));
            return p.Sprite(FilterMode.Point);
        });

        /// The Watchtower: tall and thin, with a lookout and a flag.
        public static Sprite Watchtower() => Cached(Key("tower", Palette.Keep, Palette.KeepStone, Palette.KeepRoof, Palette.Window), () =>
        {
            var p = new Painter(28, 104);
            var wall = Shade(Palette.Keep, 0.9f);
            p.Rect(8, 0, 12, 76, wall);
            p.Bricks(8, 0, 12, 76, Shade(Palette.KeepStone, 0.85f));
            p.Rect(4, 76, 20, 10, wall);
            p.Battlements(4, 86, 20, 3, 3, wall);
            p.Rect(10, 78, 8, 6, Palette.Window);
            p.Tri(2, 26, 89, 14, 100, Palette.KeepRoof);
            p.Rect(14, 98, 1, 6, Shade(Palette.KeepDoor, 0.8f));
            p.Rect(15, 100, 6, 4, Palette.KeepRoof);
            p.Rect(12, 40, 4, 6, Palette.Window);
            return p.Sprite(FilterMode.Point);
        });

        /// A stretch of curtain wall that joins the Keep's buildings.
        public static Sprite Wall() => Cached(Key("wall", Palette.Keep, Palette.KeepStone), () =>
        {
            var p = new Painter(40, 24);
            var wall = Shade(Palette.Keep, 0.88f);
            p.Rect(0, 0, 40, 20, wall);
            p.Bricks(0, 0, 40, 20, Shade(Palette.KeepStone, 0.85f));
            p.Battlements(0, 20, 40, 3, 4, wall);
            return p.Sprite(FilterMode.Point, repeat: true);
        });

        // --- Painter ---------------------------------------------------------------------------------

        /// A tiny pixel canvas. Coordinates start bottom left.
        sealed class Painter
        {
            public readonly int W, H;
            readonly Color32[] px;

            public Painter(int w, int h)
            {
                W = w; H = h;
                px = new Color32[w * h];
            }

            void Set(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                if (c.a >= 0.999f) { px[y * W + x] = c; return; }
                // Blend over what is there.
                Color under = px[y * W + x];
                float a = c.a + under.a * (1f - c.a);
                if (a <= 0f) return;
                var rgb = (new Color(c.r, c.g, c.b) * c.a + new Color(under.r, under.g, under.b) * under.a * (1f - c.a)) / a;
                px[y * W + x] = new Color(rgb.r, rgb.g, rgb.b, a);
            }

            public void Rect(int x, int y, int w, int h, Color c)
            {
                for (int j = y; j < y + h; j++)
                    for (int i = x; i < x + w; i++) Set(i, j, c);
            }

            public void Circle(int cx, int cy, int r, Color c)
            {
                for (int j = -r; j <= r; j++)
                    for (int i = -r; i <= r; i++)
                        if (i * i + j * j <= r * r + r) Set(cx + i, cy + j, c);
            }

            /// A circle that wraps around the left and right edges (for tiling strips).
            public void CircleWrapped(int cx, int cy, int r, Color c)
            {
                Circle(cx, cy, r, c);
                if (cx - r < 0) Circle(cx + W, cy, r, c);
                if (cx + r >= W) Circle(cx - W, cy, r, c);
            }

            /// A filled triangle with its base from x0 to x1 at baseY and its tip at (apexX, apexY).
            public void Tri(int x0, int x1, int baseY, int apexX, int apexY, Color c)
            {
                for (int y = baseY; y <= apexY; y++)
                {
                    float t = apexY == baseY ? 1f : (float)(y - baseY) / (apexY - baseY);
                    int l = Mathf.RoundToInt(Mathf.Lerp(x0, apexX, t)), r = Mathf.RoundToInt(Mathf.Lerp(x1, apexX, t));
                    for (int x = l; x <= r; x++) Set(x, y, c);
                }
            }

            /// Crenellations: teeth of width `tooth` along the top of a wall.
            public void Battlements(int x, int y, int w, int h, int tooth, Color c)
            {
                for (int i = x; i < x + w; i += tooth * 2) Rect(i, y, Math.Min(tooth, x + w - i), h, c);
            }

            /// Mortar lines over a wall: every fourth row, with staggered joints.
            public void Bricks(int x, int y, int w, int h, Color mortar)
            {
                for (int j = y + 3; j < y + h; j += 4)
                {
                    Rect(x, j, w, 1, mortar);
                    int offset = ((j - y) / 4) % 2 == 0 ? 0 : 3;
                    for (int i = x + offset; i < x + w; i += 6) Rect(i, j - 3, 1, 3, mortar);
                }
            }

            /// A doorway or window with a rounded top.
            public void Arch(int x, int y, int w, int h, Color c)
            {
                int r = w / 2;
                Rect(x, y, w, h - r, c);
                for (int j = 0; j < r; j++)
                    for (int i = 0; i < w; i++)
                    {
                        float dx = i - (w - 1) * 0.5f, dy = j;
                        if (dx * dx + dy * dy <= r * r) Set(x + i, y + h - r + j, c);
                    }
            }

            public Sprite Sprite(FilterMode filter, bool repeat = false)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    filterMode = filter,
                    wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                    name = "scenery",
                };
                tex.SetPixels32(px);
                tex.Apply();
                return UnityEngine.Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), Mathf.Max(W, H));
            }
        }
    }
}
