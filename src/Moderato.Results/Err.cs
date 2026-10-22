namespace Moderato.Results;

/// <summary>The failure case of <see cref="Result{T, E}"/>.</summary>
public readonly record struct Err<E>(E Error)
{
    /// <inheritdoc/>
    public override string ToString() => $"Err({Error})";
}
