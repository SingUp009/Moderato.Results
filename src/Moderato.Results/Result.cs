using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moderato.Results;

/// <summary>Either <see cref="Ok{T}"/> or <see cref="Err{E}"/>. <c>default</c> is invalid.</summary>
[Union]
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Result<T, E> : IUnion, IEquatable<Result<T, E>>
{
    internal const byte StateDefault = 0;
    internal const byte StateOk = 1;
    internal const byte StateErr = 2;

    internal readonly T _value;
    internal readonly E _error;
    internal readonly byte _state;

    /// <summary>Creates an Ok result.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result(Ok<T> ok)
    {
        _value = ok.Value;
        _error = default!;
        _state = StateOk;
    }

    /// <summary>Creates an Err result.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result(Err<E> err)
    {
        _value = default!;
        _error = err.Error;
        _state = StateErr;
    }

    /// <summary>The boxed case. Allocates; pattern matching does not use it.</summary>
    public object Value => _state switch
    {
        StateOk => new Ok<T>(_value),
        StateErr => new Err<E>(_error),
        _ => ThrowHelper.ThrowUninitialized<object>(),
    };

    /// <summary>False only for <c>default</c>.</summary>
    public bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state != StateDefault;
    }

    /// <summary>Non-boxing access to the Ok case.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(out Ok<T> ok)
    {
        ok = new Ok<T>(_value);
        return _state == StateOk;
    }

    /// <summary>Non-boxing access to the Err case.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(out Err<E> err)
    {
        err = new Err<E>(_error);
        return _state == StateErr;
    }

    /// <summary>True if Ok.</summary>
    public bool IsOk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state == StateOk;
    }

    /// <summary>True if Err.</summary>
    public bool IsErr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state == StateErr;
    }

    /// <summary>Gets the value if Ok.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOk([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _state == StateOk;
    }

    /// <summary>Gets the error if Err.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetErr([MaybeNullWhen(false)] out E error)
    {
        error = _error;
        return _state == StateErr;
    }

    /// <summary>Maps the value.</summary>
    public Result<U, E> Map<U>(Func<T, U> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (_state == StateOk) return new Result<U, E>(new Ok<U>(mapper(_value)));
        if (_state == StateErr) return new Result<U, E>(new Err<E>(_error));
        return ThrowHelper.ThrowUninitialized<Result<U, E>>();
    }

    /// <summary>Maps the value without a closure.</summary>
    public Result<U, E> Map<TState, U>(TState state, Func<TState, T, U> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (_state == StateOk) return new Result<U, E>(new Ok<U>(mapper(state, _value)));
        if (_state == StateErr) return new Result<U, E>(new Err<E>(_error));
        return ThrowHelper.ThrowUninitialized<Result<U, E>>();
    }

    /// <summary>Maps the error.</summary>
    public Result<T, F> MapErr<F>(Func<E, F> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (_state == StateOk) return new Result<T, F>(new Ok<T>(_value));
        if (_state == StateErr) return new Result<T, F>(new Err<F>(mapper(_error)));
        return ThrowHelper.ThrowUninitialized<Result<T, F>>();
    }

    /// <summary>Maps the error without a closure.</summary>
    public Result<T, F> MapErr<TState, F>(TState state, Func<TState, E, F> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (_state == StateOk) return new Result<T, F>(new Ok<T>(_value));
        if (_state == StateErr) return new Result<T, F>(new Err<F>(mapper(state, _error)));
        return ThrowHelper.ThrowUninitialized<Result<T, F>>();
    }

    /// <summary>Chains a fallible operation on the value.</summary>
    public Result<U, E> AndThen<U>(Func<T, Result<U, E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (_state == StateOk) return binder(_value);
        if (_state == StateErr) return new Result<U, E>(new Err<E>(_error));
        return ThrowHelper.ThrowUninitialized<Result<U, E>>();
    }

    /// <summary>Chains a fallible operation on the value without a closure.</summary>
    public Result<U, E> AndThen<TState, U>(TState state, Func<TState, T, Result<U, E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (_state == StateOk) return binder(state, _value);
        if (_state == StateErr) return new Result<U, E>(new Err<E>(_error));
        return ThrowHelper.ThrowUninitialized<Result<U, E>>();
    }

    /// <summary>Runs a fallible check on the value, keeping the value if it passes.</summary>
    public Result<T, E> AndThrough<U>(Func<T, Result<U, E>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (_state == StateOk) return Keep(check(_value));
        if (_state == StateErr) return this;
        return ThrowHelper.ThrowUninitialized<Result<T, E>>();
    }

    /// <summary>Runs a fallible check on the value, keeping the value if it passes.</summary>
    public Result<T, E> AndThrough(Func<T, Result<Unit, E>> check) => AndThrough<Unit>(check);

    /// <summary>Runs a fallible check on the value without a closure, keeping the value if it passes.</summary>
    public Result<T, E> AndThrough<TState, U>(TState state, Func<TState, T, Result<U, E>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (_state == StateOk) return Keep(check(state, _value));
        if (_state == StateErr) return this;
        return ThrowHelper.ThrowUninitialized<Result<T, E>>();
    }

    /// <summary>Runs a fallible check on the value without a closure, keeping the value if it passes.</summary>
    public Result<T, E> AndThrough<TState>(TState state, Func<TState, T, Result<Unit, E>> check) => AndThrough<TState, Unit>(state, check);

    /// <summary>Recovers from the error.</summary>
    public Result<T, F> OrElse<F>(Func<E, Result<T, F>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (_state == StateOk) return new Result<T, F>(new Ok<T>(_value));
        if (_state == StateErr) return binder(_error);
        return ThrowHelper.ThrowUninitialized<Result<T, F>>();
    }

    /// <summary>Recovers from the error without a closure.</summary>
    public Result<T, F> OrElse<TState, F>(TState state, Func<TState, E, Result<T, F>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (_state == StateOk) return new Result<T, F>(new Ok<T>(_value));
        if (_state == StateErr) return binder(state, _error);
        return ThrowHelper.ThrowUninitialized<Result<T, F>>();
    }

    /// <summary>Folds into a single value.</summary>
    public R Match<R>(Func<T, R> ok, Func<E, R> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (_state == StateOk) return ok(_value);
        if (_state == StateErr) return err(_error);
        return ThrowHelper.ThrowUninitialized<R>();
    }

    /// <summary>Folds into a single value without a closure.</summary>
    public R Match<TState, R>(TState state, Func<TState, T, R> ok, Func<TState, E, R> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (_state == StateOk) return ok(state, _value);
        if (_state == StateErr) return err(state, _error);
        return ThrowHelper.ThrowUninitialized<R>();
    }

    /// <summary>Runs the action for the current case.</summary>
    public void Match(Action<T> ok, Action<E> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (_state == StateOk) ok(_value);
        else if (_state == StateErr) err(_error);
        else ThrowHelper.ThrowUninitialized();
    }

    /// <summary>Runs the action for the current case without a closure.</summary>
    public void Match<TState>(TState state, Action<TState, T> ok, Action<TState, E> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);
        if (_state == StateOk) ok(state, _value);
        else if (_state == StateErr) err(state, _error);
        else ThrowHelper.ThrowUninitialized();
    }

    /// <summary>Runs a side effect on the value.</summary>
    public Result<T, E> Tap(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == StateOk) action(_value);
        else if (_state != StateErr) ThrowHelper.ThrowUninitialized();
        return this;
    }

    /// <summary>Runs a side effect on the value without a closure.</summary>
    public Result<T, E> Tap<TState>(TState state, Action<TState, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == StateOk) action(state, _value);
        else if (_state != StateErr) ThrowHelper.ThrowUninitialized();
        return this;
    }

    /// <summary>Runs a side effect on the error.</summary>
    public Result<T, E> TapErr(Action<E> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == StateErr) action(_error);
        else if (_state != StateOk) ThrowHelper.ThrowUninitialized();
        return this;
    }

    /// <summary>Runs a side effect on the error without a closure.</summary>
    public Result<T, E> TapErr<TState>(TState state, Action<TState, E> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_state == StateErr) action(state, _error);
        else if (_state != StateOk) ThrowHelper.ThrowUninitialized();
        return this;
    }

    /// <summary>Returns the value, or <paramref name="defaultValue"/> if Err.</summary>
    public T UnwrapOr(T defaultValue)
    {
        if (_state == StateOk) return _value;
        if (_state == StateErr) return defaultValue;
        return ThrowHelper.ThrowUninitialized<T>();
    }

    /// <summary>Returns the value, or one computed from the error.</summary>
    public T UnwrapOrElse(Func<E, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        if (_state == StateOk) return _value;
        if (_state == StateErr) return fallback(_error);
        return ThrowHelper.ThrowUninitialized<T>();
    }

    /// <summary>Returns the value, or one computed from the error without a closure.</summary>
    public T UnwrapOrElse<TState>(TState state, Func<TState, E, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        if (_state == StateOk) return _value;
        if (_state == StateErr) return fallback(state, _error);
        return ThrowHelper.ThrowUninitialized<T>();
    }

    /// <inheritdoc/>
    public bool Equals(Result<T, E> other)
    {
        if (_state != other._state) return false;
        return _state switch
        {
            StateOk => EqualityComparer<T>.Default.Equals(_value, other._value),
            StateErr => EqualityComparer<E>.Default.Equals(_error, other._error),
            _ => true,
        };
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Result<T, E> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _state switch
    {
        StateOk => HashCode.Combine(StateOk, _value),
        StateErr => HashCode.Combine(StateErr, _error),
        _ => 0,
    };

    /// <inheritdoc/>
    public static bool operator ==(Result<T, E> left, Result<T, E> right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Result<T, E> left, Result<T, E> right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => _state switch
    {
        StateOk => $"Ok({_value})",
        StateErr => $"Err({_error})",
        _ => "Result(default)",
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ThrowIfDefault()
    {
        if (_state == StateDefault) ThrowHelper.ThrowUninitialized();
    }

    /// <summary>This Ok result if <paramref name="check"/> is Ok, otherwise its error.</summary>
    internal Result<T, E> Keep<U>(Result<U, E> check)
    {
        if (check._state == Result<U, E>.StateOk) return this;
        if (check._state == Result<U, E>.StateErr) return new Result<T, E>(new Err<E>(check._error));
        return ThrowHelper.ThrowUninitialized<Result<T, E>>();
    }
}
