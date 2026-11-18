using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Short-lived world effects: rings that grow and fade, flashes, glowing particles, floating words, screen shake,
    /// a full-screen flash and a brief hit-stop. Everything runs on unscaled time.
    public sealed class Fx : MonoBehaviour
    {
        /// The art effects are drawn with. Set once content is loaded; empty slots fall back to shapes made in code.
        public static EffectArt Art;

        static Material fallbackGlow;

        /// The additive material glows are drawn with: the Effect Art's, else one made from the FxGlow shader.
        /// Null only if no glow shader is in the build at all (then glows fall back to the normal sprite look).
        public static Material GlowMaterial
        {
            get
            {
                if (Art != null && Art.glow != null) return Art.glow;
                if (fallbackGlow != null) return fallbackGlow;
                var shader = Shader.Find("LittleKeep/FxGlow");
                if (shader == null)
                {
                    if (!warned) Debug.LogWarning("Effect Art has no glow material and the LittleKeep/FxGlow shader is not in the build: effects will not glow. Run Little Keep > Apply Art Packs.");
                    warned = true;
                    return null;
                }
                return fallbackGlow = new Material(shader) { name = "fx-glow" };
            }
        }
        static bool warned;

        sealed class Pulse
        {
            public SpriteRenderer sr;
            public float age, life, startScale, endScale;
            public Color color;
            /// For lines: fixed length and thickness instead of growing.
            public Vector2 stretch;
        }

        readonly List<Pulse> live = new List<Pulse>();
        readonly Stack<Pulse> pool = new Stack<Pulse>();
        Camera cam;
        Vector3 camHome;
        float shake;
        Particles particles;
        SpriteRenderer screenFlash;
        Color flashColor;
        float flashAge, flashLife;
        float hitStop;

        public Particles Particles => particles ?? (particles = new Particles(transform));

        /// How fast the Battle should run right now: slowed to a crawl for a moment after a big hit.
        public float TimeScale => hitStop > 0f ? 0.12f : 1f;

        public void Init(Camera camera)
        {
            cam = camera;
            camHome = camera.transform.position;
        }

        /// Ends every running effect (a new Battle starts clean).
        public void Clear()
        {
            for (int i = live.Count - 1; i >= 0; i--) Recycle(i);
            foreach (var f in floaters) { f.text.gameObject.SetActive(false); textPool.Push(f.text); floaterPool.Push(f); }
            floaters.Clear();
            particles?.Clear();
            shake = hitStop = flashLife = 0f;
            if (screenFlash != null) screenFlash.enabled = false;
        }

        public void Ring(Vector2 pos, float radius, Color color, float life = 0.35f, float startFraction = 0.2f) =>
            Spawn(Shapes.Ring, pos, radius * 2f * startFraction, radius * 2f, color, life, 1001, false);

        public void Blob(Vector2 pos, float size, Color color, float life = 0.25f) =>
            Spawn(Shapes.Circle, pos, size, size * 1.6f, color, life, 1000, false);

        /// A soft additive glow that swells and fades: the bright core of a big blast.
        public void Glow(Vector2 pos, float size, Color color, float life = 0.3f) =>
            Spawn(Shapes.Soft, pos, size * 0.6f, size * 1.5f, color, life, 1003, true);

        /// A bolt between two points that thins and fades (Chain Skills).
        public void Line(Vector2 from, Vector2 to, Color color, float life = 0.25f, float thickness = 0.14f)
        {
            var d = to - from;
            Spawn(Shapes.Square, (from + to) * 0.5f, 1f, 1f, color, life, 1001, false);
            var p = live[live.Count - 1];
            p.stretch = new Vector2(Mathf.Max(0.05f, d.magnitude), thickness);
            p.sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            p.sr.transform.localScale = new Vector3(p.stretch.x, p.stretch.y, 1f);
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        /// Tints the whole screen for a moment. Strength is the starting alpha.
        public void Flash(Color color, float strength, float life = 0.3f)
        {
            if (cam == null) return;
            if (screenFlash == null)
            {
                screenFlash = NewRenderer();
                screenFlash.name = "fx-screen";
                screenFlash.sprite = Shapes.Square;
                if (GlowMaterial != null) screenFlash.sharedMaterial = GlowMaterial;
                screenFlash.sortingOrder = 1050;
            }
            // A stronger flash wins over a fainter one still fading (measured the way it is drawn, fading squared).
            if (flashLife > 0f)
            {
                float left = 1f - Mathf.Clamp01(flashAge / flashLife);
                if (flashColor.a * left * left > strength) return;
            }
            flashColor = color.WithAlpha(strength);
            flashAge = 0f;
            flashLife = life;
            screenFlash.enabled = true;
        }

        /// Slows the Battle almost to a stop for a moment, so a big hit lands.
        public void HitStop(float seconds) => hitStop = Mathf.Max(hitStop, seconds);

        sealed class Floater
        {
            public TMPro.TextMeshPro text;
            public float age, life;
            public Color color;
            public Vector3 from;
            public float size;
        }

        readonly List<Floater> floaters = new List<Floater>();
        readonly Stack<TMPro.TextMeshPro> textPool = new Stack<TMPro.TextMeshPro>();
        readonly Stack<Floater> floaterPool = new Stack<Floater>();

        /// Words that pop up, rise and fade (Combo names and the like).
        public void Text(Vector2 pos, string words, Color color, float size = 1f, float life = 1.1f)
        {
            var t = textPool.Count > 0 ? textPool.Pop() : NewText();
            t.gameObject.SetActive(true);
            t.text = words;
            t.fontSize = 9f * size;
            t.color = color;
            // Words near either end of the lane are pulled in so they stay on screen.
            if (cam != null)
            {
                float half = cam.orthographicSize * cam.aspect - 3f * size;
                pos.x = Mathf.Clamp(pos.x, camHome.x - half, camHome.x + half);
            }
            // Words popping up at once in the same place stack instead of overlapping.
            foreach (var other in floaters)
                if (other.age < 0.5f && Mathf.Abs(other.from.x - pos.x) < 2.5f && Mathf.Abs(other.from.y - pos.y) < 0.6f) pos.y = other.from.y + 0.65f * size;
            t.transform.position = new Vector3(pos.x, pos.y, 0f);
            var fl = floaterPool.Count > 0 ? floaterPool.Pop() : new Floater();
            fl.text = t;
            fl.age = 0f;
            fl.life = life;
            fl.color = color;
            fl.from = t.transform.position;
            fl.size = size;
            floaters.Add(fl);
        }

        TMPro.TextMeshPro NewText()
        {
            var go = new GameObject("fx-text");
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TMPro.TextMeshPro>();
            var theme = UiKit.Theme;
            if (theme != null && (theme.fontOutlined != null || theme.font != null)) t.font = theme.fontOutlined != null ? theme.fontOutlined : theme.font;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(40, 28, 20, 255);
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.sortingOrder = 1100;
            t.rectTransform.sizeDelta = new Vector2(10f, 2.5f);
            return t;
        }

        void Spawn(Sprite sprite, Vector2 pos, float from, float to, Color color, float life, int order, bool glow)
        {
            var p = pool.Count > 0 ? pool.Pop() : new Pulse { sr = NewRenderer() };
            var sr = p.sr;
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var glowMat = glow ? GlowMaterial : null;
            sr.sharedMaterial = glowMat != null ? glowMat : spriteMaterial;
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.transform.localScale = Vector3.one * from;
            sr.color = color;
            p.age = 0f;
            p.life = life;
            p.startScale = from;
            p.endScale = to;
            p.color = color;
            p.stretch = Vector2.zero;
            live.Add(p);
        }

        /// The normal sprite material, remembered from the first renderer so pooled renderers can switch back to it.
        Material spriteMaterial;

        SpriteRenderer NewRenderer()
        {
            var go = new GameObject("fx");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (spriteMaterial == null) spriteMaterial = sr.sharedMaterial;
            return sr;
        }

        void Recycle(int i)
        {
            var p = live[i];
            p.sr.transform.rotation = Quaternion.identity;
            p.sr.gameObject.SetActive(false);
            pool.Push(p);
            live.RemoveAt(i);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            hitStop = Mathf.Max(0f, hitStop - dt);
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.age += dt;
                float k = Mathf.Clamp01(p.age / p.life);
                float ease = 1f - (1f - k) * (1f - k);
                p.sr.transform.localScale = p.stretch != Vector2.zero
                    ? new Vector3(p.stretch.x, p.stretch.y * (1f - k * 0.7f), 1f)
                    : Vector3.one * Mathf.Lerp(p.startScale, p.endScale, ease);
                var c = p.color;
                c.a *= 1f - k;
                p.sr.color = c;
                if (k >= 1f) Recycle(i);
            }
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.age += dt;
                float k = Mathf.Clamp01(f.age / f.life);
                float pop = k < 0.15f ? Mathf.Lerp(0.4f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) / 0.85f);
                f.text.transform.localScale = Vector3.one * pop;
                f.text.transform.position = f.from + new Vector3(0f, k * 0.8f, 0f);
                var c = f.color;
                c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                f.text.color = c;
                if (k >= 1f)
                {
                    f.text.gameObject.SetActive(false);
                    textPool.Push(f.text);
                    floaterPool.Push(f);
                    floaters.RemoveAt(i);
                }
            }
            if (cam == null) return;
            if (screenFlash != null && screenFlash.enabled)
            {
                flashAge += dt;
                float k = Mathf.Clamp01(flashAge / Mathf.Max(0.01f, flashLife));
                if (k >= 1f) { screenFlash.enabled = false; flashLife = 0f; }
                else
                {
                    float h = cam.orthographicSize * 2f + 2f;
                    screenFlash.transform.position = new Vector3(camHome.x, camHome.y, 0f);
                    screenFlash.transform.localScale = new Vector3(h * cam.aspect + 2f, h, 1f);
                    screenFlash.color = flashColor.WithAlpha(flashColor.a * (1f - k) * (1f - k));
                }
            }
            if (shake > 0f)
            {
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
                cam.transform.position = camHome + (Vector3)(Random.insideUnitCircle * shake);
            }
            else cam.transform.position = camHome;
        }
    }
}
