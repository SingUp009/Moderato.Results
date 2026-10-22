namespace Moderato.Results.Tests;

public class ResultTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);
    private static readonly Result<int, string> Default = default;

    [Fact]
    public void Accessors()
    {
        Assert.True(Ok(1).IsOk);
        Assert.False(Ok(1).IsErr);
        Assert.True(Err("e").IsErr);
        Assert.False(Default.IsOk);
        Assert.False(Default.IsErr);

        Assert.True(Ok(1).TryGetOk(out var v));
        Assert.Equal(1, v);
        Assert.False(Ok(1).TryGetErr(out _));
        Assert.True(Err("e").TryGetErr(out var e));
        Assert.Equal("e", e);
    }

    [Fact]
    public void UnitOk()
    {
        Result<Unit, string> r = Result.Ok();
        Assert.True(r.IsOk);
        Assert.Equal("Ok(())", r.ToString());
    }

    [Fact]
    public void Map()
    {
        Assert.Equal(Ok(2), Ok(1).Map(x => x * 2));
        Assert.Equal(Err("e"), Err("e").Map(x => x * 2));
        Assert.Throws<InvalidOperationException>(() => Default.Map(x => x));
    }

    [Fact]
    public void Map_State_MatchesPlain()
    {
        Assert.Equal(Ok(1).Map(x => x + 10), Ok(1).Map(10, static (s, x) => x + s));
        Assert.Equal(Err("e").Map(x => x + 10), Err("e").Map(10, static (s, x) => x + s));
    }

    [Fact]
    public void Map_ChangesType()
    {
        Result<string, string> r = Ok(5).Map(x => x.ToString());
        Assert.Equal("Ok(5)", r.ToString());
    }

    [Fact]
    public void MapErr()
    {
        Assert.Equal<Result<int, int>>(Result.Ok(1), Ok(1).MapErr(e => e.Length));
        Assert.Equal<Result<int, int>>(Result.Err(3), Err("abc").MapErr(e => e.Length));
        Assert.Equal(Err("abc").MapErr(e => e.Length), Err("abc").MapErr(1, static (s, e) => e.Length * s));
        Assert.Throws<InvalidOperationException>(() => Default.MapErr(e => e));
    }

    [Fact]
    public void AndThen()
    {
        static Result<int, string> Half(int x) => x % 2 == 0 ? Result.Ok(x / 2) : Result.Err($"{x} is odd");

        Assert.Equal(Ok(2), Ok(4).AndThen(Half));
        Assert.Equal(Err("3 is odd"), Ok(3).AndThen(Half));
        Assert.Equal(Err("e"), Err("e").AndThen(Half));
        Assert.Equal(Ok(4).AndThen(Half), Ok(4).AndThen(0, static (_, x) => Half(x)));
        Assert.Throws<InvalidOperationException>(() => Default.AndThen(Half));
    }

    [Fact]
    public void AndThen_DoesNotInvokeBinderOnErr()
    {
        var called = false;
        Err("e").AndThen(x => { called = true; return Ok(x); });
        Assert.False(called);
    }

    [Fact]
    public void OrElse()
    {
        Assert.Equal(Ok(1), Ok(1).OrElse(_ => Ok(0)));
        Assert.Equal(Ok(0), Err("e").OrElse(_ => Ok(0)));
        Assert.Equal<Result<int, int>>(Result.Err(1), Err("e").OrElse<int>(e => Result.Err(e.Length)));
        Assert.Equal(Ok(7), Err("e").OrElse(7, static Result<int, string> (s, _) => Result.Ok(s)));
        Assert.Throws<InvalidOperationException>(() => Default.OrElse(_ => Ok(0)));
    }

    [Fact]
    public void Match()
    {
        Assert.Equal("ok 1", Ok(1).Match(x => $"ok {x}", e => $"err {e}"));
        Assert.Equal("err e", Err("e").Match(x => $"ok {x}", e => $"err {e}"));
        Assert.Equal("ok! 1", Ok(1).Match("!", static (s, x) => $"ok{s} {x}", static (s, e) => $"err{s} {e}"));
        Assert.Throws<InvalidOperationException>(() => Default.Match(x => x, e => 0));
    }

    [Fact]
    public void Match_Actions()
    {
        var log = new List<string>();
        Ok(1).Match(x => log.Add($"ok {x}"), e => log.Add($"err {e}"));
        Err("e").Match(log, static (l, x) => l.Add($"ok {x}"), static (l, e) => l.Add($"err {e}"));
        Assert.Equal(new[] { "ok 1", "err e" }, log);
        Assert.Throws<InvalidOperationException>(() => Default.Match(_ => { }, _ => { }));
    }

    [Fact]
    public void Tap_And_TapErr()
    {
        var log = new List<string>();
        var r1 = Ok(1).Tap(x => log.Add($"tap {x}")).TapErr(e => log.Add($"tapErr {e}"));
        var r2 = Err("e").Tap(log, static (l, x) => l.Add($"tap {x}")).TapErr(log, static (l, e) => l.Add($"tapErr {e}"));

        Assert.Equal(Ok(1), r1);
        Assert.Equal(Err("e"), r2);
        Assert.Equal(new[] { "tap 1", "tapErr e" }, log);
        Assert.Throws<InvalidOperationException>(() => Default.Tap(_ => { }));
        Assert.Throws<InvalidOperationException>(() => Default.TapErr(_ => { }));
    }

    [Fact]
    public void Unwrap()
    {
        Assert.Equal(1, Ok(1).UnwrapOr(0));
        Assert.Equal(0, Err("e").UnwrapOr(0));
        Assert.Equal(3, Err("abc").UnwrapOrElse(e => e.Length));
        Assert.Equal(6, Err("abc").UnwrapOrElse(2, static (s, e) => e.Length * s));
        Assert.Throws<InvalidOperationException>(() => Default.UnwrapOr(0));
    }

    [Fact]
    public void Equality()
    {
        Assert.True(Ok(1) == Ok(1));
        Assert.True(Ok(1) != Ok(2));
        Assert.True(Ok(1) != Err("1"));
        Assert.True(Default == default(Result<int, string>));
        Assert.Equal(Ok(1).GetHashCode(), Ok(1).GetHashCode());
        Assert.True(Ok(1).Equals((object)Ok(1)));
        Assert.False(Ok(1).Equals((object)1));
    }

    [Fact]
    public void Formatting()
    {
        Assert.Equal("Ok(1)", Ok(1).ToString());
        Assert.Equal("Err(e)", Err("e").ToString());
        Assert.Equal("Result(default)", Default.ToString());
        Assert.Equal("Ok(1)", new Ok<int>(1).ToString());
        Assert.Equal("Err(e)", new Err<string>("e").ToString());
    }
}
