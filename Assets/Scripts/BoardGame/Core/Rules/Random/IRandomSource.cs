namespace GCCC.BoardGame.Core.Rules.Random
{
    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);

        double NextDouble();
    }
}
