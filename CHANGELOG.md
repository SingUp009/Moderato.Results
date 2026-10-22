# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.2] - 2026-10-22

### Breaking changes

- `Result.Try` and `Result.TryAsync` no longer turn `OperationCanceledException` into an Err; cancellation now propagates. ([#4](https://github.com/SingUp009/Moderato.Results/pull/4))
- `Result<T, E>.Value` returns `null` for `default` instead of throwing `InvalidOperationException`, as the union pattern expects. ([#3](https://github.com/SingUp009/Moderato.Results/pull/3))
- Every API that takes a delegate throws `ArgumentNullException` for `null`. Async APIs throw at the call, before returning a task. ([#7](https://github.com/SingUp009/Moderato.Results/pull/7))

### Fixed

- `Combine` over a span or a sequence checks every result for `default`, including those after the first Err. ([#5](https://github.com/SingUp009/Moderato.Results/pull/5))
- `CombineAsync` / `CombineAllAsync` over `Task` rethrow the first exception in argument order, like the `ValueTask` overloads. ([#6](https://github.com/SingUp009/Moderato.Results/pull/6))

### Changed

- Packages carry repository metadata and Source Link, and ship a `.snupkg` symbol package. ([#10](https://github.com/SingUp009/Moderato.Results/pull/10))
- NuGet publishing runs in its own job, gated by the `nuget` environment. ([#8](https://github.com/SingUp009/Moderato.Results/pull/8))
- The README covers single consumption of `ValueTask` and the size of `Result<T, E>`. ([#9](https://github.com/SingUp009/Moderato.Results/pull/9))

## [0.1.0-preview.1] - 2026-10-22

### Added

- `Result<T, E>`, a C# 15 union of `Ok<T>` and `Err<E>` with non-boxing pattern matching.
- `Map`, `MapErr`, `AndThen`, `AndThrough`, `OrElse`, `Match`, `Tap`, `TapErr`, `UnwrapOr` and `UnwrapOrElse`, each with a closure-free state overload.
- `Result.Ok`, `Result.Err`, `Result.Try`, `Result.TryAsync`, `Result.Combine`, `Result.CombineAll`, `Result.CombineAsync` and `Result.CombineAllAsync`.
- LINQ query syntax over `Result`, `ValueTask<Result>` and `Task<Result>`.
- Async extensions on `ValueTask<Result<T, E>>` and `Task<Result<T, E>>`.

[Unreleased]: https://github.com/SingUp009/Moderato.Results/compare/v0.1.0-preview.2...HEAD
[0.1.0-preview.2]: https://github.com/SingUp009/Moderato.Results/compare/v0.1.0-preview.1...v0.1.0-preview.2
[0.1.0-preview.1]: https://github.com/SingUp009/Moderato.Results/releases/tag/v0.1.0-preview.1
