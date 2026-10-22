namespace Moderato.Results.Tests;

public class AndThroughTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);
    private static readonly Result<int, string> Default = default;

    private static Result<Unit, string> Positive(int x) => x > 0 ? Result.Ok() : Result.Err($"{x} is not positive");
    private static Result<string, string> Describe(int x) => Result.Ok($"#{x}");

    private static async ValueTask<Result<Unit, string>> PositiveLater(int x)
    {
        await Task.Yield();
        return Positive(x);
    }

    [Fact]
    public void KeepsValueWhenCheckPasses()
    {
        Assert.Equal(Ok(1), Ok(1).AndThrough(Positive));
        Assert.Equal(Ok(1), Ok(1).AndThrough(Describe));
        Assert.Equal(Ok(1), Ok(1).AndThrough(0, static (min, x) => x > min ? Result.Ok() : Result.Err("small")));
    }

    [Fact]
    public void ReturnsCheckError()
    {
        Assert.Equal(Err("-1 is not positive"), Ok(-1).AndThrough(Positive));
        Assert.Equal(Err("small"), Ok(1).AndThrough(5, static (min, x) => x > min ? Result.Ok() : Result.Err("small")));
    }

    [Fact]
    public void BranchingLambda_InfersWithoutTypeArguments()
    {
        Assert.Equal(Err("odd"), Ok(3).AndThrough(x => x % 2 == 0 ? Result.Ok() : Result.Err("odd")));
    }

    [Fact]
    public void SkipsCheckOnErr()
    {
        var called = false;
        Assert.Equal(Err("e"), Err("e").AndThrough(x => { called = true; return Positive(x); }));
        Assert.False(called);
        Assert.Throws<InvalidOperationException>(() => Default.AndThrough(Positive));
    }

    [Fact]
    public async Task Async()
    {
        Assert.Equal(Ok(1), await Ok(1).AndThroughAsync(PositiveLater));
        Assert.Equal(Err("0 is not positive"), await Ok(0).AndThroughAsync(PositiveLater));
        Assert.Equal(Err("e"), await Err("e").AndThroughAsync(PositiveLater));
        Assert.Equal(Ok(2), await Ok(2).AndThroughAsync(1, static async (min, x) =>
        {
            await Task.Yield();
            return x > min ? Result.Ok() : Result.Err("small");
        }));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValueTaskAndTaskReceivers(bool completed)
    {
        ValueTask<Result<int, string>> Source(int v) => completed ? new(Ok(v)) : Later(Ok(v));

        Assert.Equal(Ok(1), await Source(1).AndThrough(Positive));
        Assert.Equal(Err("-1 is not positive"), await Source(-1).AndThroughAsync(PositiveLater));
        Assert.Equal(Ok(1), await Source(1).AndThrough(Describe));
        Assert.Equal(Ok(3), await AsTask(Ok(3)).AndThrough(2, static (min, x) => x > min ? Result.Ok() : Result.Err("small")));
        Assert.Equal(Err("-2 is not positive"), await AsTask(Ok(-2)).AndThroughAsync(PositiveLater));
    }

    private static async ValueTask<Result<int, string>> Later(Result<int, string> r)
    {
        await Task.Yield();
        return r;
    }

    private static async Task<Result<int, string>> AsTask(Result<int, string> r)
    {
        await Task.Yield();
        return r;
    }
}
