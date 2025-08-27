using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Queries.Get;

public sealed class GetLocalizationTemplateQuery : IRequest<GetLocalizationTemplateResult>
{
    public string? Org { get; set; }
    public required string Id { get; init; }
}

[GenerateOneOf]
public sealed partial class GetLocalizationTemplateResult : OneOfBase<LocalizationTemplateSetDto, EntityNotFound, ValidationError>;

internal sealed class GetLocalizationTemplateQueryHandler : IRequestHandler<GetLocalizationTemplateQuery, GetLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserOrganizationRegistry _userOrganizationRegistry;

    public GetLocalizationTemplateQueryHandler(IDialogDbContext db, IUserOrganizationRegistry userOrganizationRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userOrganizationRegistry = userOrganizationRegistry ?? throw new ArgumentNullException(nameof(userOrganizationRegistry));
    }

    public async Task<GetLocalizationTemplateResult> Handle(GetLocalizationTemplateQuery query, CancellationToken cancellationToken)
    {
        query.Org ??= await _userOrganizationRegistry.GetCurrentUserOrgShortNameStrict(cancellationToken);

        var templateSet = await _db.LocalizationTemplateSets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Org == query.Org && x.Id == query.Id, cancellationToken);

        if (templateSet is null)
        {
            return new EntityNotFound<LocalizationTemplateSet>([query.Org, query.Id]);
        }

        return templateSet.ToDto();
    }
}
