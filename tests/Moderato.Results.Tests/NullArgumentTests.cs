namespace Moderato.Results.Tests;

public class NullArgumentTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

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

    [Fact]
    public void SyncMembers_RejectNullDelegates_InEveryState()
    {
        foreach (var r in new[] { Ok(1), Err("e") })
        {
            Assert.Throws<ArgumentNullException>("mapper", () => r.Map((Func<int, int>)null!));
            Assert.Throws<ArgumentNullException>("mapper", () => r.MapErr(0, (Func<int, string, int>)null!));
            Assert.Throws<ArgumentNullException>("binder", () => r.AndThen((Func<int, Result<int, string>>)null!));
            Assert.Throws<ArgumentNullException>("check", () => r.AndThrough((Func<int, Result<Unit, string>>)null!));
            Assert.Throws<ArgumentNullException>("binder", () => r.OrElse((Func<string, Result<int, string>>)null!));
            Assert.Throws<ArgumentNullException>("err", () => r.Match(static x => x, (Func<string, int>)null!));
            Assert.Throws<ArgumentNullException>("ok", () => r.Match(null!, static (string _) => { }));
            Assert.Throws<ArgumentNullException>("action", () => r.Tap((Action<int>)null!));
            Assert.Throws<ArgumentNullException>("action", () => r.TapErr((Action<string>)null!));
            Assert.Throws<ArgumentNullException>("fallback", () => r.UnwrapOrElse((Func<string, int>)null!));
        }
    }

    [Fact]
    public void Try_RejectsNullDelegates()
    {
        Assert.Throws<ArgumentNullException>("func", () => Result.Try((Func<int>)null!));
        Assert.Throws<ArgumentNullException>("errorMapper", () => Result.Try(static () => 1, (Func<Exception, string>)null!));
    }

    [Fact]
    public void AsyncMembers_ThrowBeforeReturningATask()
    {
        Assert.Throws<ArgumentNullException>("mapper", () => { _ = Ok(1).MapAsync((Func<int, ValueTask<int>>)null!); });
        Assert.Throws<ArgumentNullException>("check", () => { _ = Ok(1).AndThroughAsync((Func<int, ValueTask<Result<Unit, string>>>)null!); });
        Assert.Throws<ArgumentNullException>("mapper", () => { _ = Later(Ok(1)).Map((Func<int, int>)null!); });
        Assert.Throws<ArgumentNullException>("binder", () => { _ = Later(Ok(1)).AndThenAsync((Func<int, ValueTask<Result<int, string>>>)null!); });
        Assert.Throws<ArgumentNullException>("fallback", () => { _ = AsTask(Ok(1)).UnwrapOrElse((Func<string, int>)null!); });
        Assert.Throws<ArgumentNullException>("func", () => { _ = Result.TryAsync((Func<ValueTask<int>>)null!); });
        Assert.Throws<ArgumentNullException>("errorMapper", () => { _ = Result.TryAsync(static () => new ValueTask<int>(1), (Func<Exception, string>)null!); });
    }

    [Fact]
    public void Linq_RejectsNullSelectors()
    {
        Assert.Throws<ArgumentNullException>("selector", () => Ok(1).Select((Func<int, int>)null!));
        Assert.Throws<ArgumentNullException>("selector", () => { _ = Later(Ok(1)).Select((Func<int, int>)null!); });
        Assert.Throws<ArgumentNullException>("selector", () => { _ = AsTask(Ok(1)).Select((Func<int, int>)null!); });
        Assert.Throws<ArgumentNullException>("selector", () => Ok(1).SelectMany((Func<int, Result<int, string>>)null!, static (a, b) => a + b));
        Assert.Throws<ArgumentNullException>("resultSelector", () => { _ = Later(Ok(1)).SelectMany(static x => Ok(x), (Func<int, int, int>)null!); });
    }
}
