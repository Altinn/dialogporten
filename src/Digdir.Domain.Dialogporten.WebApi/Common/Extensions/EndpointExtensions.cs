using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FastEndpoints;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

public static class EndpointExtensions
{
    public static Task BadRequestAsync(this IEndpoint ep, ValidationError failure, CancellationToken cancellationToken = default)
        => ep.BadRequestAsync(failure.Errors, cancellationToken);

    public static Task BadRequestAsync(this IEndpoint ep, IEnumerable<ValidationFailure> failures, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(failures.ToList(), cancellation: cancellationToken);

    public static Task BadRequestAsync(this IEndpoint ep, BadRequest badRequest, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(
            badRequest.ToValidationResults(),
            cancellation: cancellationToken);

    public static Task PreconditionFailed(this IEndpoint ep, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync([], StatusCodes.Status412PreconditionFailed, cancellation: cancellationToken);

    public static Task NotFoundAsync(this IEndpoint ep, EntityNotFound notFound, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(
            notFound.ToValidationResults(),
            StatusCodes.Status404NotFound,
            cancellation: cancellationToken);

    public static Task GoneAsync(this IEndpoint ep, EntityDeleted deleted, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(
            deleted.ToValidationResults(),
            StatusCodes.Status410Gone,
            cancellation: cancellationToken);

    public static Task ForbiddenAsync(this IEndpoint ep, Forbidden forbidden, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(
            forbidden.ToValidationResults(),
            StatusCodes.Status403Forbidden,
            cancellation: cancellationToken);

    public static Task UnprocessableEntityAsync(this IEndpoint ep, DomainError domainError, CancellationToken cancellationToken = default)
        => ep.HttpContext.Response.SendErrorsAsync(
            domainError.ToValidationResults(),
            StatusCodes.Status422UnprocessableEntity,
            cancellation: cancellationToken);

    public static Task ConflictAsync(this IEndpoint ep, Conflict conflict, CancellationToken cancellationToken = default) =>
        ep.HttpContext.Response.SendErrorsAsync(
            conflict.ToValidationResults(),
            StatusCodes.Status409Conflict,
            cancellation: cancellationToken);
}
