using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ErrorOr;
using OneOf;
using Moderato.Results;
using FR = FluentResults;
using LE = LanguageExt;

namespace Moderato.Results.Benchmarks;

// Create → Map → AndThen → Match, on the success and failure path.
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ChainBenchmarks
{
    [Params(false, true)]
    public bool Fail;

    private int _input;
    private int _offset;

    [GlobalSetup]
    public void Setup()
    {
        _input = 20;
        _offset = 2;

        // Every row must compute the same thing. A mismatch means some row takes a different path,
        // which is how _offset = 1 once had the state/capturing rows measuring an Err short-circuit.
        int[] rows = [Moderato_Static(), Moderato_State(), Moderato_Capturing(), Moderato_Switch(),
                      Exceptions(), ErrorOr_(), FluentResults_(), OneOf_(), LanguageExt_()];
        if (rows.Distinct().Count() != 1)
            throw new InvalidOperationException("Benchmarks disagree: " + string.Join(", ", rows));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Result<int, string> ModeratoSource(int x) => Fail ? Result.Err("failed") : Result.Ok(x);

    private static Result<int, string> ModeratoHalf(int x) => x % 2 == 0 ? Result.Ok(x / 2) : Result.Err("odd");

    [Benchmark(Baseline = true, Description = "Moderato.Results (static lambdas)")]
    [BenchmarkCategory("Chain")]
    public int Moderato_Static()
        => ModeratoSource(_input)
            .Map(static x => x + 2)
            .AndThen(static x => ModeratoHalf(x))
            .Match(static x => x, static _ => -1);

    [Benchmark(Description = "Moderato.Results (state overloads)")]
    [BenchmarkCategory("Chain")]
    public int Moderato_State()
        => ModeratoSource(_input)
            .Map(_offset, static (o, x) => x + o)
            .AndThen(static x => ModeratoHalf(x))
            .Match(_offset, static (_, x) => x, static (_, _) => -1);

    [Benchmark(Description = "Moderato.Results (capturing lambdas)")]
    [BenchmarkCategory("Chain")]
    public int Moderato_Capturing()
    {
        var offset = _offset;
        return ModeratoSource(_input)
            .Map(x => x + offset)
            .AndThen(static x => ModeratoHalf(x))
            .Match(x => x, _ => -1);
    }

    [Benchmark(Description = "Moderato.Results (switch)")]
    [BenchmarkCategory("Chain")]
    public int Moderato_Switch()
        => ModeratoSource(_input).Map(static x => x + 2).AndThen(static x => ModeratoHalf(x)) switch
        {
            Ok<int>(var v) => v,
            Err<string> => -1,
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ExceptionSource(int x) => Fail ? throw new InvalidOperationException("failed") : x;

    private static int ExceptionHalf(int x) => x % 2 == 0 ? x / 2 : throw new InvalidOperationException("odd");

    [Benchmark(Description = "Exceptions (throw/catch)")]
    [BenchmarkCategory("Chain")]
    public int Exceptions()
    {
        try
        {
            return ExceptionHalf(ExceptionSource(_input) + 2);
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ErrorOr<int> ErrorOrSource(int x) => Fail ? Error.Failure(description: "failed") : x;

    private static ErrorOr<int> ErrorOrHalf(int x) => x % 2 == 0 ? x / 2 : Error.Failure(description: "odd");

    [Benchmark(Description = "ErrorOr")]
    [BenchmarkCategory("Chain")]
    public int ErrorOr_()
        => ErrorOrSource(_input)
            .Then(static x => x + 2)
            .Then(static x => ErrorOrHalf(x))
            .Match(static x => x, static _ => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private FR.Result<int> FluentSource(int x) => Fail ? FR.Result.Fail<int>("failed") : FR.Result.Ok(x);

    private static FR.Result<int> FluentHalf(int x) => x % 2 == 0 ? FR.Result.Ok(x / 2) : FR.Result.Fail<int>("odd");

    [Benchmark(Description = "FluentResults")]
    [BenchmarkCategory("Chain")]
    public int FluentResults_()
    {
        var r = FluentSource(_input).Map(static x => x + 2).Bind(static x => FluentHalf(x));
        return r.IsSuccess ? r.Value : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private OneOf<int, string> OneOfSource(int x) => Fail ? "failed" : x;

    private static OneOf<int, string> OneOfHalf(int x) => x % 2 == 0 ? x / 2 : "odd";

    [Benchmark(Description = "OneOf")]
    [BenchmarkCategory("Chain")]
    public int OneOf_()
        => OneOfSource(_input)
            .MapT0(static x => x + 2)
            .Match(static x => OneOfHalf(x), static e => e)
            .Match(static x => x, static _ => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private LE.Either<string, int> LanguageExtSource(int x)
        => Fail ? LE.Prelude.Left<string, int>("failed") : LE.Prelude.Right<string, int>(x);

    private static LE.Either<string, int> LanguageExtHalf(int x)
        => x % 2 == 0 ? LE.Prelude.Right<string, int>(x / 2) : LE.Prelude.Left<string, int>("odd");

    [Benchmark(Description = "LanguageExt (Either)")]
    [BenchmarkCategory("Chain")]
    public int LanguageExt_()
        => LanguageExtSource(_input)
            .Map(static x => x + 2)
            .Bind(static x => LanguageExtHalf(x))
            .Match(Right: static x => x, Left: static _ => -1);
}
