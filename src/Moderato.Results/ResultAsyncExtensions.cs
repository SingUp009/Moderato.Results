namespace Moderato.Results;

/// <summary>Async operations on a <see cref="Result{T, E}"/>.</summary>
public static class ResultAsyncExtensions
{
    /// <summary>Maps the value asynchronously.</summary>
    public static ValueTask<Result<U, E>> MapAsync<T, E, U>(this Result<T, E> source, Func<T, ValueTask<U>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (source._state == Result<T, E>.StateOk) return WrapOk<U, E>(mapper(source._value));
        if (source._state == Result<T, E>.StateErr) return new(new Result<U, E>(new Err<E>(source._error)));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<U, E>>>();
    }

    /// <summary>Maps the value asynchronously without a closure.</summary>
    public static ValueTask<Result<U, E>> MapAsync<T, E, TState, U>(this Result<T, E> source, TState state, Func<TState, T, ValueTask<U>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (source._state == Result<T, E>.StateOk) return WrapOk<U, E>(mapper(state, source._value));
        if (source._state == Result<T, E>.StateErr) return new(new Result<U, E>(new Err<E>(source._error)));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<U, E>>>();
    }

    /// <summary>Maps the error asynchronously.</summary>
    public static ValueTask<Result<T, F>> MapErrAsync<T, E, F>(this Result<T, E> source, Func<E, ValueTask<F>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (source._state == Result<T, E>.StateOk) return new(new Result<T, F>(new Ok<T>(source._value)));
        if (source._state == Result<T, E>.StateErr) return WrapErr<T, F>(mapper(source._error));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, F>>>();
    }

    /// <summary>Maps the error asynchronously without a closure.</summary>
    public static ValueTask<Result<T, F>> MapErrAsync<T, E, TState, F>(this Result<T, E> source, TState state, Func<TState, E, ValueTask<F>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (source._state == Result<T, E>.StateOk) return new(new Result<T, F>(new Ok<T>(source._value)));
        if (source._state == Result<T, E>.StateErr) return WrapErr<T, F>(mapper(state, source._error));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, F>>>();
    }

    /// <summary>Chains an async fallible operation on the value.</summary>
    public static ValueTask<Result<U, E>> AndThenAsync<T, E, U>(this Result<T, E> source, Func<T, ValueTask<Result<U, E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (source._state == Result<T, E>.StateOk) return binder(source._value);
        if (source._state == Result<T, E>.StateErr) return new(new Result<U, E>(new Err<E>(source._error)));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<U, E>>>();
    }

    /// <summary>Chains an async fallible operation on the value without a closure.</summary>
    public static ValueTask<Result<U, E>> AndThenAsync<T, E, TState, U>(this Result<T, E> source, TState state, Func<TState, T, ValueTask<Result<U, E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (source._state == Result<T, E>.StateOk) return binder(state, source._value);
        if (source._state == Result<T, E>.StateErr) return new(new Result<U, E>(new Err<E>(source._error)));
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<U, E>>>();
    }

    /// <summary>Runs an async fallible check on the value, keeping the value if it passes.</summary>
    public static ValueTask<Result<T, E>> AndThroughAsync<T, E, U>(this Result<T, E> source, Func<T, ValueTask<Result<U, E>>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (source._state == Result<T, E>.StateOk) return Through(check(source._value), source);
        if (source._state == Result<T, E>.StateErr) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    /// <summary>Runs an async fallible check on the value, keeping the value if it passes.</summary>
    public static ValueTask<Result<T, E>> AndThroughAsync<T, E>(this Result<T, E> source, Func<T, ValueTask<Result<Unit, E>>> check)
        => source.AndThroughAsync<T, E, Unit>(check);

    /// <summary>Runs an async fallible check on the value without a closure, keeping the value if it passes.</summary>
    public static ValueTask<Result<T, E>> AndThroughAsync<T, E, TState, U>(this Result<T, E> source, TState state, Func<TState, T, ValueTask<Result<U, E>>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (source._state == Result<T, E>.StateOk) return Through(check(state, source._value), source);
        if (source._state == Result<T, E>.StateErr) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    /// <summary>Runs an async fallible check on the value without a closure, keeping the value if it passes.</summary>
    public static ValueTask<Result<T, E>> AndThroughAsync<T, E, TState>(this Result<T, E> source, TState state, Func<TState, T, ValueTask<Result<Unit, E>>> check)
        => source.AndThroughAsync<T, E, TState, Unit>(state, check);

    /// <summary>Recovers from the error asynchronously.</summary>
    public static ValueTask<Result<T, F>> OrElseAsync<T, E, F>(this Result<T, E> source, Func<E, ValueTask<Result<T, F>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (source._state == Result<T, E>.StateOk) return new(new Result<T, F>(new Ok<T>(source._value)));
        if (source._state == Result<T, E>.StateErr) return binder(source._error);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, F>>>();
    }

    /// <summary>Recovers from the error asynchronously without a closure.</summary>
    public static ValueTask<Result<T, F>> OrElseAsync<T, E, TState, F>(this Result<T, E> source, TState state, Func<TState, E, ValueTask<Result<T, F>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (source._state == Result<T, E>.StateOk) return new(new Result<T, F>(new Ok<T>(source._value)));
        if (source._state == Result<T, E>.StateErr) return binder(state, source._error);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, F>>>();
    }

    /// <summary>Folds into a single value asynchronously.</summary>
    public static ValueTask<R> MatchAsync<T, E, R>(this Result<T, E> source, Func<T, ValueTask<R>> ok, Func<E, ValueTask<R>> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (source._state == Result<T, E>.StateOk) return ok(source._value);
        if (source._state == Result<T, E>.StateErr) return err(source._error);
        return ThrowHelper.ThrowUninitialized<ValueTask<R>>();
    }

    /// <summary>Folds into a single value asynchronously without a closure.</summary>
    public static ValueTask<R> MatchAsync<T, E, TState, R>(this Result<T, E> source, TState state, Func<TState, T, ValueTask<R>> ok, Func<TState, E, ValueTask<R>> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (source._state == Result<T, E>.StateOk) return ok(state, source._value);
        if (source._state == Result<T, E>.StateErr) return err(state, source._error);
        return ThrowHelper.ThrowUninitialized<ValueTask<R>>();
    }

    /// <summary>Runs an async side effect on the value.</summary>
    public static ValueTask<Result<T, E>> TapAsync<T, E>(this Result<T, E> source, Func<T, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (source._state == Result<T, E>.StateOk) return Then(action(source._value), source);
        if (source._state == Result<T, E>.StateErr) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    /// <summary>Runs an async side effect on the value without a closure.</summary>
    public static ValueTask<Result<T, E>> TapAsync<T, E, TState>(this Result<T, E> source, TState state, Func<TState, T, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (source._state == Result<T, E>.StateOk) return Then(action(state, source._value), source);
        if (source._state == Result<T, E>.StateErr) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    /// <summary>Runs an async side effect on the error.</summary>
    public static ValueTask<Result<T, E>> TapErrAsync<T, E>(this Result<T, E> source, Func<E, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (source._state == Result<T, E>.StateErr) return Then(action(source._error), source);
        if (source._state == Result<T, E>.StateOk) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    /// <summary>Runs an async side effect on the error without a closure.</summary>
    public static ValueTask<Result<T, E>> TapErrAsync<T, E, TState>(this Result<T, E> source, TState state, Func<TState, E, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (source._state == Result<T, E>.StateErr) return Then(action(state, source._error), source);
        if (source._state == Result<T, E>.StateOk) return new(source);
        return ThrowHelper.ThrowUninitialized<ValueTask<Result<T, E>>>();
    }

    private static ValueTask<Result<U, E>> WrapOk<U, E>(ValueTask<U> task)
    {
        if (task.IsCompletedSuccessfully) return new(new Result<U, E>(new Ok<U>(task.Result)));
        return Await(task);

        static async ValueTask<Result<U, E>> Await(ValueTask<U> task)
            => new Result<U, E>(new Ok<U>(await task.ConfigureAwait(false)));
    }

    private static ValueTask<Result<T, F>> WrapErr<T, F>(ValueTask<F> task)
    {
        if (task.IsCompletedSuccessfully) return new(new Result<T, F>(new Err<F>(task.Result)));
        return Await(task);

        static async ValueTask<Result<T, F>> Await(ValueTask<F> task)
            => new Result<T, F>(new Err<F>(await task.ConfigureAwait(false)));
    }

    private static ValueTask<Result<T, E>> Through<T, E, U>(ValueTask<Result<U, E>> task, Result<T, E> source)
    {
        if (task.IsCompletedSuccessfully) return new(source.Keep(task.Result));
        return Await(task, source);

        static async ValueTask<Result<T, E>> Await(ValueTask<Result<U, E>> task, Result<T, E> source)
            => source.Keep(await task.ConfigureAwait(false));
    }

    private static ValueTask<Result<T, E>> Then<T, E>(ValueTask task, Result<T, E> result)
    {
        if (task.IsCompletedSuccessfully)
        {
            task.GetAwaiter().GetResult();
            return new(result);
        }

        return Await(task, result);

        static async ValueTask<Result<T, E>> Await(ValueTask task, Result<T, E> result)
        {
            await task.ConfigureAwait(false);
            return result;
        }
    }
}
