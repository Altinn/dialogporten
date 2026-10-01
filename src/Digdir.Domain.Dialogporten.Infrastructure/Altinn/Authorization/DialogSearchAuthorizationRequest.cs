using System.Security.Claims;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed class DialogSearchAuthorizationRequest
{
    public required IPartyIdentifier EndUserPartyIdentifier { get; set; }
    public required List<Claim> Claims { get; init; }
    public List<string> ConstraintParties { get; set; } = [];
    public List<string> ConstraintServiceResources { get; set; } = [];
}
