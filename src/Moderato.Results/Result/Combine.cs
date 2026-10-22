using System.Runtime.InteropServices;

namespace Moderato.Results;

public static partial class Result
{
    /// <summary>Ok with all values, or the first Err.</summary>
    public static Result<T[], E> Combine<T, E>(ReadOnlySpan<Result<T, E>> results)
    {
        var firstErr = -1;
        for (var i = 0; i < results.Length; i++)
        {
            ref readonly var r = ref results[i];
            r.ThrowIfDefault();
            if (firstErr < 0 && r._state == Result<T, E>.StateErr) firstErr = i;
        }

        if (firstErr >= 0) return new Result<T[], E>(new Err<E>(results[firstErr]._error));

        var values = results.Length == 0 ? [] : new T[results.Length];
        for (var i = 0; i < results.Length; i++) values[i] = results[i]._value;
        return new Result<T[], E>(new Ok<T[]>(values));
    }

    /// <summary>Ok with all values, or the first Err.</summary>
    public static Result<T[], E> Combine<T, E>(IEnumerable<Result<T, E>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results is Result<T, E>[] array) return Combine((ReadOnlySpan<Result<T, E>>)array);
        if (results is List<Result<T, E>> list) return Combine((ReadOnlySpan<Result<T, E>>)CollectionsMarshal.AsSpan(list));

        var buffer = new List<Result<T, E>>(results);
        return Combine((ReadOnlySpan<Result<T, E>>)CollectionsMarshal.AsSpan(buffer));
    }

    /// <summary>Ok with all values, or Err with every error.</summary>
    public static Result<T[], E[]> CombineAll<T, E>(ReadOnlySpan<Result<T, E>> results)
    {
        var errorCount = 0;
        for (var i = 0; i < results.Length; i++)
        {
            ref readonly var r = ref results[i];
            r.ThrowIfDefault();
            if (r._state == Result<T, E>.StateErr) errorCount++;
        }

        if (errorCount == 0)
        {
            var values = results.Length == 0 ? [] : new T[results.Length];
            for (var i = 0; i < results.Length; i++) values[i] = results[i]._value;
            return new Result<T[], E[]>(new Ok<T[]>(values));
        }

        var errors = new E[errorCount];
        var j = 0;
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i]._state == Result<T, E>.StateErr) errors[j++] = results[i]._error;
        }

        return new Result<T[], E[]>(new Err<E[]>(errors));
    }

    /// <summary>Ok with all values, or Err with every error.</summary>
    public static Result<T[], E[]> CombineAll<T, E>(IEnumerable<Result<T, E>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results is Result<T, E>[] array) return CombineAll((ReadOnlySpan<Result<T, E>>)array);
        if (results is List<Result<T, E>> list) return CombineAll((ReadOnlySpan<Result<T, E>>)CollectionsMarshal.AsSpan(list));

        var buffer = new List<Result<T, E>>(results);
        return CombineAll(CollectionsMarshal.AsSpan(buffer));
    }
}
