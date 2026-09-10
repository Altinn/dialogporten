using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;
using FastEndpoints;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

public static class EndpointExtensions
{
    public static Task BadRequestAsync(this IEndpoint ep, ValidationError failure, CancellationToken cancellationToken = default)
        => ep.BadRequestAsync(failure.Errors, cancellationToken);

    public static Task BadRequestAsync(this IEndpoint ep, IEnumerable<ValidationFailure> failures, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(failures.ToList(), cancellation: cancellationToken);

    public static Task BadRequestAsync(this IEndpoint ep, BadRequest badRequest, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            badRequest.ToValidationResults(),
            cancellation: cancellationToken);

    public static Task PreconditionFailed(this IEndpoint ep, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync([], StatusCodes.Status412PreconditionFailed, cancellation: cancellationToken);

    public static Task NotFoundAsync(this IEndpoint ep, EntityNotFound notFound, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            notFound.ToValidationResults(),
            StatusCodes.Status404NotFound,
            cancellation: cancellationToken);

    public static Task NotVisibleAsync(this IEndpoint ep, EntityNotVisible notVisible, CancellationToken cancellationToken = default)
    {
        ep.HttpContext.Response.Headers.Expires = notVisible.VisibleFrom.ToString("r");
        return ep.HttpContext.Response.SendProblemDetailsAsync(
            notVisible.ToValidationResults(),
            StatusCodes.Status404NotFound,
            cancellation: cancellationToken);
    }

    public static Task ExpiredAsync(this IEndpoint ep, EntityExpired expired, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            expired.ToValidationResults(),
            StatusCodes.Status404NotFound,
            cancellation: cancellationToken);

    public static Task GoneAsync(this IEndpoint ep, EntityDeleted deleted, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            deleted.ToValidationResults(),
            StatusCodes.Status410Gone,
            cancellation: cancellationToken);

    public static Task ForbiddenAsync(this IEndpoint ep, Forbidden forbidden, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            forbidden.ToValidationResults(),
            StatusCodes.Status403Forbidden,
            cancellation: cancellationToken);

    public static Task UnprocessableEntityAsync(this IEndpoint ep, DomainError domainError, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendProblemDetailsAsync(
            domainError.ToValidationResults(),
            StatusCodes.Status422UnprocessableEntity,
            cancellation: cancellationToken);

    public static Task ConflictAsync(this IEndpoint ep, Conflict conflict, CancellationToken cancellationToken = default) =>
        ep.HttpContext.Response.SendProblemDetailsAsync(
            conflict.ToValidationResults(),
            StatusCodes.Status409Conflict,
            cancellation: cancellationToken);
}
