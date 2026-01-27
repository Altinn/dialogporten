using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FastEndpoints;
using FluentValidation.Results;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

public static class EndpointExtensions
{
    extension(IEndpoint ep)
    {
        public Task BadRequestAsync(ValidationError failure, CancellationToken cancellationToken = default)
            => ep.BadRequestAsync(failure.Errors, cancellationToken);

        public Task BadRequestAsync(IEnumerable<ValidationFailure> failures, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(failures.ToList(), cancellation: cancellationToken);

        public Task BadRequestAsync(BadRequest badRequest, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(
                badRequest.ToValidationResults(),
                cancellation: cancellationToken);

        public Task PreconditionFailed(CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync([], StatusCodes.Status412PreconditionFailed, cancellation: cancellationToken);

        public Task NotFoundAsync(EntityNotFound notFound, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(
                notFound.ToValidationResults(),
                StatusCodes.Status404NotFound,
                cancellation: cancellationToken);

        public Task NotVisibleAsync(EntityNotVisible notVisible, CancellationToken cancellationToken = default)
        {
            ep.HttpContext.Response.Headers.Expires = notVisible.VisibleFrom.ToString("r");
            return ep.HttpContext.Response.SendErrorsAsync(
                notVisible.ToValidationResults(),
                StatusCodes.Status404NotFound,
                cancellation: cancellationToken);
        }

        public Task GoneAsync(EntityDeleted deleted, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(
                deleted.ToValidationResults(),
                StatusCodes.Status410Gone,
                cancellation: cancellationToken);

        public Task ForbiddenAsync(Forbidden forbidden, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(
                forbidden.ToValidationResults(),
                StatusCodes.Status403Forbidden,
                cancellation: cancellationToken);

        public Task UnprocessableEntityAsync(DomainError domainError, CancellationToken cancellationToken = default)
            => ep.HttpContext.Response.SendErrorsAsync(
                domainError.ToValidationResults(),
                StatusCodes.Status422UnprocessableEntity,
                cancellation: cancellationToken);

        public Task ConflictAsync(Conflict conflict, CancellationToken cancellationToken = default) =>
            ep.HttpContext.Response.SendErrorsAsync(
                conflict.ToValidationResults(),
                StatusCodes.Status409Conflict,
                cancellation: cancellationToken);
    }
}
