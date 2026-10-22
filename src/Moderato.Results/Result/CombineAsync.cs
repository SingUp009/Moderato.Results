using System.Runtime.ExceptionServices;

namespace Moderato.Results;

public static partial class Result
{
    /// <summary>Awaits every result; Ok with all values, or the first Err.</summary>
    /// <remarks>The sequence is enumerated before anything is awaited, so lazily created tasks run concurrently.</remarks>
    public static ValueTask<Result<T[], E>> CombineAsync<T, E>(IEnumerable<ValueTask<Result<T, E>>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return Core(results as ValueTask<Result<T, E>>[] ?? results.ToArray());

        static async ValueTask<Result<T[], E>> Core(ValueTask<Result<T, E>>[] tasks)
            => Combine((ReadOnlySpan<Result<T, E>>)await AwaitAll(tasks).ConfigureAwait(false));
    }

    /// <summary>Awaits every result; Ok with all values, or the first Err.</summary>
    /// <remarks>The sequence is enumerated before anything is awaited, so lazily created tasks run concurrently.</remarks>
    public static ValueTask<Result<T[], E>> CombineAsync<T, E>(IEnumerable<Task<Result<T, E>>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return Core(Task.WhenAll(results));

        static async ValueTask<Result<T[], E>> Core(Task<Result<T, E>[]> all)
            => Combine((ReadOnlySpan<Result<T, E>>)await all.ConfigureAwait(false));
    }

    /// <summary>Awaits every result; Ok with all values, or Err with every error.</summary>
    /// <remarks>The sequence is enumerated before anything is awaited, so lazily created tasks run concurrently.</remarks>
    public static ValueTask<Result<T[], E[]>> CombineAllAsync<T, E>(IEnumerable<ValueTask<Result<T, E>>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return Core(results as ValueTask<Result<T, E>>[] ?? results.ToArray());

        static async ValueTask<Result<T[], E[]>> Core(ValueTask<Result<T, E>>[] tasks)
            => CombineAll((ReadOnlySpan<Result<T, E>>)await AwaitAll(tasks).ConfigureAwait(false));
    }

    /// <summary>Awaits every result; Ok with all values, or Err with every error.</summary>
    /// <remarks>The sequence is enumerated before anything is awaited, so lazily created tasks run concurrently.</remarks>
    public static ValueTask<Result<T[], E[]>> CombineAllAsync<T, E>(IEnumerable<Task<Result<T, E>>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return Core(Task.WhenAll(results));

        static async ValueTask<Result<T[], E[]>> Core(Task<Result<T, E>[]> all)
            => CombineAll((ReadOnlySpan<Result<T, E>>)await all.ConfigureAwait(false));
    }

    // Awaits every task, even after one throws, then rethrows the first exception.
    private static async ValueTask<Result<T, E>[]> AwaitAll<T, E>(ValueTask<Result<T, E>>[] tasks)
    {
        var results = new Result<T, E>[tasks.Length];
        ExceptionDispatchInfo? error = null;
        for (var i = 0; i < tasks.Length; i++)
        {
            try
            {
                results[i] = await tasks[i].ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error ??= ExceptionDispatchInfo.Capture(ex);
            }
        }

        error?.Throw();
        return results;
    }
}
