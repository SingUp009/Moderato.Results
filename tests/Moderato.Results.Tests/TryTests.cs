namespace Moderato.Results.Tests;

public class TryTests
{
    [Fact]
    public void Try_CapturesExceptions()
    {
        var ok = Result.Try(() => int.Parse("12"));
        var err = Result.Try(() => int.Parse("x"));

        Assert.Equal(12, ok.UnwrapOr(0));
        Assert.True(err.TryGetErr(out var ex));
        Assert.IsType<FormatException>(ex);
    }

    [Fact]
    public void Try_WithMapperAndState()
    {
        Assert.Equal("FormatException", Result.Try(() => int.Parse("x"), ex => ex.GetType().Name).Match(_ => "", e => e));
        Assert.Equal(12, Result.Try("12", static s => int.Parse(s)).UnwrapOr(0));
        Assert.Equal("bad", Result.Try("x", static s => int.Parse(s), _ => "bad").Match(_ => "", e => e));
    }
}
