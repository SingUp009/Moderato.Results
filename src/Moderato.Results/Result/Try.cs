using System.Diagnostics.CodeAnalysis;

namespace Moderato.Results;

public static partial class Result
{
    /// <summary>Runs <paramref name="func"/>, capturing exceptions as Err. Cancellation propagates.</summary>
    public static Result<T, Exception> Try<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
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
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
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
        ArgumentNullException.ThrowIfNull(func);
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
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
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
    public static ValueTask<Result<T, Exception>> TryAsync<T>(Func<ValueTask<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryCore(func, static (f, _) => f(), Identity, default);
    }

    /// <summary>Awaits <paramref name="func"/>, mapping exceptions to Err. Cancellation propagates.</summary>
    public static ValueTask<Result<T, E>> TryAsync<T, E>(Func<ValueTask<T>> func, Func<Exception, E> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryCore(func, static (f, _) => f(), errorMapper, default);
    }

    /// <summary>Awaits <paramref name="func"/> without a closure, capturing exceptions as Err. Cancellation propagates.</summary>
    public static ValueTask<Result<T, Exception>> TryAsync<TState, T>(TState state, Func<TState, ValueTask<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryCore((state, func), static (s, _) => s.func(s.state), Identity, default);
    }

    /// <summary>Awaits <paramref name="func"/> without a closure, mapping exceptions to Err. Cancellation propagates.</summary>
    public static ValueTask<Result<T, E>> TryAsync<TState, T, E>(TState state, Func<TState, ValueTask<T>> func, Func<Exception, E> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryCore((state, func), static (s, _) => s.func(s.state), errorMapper, default);
    }

    /// <summary>Awaits <paramref name="func"/> with <paramref name="cancellationToken"/>, capturing exceptions as Err. Cancellation of <paramref name="cancellationToken"/> propagates.</summary>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = OptionalTokenJustification)]
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = OptionalTokenJustification)]
    public static ValueTask<Result<T, Exception>> TryAsync<T>(Func<CancellationToken, ValueTask<T>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryCore(func, static (f, ct) => f(ct), Identity, cancellationToken);
    }

    /// <summary>Awaits <paramref name="func"/> with <paramref name="cancellationToken"/>, mapping exceptions to Err. Cancellation of <paramref name="cancellationToken"/> propagates.</summary>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = OptionalTokenJustification)]
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = OptionalTokenJustification)]
    public static ValueTask<Result<T, E>> TryAsync<T, E>(Func<CancellationToken, ValueTask<T>> func, Func<Exception, E> errorMapper, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryCore(func, static (f, ct) => f(ct), errorMapper, cancellationToken);
    }

    /// <summary>Awaits <paramref name="func"/> with <paramref name="cancellationToken"/> and without a closure, capturing exceptions as Err. Cancellation of <paramref name="cancellationToken"/> propagates.</summary>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = OptionalTokenJustification)]
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = OptionalTokenJustification)]
    public static ValueTask<Result<T, Exception>> TryAsync<TState, T>(TState state, Func<TState, CancellationToken, ValueTask<T>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryCore(state, func, Identity, cancellationToken);
    }

    /// <summary>Awaits <paramref name="func"/> with <paramref name="cancellationToken"/> and without a closure, mapping exceptions to Err. Cancellation of <paramref name="cancellationToken"/> propagates.</summary>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = OptionalTokenJustification)]
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads", Justification = OptionalTokenJustification)]
    public static ValueTask<Result<T, E>> TryAsync<TState, T, E>(TState state, Func<TState, CancellationToken, ValueTask<T>> func, Func<Exception, E> errorMapper, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryCore(state, func, errorMapper, cancellationToken);
    }

    private const string OptionalTokenJustification = "Overloads differ by delegate shape; the trailing token follows the repository's async convention.";

    private static readonly Func<Exception, Exception> Identity = static ex => ex;

    private static async ValueTask<Result<T, E>> TryCore<TState, T, E>(
        TState state, Func<TState, CancellationToken, ValueTask<T>> func, Func<Exception, E> errorMapper, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return new Result<T, E>(new Ok<T>(await func(state, cancellationToken).ConfigureAwait(false)));
        }
        catch (Exception ex) when (!IsCallerCancellation(ex, cancellationToken))
        {
            return new Result<T, E>(new Err<E>(errorMapper(ex)));
        }
    }

    // The caller's token decides, not the exception's: callees such as HttpClient throw with a linked token.
    private static bool IsCallerCancellation(Exception ex, CancellationToken cancellationToken)
        => ex is OperationCanceledException && (!cancellationToken.CanBeCanceled || cancellationToken.IsCancellationRequested);
}
