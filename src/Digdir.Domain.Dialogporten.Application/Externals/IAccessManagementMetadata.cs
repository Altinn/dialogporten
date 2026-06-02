using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IAccessManagementMetadata
{
    Task<AccessManagementMetadata> GetMetadata(CancellationToken cancellationToken);
}

public sealed record AccessManagementMetadata(
    IReadOnlyDictionary<string, AccessManagementRoleMetadata> RolesBySubject,
    IReadOnlyDictionary<string, AccessManagementAccessPackageMetadata> AccessPackagesBySubject);

public sealed record AccessManagementRoleMetadata(
    Guid Id,
    string Urn,
    IReadOnlyList<LocalizationDto> Name,
    LinkDto Links);

public sealed record AccessManagementAccessPackageMetadata(
    string Urn,
    IReadOnlyList<LocalizationDto> Name,
    LinkDto Links);
