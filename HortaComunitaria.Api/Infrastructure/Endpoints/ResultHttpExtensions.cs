using HortaComunitaria.Api.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class ResultHttpExtensions
{
    internal static IResult ToHttpResult<T>(this Result<T> result, HttpContext context, Func<T, IResult>? success = null) =>
        result.Match<IResult>(success ?? (value => Results.Ok(value)), error =>
        {
            var status = error.Category switch
            {
                ErrorCategory.Validation => 400,
                ErrorCategory.NotFound => 404,
                ErrorCategory.Conflict => 409,
                _ => throw new InvalidOperationException("Categoria de erro sem tradução.")
            };
            ProblemDetails problem = error.Fields is null
                ? new ProblemDetails()
                : new HttpValidationProblemDetails(error.Fields);
            problem.Status = status;
            problem.Title = error.Message;
            problem.Extensions["code"] = error.Code;
            problem.Extensions["traceId"] = context.Items["TraceId"] ?? context.TraceIdentifier;
            return Results.Problem(problem);
        });
}
