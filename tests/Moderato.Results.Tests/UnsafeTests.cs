using Moderato.Results.Unsafe;

namespace Moderato.Results.Tests;

public class UnsafeTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    [Fact]
    public void Unwrap_ReturnsValueOrThrows()
    {
        Assert.Equal(2, Ok(2).Unwrap());

        var ex = Assert.Throws<InvalidOperationException>(() => Err("boom").Unwrap());
        Assert.Equal("Called Unwrap on Err(boom).", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void UnwrapErr_ReturnsErrorOrThrows()
    {
        Assert.Equal("boom", Err("boom").UnwrapErr());

        var ex = Assert.Throws<InvalidOperationException>(() => Ok(2).UnwrapErr());
        Assert.Equal("Called UnwrapErr on Ok(2).", ex.Message);
    }

    [Fact]
    public void Unwrap_KeepsExceptionErrorAsInner()
    {
        var error = new FormatException("bad");
        Result<int, Exception> r = Result.Err<Exception>(error);

        var ex = Assert.Throws<InvalidOperationException>(() => r.Unwrap());
        Assert.Same(error, ex.InnerException);
    }

    [Fact]
    public void Default_Throws()
    {
        Result<int, string> d = default;
        Assert.Throws<InvalidOperationException>(() => d.Unwrap());
        Assert.Throws<InvalidOperationException>(() => d.UnwrapErr());
    }
}
