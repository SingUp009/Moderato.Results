namespace Moderato.Results;

/// <summary>The success case of <see cref="Result{T, E}"/>.</summary>
public readonly record struct Ok<T>(T Value)
{
    /// <inheritdoc/>
    public override string ToString() => $"Ok({Value})";
}
