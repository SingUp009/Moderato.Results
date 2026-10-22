namespace Moderato.Results.Tests;

public class CombineAsyncTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    private static async ValueTask<Result<T, string>> Later<T>(Result<T, string> r)
    {
        await Task.Yield();
        return r;
    }

    private static async Task<Result<T, string>> AsTask<T>(Result<T, string> r)
    {
        await Task.Yield();
        return r;
    }

    [Fact]
    public async Task Sequence_ValueTask()
    {
        var ok = await Result.CombineAsync([Later(Ok(1)), new(Ok(2)), Later(Ok(3))]);
        Assert.True(ok.TryGetOk(out var values));
        Assert.Equal([1, 2, 3], values);

        Assert.Equal<Result<int[], string>>(Result.Err("first"),
            await Result.CombineAsync([Later(Ok(1)), Later(Err("first")), Later(Err("second"))]));
    }

    [Fact]
    public async Task Sequence_Task()
    {
        var ok = await Result.CombineAsync(new[] { 1, 2, 3 }.Select(i => AsTask(Ok(i))));
        Assert.True(ok.TryGetOk(out var values));
        Assert.Equal([1, 2, 3], values);

        var all = await Result.CombineAllAsync([AsTask(Err("a")), AsTask(Ok(2)), AsTask(Err("b"))]);
        Assert.True(all.TryGetErr(out var errors));
        Assert.Equal(["a", "b"], errors);
    }

    [Fact]
    public async Task Sequence_StartsLazyTasksBeforeAwaiting()
    {
        var gate = new TaskCompletionSource();
        var started = 0;

        async ValueTask<Result<int, string>> Wait(int i)
        {
            Interlocked.Increment(ref started);
            await gate.Task;
            return Ok(i);
        }

        var combined = Result.CombineAsync(Enumerable.Range(0, 3).Select(Wait));
        Assert.Equal(3, started);
        gate.SetResult();
        Assert.True((await combined).IsOk);
    }

    [Fact]
    public async Task Sequence_AwaitsEveryTaskBeforeRethrowing()
    {
        var gate = new TaskCompletionSource();
        var finished = false;

        async ValueTask<Result<int, string>> Throws()
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }

        async ValueTask<Result<int, string>> Slow()
        {
            await gate.Task;
            finished = true;
            return Ok(1);
        }

        var combined = Result.CombineAsync([Throws(), Slow()]).AsTask();
        await Task.Delay(50);
        Assert.False(combined.IsCompleted);
        gate.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => combined);
        Assert.True(finished);
    }

    [Fact]
    public async Task Sequence_Task_AwaitsEveryTaskBeforeRethrowing()
    {
        var gate = new TaskCompletionSource();
        var finished = false;

        async Task<Result<int, string>> Throws()
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }

        async Task<Result<int, string>> Slow()
        {
            await gate.Task;
            finished = true;
            return Ok(1);
        }

        var combined = Result.CombineAsync([Throws(), Slow()]).AsTask();
        await Task.Delay(50);
        Assert.False(combined.IsCompleted);
        gate.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => combined);
        Assert.True(finished);
    }

    [Fact]
    public async Task Sequence_RethrowsFirstExceptionInArgumentOrder()
    {
        // Task.WhenAll would prefer the fault; both overloads follow argument order instead.
        Task<Result<int, string>> Canceled() => Task.FromCanceled<Result<int, string>>(new CancellationToken(true));
        Task<Result<int, string>> Faulted() => Task.FromException<Result<int, string>>(new InvalidOperationException());

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await Result.CombineAsync([Canceled(), Faulted()]));
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await Result.CombineAllAsync([Canceled(), Faulted()]));
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Result.CombineAsync([new ValueTask<Result<int, string>>(Canceled()), new ValueTask<Result<int, string>>(Faulted())]));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await Result.CombineAsync([Faulted(), Canceled()]));
    }

    [Fact]
    public async Task Tuple_ValueTaskAndTask()
    {
        var r = await Result.CombineAsync(Later(Ok(1)), Later<string>(Result.Ok("a")), new ValueTask<Result<int, string>>(Ok(3)));
        Assert.True(r.TryGetOk(out var t));
        Assert.Equal((1, "a", 3), t);

        var k = await Result.CombineAsync(AsTask(Ok(1)), AsTask(Err("x")));
        Assert.True(k.TryGetErr(out var e));
        Assert.Equal("x", e);
    }

    [Fact]
    public async Task Tuple_CombineAll_CollectsEveryError()
    {
        var r = await Result.CombineAllAsync(Later(Err("a")), Later(Ok(2)), AsTask(Err("c")).AsValueTask());
        Assert.True(r.TryGetErr(out var errors));
        Assert.Equal(["a", "c"], errors);
    }

    [Fact]
    public async Task Tuple_ThrowsOnDefault()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Result.CombineAsync(Later(Ok(1)), new ValueTask<Result<int, string>>(default(Result<int, string>))));
    }
}

file static class TaskExtensions
{
    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new(task);
}
