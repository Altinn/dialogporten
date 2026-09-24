using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;

/// <summary>
/// Normalizes the reduced <see cref="DialogContentSummary"/> returned by list endpoints into the full
/// <see cref="Content"/> model. Shared <c>ContentValue</c>s are reused by reference. The summary omits
/// <c>AdditionalInfo</c> and <c>MainContentReference</c>, which are left null on the result.
/// </summary>
internal static class ContentMappingExtensions
{
    internal static Content ToContent(this DialogContentSummary source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        SenderName = source.SenderName,
        ExtendedStatus = source.ExtendedStatus,
    };
}
