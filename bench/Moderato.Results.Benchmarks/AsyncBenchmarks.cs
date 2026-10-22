using BenchmarkDotNet.Attributes;
using Moderato.Results;

namespace Moderato.Results.Benchmarks;

// An async pipeline whose steps complete synchronously.
[MemoryDiagnoser]
public class AsyncBenchmarks
{
    [Params(false, true)]
    public bool Fail;

    private ValueTask<Result<int, string>> Load(int x)
        => new(Fail ? Result.Err("failed") : Result.Ok(x));

    private static ValueTask<Result<int, string>> Half(int x)
        => new(x % 2 == 0 ? Result.Ok(x / 2) : Result.Err("odd"));

    private ValueTask<int> LoadOrThrow(int x)
        => Fail ? ValueTask.FromException<int>(new InvalidOperationException("failed")) : new(x);

    [Benchmark(Baseline = true, Description = "Moderato.Results (fluent)")]
    public async ValueTask<int> Fluent()
        => await Load(20)
            .Map(static x => x + 2)
            .AndThenAsync(static x => Half(x))
            .Match(static x => x, static _ => -1);

    [Benchmark(Description = "Moderato.Results (LINQ)")]
    public async ValueTask<int> Linq()
    {
        var r = await (
            from a in Load(20)
            from b in Half(a + 2)
            select b);
        return r.UnwrapOr(-1);
    }

    [Benchmark(Description = "Exceptions (await + catch)")]
    public async ValueTask<int> Exceptions()
    {
        try
        {
            var x = await LoadOrThrow(20) + 2;
            return x % 2 == 0 ? x / 2 : throw new InvalidOperationException("odd");
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }
}
