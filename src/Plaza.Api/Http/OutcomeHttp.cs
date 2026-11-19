using Plaza.SharedKernel;

namespace Plaza.Api.Http;

/// <summary>Maps use-case failures to RFC 9457 ProblemDetails with a stable <c>code</c> extension.</summary>
internal static class OutcomeHttp
{
    public static IResult Problem(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        var (status, title) = failure.Kind switch
        {
            FailureKind.Validation => (StatusCodes.Status400BadRequest, "Invalid request"),
            FailureKind.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            FailureKind.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            FailureKind.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };
        return Results.Problem(
            statusCode: status,
            title: title,
            detail: failure.Message,
            extensions: new Dictionary<string, object?> { ["code"] = failure.Code });
    }

    public static IResult ToResult<T>(this Outcome<T> outcome, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return outcome.Succeeded ? onSuccess(outcome.Value) : Problem(outcome.Failure!);
    }
}
