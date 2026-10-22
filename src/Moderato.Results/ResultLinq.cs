namespace Moderato.Results;

/// <summary>LINQ query syntax for <see cref="Result{T, E}"/>. The first Err short-circuits.</summary>
public static class ResultLinq
{
    /// <summary><c>select</c></summary>
    public static Result<U, E> Select<T, E, U>(this Result<T, E> source, Func<T, U> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return source.Map(selector);
    }

    /// <summary><c>from … from</c></summary>
    public static Result<U, E> SelectMany<T, E, U>(this Result<T, E> source, Func<T, Result<U, E>> selector)
        => source.AndThen(selector);

    /// <summary><c>from … from … select</c></summary>
    public static Result<V, E> SelectMany<T, E, U, V>(
        this Result<T, E> source,
        Func<T, Result<U, E>> selector,
        Func<T, U, V> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(resultSelector);
        if (source._state == Result<T, E>.StateOk)
        {
            var t = source._value;
            var next = selector(t);
            if (next._state == Result<U, E>.StateOk) return new Result<V, E>(new Ok<V>(resultSelector(t, next._value)));
            if (next._state == Result<U, E>.StateErr) return new Result<V, E>(new Err<E>(next._error));
            return ThrowHelper.ThrowUninitialized<Result<V, E>>();
        }

        if (source._state == Result<T, E>.StateErr) return new Result<V, E>(new Err<E>(source._error));
        return ThrowHelper.ThrowUninitialized<Result<V, E>>();
    }
}
