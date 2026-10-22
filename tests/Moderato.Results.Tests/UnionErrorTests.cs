namespace Moderato.Results.Tests;

public record NotFound(int Id);
public record Forbidden(string Reason);
public union AppError(NotFound, Forbidden);

public class UnionErrorTests
{
    private static Result<int, NotFound> Find(int id) => id > 0 ? Result.Ok(id) : Result.Err(new NotFound(id));
    private static Result<int, Forbidden> Authorize(int x) => x < 100 ? Result.Ok(x) : Result.Err(new Forbidden("too large"));

    private static Result<int, AppError> Load(int id) =>
        Find(id).MapErr<AppError>(static e => e)
                .AndThen(static x => Authorize(x).MapErr<AppError>(static e => e));

    private static string Describe(Result<int, AppError> r) => r switch
    {
        Ok<int>(var v) => $"ok {v}",
        Err<AppError>(NotFound n) => $"not found {n.Id}",
        Err<AppError>(Forbidden f) => $"forbidden: {f.Reason}",
    };

    [Fact]
    public void NestedSwitch_IsExhaustiveOverErrorCases()
    {
        Assert.Equal("ok 1", Describe(Load(1)));
        Assert.Equal("not found 0", Describe(Load(0)));
        Assert.Equal("forbidden: too large", Describe(Load(100)));
    }

    [Fact]
    public void ErrWithExplicitErrorType_ConvertsToResult()
    {
        Result<int, AppError> r = Result.Err<AppError>(new NotFound(7));
        Assert.Equal("not found 7", Describe(r));
    }
}
