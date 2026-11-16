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

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

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
                p.sr.transform.localScale = Vector3.one * Mathf.Lerp(p.startScale, p.endScale, ease);
                var c = p.color;
                c.a *= 1f - k;
                p.sr.color = c;
                if (k >= 1f)
                {
                    p.sr.gameObject.SetActive(false);
                    pool.Push(p.sr);
                    live.RemoveAt(i);
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
