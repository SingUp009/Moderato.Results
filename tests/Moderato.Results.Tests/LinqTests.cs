namespace Moderato.Results.Tests;

public class LinqTests
{
    private static Result<int, string> Ok(int v) => Result.Ok(v);
    private static Result<int, string> Err(string e) => Result.Err(e);

    [Fact]
    public void Linq_ComposesAndShortCircuits()
    {
        var calls = 0;
        Result<int, string> Step(int x)
        {
            calls++;
            return x > 0 ? Ok(x) : Err($"step {x}");
        }

        var ok =
            from a in Step(1)
            from b in Step(2)
            select a + b;
        Assert.Equal(Ok(3), ok);

        calls = 0;
        var err =
            from a in Step(-1)
            from b in Step(2)
            select a + b;
        Assert.Equal(Err("step -1"), err);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Linq_SelectOnly()
    {
        var r = from a in Ok(2) select a * 10;
        Assert.Equal(Ok(20), r);
    }
}
