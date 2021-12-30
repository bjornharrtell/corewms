
using System.Collections.Concurrent;

namespace CoreWms;

public static class EnumerableExtensions
{
    public static async Task<IEnumerable<TOut>> SelectAsync<TIn, TOut>(this IEnumerable<TIn> source,
            Func<TIn, Task<TOut>> body,
            int maxDegreeOfParallelism)
    {
        var bag = new ConcurrentBag<TOut>();
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };
        await Parallel.ForEachAsync(source, parallelOptions, async (e, token) =>
        {
            bag.Add(await body(e));
        });
        return bag;
    }

}