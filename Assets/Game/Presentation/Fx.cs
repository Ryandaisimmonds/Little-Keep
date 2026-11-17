using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Short-lived world effects: rings that grow and fade, flashes, screen shake.
    public sealed class Fx : MonoBehaviour
    {
        sealed class Pulse
        {
            public SpriteRenderer sr;
            public float age, life, startScale, endScale;
            public Color color;
            /// For lines: fixed length and thickness instead of growing.
            public Vector2 stretch;
        }

        readonly List<Pulse> live = new List<Pulse>();
        readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        Camera cam;
        Vector3 camHome;
        float shake;

        public void Init(Camera camera)
        {
            cam = camera;
            camHome = camera.transform.position;
        }

        public void Ring(Vector2 pos, float radius, Color color, float life = 0.35f, float startFraction = 0.2f) =>
            Spawn(Shapes.Ring, pos, radius * 2f * startFraction, radius * 2f, color, life, 1001);

        public void Blob(Vector2 pos, float size, Color color, float life = 0.25f) =>
            Spawn(Shapes.Circle, pos, size, size * 1.6f, color, life, 1000);

        /// A bolt between two points that thins and fades (Chain Skills).
        public void Line(Vector2 from, Vector2 to, Color color, float life = 0.25f)
        {
            var d = to - from;
            Spawn(Shapes.Square, (from + to) * 0.5f, 1f, 1f, color, life, 1001);
            var p = live[live.Count - 1];
            p.stretch = new Vector2(Mathf.Max(0.05f, d.magnitude), 0.14f);
            p.sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            p.sr.transform.localScale = new Vector3(p.stretch.x, p.stretch.y, 1f);
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        sealed class Floater
        {
            public TMPro.TextMeshPro text;
            public float age, life;
            public Color color;
            public Vector3 from;
        }

        readonly List<Floater> floaters = new List<Floater>();

        /// Words that pop up, rise and fade (Combo names and the like).
        public void Text(Vector2 pos, string words, Color color, float size = 1f, float life = 1.1f)
        {
            var go = new GameObject("fx-text");
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<TMPro.TextMeshPro>();
            var theme = UiKit.Theme;
            if (theme != null && (theme.fontOutlined != null || theme.font != null)) t.font = theme.fontOutlined != null ? theme.fontOutlined : theme.font;
            t.text = words;
            t.fontSize = 9f * size;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(40, 28, 20, 255);
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.color = color;
            t.sortingOrder = 1100;
            t.rectTransform.sizeDelta = new Vector2(8f, 2f);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            floaters.Add(new Floater { text = t, life = life, color = color, from = go.transform.position });
        }

        void Spawn(Sprite sprite, Vector2 pos, float from, float to, Color color, float life, int order)
        {
            var sr = pool.Count > 0 ? pool.Pop() : NewRenderer();
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.transform.localScale = Vector3.one * from;
            sr.color = color;
            live.Add(new Pulse { sr = sr, life = life, startScale = from, endScale = to, color = color });
        }

        SpriteRenderer NewRenderer()
        {
            var go = new GameObject("fx");
            go.transform.SetParent(transform, false);
            return go.AddComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
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
                if (k >= 1f)
                {
                    p.sr.transform.rotation = Quaternion.identity;
                    p.sr.gameObject.SetActive(false);
                    pool.Push(p.sr);
                    live.RemoveAt(i);
                }
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
                    Destroy(f.text.gameObject);
                    floaters.RemoveAt(i);
                }
            }
            if (cam == null) return;
            if (shake > 0f)
            {
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
                cam.transform.position = camHome + (Vector3)(Random.insideUnitCircle * shake);
            }
            else cam.transform.position = camHome;
        }
    }
}
