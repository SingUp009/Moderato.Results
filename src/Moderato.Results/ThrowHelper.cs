using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moderato.Results;

internal static class ThrowHelper
{
    private const string UninitializedMessage =
        "The Result is default (uninitialized). Create results with Result.Ok(...) or Result.Err(...).";

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUninitialized() => throw new InvalidOperationException(UninitializedMessage);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static TReturn ThrowUninitialized<TReturn>() => throw new InvalidOperationException(UninitializedMessage);
}
