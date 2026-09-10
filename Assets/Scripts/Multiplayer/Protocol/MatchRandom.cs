using System;

namespace TurnBasedGame.Multiplayer.Protocol
{
    [Serializable]
    public struct MatchRandomState
    {
        public uint Seed;
        public uint State;
        public ulong Sequence;
    }

    // Version 1: xorshift32, independent of UnityEngine.Random and System.Random versions.
    public sealed class MatchRandom
    {
        private MatchRandomState state;
        public MatchRandom(uint seed)
        {
            if (seed == 0) throw new ArgumentOutOfRangeException(nameof(seed));
            state = new MatchRandomState { Seed = seed, State = seed };
        }
        public MatchRandomState Capture() => state;
        public void Restore(MatchRandomState saved)
        {
            if (saved.Seed == 0 || saved.State == 0) throw new ArgumentException("Invalid RNG state.");
            state = saved;
        }
        private uint NextUInt()
        {
            uint x = state.State;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state.State = x;
            state.Sequence = checked(state.Sequence + 1);
            return x;
        }
        public float Value() => (NextUInt() >> 8) * (1f / 16777216f);
        public float Range(float min, float max) => min + (max - min) * Value();
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint bound = (uint)((long)maxExclusive - min);
            // xorshift32 emits 1..uint.MaxValue. Rejection avoids modulo bias.
            uint limit = uint.MaxValue - uint.MaxValue % bound;
            uint value;
            do { value = NextUInt(); } while (value > limit);
            return (int)(min + (long)((value - 1) % bound));
        }
    }
}
