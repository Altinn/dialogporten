using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using DialogportenAuthorizationPolicy = Digdir.Domain.Dialogporten.WebApi.Common.Authorization.AuthorizationPolicy;
using IMapper = AutoMapper.IMapper;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Patch;

/* Since minimal apis don't support JSON patch documents out of the box,
 * and we've not been able to find a good alternative library for it yet,
 * we've decided to use the old way of doing it using controllers and
 * NewtonsoftJson. System.Text.Json will still be used for everything
 * else. */

[ApiController]
[Route("api/v1/serviceowner/dialogs")]
[Tags(ServiceOwnerGroup.RoutePrefix)]
[Authorize(Policy = DialogportenAuthorizationPolicy.ServiceProvider)]
public sealed class PatchDialogsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public PatchDialogsController(ISender sender, IMapper mapper)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Patch a single dialog
    /// </summary>
    /// <remarks>
    /// Patches a dialog aggregate with a RFC6902 JSON Patch document. The patch document must be a JSON array of RFC6902 operations.
    /// See [https://tools.ietf.org/html/rfc6902](https://tools.ietf.org/html/rfc6902) for more information.
    ///
    /// Optimistic concurrency control is implemented using the If-Match header. Supply the Revision value from the GetDialog endpoint to ensure that the dialog is not modified/deleted by another request in the meantime.
    /// </remarks>
    /// <response code="204">Patch was successfully applied.</response>
    /// <response code="400">Validation error occurred. See problem details for a list of errors.</response>
    /// <response code="401">Missing or invalid authentication token. Requires a Maskinporten-token with the scope \"digdir:dialogporten.serviceprovider\"</response>
    /// <response code="403">Unauthorized to update a dialog for the given serviceResource (not owned by authenticated organization or has additional scope requirements defined in policy)</response>
    /// <response code="404">The given dialog ID was not found or is deleted</response>
    /// <response code="410">The dialog with the given key is removed</response>
    /// <response code="412">The supplied Revision does not match the current Revision of the dialog</response>
    /// <response code="422">Domain error occurred. See problem details for a list of errors.</response>
    [HttpPatch("{dialogId}")]

    [OpenApiOperation("V1ServiceOwnerDialogsPatchDialog")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseHeader(StatusCodes.Status204NoContent, Constants.ETag, "The new UUID ETag of the dialog", "123e4567-e89b-12d3-a456-426614174000")]
    public async Task<IActionResult> Patch(
        [FromRoute] Guid dialogId,
        [FromHeader(Name = Constants.IfMatch)] Guid? etag,
        [FromBody] JsonPatchDocument<UpdateDialogDto> patchDocument,
        [OpenApiIgnore][FromQuery] bool? disableAltinnEvents,
        CancellationToken ct)
    {
        var dialogQueryResult = await _sender.Send(new GetDialogQuery { DialogId = dialogId }, ct);
        if (!dialogQueryResult.TryPickT0(out var dialog, out var errors))
        {
            return errors.Match<IActionResult>(
                notFound => NotFound(HttpContext.GetResponseOrDefault(StatusCodes.Status404NotFound,
                    notFound.ToValidationResults())),
                validationFailed =>
                    BadRequest(HttpContext.GetResponseOrDefault(StatusCodes.Status400BadRequest,
                        validationFailed.Errors.ToList())));
        }

        var updateDialogDto = _mapper.Map<UpdateDialogDto>(dialog);
        patchDocument.ApplyTo(updateDialogDto, ModelState);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new UpdateDialogCommand
        {
            Id = dialogId,
            IfMatchDialogRevision = etag,
            Dto = updateDialogDto,
            DisableAltinnEvents = disableAltinnEvents ?? false,
        };

        var result = await _sender.Send(command, ct);
        return result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return (IActionResult)NoContent();
            },
            notFound => NotFound(HttpContext.GetResponseOrDefault(StatusCodes.Status404NotFound, notFound.ToValidationResults())),
            entityDeleted => StatusCode(StatusCodes.Status410Gone, HttpContext.GetResponseOrDefault(StatusCodes.Status410Gone, entityDeleted.ToValidationResults())),
            validationFailed => BadRequest(HttpContext.GetResponseOrDefault(StatusCodes.Status400BadRequest, validationFailed.Errors.ToList())),
            forbidden => new ObjectResult(HttpContext.GetResponseOrDefault(StatusCodes.Status403Forbidden, forbidden.ToValidationResults())),
            domainError => UnprocessableEntity(HttpContext.GetResponseOrDefault(StatusCodes.Status422UnprocessableEntity, domainError.ToValidationResults())),
            concurrencyError => new ObjectResult(HttpContext.GetResponseOrDefault(StatusCodes.Status412PreconditionFailed)) { StatusCode = StatusCodes.Status412PreconditionFailed }
        );
    }
}
