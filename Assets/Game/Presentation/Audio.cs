using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Every sound the game makes. Screens and effects name a Sound; only Audio decides what it sounds like.
    public enum Sound
    {
        Tap, Attack, HeroShot, HeroSwing, Hit,
        SkillFire, SkillFrost, SkillSpark, SkillBloom, SkillPlain, Zap,
        Combo, Rally, KeepHit, HeroDefeated, KnockedOut, Recovered, CreatureFreed, Heal,
        Won, Lost, SkillUpgrade, LearnSkill, MasterForm, Befriend, Gold, Gems, NewFamiliar,
    }

    /// The one audio module: all sound goes through Play. Clips are stand-ins synthesised from tones and noise,
    /// so there is nothing to license; swap in recorded clips by giving Audio a clip per Sound (see Override).
    public static class Audio
    {
        const int Rate = 22050;
        const int Voices = 14;
        const string MuteKey = "littlekeep.muted";

        static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
        static readonly Dictionary<Sound, AudioClip> overrides = new Dictionary<Sound, AudioClip>();
        static readonly Dictionary<Sound, float> lastPlayed = new Dictionary<Sound, float>();
        static AudioSource[] sources;
        static GameObject host;
        static int next;
        static System.Random jitter = new System.Random(7);

        static bool? muted;

        /// Read from PlayerPrefs once, not on every sound.
        public static bool Muted
        {
            get => muted ?? (muted = PlayerPrefs.GetInt(MuteKey, 0) == 1).Value;
            set
            {
                muted = value;
                PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// Builds every clip up front, so the first Combo of a Battle doesn't stall while its sound is made.
        public static void Prewarm()
        {
            foreach (Sound s in Enum.GetValues(typeof(Sound))) ClipFor(s);
        }

        /// Use a recorded clip for a Sound instead of the synthesised stand-in.
        public static void Override(Sound sound, AudioClip clip)
        {
            if (clip != null) overrides[sound] = clip;
            else overrides.Remove(sound);
        }

        public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
        {
            if (Muted || !Application.isPlaying) return;
            float now = Time.unscaledTime;
            // The clock restarts with each play in the editor, so a time from the future means "long ago".
            if (lastPlayed.TryGetValue(sound, out var last) && now >= last && now - last < MinGap(sound)) return;
            lastPlayed[sound] = now;

            var clip = ClipFor(sound);
            if (clip == null) return;
            var source = NextSource();
            source.pitch = pitch * (1f + (float)(jitter.NextDouble() - 0.5) * 0.08f);
            source.PlayOneShot(clip, volume * Loudness(sound));
        }

        /// Busy sounds are spaced out so a full squad doesn't become a wall of noise.
        static float MinGap(Sound s)
        {
            switch (s)
            {
                case Sound.Attack: return 0.07f;
                case Sound.Hit: return 0.05f;
                case Sound.HeroShot:
                case Sound.HeroSwing: return 0.12f;
                case Sound.Heal: return 0.25f;
                case Sound.Tap: return 0.03f;
                default: return 0.02f;
            }
        }

        static float Loudness(Sound s)
        {
            switch (s)
            {
                case Sound.Attack: return 0.25f;
                case Sound.Hit: return 0.3f;
                case Sound.HeroShot:
                case Sound.HeroSwing: return 0.3f;
                case Sound.Heal: return 0.35f;
                case Sound.Tap: return 0.5f;
                case Sound.Combo:
                case Sound.Rally:
                case Sound.Won:
                case Sound.Lost: return 0.9f;
                default: return 0.6f;
            }
        }

        static AudioSource NextSource()
        {
            if (host == null)
            {
                host = new GameObject("Audio");
                UnityEngine.Object.DontDestroyOnLoad(host);
                sources = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    sources[i] = host.AddComponent<AudioSource>();
                    sources[i].playOnAwake = false;
                }
            }
            // Prefer a quiet voice, so re-pitching it can't bend a sound that is still playing.
            for (int i = 0; i < sources.Length; i++)
            {
                next = (next + 1) % sources.Length;
                if (!sources[next].isPlaying) return sources[next];
            }
            next = (next + 1) % sources.Length;
            return sources[next];
        }

        static AudioClip ClipFor(Sound s)
        {
            if (overrides.TryGetValue(s, out var o) && o != null) return o;
            // Clips survive play sessions without a domain reload but their data may not: use Unity's null check.
            if (clips.TryGetValue(s, out var c) && c != null) return c;
            c = Synth(s);
            clips[s] = c;
            return c;
        }

        // --- Synthesis ------------------------------------------------------------------------------

        enum Wave { Sine, Square, Triangle, Noise }

        sealed class Buffer
        {
            public readonly float[] data;
            public Buffer(float seconds) { data = new float[Mathf.Max(1, (int)(seconds * Rate))]; }
        }

        static readonly System.Random noise = new System.Random(11);

        /// Adds a note to the buffer: frequency sweeps from f0 to f1 with a short attack and an exponential decay.
        static void Note(Buffer b, float start, float length, float f0, float f1, Wave wave, float gain, float decay = 6f)
        {
            int from = (int)(start * Rate), count = (int)(length * Rate);
            double phase = 0;
            for (int i = 0; i < count && from + i < b.data.Length; i++)
            {
                float t = (float)i / count;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float p = (float)(phase - Math.Floor(phase));
                float v;
                switch (wave)
                {
                    case Wave.Square: v = p < 0.5f ? 1f : -1f; break;
                    case Wave.Triangle: v = 4f * Mathf.Abs(p - 0.5f) - 1f; break;
                    case Wave.Noise: v = (float)(noise.NextDouble() * 2 - 1); break;
                    default: v = Mathf.Sin(p * Mathf.PI * 2f); break;
                }
                float attack = Mathf.Clamp01(i / (Rate * 0.004f));
                float env = attack * Mathf.Exp(-decay * t);
                b.data[from + i] += v * env * gain;
            }
        }

        static void Arpeggio(Buffer b, float start, float step, Wave wave, float gain, params float[] freqs)
        {
            for (int i = 0; i < freqs.Length; i++) Note(b, start + i * step, step * 2.2f, freqs[i], freqs[i], wave, gain, 4f);
        }

        static AudioClip Synth(Sound s)
        {
            Buffer b;
            switch (s)
            {
                case Sound.Tap:
                    b = new Buffer(0.06f); Note(b, 0, 0.06f, 1200, 900, Wave.Triangle, 0.6f, 9f); break;
                case Sound.Attack:
                    b = new Buffer(0.08f); Note(b, 0, 0.08f, 900, 500, Wave.Triangle, 0.5f, 8f); break;
                case Sound.HeroShot:
                    b = new Buffer(0.12f); Note(b, 0, 0.12f, 500, 300, Wave.Square, 0.25f, 7f); Note(b, 0, 0.05f, 0, 0, Wave.Noise, 0.2f, 8f); break;
                case Sound.HeroSwing:
                    b = new Buffer(0.12f); Note(b, 0, 0.12f, 0, 0, Wave.Noise, 0.35f, 9f); Note(b, 0, 0.1f, 220, 140, Wave.Triangle, 0.3f, 8f); break;
                case Sound.Hit:
                    b = new Buffer(0.07f); Note(b, 0, 0.07f, 0, 0, Wave.Noise, 0.4f, 10f); Note(b, 0, 0.06f, 260, 160, Wave.Sine, 0.4f, 9f); break;
                case Sound.SkillFire:
                    b = new Buffer(0.35f); Note(b, 0, 0.35f, 0, 0, Wave.Noise, 0.45f, 5f); Note(b, 0, 0.3f, 180, 90, Wave.Triangle, 0.5f, 5f); break;
                case Sound.SkillFrost:
                    b = new Buffer(0.4f); Arpeggio(b, 0, 0.05f, Wave.Sine, 0.35f, 1568, 1976, 2349, 2637); break;
                case Sound.SkillSpark:
                    b = new Buffer(0.25f); Note(b, 0, 0.25f, 1800, 600, Wave.Square, 0.22f, 5f); Note(b, 0.03f, 0.2f, 0, 0, Wave.Noise, 0.15f, 8f); break;
                case Sound.SkillBloom:
                    b = new Buffer(0.4f); Note(b, 0, 0.4f, 330, 660, Wave.Sine, 0.45f, 4f); Note(b, 0.05f, 0.35f, 495, 990, Wave.Triangle, 0.25f, 4f); break;
                case Sound.SkillPlain:
                    b = new Buffer(0.25f); Note(b, 0, 0.25f, 700, 350, Wave.Triangle, 0.45f, 6f); break;
                case Sound.Zap:
                    b = new Buffer(0.12f); Note(b, 0, 0.12f, 2400, 900, Wave.Square, 0.18f, 7f); break;
                case Sound.Combo:
                    b = new Buffer(0.7f);
                    Note(b, 0, 0.5f, 0, 0, Wave.Noise, 0.4f, 4f);
                    Note(b, 0, 0.45f, 120, 60, Wave.Sine, 0.7f, 4f);
                    Arpeggio(b, 0.05f, 0.06f, Wave.Triangle, 0.3f, 784, 988, 1175, 1568);
                    break;
                case Sound.Rally:
                    b = new Buffer(0.8f); Arpeggio(b, 0, 0.09f, Wave.Square, 0.18f, 392, 523, 659, 784); Note(b, 0.36f, 0.4f, 784, 784, Wave.Triangle, 0.35f, 3f); break;
                case Sound.KeepHit:
                    b = new Buffer(0.35f); Note(b, 0, 0.35f, 110, 55, Wave.Sine, 0.8f, 5f); Note(b, 0, 0.15f, 0, 0, Wave.Noise, 0.35f, 7f); break;
                case Sound.HeroDefeated:
                    b = new Buffer(0.2f); Note(b, 0, 0.2f, 600, 1200, Wave.Triangle, 0.35f, 6f); Note(b, 0, 0.1f, 0, 0, Wave.Noise, 0.15f, 9f); break;
                case Sound.KnockedOut:
                    b = new Buffer(0.4f); Note(b, 0, 0.4f, 520, 180, Wave.Triangle, 0.45f, 3f); break;
                case Sound.Recovered:
                    b = new Buffer(0.3f); Arpeggio(b, 0, 0.06f, Wave.Triangle, 0.3f, 523, 659, 784); break;
                case Sound.CreatureFreed:
                case Sound.Befriend:
                    b = new Buffer(0.6f); Arpeggio(b, 0, 0.08f, Wave.Sine, 0.35f, 659, 784, 988, 1319); break;
                case Sound.Heal:
                    b = new Buffer(0.2f); Note(b, 0, 0.2f, 880, 1320, Wave.Sine, 0.25f, 5f); break;
                case Sound.Won:
                    b = new Buffer(1.2f); Arpeggio(b, 0, 0.12f, Wave.Triangle, 0.4f, 523, 659, 784, 1047); Note(b, 0.48f, 0.7f, 1047, 1047, Wave.Sine, 0.4f, 2f); break;
                case Sound.Lost:
                    b = new Buffer(1.2f); Arpeggio(b, 0, 0.18f, Wave.Triangle, 0.4f, 392, 349, 311); Note(b, 0.54f, 0.6f, 262, 247, Wave.Sine, 0.4f, 2.5f); break;
                case Sound.SkillUpgrade:
                    b = new Buffer(0.4f); Arpeggio(b, 0, 0.07f, Wave.Triangle, 0.35f, 587, 784, 988); break;
                case Sound.LearnSkill:
                    b = new Buffer(0.6f); Arpeggio(b, 0, 0.07f, Wave.Sine, 0.4f, 523, 659, 784, 1047, 1319); break;
                case Sound.MasterForm:
                    b = new Buffer(1f); Arpeggio(b, 0, 0.08f, Wave.Square, 0.16f, 523, 659, 784, 1047, 1319, 1568); Note(b, 0.48f, 0.5f, 1568, 1568, Wave.Sine, 0.35f, 2f); break;
                case Sound.Gold:
                    b = new Buffer(0.25f); Note(b, 0, 0.08f, 1976, 1976, Wave.Square, 0.15f, 5f); Note(b, 0.06f, 0.18f, 2637, 2637, Wave.Square, 0.15f, 4f); break;
                case Sound.Gems:
                    b = new Buffer(0.5f); Arpeggio(b, 0, 0.05f, Wave.Sine, 0.3f, 1319, 1568, 1976, 2637); break;
                case Sound.NewFamiliar:
                    b = new Buffer(1.1f); Arpeggio(b, 0, 0.1f, Wave.Triangle, 0.35f, 392, 523, 659, 784, 1047); Note(b, 0.5f, 0.6f, 1047, 1047, Wave.Sine, 0.35f, 2f); break;
                default:
                    return null;
            }
            // Keep everything below full scale.
            float peak = 0f;
            foreach (var v in b.data) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0.95f) for (int i = 0; i < b.data.Length; i++) b.data[i] *= 0.95f / peak;
            var clip = AudioClip.Create("sfx-" + s, b.data.Length, 1, Rate, false);
            clip.SetData(b.data, 0);
            return clip;
        }
    }
}
