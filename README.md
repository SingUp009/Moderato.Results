# Moderato.Results

[neverthrow](https://github.com/supermacro/neverthrow) for C#

```csharp
// try / catch
try
{
    var user = await GetUserAsync(id);
    var profile = await GetProfileAsync(user);
    ShowProfile(profile);
}
catch (Exception e)
{
    // e is Exception
    // Did GetUserAsync throw? GetProfileAsync?
    // What type of error is it?
    // The type cannot tell you.
    Console.Error.WriteLine(e);
}
```

```csharp
using Moderato.Results;

var result = await GetUserAsync(id)
    .AndThenAsync(GetProfileAsync); // GetProfileAsync never runs if GetUserAsync returned an Err

result.Match
(
    profile => ShowProfile(profile),
    error   => ShowError(error) // error is an AppError
);
```

`GetUserAsync` returns `ValueTask<Result<User, AppError>>`. Both the possibility of failure and the type of that failure are visible in the signature. `AndThenAsync` carries an Err straight through, so there is no branch to write in between.

```csharp
var message = result switch
{
    Ok<Profile>(var profile)   => profile.Bio,
    Err<AppError>(NotFound n)  => $"no user {n.Id}",
    Err<AppError>(Forbidden f) => $"forbidden: {f.Reason}",
};
```

> [!warning]
> .NET 11 is preview now.
> So this library is also preview.

## Table of contents

- [Moderato.Results](#moderatoresults)
  - [Table of contents](#table-of-contents)
  - [Requirements](#requirements)
  - [Creating a Result](#creating-a-result)
  - [Using a Result](#using-a-result)
  - [Union error types](#union-error-types)
  - [API](#api)
    - [Map](#map)
    - [MapErr](#maperr)
    - [AndThen](#andthen)
    - [AndThrough](#andthrough)
    - [OrElse](#orelse)
    - [Match](#match)
    - [UnwrapOr and UnwrapOrElse](#unwrapor-and-unwraporelse)
    - [Tap and TapErr](#tap-and-taperr)
    - [Combine](#combine)
    - [Try](#try)
    - [LINQ query syntax](#linq-query-syntax)
    - [Async](#async)
  - [Caveats](#caveats)
    - [default(Result) is an invalid state](#defaultresult-is-an-invalid-state)
    - [Lambdas that build a Result inline need a type](#lambdas-that-build-a-result-inline-need-a-type)
    - [Async results can be awaited only once](#async-results-can-be-awaited-only-once)
    - [Result stores both T and E](#result-stores-both-t-and-e)
  - [Installation](#installation)
    - [From source](#from-source)
  - [License](#license)

## Requirements

- .NET 11 SDK (`11.0.100-rc.2` or later), `net11.0`
- C# 15

The library is Native AOT compatible (`IsAotCompatible`).

## Creating a Result

```csharp
Result<int, string> ok   = Result.Ok(42);      // Ok<int>     → Result<int, string>
Result<int, string> err  = Result.Err("boom"); // Err<string> → Result<int, string>
Result<Unit, string> done = Result.Ok();       // for operations that return nothing
```

## Using a Result

```csharp
if (result is Ok<int>(var value)) { ... }

// Accessors
result.IsOk; result.IsErr;
result.TryGetOk(out var value); result.TryGetErr(out var error);

// Folding
string text = result.Match(v => $"ok {v}", e => $"err {e}");
int n = result.UnwrapOr(0);
```

## Union error types

```csharp
public record NotFound(int Id);
public record Forbidden(string Reason);
public union AppError(NotFound, Forbidden);

Result<User, AppError> Load(int id) =>
    FindUser(id).MapErr<AppError>(static e => e) // Result<User, NotFound> → Result<User, AppError>
        .AndThen(static user => Authorize(user).MapErr<AppError>(static e => e));

var message = Load(42) switch
{
    Ok<User>(var user)         => $"Hello, {user.Name}",
    Err<AppError>(NotFound n)  => $"no user {n.Id}",
    Err<AppError>(Forbidden f) => $"forbidden: {f.Reason}",
};
```

When you build a union-typed Err directly, name the type: `Result.Err<AppError>(new NotFound(id))`. An `Err<NotFound>` does not convert to `Result<T, AppError>` on its own.

## API

| neverthrow | Moderato.Results | state overload |
| --- | --- | --- |
| `map` | `Map(Func<T, U>)` | `Map(state, Func<TState, T, U>)` |
| `mapErr` | `MapErr(Func<E, F>)` | `MapErr(state, …)` |
| `andThen` | `AndThen(Func<T, Result<U, E>>)` | `AndThen(state, …)` |
| `andThrough` | `AndThrough(Func<T, Result<U, E>>)` | `AndThrough(state, …)` |
| `orElse` | `OrElse(Func<E, Result<T, F>>)` | `OrElse(state, …)` |
| `match` | `Match(ok, err)`, plus an `Action` overload | `Match(state, ok, err)` |
| `andTee` / `orTee` | `Tap(Action<T>)` / `TapErr(Action<E>)` | `Tap(state, …)` / `TapErr(state, …)` |
| `unwrapOr` | `UnwrapOr(T)`, `UnwrapOrElse(Func<E, T>)` | `UnwrapOrElse(state, …)` |
| `combine` | `Result.Combine(r1, …, r8)` → tuple, `Result.Combine(IEnumerable / ReadOnlySpan)` → `T[]` | - |
| `combineWithAllErrors` | `Result.CombineAll(…)` → `Result<…, E[]>` | - |
| `fromThrowable` | `Result.Try(func)`, `Result.Try(func, ex => error)` | `Result.Try(state, func, …)` |
| `safeTry` | LINQ query syntax | - |
| `ResultAsync` | extension methods on `ValueTask<Result<T, E>>` / `Task<Result<T, E>>`, `Result.TryAsync` | `MapAsync(state, …)`, `TryAsync(state, …)` |
| `ResultAsync.combine` | `Result.CombineAsync(…)` / `Result.CombineAllAsync(…)` | - |

The examples below all use these values.

```csharp
Result<int, string> ok  = Result.Ok(2);
Result<int, string> err = Result.Err("boom");
```

### Map

Transforms the value. An Err passes straight through and the mapper is never called.

```csharp
ok.Map(x => x * 10);    // Ok(20)
err.Map(x => x * 10);   // Err(boom)

var factor = 10;
ok.Map(factor, static (f, x) => x * f);   // Ok(20)
```

### MapErr

Transforms the error. An Ok passes straight through. The error type changes, so the result type does too.

```csharp
err.MapErr(e => e.Length);   // Err(4) as Result<int, int>
ok.MapErr(e => e.Length);    // Ok(2) as Result<int, int>
```

### AndThen

Chains a fallible step. On an Err the binder is never called, and that error travels to the end untouched.

```csharp
Result<int, string> Half(int x) => x % 2 == 0 ? Result.Ok(x / 2) : Result.Err("odd");
Result<int, string> three = Result.Ok(3);

ok.AndThen(Half);                      // Ok(1)
three.AndThen(Half);                   // Err(odd)
err.AndThen(Half);                     // Err(boom), Half is never called
```

### AndThrough

Where `AndThen` returns the next value, `AndThrough` keeps the original value and only runs a check. If the check is Err, you get that error.

```csharp
Result<Unit, string> Positive(int x) => x > 0 ? Result.Ok() : Result.Err($"{x} is not positive");
Result<int, string> negative = Result.Ok(-1);

ok.AndThrough(Positive);               // Ok(2)
negative.AndThrough(Positive);         // Err(-1 is not positive)
```

### OrElse

Recovers from the error. On an Ok the binder is never called.

```csharp
Result<int, string> fallback = Result.Ok(0);

err.OrElse(_ => fallback);                    // Ok(0)
ok.OrElse(_ => fallback);                     // Ok(2)
err.OrElse<int>(e => Result.Err(e.Length));   // Err(4) as Result<int, int>
```

### Match

Folds both cases into one value. The `Action` overload returns nothing.

```csharp
ok.Match(x => $"ok {x}", e => $"err {e}");    // "ok 2"
err.Match(x => $"ok {x}", e => $"err {e}");   // "err boom"

ok.Match(x => Console.WriteLine(x), e => Console.Error.WriteLine(e));
```

### UnwrapOr and UnwrapOrElse

Take the value out, falling back to a default on an Err. `UnwrapOrElse` builds that default from the error.

```csharp
ok.UnwrapOr(0);                    // 2
err.UnwrapOr(0);                   // 0
err.UnwrapOrElse(e => e.Length);   // 4
```

### Tap and TapErr

Run a side effect without touching the value or the error. Both return the Result they were given.

```csharp
ok.Tap(x => Console.WriteLine(x))       // prints 2, returns Ok(2)
  .TapErr(e => Console.WriteLine(e));   // never called

err.TapErr(e => Console.WriteLine(e));  // prints boom, returns Err(boom)
```

### Combine

The two- to eight-argument overloads return a tuple; the `ReadOnlySpan` and `IEnumerable` overloads return an array. `Combine` short-circuits on the first Err, while `CombineAll` collects every error into an `E[]`.

```csharp
Result<string, string> name = Result.Ok("ada");
Result<int, string> other = Result.Err("bad");

Result.Combine(ok, name);            // Ok((2, ada))
Result.Combine(ok, err);             // Err(boom)
Result.Combine(new[] { ok, ok });    // Ok(int[] { 2, 2 })

Result.CombineAll(err, ok, other);   // Err(string[] { "boom", "bad" })
```

### Try

`Result.Try` turns exceptions into an Err. `OperationCanceledException` is not an error, so it propagates.

```csharp
Result.Try(() => int.Parse("12"));                          // Ok(12)
Result.Try(() => int.Parse("x"));                           // Err(System.FormatException: …)
Result.Try(() => int.Parse("x"), ex => ex.GetType().Name);  // Err(FormatException)
```

`Result.TryAsync` works the same way, and takes an optional `CancellationToken` as its last argument. Pass the token, and only the cancellation you asked for propagates; any other `OperationCanceledException` becomes an Err. Use the overloads whose delegate receives the token to pass it on.

```csharp
var parsed = await Result.TryAsync(ct => ReadAsync(path, ct), ex => AppError.Io(ex), cancellationToken);
```

> [!important]
> `HttpClient` reports a timeout as a `TaskCanceledException`. Without a token, `TryAsync` lets it propagate like any cancellation. Pass your `CancellationToken` so that a timeout becomes an Err while your own cancellation still propagates.

### LINQ query syntax

This is the equivalent of neverthrow's `safeTry`. The first Err short-circuits the remaining steps.

Query syntax keeps every earlier binding in scope, so reach for it when a later step needs a value from an earlier one.

```csharp
Result<Order, AppError> order =
    from user in FindUser(id)
    from cart in LoadCart(user)
    select new Order(user, cart);
```

### Async

Methods without the `Async` suffix take synchronous delegates; methods with it take delegates returning `ValueTask`. One chain can mix `Result`, `ValueTask<Result>` and `Task<Result>` freely.

```csharp
int total = await FindUserAsync(id)                        // ValueTask<Result<User, AppError>>
    .AndThenAsync(async user => await LoadCartTask(user))  // a step returning Task<Result<…>>
    .AndThrough(cart => Validate(cart))                    // a step returning Result<…>
    .Map(cart => cart.Total)                               // a sync step
    .UnwrapOr(0);

var users = await Result.CombineAsync(ids.Select(FindUserAsync));   // Result<User[], AppError>
```

Async delegates return `ValueTask`. To call a method that returns `Task<T>`, write `async x => await LoadAsync(x)`.

`Result.CombineAsync` enumerates the sequence before it awaits anything, so lazily created tasks run concurrently. If something throws, it awaits every task and then rethrows the first exception in argument order. The remaining exceptions are not reported.

## Caveats

### default(Result) is an invalid state

`default(Result<T, E>)` is neither Ok nor Err. `HasValue`, `IsOk` and `IsErr` return `false`, and so do `TryGetOk`, `TryGetErr` and `TryGetValue`. `Value` is `null`. Members that work with the value, such as `Map` and `AndThen`, throw `InvalidOperationException`. Always create results with `Result.Ok` or `Result.Err`.

### Lambdas that build a Result inline need a type

C# does not infer a lambda's return type through a union conversion, so `result.AndThen(x => x > 0 ? Result.Ok(x) : Result.Err("neg"))` gives CS0411. Write the type rather than a cast.

```csharp
result.AndThen<int>(x => x > 0 ? Result.Ok(x) : Result.Err("neg"));                   // type argument
result.AndThen(Result<int, string> (x) => x > 0 ? Result.Ok(x) : Result.Err("neg"));  // lambda return type
```

The second form also works on state overloads and on calls against `ValueTask` and `Task`. Neither is needed when you pass a method that returns `Result<…>`, as in `.AndThen(Validate)`. An `AndThrough` check returning `Result<Unit, E>` infers on its own.

### Async results can be awaited only once

The async APIs return `ValueTask<Result<T, E>>`. Like any `ValueTask`, it must be consumed exactly once: don't await it twice or read `.Result` twice. To keep it around or await it from several places, convert it first with `.AsTask()` or `.Preserve()`.

### Result stores both T and E

`Result<T, E>` is a struct with a field for each of `T` and `E`, so its size is roughly the sum of the two. With small types or references this costs nothing noticeable, but a large struct payload is copied at every step of a chain and into each async state machine. Prefer references or small error types for large payloads.

## Installation

```
dotnet add package Moderato.Results --prerelease
```

### From source

```
dotnet build
dotnet test
dotnet run tools/Generate.cs       # regenerate *.g.cs
```

`tools/Generate.cs` produces the `*.g.cs` files under `src/Moderato.Results`: the async receiver overloads and the arity 2 through 8 `Combine` methods. Edit the generator and regenerate rather than editing them by hand.

## License

MIT. See [LICENSE](LICENSE).
