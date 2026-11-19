namespace Plaza.SharedKernel;

/// <summary>Why a use case did not succeed. The API maps each kind to one HTTP status.</summary>
public enum FailureKind
{
    None = 0,
    Validation,
    NotFound,
    Forbidden,
    Conflict,
}

/// <summary>A failure with a stable machine code (for clients) and a human message.</summary>
public sealed record Failure(FailureKind Kind, string Code, string Message)
{
    public static Failure Validation(string code, string message) => new(FailureKind.Validation, code, message);

    public static Failure NotFound(string code, string message) => new(FailureKind.NotFound, code, message);

    public static Failure Forbidden(string code, string message) => new(FailureKind.Forbidden, code, message);

    public static Failure Conflict(string code, string message) => new(FailureKind.Conflict, code, message);
}

/// <summary>Result of a use case: either a value or a failure. No exceptions for expected outcomes.</summary>
public sealed class Outcome<T>
{
    private readonly T? _value;

    internal Outcome(T? value, Failure? failure)
    {
        _value = value;
        Failure = failure;
    }

    public Failure? Failure { get; }

    public bool Succeeded => Failure is null;

    public T Value => Succeeded
        ? _value!
        : throw new InvalidOperationException($"Outcome failed: {Failure!.Code}");

    public static implicit operator Outcome<T>(Failure failure) => new(default, failure);
}

/// <summary>Factory methods for <see cref="Outcome{T}"/>.</summary>
public static class Outcome
{
    public static Outcome<T> Ok<T>(T value) => new(value, null);

    public static Outcome<T> Fail<T>(Failure failure) => new(default, failure);
}
