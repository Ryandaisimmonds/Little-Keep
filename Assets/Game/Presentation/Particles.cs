using UnityEngine;

namespace LittleKeep.Game
{
    /// The kinds of particle effects are made of. Each has its own sprite and motion: embers rise, shards tumble and
    /// fall, sparks streak and stop short, petals drift.
    public enum Mote { Soft, Spark, Ember, Shard, Petal, Star }

    /// Glowing particles, one Unity particle system per Mote kind (so a busy Battle is a handful of draw calls).
    /// Everything runs on unscaled time, like the rest of Fx, so effects finish while the Battle is paused or slowed.
    public sealed class Particles
    {
        /// Caps the glow drawn over the screen: overlapping additive particles are what a phone's GPU feels most.
        public const int MaxPerKind = 250;

        static readonly int KindCount = System.Enum.GetValues(typeof(Mote)).Length;
        // Indexed by Mote: an array, not a dictionary, so the many lookups per frame allocate nothing.
        readonly ParticleSystem[] systems = new ParticleSystem[KindCount];
        // Shared by every Fx and kept across Battles (and play sessions in the editor), so none are leaked.
        static readonly Material[] materials = new Material[KindCount];
        readonly Transform parent;

        public Particles(Transform parent)
        {
            this.parent = parent;
        }

        /// A burst of `count` particles flying out from `pos`.
        /// speed: how fast they leave; spread: how far from the centre they start; bias: a push every particle gets
        /// (up for rising columns, down for confetti); swirl: sideways speed, so the burst spins (+ one way, - the other).
        public void Burst(Mote kind, Vector2 pos, int count, float speed, float size, Color a, Color b, float life,
            float spread = 0f, Vector2 bias = default, float swirl = 0f)
        {
            if (count <= 0) return;
            var ps = SystemFor(kind);
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            float spin = LookOf(kind).spin;
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitCircle;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                var unit = dir.normalized;
                var v = unit * speed * Random.Range(0.45f, 1f) + bias;
                if (swirl != 0f) v += new Vector2(-unit.y, unit.x) * swirl * Random.Range(0.7f, 1f);
                ep.position = new Vector3(pos.x + dir.x * spread, pos.y + dir.y * spread, 0f);
                ep.velocity = new Vector3(v.x, v.y, 0f);
                ep.startSize = size * Random.Range(0.7f, 1.3f);
                ep.startColor = Color.Lerp(a, b, Random.value);
                ep.startLifetime = life * Random.Range(0.7f, 1.2f);
                ep.rotation = Random.Range(0f, 360f);
                ep.angularVelocity = Random.Range(-spin, spin);
                ps.Emit(ep, 1);
            }
        }

        /// One particle left where something is, drifting a little: projectile trails.
        public void Trail(Mote kind, Vector2 pos, float size, Color color, float life)
        {
            var ps = SystemFor(kind);
            var jitter = Random.insideUnitCircle * 0.25f;
            var ep = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = false,
                position = new Vector3(pos.x, pos.y, 0f),
                velocity = new Vector3(jitter.x, jitter.y, 0f),
                startSize = size * Random.Range(0.8f, 1.2f),
                startColor = color,
                startLifetime = life,
                rotation = Random.Range(0f, 360f),
            };
            ps.Emit(ep, 1);
        }

        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var ps in systems) if (ps != null) n += ps.particleCount;
                return n;
            }
        }

        public void Clear()
        {
            foreach (var ps in systems) if (ps != null) ps.Clear();
        }

        ParticleSystem SystemFor(Mote kind)
        {
            var ps = systems[(int)kind];
            if (ps != null) return ps;
            return systems[(int)kind] = Build(kind);
        }

        ParticleSystem Build(Mote kind)
        {
            var go = new GameObject("fx-" + kind);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = MaxPerKind;
            main.startSpeed = 0f;
            var look = LookOf(kind);
            main.gravityModifier = look.gravity;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            // Drag: particles leave fast and slow down, which reads as a burst rather than a spray.
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1000f;
            limit.drag = look.drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, look.size);

            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            colour.color = new ParticleSystem.MinMaxGradient(Fade(look.hold));

            var sprite = SpriteFor(kind);
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.SetSprite(0, sprite);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialFor(kind, sprite);
            r.sortingOrder = 1002;
            r.alignment = ParticleSystemRenderSpace.View;
            if (kind == Mote.Spark)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.05f;
                r.lengthScale = 1.2f;
            }
            else r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        static Sprite SpriteFor(Mote kind)
        {
            var art = Fx.Art;
            Sprite chosen = null;
            if (art != null)
                switch (kind)
                {
                    case Mote.Soft: chosen = art.soft; break;
                    case Mote.Spark: chosen = art.spark; break;
                    case Mote.Ember: chosen = art.ember; break;
                    case Mote.Shard: chosen = art.shard; break;
                    case Mote.Petal: chosen = art.petal; break;
                    case Mote.Star: chosen = art.star; break;
                }
            if (chosen != null) return chosen;
            switch (kind)
            {
                case Mote.Shard: return Shapes.Shard;
                case Mote.Petal: return Shapes.Petal;
                case Mote.Star: return Shapes.Star;
                default: return Shapes.Soft;
            }
        }

        /// How each kind of particle moves and changes over its life.
        struct Look
        {
            /// Gravity: negative rises (embers), positive falls (shards).
            public float gravity, drag, spin;
            /// How long it stays at full strength before fading, as a fraction of its life.
            public float hold;
            public AnimationCurve size;
        }

        static readonly Look[] looks = new Look[KindCount];

        static Look LookOf(Mote kind)
        {
            var l = looks[(int)kind];
            if (l.size != null) return l;
            switch (kind)
            {
                case Mote.Ember:
                    l = new Look { gravity = -0.12f, drag = 1.6f, hold = 0.6f, size = Curve(0f, 0.6f, 0.2f, 1f, 1f, 0f) }; // heat rises
                    break;
                case Mote.Shard:
                    l = new Look { gravity = 0.3f, drag = 1.2f, spin = 540f, hold = 0.6f, size = Curve(0f, 1f, 0.7f, 0.9f, 1f, 0f) };
                    break;
                case Mote.Spark:
                    l = new Look { gravity = 0.08f, drag = 5f, hold = 0.6f, size = Curve(0f, 1f, 1f, 0.2f) };
                    break;
                case Mote.Petal:
                    l = new Look { gravity = 0.05f, drag = 3f, spin = 240f, hold = 0.6f, size = Curve(0f, 0.5f, 0.15f, 1f, 1f, 0.8f) };
                    break;
                case Mote.Star:
                    l = new Look { drag = 3.5f, spin = 90f, hold = 0.6f, size = Curve(0f, 0f, 0.15f, 1.3f, 0.4f, 0.9f, 1f, 0f) };
                    break;
                default:
                    l = new Look { drag = 2.5f, hold = 0.2f, size = Curve(0f, 0.7f, 0.25f, 1f, 1f, 0f) };
                    break;
            }
            return looks[(int)kind] = l;
        }

        /// A curve through (time, value) pairs.
        static AnimationCurve Curve(params float[] timeValue)
        {
            var keys = new Keyframe[timeValue.Length / 2];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(timeValue[i * 2], timeValue[i * 2 + 1]);
            return new AnimationCurve(keys);
        }

        static Material MaterialFor(Mote kind, Sprite sprite)
        {
            var m = materials[(int)kind];
            if (m != null && m.mainTexture == sprite.texture) return m;
            var glow = Fx.GlowMaterial;
            if (glow == null) return null;
            if (m == null) m = materials[(int)kind] = new Material(glow) { name = "fx-" + kind };
            m.mainTexture = sprite.texture;
            return m;
        }

        static Gradient Fade(float hold)
        {
            // Colour comes from each particle's start colour; this only fades it out.
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, hold), new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }
}
