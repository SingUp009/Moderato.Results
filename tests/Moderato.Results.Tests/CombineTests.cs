namespace Moderato.Results.Tests;

public class CombineTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    [Fact]
    public void Combine_Tuple_AllOk()
    {
        Result<string, string> s = Result.Ok("a");
        var r = Result.Combine(Ok(1), s, Ok(3));
        Assert.True(r.TryGetOk(out var t));
        Assert.Equal((1, "a", 3), t);
    }

    [Fact]
    public void Combine_Tuple_FirstErrWins()
    {
        var r = Result.Combine(Ok(1), Err("first"), Err("second"));
        Assert.True(r.TryGetErr(out var e));
        Assert.Equal("first", e);
    }

    [Fact]
    public void Combine_Tuple_EightArity()
    {
        var r = Result.Combine(Ok(1), Ok(2), Ok(3), Ok(4), Ok(5), Ok(6), Ok(7), Ok(8));
        Assert.True(r.TryGetOk(out var t));
        Assert.Equal(8, t.Item8);
    }

    [Fact]
    public void Combine_Sequence()
    {
        Assert.Equal(new[] { 1, 2, 3 }, Result.Combine(new[] { Ok(1), Ok(2), Ok(3) }).UnwrapOr([]));
        Assert.Equal("b", Result.Combine(new[] { Ok(1), Err("b"), Err("c") }).Match(_ => "", e => e));
        Assert.Equal(new[] { 1, 2 }, Result.Combine(Enumerable.Range(1, 2).Select(Ok)).UnwrapOr([]));
        Assert.Empty(Result.Combine(Array.Empty<Result<int, string>>()).UnwrapOr([1]));
        Assert.Equal(new[] { 1, 2 }, Result.Combine(new List<Result<int, string>> { Ok(1), Ok(2) }).UnwrapOr([]));
        Assert.Equal("b", Result.Combine(new List<Result<int, string>> { Ok(1), Err("b") }).Match(_ => "", e => e));
    }

    [Fact]
    public void Combine_ThrowsOnDefault()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Combine(Ok(1), default(Result<int, string>)));
        Assert.Throws<InvalidOperationException>(() => Result.Combine(new[] { Ok(1), default }));
    }

    [Fact]
    public void Combine_ThrowsOnDefaultAfterErr()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Combine(Err("e"), default(Result<int, string>)));
        Assert.Throws<InvalidOperationException>(() => Result.Combine(new[] { Err("e"), default }));
        Assert.Throws<InvalidOperationException>(() => Result.Combine(new List<Result<int, string>> { Err("e"), default }));
        Assert.Throws<InvalidOperationException>(() => Result.Combine(new[] { Err("e"), default }.Select(r => r)));
    }

    [Fact]
    public void CombineAll_CollectsEveryError()
    {
        var tuple = Result.CombineAll(Err("a"), Ok(2), Err("c"));
        Assert.True(tuple.TryGetErr(out var errors));
        Assert.Equal(new[] { "a", "c" }, errors);

        var seq = Result.CombineAll(new List<Result<int, string>> { Err("x"), Ok(1), Err("y") });
        Assert.Equal(new[] { "x", "y" }, seq.Match(_ => [], e => e));

        var ok = Result.CombineAll(Ok(1), Ok(2));
        Assert.True(ok.TryGetOk(out var values));
        Assert.Equal((1, 2), values);
    }
}
