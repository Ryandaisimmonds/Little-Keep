using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// The Battle's effect recipes, built from Fx's pieces. Each Element has its own particles, and a Skill's
    /// effect grows with it in three clear steps: base, upgraded, Master Form. Combos and Rally are the loudest
    /// moments on screen.
    public static class Effects
    {
        /// How an Element's effects look: its main particle, an accent particle and two colours.
        public struct Style
        {
            public Mote main, accent;
            public Color a, b;
        }

        public static Style StyleOf(Element e, Color plain)
        {
            switch (e)
            {
                case Element.Fire: return new Style { main = Mote.Ember, accent = Mote.Soft, a = ElementLook.Color(e), b = new Color(1f, 0.85f, 0.35f) };
                case Element.Frost: return new Style { main = Mote.Shard, accent = Mote.Star, a = ElementLook.Color(e), b = new Color(0.9f, 0.97f, 1f) };
                case Element.Spark: return new Style { main = Mote.Spark, accent = Mote.Star, a = ElementLook.Color(e), b = Color.white };
                case Element.Bloom: return new Style { main = Mote.Petal, accent = Mote.Soft, a = ElementLook.Color(e), b = new Color(1f, 0.75f, 0.85f) };
                default: return new Style { main = Mote.Soft, accent = Mote.Star, a = plain, b = Color.Lerp(plain, Color.white, 0.5f) };
            }
        }

        // One row per growth step: base, upgraded, Master Form.
        static readonly int[] MainCount = { 10, 24, 46 };
        static readonly int[] AccentCount = { 0, 10, 24 };
        static readonly float[] Speed = { 1.6f, 2.1f, 2.8f };
        static readonly float[] MoteSize = { 0.22f, 0.29f, 0.38f };
        static readonly float[] MoteLife = { 0.45f, 0.6f, 0.8f };
        static readonly float[] Shake = { 0.03f, 0.07f, 0.14f };

        /// An area Skill landing: a burst of radius r.
        public static void SkillBurst(Fx fx, Vector2 pos, float r, Element element, Color plain, SkillGrowth growth)
        {
            int g = (int)growth;
            var s = StyleOf(element, plain);
            var p = fx.Particles;

            fx.Ring(pos, r, s.a, 0.4f + g * 0.1f);
            fx.Blob(pos, r * 1.2f, s.a.WithAlpha(0.3f + g * 0.12f), 0.3f + g * 0.1f);
            p.Burst(s.main, pos, MainCount[g], r * Speed[g], MoteSize[g], s.a, s.b, MoteLife[g], r * 0.3f);
            if (g >= 1)
            {
                // Upgraded: a second ring, a glowing core and accent twinkles.
                fx.Ring(pos, r * 1.3f, s.b, 0.5f, 0.3f);
                fx.Glow(pos, r * 1.2f, s.a.WithAlpha(0.7f), 0.3f);
                p.Burst(s.accent, pos, AccentCount[g], r * Speed[g] * 0.8f, MoteSize[g] * 1.2f, s.b, Color.white, MoteLife[g], r * 0.5f);
            }
            if (g >= 2)
            {
                // Master Form: a white shockwave, a column of light rising out of the blast and a flash of its colour.
                fx.Ring(pos, r * 1.7f, Color.white.WithAlpha(0.9f), 0.6f, 0.4f);
                fx.Glow(pos, r * 1.8f, Color.Lerp(s.a, Color.white, 0.4f).WithAlpha(0.55f), 0.25f);
                p.Burst(Mote.Soft, pos, 18, r * 0.6f, 0.35f, s.a, s.b, 0.9f, r * 0.4f, new Vector2(0f, r * 2.2f));
                p.Burst(Mote.Star, pos, 12, r * 3f, 0.45f, Color.white, s.b, 0.7f);
                fx.Flash(s.a, 0.18f, 0.35f);
            }
            fx.Shake(Shake[g]);
        }

        /// The caster as it casts: a few motes at base, a ring at upgraded, a glow and starburst at Master Form.
        public static void Cast(Fx fx, Vector2 pos, Element element, Color plain, SkillGrowth growth)
        {
            int g = (int)growth;
            var s = StyleOf(element, plain);
            var p = fx.Particles;
            p.Burst(s.main, pos, 4 + g * 6, 1.6f + g * 0.6f, MoteSize[g] * 0.8f, s.a, s.b, 0.4f);
            if (g >= 1) fx.Ring(pos, 0.6f + g * 0.25f, s.a, 0.3f);
            if (g >= 2)
            {
                fx.Glow(pos, 1.4f, s.a.WithAlpha(0.8f), 0.3f);
                p.Burst(Mote.Star, pos, 10, 2.6f, 0.35f, Color.white, s.b, 0.5f);
            }
        }

        /// A hit from an Element or a Skill: a splash of the Element's particles, bigger as the Skill grows
        /// (so a Volley's or Chain's every hit escalates, not just area Skills).
        public static void Splash(Fx fx, Vector2 pos, Element element, Color plain, SkillGrowth growth)
        {
            int g = (int)growth;
            var s = StyleOf(element, plain);
            fx.Blob(pos, 0.5f + g * 0.2f, s.a.WithAlpha(0.8f), 0.2f + g * 0.05f);
            fx.Particles.Burst(s.main, pos, SplashCount[g], 2.2f + g * 0.6f, MoteSize[g] * 0.8f, s.a, s.b, 0.35f + g * 0.1f);
            if (g >= 1) fx.Ring(pos, 0.45f + g * 0.15f, s.b, 0.25f, 0.3f);
            if (g >= 2)
            {
                fx.Glow(pos, 1f, Color.Lerp(s.a, Color.white, 0.4f).WithAlpha(0.6f), 0.2f);
                fx.Particles.Burst(Mote.Star, pos, 4, 2.8f, 0.3f, Color.white, s.b, 0.4f);
            }
        }
        static readonly int[] SplashCount = { 5, 9, 14 };

        /// How many trail particles a Skill projectile leaves per second, by growth (plain shots leave none).
        public static float TrailRate(bool fromSkill, SkillGrowth growth) => fromSkill ? TrailPerSecond[(int)growth] : 0f;
        static readonly float[] TrailPerSecond = { 10f, 24f, 50f };

        public static void TrailBit(Fx fx, Vector2 pos, Element element, Color plain, SkillGrowth growth)
        {
            int g = (int)growth;
            var s = StyleOf(element, plain);
            // Embers, petals and shards look right trailing; sparks and plain Skills leave a soft glow.
            var kind = s.main == Mote.Spark ? Mote.Soft : s.main;
            fx.Particles.Trail(kind, pos, 0.16f + g * 0.06f, Color.Lerp(s.a, s.b, Random.value), 0.25f + g * 0.08f);
        }

        /// A bolt between two Heroes (Chain), thicker, longer-lasting and showering more sparks as the Skill grows.
        public static void Zap(Fx fx, Vector2 from, Vector2 to, Element element, Color plain, SkillGrowth growth)
        {
            int g = (int)growth;
            var s = StyleOf(element, plain);
            fx.Line(from, to, s.a, 0.25f + g * 0.07f, 0.14f + g * 0.08f);
            fx.Line(from, to, Color.white.WithAlpha(0.9f), 0.15f + g * 0.05f, 0.05f + g * 0.03f);
            fx.Blob(to, 0.6f + g * 0.25f, s.a.WithAlpha(0.8f), 0.2f);
            fx.Particles.Burst(Mote.Spark, to, 6 + g * 6, 4f + g, 0.14f + g * 0.04f, s.a, Color.white, 0.25f + g * 0.05f);
            if (g >= 2) fx.Glow(to, 1.1f, Color.Lerp(s.a, Color.white, 0.4f).WithAlpha(0.6f), 0.2f);
        }

        /// Two Elements meeting: a two-coloured spiral, both Elements' particles, a white flash and a moment's hit-stop.
        public static void Combo(Fx fx, Vector2 pos, float r, Element first, Element second, string name)
        {
            var a = StyleOf(first, Color.white);
            var b = StyleOf(second, Color.white);
            var mid = Color.Lerp(a.a, b.a, 0.5f);
            var p = fx.Particles;

            fx.HitStop(0.09f);
            fx.Flash(mid, 0.28f, 0.4f);
            fx.Glow(pos, r * 2f, Color.white.WithAlpha(0.9f), 0.3f);
            fx.Blob(pos, r * 1.3f, mid.WithAlpha(0.55f), 0.45f);
            fx.Ring(pos, r * 1.2f, a.a, 0.5f, 0.1f);
            fx.Ring(pos, r * 1.6f, b.a, 0.7f, 0.15f);
            fx.Ring(pos, r * 2.1f, Color.white.WithAlpha(0.85f), 0.8f, 0.3f);
            // The two Elements spin round each other in opposite directions.
            p.Burst(Mote.Soft, pos, 28, r * 2.2f, 0.32f, a.a, a.b, 0.7f, r * 0.2f, default, r * 3f);
            p.Burst(Mote.Soft, pos, 28, r * 2.2f, 0.32f, b.a, b.b, 0.7f, r * 0.2f, default, -r * 3f);
            p.Burst(a.main, pos, 18, r * 2.6f, 0.3f, a.a, a.b, 0.7f);
            p.Burst(b.main, pos, 18, r * 2.6f, 0.3f, b.a, b.b, 0.7f);
            p.Burst(Mote.Star, pos, 14, r * 3.4f, 0.45f, Color.white, mid, 0.7f);
            fx.Text(pos + new Vector2(0f, 1f), name, Color.Lerp(mid, Color.white, 0.25f), 1.15f, 1.1f);
            fx.Shake(0.28f);
        }

        static readonly Color[] Confetti =
        {
            new Color(1f, 0.55f, 0.8f), new Color(1f, 0.85f, 0.35f), new Color(0.5f, 0.85f, 1f), new Color(0.6f, 0.95f, 0.45f), Color.white,
        };

        /// The Keeper's Rally: a flash of the Rally colour, rings sweeping down the lane, stars rising off every
        /// Fighter and confetti falling over the field.
        public static void Rally(Fx fx, Vector2 keeper, List<Vector2> fighters, BattleTuning lane, Color rally)
        {
            var p = fx.Particles;
            fx.HitStop(0.06f);
            fx.Flash(rally, 0.35f, 0.5f);
            fx.Ring(keeper, 6f, rally, 0.6f, 0.05f);
            fx.Ring(keeper, 9f, Color.white.WithAlpha(0.8f), 0.8f, 0.1f);
            fx.Glow(keeper, 2.4f, rally, 0.4f);
            p.Burst(Mote.Star, keeper, 36, 5f, 0.5f, rally, new Color(1f, 0.9f, 0.5f), 0.9f, 0.3f, new Vector2(0f, 2f));
            for (int i = 0; i < fighters.Count; i++)
            {
                fx.Ring(fighters[i], 0.9f, rally, 0.4f, 0.2f);
                p.Burst(Mote.Star, fighters[i], 10, 1.6f, 0.35f, Color.white, rally, 0.8f, 0.2f, new Vector2(0f, 2.4f));
            }
            for (int i = 0; i < 60; i++)
            {
                float x = Random.Range(lane.KeepX, lane.KeepX + lane.LaneLength * 0.75f);
                var c = Confetti[Random.Range(0, Confetti.Length)];
                p.Burst(Mote.Petal, new Vector2(x, lane.LaneHalfHeight + Random.Range(0.6f, 1.6f)), 1, 0.8f, 0.3f, c, c, 1.6f, 0f, new Vector2(0f, -1.6f));
            }
            fx.Text(keeper + new Vector2(1.6f, 1.4f), "Rally!", Color.Lerp(rally, Color.white, 0.3f), 1.5f, 1.2f);
            fx.Shake(0.18f);
        }

        /// A puff of smoke and a few stars: a Hero beaten, a Fighter knocked out.
        public static void Poof(Fx fx, Vector2 pos, Color tint, int stars)
        {
            fx.Particles.Burst(Mote.Soft, pos, 10, 1.8f, 0.4f, tint, Color.white.WithAlpha(0.6f), 0.5f, 0.2f, new Vector2(0f, 0.6f));
            if (stars > 0) fx.Particles.Burst(Mote.Star, pos, stars, 2.4f, 0.3f, Color.white, new Color(1f, 0.9f, 0.55f), 0.5f);
        }
    }
}
