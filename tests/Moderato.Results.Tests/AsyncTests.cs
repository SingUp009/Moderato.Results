namespace Moderato.Results.Tests;

public class AsyncTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    private static ValueTask<Result<int, string>> Sync(Result<int, string> r) => new(r);

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

    public static TheoryData<bool> Completion => new() { true, false };

    private static ValueTask<Result<int, string>> Source(Result<int, string> r, bool completed)
        => completed ? Sync(r) : Later(r);

    [Fact]
    public async Task SyncReceiver_MapAsync_AndThenAsync()
    {
        Assert.Equal(Ok(2), await Ok(1).MapAsync(async x => { await Task.Yield(); return x * 2; }));
        Assert.Equal(Err("e"), await Err("e").MapAsync(x => new ValueTask<int>(x * 2)));
        Assert.Equal(Ok(11), await Ok(1).MapAsync(10, static (s, x) => new ValueTask<int>(x + s)));

        Assert.Equal(Ok(3), await Ok(1).AndThenAsync(x => Sync(Ok(x + 2))));
        Assert.Equal(Err("no"), await Ok(1).AndThenAsync(_ => Later(Err("no"))));
        Assert.Equal(Ok(5), await Ok(1).AndThenAsync(4, static (s, x) => Sync(Ok(x + s))));
    }

    [Fact]
    public async Task SyncReceiver_MapErrAsync_OrElseAsync_MatchAsync()
    {
        Assert.Equal<Result<int, int>>(Result.Err(3), await Err("abc").MapErrAsync(e => new ValueTask<int>(e.Length)));
        Assert.Equal(Ok(0), await Err("e").OrElseAsync(_ => Later(Ok(0))));
        Assert.Equal("ok 1", await Ok(1).MatchAsync(x => new ValueTask<string>($"ok {x}"), e => new ValueTask<string>(e)));
        Assert.Equal("E!", await Err("E").MatchAsync("!", static (s, x) => new ValueTask<string>($"{x}"), static (s, e) => new ValueTask<string>(e + s)));
    }

    [Fact]
    public async Task SyncReceiver_TapAsync()
    {
        var log = new List<string>();
        var r = await Ok(1).TapAsync(async x => { await Task.Yield(); log.Add($"tap {x}"); });
        var e = await Err("e").TapErrAsync(x => { log.Add($"err {x}"); return ValueTask.CompletedTask; });
        Assert.Equal(Ok(1), r);
        Assert.Equal(Err("e"), e);
        Assert.Equal(new[] { "tap 1", "err e" }, log);
    }

    [Theory]
    [MemberData(nameof(Completion))]
    public async Task ValueTaskReceiver_Pipeline(bool completed)
    {
        var r = await Source(Ok(1), completed)
            .Map(x => x + 1)
            .AndThen(x => Ok(x * 10))
            .AndThenAsync(x => Later(Ok(x + 1)))
            .MapAsync(x => new ValueTask<int>(x * 2));
        Assert.Equal(Ok(42), r);

        var err = await Source(Err("e"), completed)
            .Map(x => x + 1)
            .MapErr(e => e.ToUpperInvariant());
        Assert.Equal(Err("E"), err);
    }

    [Theory]
    [MemberData(nameof(Completion))]
    public async Task ValueTaskReceiver_StateOverloads(bool completed)
    {
        Assert.Equal(Ok(11), await Source(Ok(1), completed).Map(10, static (s, x) => x + s));
        Assert.Equal(Ok(11), await Source(Ok(1), completed).AndThen(10, static (s, x) => Ok(x + s)));
        Assert.Equal("1!", await Source(Ok(1), completed).Match("!", static (s, x) => $"{x}{s}", static (s, e) => e));
    }

    [Theory]
    [MemberData(nameof(Completion))]
    public async Task ValueTaskReceiver_TerminalOps(bool completed)
    {
        Assert.Equal("ok 1", await Source(Ok(1), completed).Match(x => $"ok {x}", e => e));
        Assert.Equal(0, await Source(Err("e"), completed).UnwrapOr(0));
        Assert.Equal(1, await Source(Err("e"), completed).UnwrapOrElse(e => e.Length));

        var log = new List<string>();
        await Source(Ok(1), completed).Match(x => log.Add($"ok {x}"), e => log.Add(e));
        await Source(Ok(2), completed).Tap(x => log.Add($"tap {x}"));
        Assert.Equal(new[] { "ok 1", "tap 2" }, log);
    }

    [Theory]
    [MemberData(nameof(Completion))]
    public async Task ValueTaskReceiver_OrElse(bool completed)
    {
        Assert.Equal(Ok(9), await Source(Err("e"), completed).OrElse(_ => Ok(9)));
        Assert.Equal(Ok(9), await Source(Err("e"), completed).OrElseAsync(_ => Later(Ok(9))));
        Assert.Equal(Ok(1), await Source(Ok(1), completed).OrElse(_ => Ok(9)));
    }

    [Fact]
    public async Task TaskReceiver_Pipeline()
    {
        var r = await AsTask(Ok(1)).Map(x => x + 1).AndThenAsync(x => Later(Ok(x * 2)));
        Assert.Equal(Ok(4), r);
        Assert.Equal("e", await AsTask(Err("e")).Match(x => "", e => e));
    }

    [Fact]
    public async Task TryAsync_CapturesExceptions()
    {
        var ok = await Result.TryAsync(async () => { await Task.Yield(); return 1; });
        var err = await Result.TryAsync<int>(async () => { await Task.Yield(); throw new InvalidOperationException("boom"); });
        var mapped = await Result.TryAsync<int, string>(() => throw new FormatException(), ex => ex.GetType().Name);
        var stated = await Result.TryAsync(5, static async s => { await Task.Yield(); return s * 2; });

        Assert.Equal(1, ok.UnwrapOr(0));
        Assert.True(err.TryGetErr(out var ex));
        Assert.Equal("boom", ex.Message);
        Assert.Equal("FormatException", mapped.Match(_ => "", e => e));
        Assert.Equal(10, stated.UnwrapOr(0));
    }

    [Fact]
    public async Task TryAsync_DoesNotCaptureCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.TryAsync(async () => { await Task.Delay(1000, cts.Token); return 1; }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.TryAsync(async () => { await Task.Delay(1000, cts.Token); return 1; }, ex => ex.Message));
    }

    [Fact]
    public async Task AsyncLinq_MixesSyncValueTaskAndTask()
    {
        var r = await (
            from a in Later(Ok(1))
            from b in Ok(a + 1)
            from c in AsTask(Ok(b + 1))
            from d in Sync(Ok(c + 1))
            select a + b + c + d);
        Assert.Equal(Ok(10), r);
    }

    [Fact]
    public async Task AsyncLinq_ShortCircuits()
    {
        var calls = 0;
        ValueTask<Result<int, string>> Step(Result<int, string> r)
        {
            calls++;
            return Later(r);
        }

        var r = await (
            from a in Step(Err("first"))
            from b in Step(Ok(2))
            select a + b);
        Assert.Equal(Err("first"), r);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AsyncLinq_SyncSourceAsyncSelector()
    {
        var r = await (from a in Ok(1) from b in Later(Ok(2)) select a + b);
        Assert.Equal(Ok(3), r);
    }
}
