namespace WorkDesignBlazor.Data;

using System;

public static class DataHelper
{
    private static readonly Random Random = new();

    public static IEnumerable<int> CreateRandomData(int count, int min, int max)
    {
        var factors = new int[count];
        lock (Random)
        {
            for (var i = 0; i < count; i++)
            {
                factors[i] = Random.Next(min, max);
            }
        }

        return factors;
    }

    public static int CreateRandom(int min, int max)
    {
        lock (Random)
        {
            return Random.Next(min, max);
        }
    }
}
