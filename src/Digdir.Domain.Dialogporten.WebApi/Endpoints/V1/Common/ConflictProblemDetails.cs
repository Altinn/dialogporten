using Digdir.Domain.Dialogporten.WebApi.Common;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common;

[OpenApiTypeName("ConflictProblemDetails")]
public sealed class ConflictProblemDetails(IDictionary<string, string[]> errors) : ValidationProblemDetails(errors)
{
    /// <summary>
    /// All conflicting idempotent keys.
    /// Set if applicable, otherwise null.
    /// </summary>
    public List<string>? ConflictingIdempotentKeys { get; set; }

    /// <summary>
    /// The conflicting DialogId.
    /// Set if applicable, otherwise null.
    /// </summary>
    public Guid? ConflictingDialogId { get; set; }
}
