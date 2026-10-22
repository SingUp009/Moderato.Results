namespace Moderato.Results.Tests;

public class TryAsyncCancellationTests
{
    // What HttpClient throws on a timeout: a cancellation that the caller did not request.
    private static async ValueTask<int> TimesOut()
    {
        await Task.Yield();
        throw new TaskCanceledException("timeout", new TimeoutException());
    }

    [Fact]
    public async Task WithoutToken_EveryCancellationPropagates()
    {
        // This test is about calls without a token, so it must not pass the test's token (xUnit1051).
#pragma warning disable xUnit1051
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await Result.TryAsync(TimesOut));
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await Result.TryAsync(TimesOut, ex => ex.Message));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task WithToken_OtherCancellationBecomesErr()
    {
        using var cts = new CancellationTokenSource();

        var r = await Result.TryAsync(_ => TimesOut(), cts.Token);
        Assert.True(r.TryGetErr(out var ex));
        Assert.IsType<TimeoutException>(ex.InnerException);

        Assert.Equal<Result<int, string>>(Result.Err("timeout"), await Result.TryAsync(_ => TimesOut(), static ex => ex.Message, cts.Token));
        Assert.True((await Result.TryAsync(0, static (_, _) => TimesOut(), cts.Token)).IsErr);
    }

    [Fact]
    public async Task WithToken_CallerCancellationPropagates()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.TryAsync(async ct => { await Task.Delay(1000, ct); return 1; }, cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.TryAsync("s", static async (_, ct) => { await Task.Delay(1000, ct); return 1; }, static ex => ex.Message, cts.Token));
    }

    [Fact]
    public async Task WithToken_CallerCancellationThroughALinkedTokenPropagates()
    {
        using var cts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
        cts.Cancel();

        // The exception carries the linked token, not the caller's, yet the caller asked for it.
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Result.TryAsync<int>(_ => throw new OperationCanceledException(linked.Token), cts.Token));
    }

    [Fact]
    public async Task TokenIsPassedToFunc()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        Assert.True((await Result.TryAsync(ct => new ValueTask<bool>(ct == token), token)).UnwrapOr(false));
        Assert.True((await Result.TryAsync(token, static (t, ct) => new ValueTask<bool>(ct == t), token)).UnwrapOr(false));
    }

    [Fact]
    public async Task OtherExceptionsBecomeErr_WithOrWithoutToken()
    {
        using var cts = new CancellationTokenSource();
        static ValueTask<int> Fails() => throw new FormatException();

        Assert.True((await Result.TryAsync(Fails)).IsErr);
        Assert.True((await Result.TryAsync(_ => Fails(), cts.Token)).IsErr);
    }

    [Fact]
    public async Task AlreadyCanceledToken_StopsBeforeTheDelegateRuns()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var called = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.TryAsync(_ => { called = true; return new ValueTask<int>(1); }, cts.Token));
        Assert.False(called);
    }

    private static class Api
    {
        public static ValueTask<int> LoadAsync() => new(1);
        public static ValueTask<int> LoadAsync(CancellationToken cancellationToken) => new(2);
        public static ValueTask<int> LoadWithOptionalTokenAsync(CancellationToken cancellationToken = default) => new(3);
    }

    [Fact]
    public async Task MethodGroups_BindWithoutAmbiguity()
    {
        // A BCL-style pair of overloads must not make TryAsync ambiguous (CS0121).
        var token = TestContext.Current.CancellationToken;
        Assert.Equal(2, (await Result.TryAsync(Api.LoadAsync, token)).UnwrapOr(0));
        Assert.Equal(3, (await Result.TryAsync(Api.LoadWithOptionalTokenAsync, token)).UnwrapOr(0));
#pragma warning disable xUnit1051 // The token-less pair member is what this line checks.
        Assert.Equal(1, (await Result.TryAsync(Api.LoadAsync)).UnwrapOr(0));
#pragma warning restore xUnit1051
    }

    [Fact]
    public void TokenOverloads_RejectNullDelegates()
    {
        Assert.Throws<ArgumentNullException>("func", () => { _ = Result.TryAsync((Func<CancellationToken, ValueTask<int>>)null!, TestContext.Current.CancellationToken); });
        Assert.Throws<ArgumentNullException>("errorMapper", () => { _ = Result.TryAsync(static _ => new ValueTask<int>(1), (Func<Exception, string>)null!, TestContext.Current.CancellationToken); });
    }
}
