namespace LittleKeep.Rules
{
    /// Small deterministic random source (xorshift32), so a seed gives the same Battle on every device.
    public sealed class Rng
    {
        uint state;

        public Rng(int seed) { state = (uint)seed * 2654435761u + 0x9E3779B9u; if (state == 0) state = 1; }

        public uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        /// 0 (inclusive) to 1 (exclusive).
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);
        public float Range(float min, float max) => min + (max - min) * NextFloat();
        public int Range(int minInclusive, int maxExclusive) => minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
    }
}
