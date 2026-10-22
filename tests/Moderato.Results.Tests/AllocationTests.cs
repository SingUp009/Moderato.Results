namespace Moderato.Results.Tests;

public class AllocationTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    private static long Measure(Action action)
    {
        action();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static int sink;

    [Fact]
    public void PatternMatching_DoesNotBox()
    {
        var ok = Ok(1);
        var err = Err("e");
        Assert.Equal(0, Measure(() =>
        {
            sink += ok switch { Ok<int>(var v) => v, Err<string> => -1 };
            sink += err switch { Ok<int>(var v) => v, Err<string> => -1 };
            if (ok is Ok<int> o) sink += o.Value;
        }));
    }

    [Fact]
    public void Construction_DoesNotAllocate()
    {
        Assert.Equal(0, Measure(() =>
        {
            Result<int, string> a = Result.Ok(1);
            Result<int, string> b = Result.Err("e");
            sink += a.IsOk ? 1 : 0;
            sink += b.IsErr ? 1 : 0;
        }));
    }

    [Fact]
    public void StaticLambdaChains_DoNotAllocate()
    {
        var ok = Ok(1);
        var err = Err("e");
        Assert.Equal(0, Measure(() =>
        {
            sink += ok.Map(static x => x + 1)
                      .AndThen<int>(static x => Result.Ok(x * 2))
                      .AndThrough(static x => x > 0 ? Result.Ok() : Result.Err("neg"))
                      .MapErr(static e => e.Length)
                      .Match(static x => x, static e => -e);
            sink += err.Map(static x => x + 1).UnwrapOr(0);
        }));
    }

    [Fact]
    public void StateOverloads_DoNotAllocate()
    {
        var ok = Ok(1);
        var offset = 10;
        Assert.Equal(0, Measure(() =>
        {
            sink += ok.Map(offset, static (o, x) => x + o)
                      .AndThen(offset, static Result<int, string> (o, x) => Result.Ok(x * o))
                      .Match(offset, static (o, x) => x + o, static (o, _) => o);
        }));
    }

    [Fact]
    public void CapturingLambda_DoesAllocate()
    {
        var ok = Ok(1);
        Assert.True(Measure(() => sink += Capturing(ok, sink & 1)) > 0);
        Assert.Equal(0, Measure(() => sink += State(ok, sink & 1)));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int Capturing(Result<int, string> r, int offset) => r.Map(x => x + offset).UnwrapOr(0);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int State(Result<int, string> r, int offset) => r.Map(offset, static (o, x) => x + o).UnwrapOr(0);

    [Fact]
    public void Combine_Tuple_DoesNotAllocate()
    {
        var a = Ok(1);
        var b = Ok(2);
        Assert.Equal(0, Measure(() =>
        {
            var r = Result.Combine(a, b);
            sink += r.IsOk ? 1 : 0;
        }));
    }

    [Fact]
    public void CompletedValueTaskPipeline_DoesNotAllocate()
    {
        var source = new ValueTask<Result<int, string>>(Ok(1));
        Assert.Equal(0, Measure(() =>
        {
            var t = source.Map(static x => x + 1)
                          .AndThen(static Result<int, string> (x) => Result.Ok(x * 2))
                          .MapAsync(static x => new ValueTask<int>(x + 1));
            sink += t.Result.UnwrapOr(0);
        }));
    }
}
