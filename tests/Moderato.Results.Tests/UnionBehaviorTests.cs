using System.Runtime.CompilerServices;

namespace Moderato.Results.Tests;

public class UnionBehaviorTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    public static TheoryData<string> States => new() { "ok", "err", "default" };

    private static Result<int, string> Make(string state) => state switch
    {
        "ok" => Ok(42),
        "err" => Err("boom"),
        _ => default,
    };

    [Theory]
    [MemberData(nameof(States))]
    public void AccessPattern_IsConsistentWithValue(string state)
    {
        var r = Make(state);

        // A miss leaves the out parameter at default, in every state.
        if (!r.TryGetValue(out Ok<int> missedOk)) Assert.Equal(default, missedOk);
        if (!r.TryGetValue(out Err<string> missedErr)) Assert.Equal(default, missedErr);

        if (state == "default")
        {
            Assert.False(r.HasValue);
            Assert.False(r.TryGetValue(out Ok<int> _));
            Assert.False(r.TryGetValue(out Err<string> _));
            Assert.Throws<InvalidOperationException>(() => r.Value);
            return;
        }

        var value = r.Value;
        Assert.True(r.HasValue);
        Assert.Equal(value is Ok<int>, r.TryGetValue(out Ok<int> ok));
        Assert.Equal(value is Err<string>, r.TryGetValue(out Err<string> err));
        if (value is Ok<int> boxedOk) Assert.Equal(boxedOk, ok);
        if (value is Err<string> boxedErr) Assert.Equal(boxedErr, err);
    }

    [Fact]
    public void UnionConversion_FromCaseTypes()
    {
        Result<int, string> ok = new Ok<int>(1);
        Result<int, string> err = new Err<string>("e");

        Assert.True(ok.IsOk);
        Assert.True(err.IsErr);
    }

    [Fact]
    public void Switch_IsExhaustiveOverOkAndErr()
    {
        // Compiles only because Ok/Err are exhaustive (warnings are errors).
        static string Describe(Result<int, string> r) => r switch
        {
            Ok<int>(var v) => $"ok {v}",
            Err<string>(var e) => $"err {e}",
        };

        Assert.Equal("ok 1", Describe(Ok(1)));
        Assert.Equal("err x", Describe(Err("x")));
    }

    [Fact]
    public void Switch_OnDefault_Throws()
    {
        Result<int, string> r = default;
        Assert.ThrowsAny<Exception>(() => r switch { Ok<int> => 1, Err<string> => 2 });
    }

    [Fact]
    public void SameTypeForValueAndError_IsNotConfused()
    {
        Result<int, int> ok = Result.Ok(1);
        Result<int, int> err = Result.Err(1);

        Assert.True(ok.IsOk);
        Assert.True(err.IsErr);
        Assert.NotEqual(ok, err);
        Assert.Equal("ok", ok switch { Ok<int> => "ok", Err<int> => "err" });
        Assert.Equal("err", err switch { Ok<int> => "ok", Err<int> => "err" });
    }

    [Fact]
    public void TypePattern_UsesUnionMatching()
    {
        var r = Ok(5);
        Assert.True(r is Ok<int> { Value: 5 });
        Assert.False(r is Err<string>);
    }

    [Fact]
    public void ImplementsIUnion()
    {
        IUnion union = Ok(3);
        Assert.Equal(new Ok<int>(3), union.Value);
    }

    [Fact]
    public void IsMarkedAsUnion()
    {
        Assert.True(Attribute.IsDefined(typeof(Result<int, string>), typeof(UnionAttribute)));
    }
}
