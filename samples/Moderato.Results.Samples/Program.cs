using Moderato.Results;

// Ok / Err convert to Result
static Result<int, string> ParsePositive(string text)
{
    if (!int.TryParse(text, out var n)) return Result.Err($"'{text}' is not a number");
    if (n <= 0) return Result.Err($"{n} is not positive");
    return Result.Ok(n);
}

// Exhaustive switch: no default arm needed
static string Describe(Result<int, string> r) => r switch
{
    Ok<int>(var value) => $"ok: {value}",
    Err<string>(var error) => $"err: {error}",
};

Console.WriteLine(Describe(ParsePositive("42")));
Console.WriteLine(Describe(ParsePositive("-1")));
Console.WriteLine(Describe(ParsePositive("abc")));

// Chaining
var doubled = ParsePositive("21").Map(x => x * 2).Match(x => $"doubled: {x}", e => $"failed: {e}");
Console.WriteLine(doubled);

// State overload: no closure
var offset = 100;
Console.WriteLine(ParsePositive("1").Map(offset, static (o, x) => x + o));

// LINQ
var sum =
    from a in ParsePositive("1")
    from b in ParsePositive("2")
    select a + b;
Console.WriteLine($"linq: {sum}");

// Combine
Console.WriteLine(Result.Combine(ParsePositive("1"), ParsePositive("2"), ParsePositive("3")));
Console.WriteLine(Result.CombineAll(ParsePositive("x"), ParsePositive("2"), ParsePositive("-3")).MapErr(es => string.Join(" / ", es)));

// Try
Console.WriteLine(Result.Try(() => int.Parse("oops"), ex => ex.GetType().Name));

// Pattern matching does not box
var ok = ParsePositive("7");
_ = Describe(ok);
var before = GC.GetAllocatedBytesForCurrentThread();
var hits = 0;
for (var i = 0; i < 1_000; i++)
{
    if (ok is Ok<int> o && o.Value == 7) hits++;
    hits += ok switch { Ok<int> => 1, Err<string> => 0 };
}
var after = GC.GetAllocatedBytesForCurrentThread();
Console.WriteLine($"pattern matching allocated {after - before} bytes over 2,000 matches (hits={hits})");
