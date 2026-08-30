using System;

namespace GCCC.BoardGame.Core.Rules.Random
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random random;

        public SystemRandomSource() : this(new System.Random())
        {
        }

        public SystemRandomSource(System.Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }
    }
}
