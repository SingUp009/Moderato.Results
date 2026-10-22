namespace Moderato.Results.Tests;

public class LambdaTypingTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);

    [Fact]
    public void ExplicitTypeArgument_OnInstanceMethods()
    {
        Assert.Equal(Ok(4), Ok(4).AndThen<int>(x => x > 0 ? Result.Ok(x) : Result.Err("neg")));
        Assert.Equal(Ok(4), Ok(4).AndThen<int>(x =>
        {
            if (x < 0) return Result.Err("neg");
            return Result.Ok(x);
        }));
    }

    [Fact]
    public async Task ExplicitReturnType_WorksEverywhere()
    {
        Assert.Equal(Ok(2), Ok(1).AndThen(Result<int, string> (x) => x > 0 ? Result.Ok(x * 2) : Result.Err("neg")));
        Assert.Equal(Ok(3), Ok(1).AndThen(2, static Result<int, string> (s, x) => Result.Ok(x + s)));
        Assert.Equal(Ok(2), await new ValueTask<Result<int, string>>(Ok(1)).AndThen(static Result<int, string> (x) => Result.Ok(x * 2)));
        Assert.Equal(Ok(2), await Ok(1).AndThenAsync(static async ValueTask<Result<int, string>> (x) =>
        {
            await Task.Yield();
            return Result.Ok(x * 2);
        }));
    }
}
