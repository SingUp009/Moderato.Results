namespace Moderato.Results;

public static partial class Result
{
    /// <summary>Runs <paramref name="func"/>, capturing exceptions as Err. Cancellation propagates.</summary>
    public static Result<T, Exception> Try<T>(Func<T> func)
    {
        try
        {
            return new Result<T, Exception>(new Ok<T>(func()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, Exception>(new Err<Exception>(ex));
        }
    }

    /// <summary>Runs <paramref name="func"/>, mapping exceptions to Err. Cancellation propagates.</summary>
    public static Result<T, E> Try<T, E>(Func<T> func, Func<Exception, E> errorMapper)
    {
        try
        {
            return new Result<T, E>(new Ok<T>(func()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, E>(new Err<E>(errorMapper(ex)));
        }
    }

    /// <summary>Runs <paramref name="func"/> without a closure, capturing exceptions as Err. Cancellation propagates.</summary>
    public static Result<T, Exception> Try<TState, T>(TState state, Func<TState, T> func)
    {
        try
        {
            return new Result<T, Exception>(new Ok<T>(func(state)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, Exception>(new Err<Exception>(ex));
        }
    }

    /// <summary>Runs <paramref name="func"/> without a closure, mapping exceptions to Err. Cancellation propagates.</summary>
    public static Result<T, E> Try<TState, T, E>(TState state, Func<TState, T> func, Func<Exception, E> errorMapper)
    {
        try
        {
            return new Result<T, E>(new Ok<T>(func(state)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, E>(new Err<E>(errorMapper(ex)));
        }
    }

    /// <summary>Awaits <paramref name="func"/>, capturing exceptions as Err. Cancellation propagates.</summary>
    public static async ValueTask<Result<T, Exception>> TryAsync<T>(Func<ValueTask<T>> func)
    {
        try
        {
            return new Result<T, Exception>(new Ok<T>(await func().ConfigureAwait(false)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, Exception>(new Err<Exception>(ex));
        }
    }

    /// <summary>Awaits <paramref name="func"/>, mapping exceptions to Err. Cancellation propagates.</summary>
    public static async ValueTask<Result<T, E>> TryAsync<T, E>(Func<ValueTask<T>> func, Func<Exception, E> errorMapper)
    {
        try
        {
            return new Result<T, E>(new Ok<T>(await func().ConfigureAwait(false)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, E>(new Err<E>(errorMapper(ex)));
        }
    }

    /// <summary>Awaits <paramref name="func"/> without a closure, capturing exceptions as Err. Cancellation propagates.</summary>
    public static async ValueTask<Result<T, Exception>> TryAsync<TState, T>(TState state, Func<TState, ValueTask<T>> func)
    {
        try
        {
            return new Result<T, Exception>(new Ok<T>(await func(state).ConfigureAwait(false)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, Exception>(new Err<Exception>(ex));
        }
    }

    /// <summary>Awaits <paramref name="func"/> without a closure, mapping exceptions to Err. Cancellation propagates.</summary>
    public static async ValueTask<Result<T, E>> TryAsync<TState, T, E>(TState state, Func<TState, ValueTask<T>> func, Func<Exception, E> errorMapper)
    {
        try
        {
            return new Result<T, E>(new Ok<T>(await func(state).ConfigureAwait(false)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new Result<T, E>(new Err<E>(errorMapper(ex)));
        }
    }
}
