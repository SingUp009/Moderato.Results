using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moderato.Results.Unsafe;

/// <summary>
/// Throwing accessors for <see cref="Result{T, E}"/>, meant for tests and prototypes.
/// They live in their own namespace so they are only in scope where it is imported.
/// </summary>
public static class ResultUnsafeExtensions
{
    /// <summary>Returns the value, or throws <see cref="InvalidOperationException"/> if Err.</summary>
    /// <remarks>An <see cref="Exception"/> error becomes the inner exception.</remarks>
    public static T Unwrap<T, E>(this Result<T, E> source)
    {
        if (source._state == Result<T, E>.StateOk) return source._value;
        if (source._state == Result<T, E>.StateErr) ThrowUnwrapOnErr(source._error);
        return ThrowHelper.ThrowUninitialized<T>();
    }

    /// <summary>Returns the error, or throws <see cref="InvalidOperationException"/> if Ok.</summary>
    public static E UnwrapErr<T, E>(this Result<T, E> source)
    {
        if (source._state == Result<T, E>.StateErr) return source._error;
        if (source._state == Result<T, E>.StateOk) ThrowUnwrapErrOnOk(source._value);
        return ThrowHelper.ThrowUninitialized<E>();
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwrapOnErr<E>(E error)
        => throw new InvalidOperationException($"Called Unwrap on Err({error}).", error as Exception);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwrapErrOnOk<T>(T value)
        => throw new InvalidOperationException($"Called UnwrapErr on Ok({value}).");
}
