namespace Moderato.Results;

/// <summary>The value of an operation that returns nothing.</summary>
public readonly record struct Unit
{
    /// <summary>The only <see cref="Unit"/> value.</summary>
    public static Unit Value => default;

    /// <inheritdoc/>
    public override string ToString() => "()";
}
