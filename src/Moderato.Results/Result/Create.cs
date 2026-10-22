using System.Runtime.CompilerServices;

namespace Moderato.Results;

/// <summary>Factories and utilities for <see cref="Result{T, E}"/>.</summary>
public static partial class Result
{
    /// <summary>Creates an Ok case.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ok<T> Ok<T>(T value) => new(value);

    /// <summary>Creates an Ok case carrying <see cref="Unit"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ok<Unit> Ok() => default;

    /// <summary>Creates an Err case.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Err<E> Err<E>(E error) => new(error);
}
