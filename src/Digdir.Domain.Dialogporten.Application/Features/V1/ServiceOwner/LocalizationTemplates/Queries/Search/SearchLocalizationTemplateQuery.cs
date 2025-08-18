using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Queries.Search;

public class SearchLocalizationTemplateQuery : IRequest<SearchLocalizationTemplateResult>
{
    public string? Org { get; set; }
}

[GenerateOneOf]
public sealed partial class SearchLocalizationTemplateResult : OneOfBase<List<string>, EntityNotFound, ValidationError>;

internal sealed class SearchLocalizationTemplateQueryHandler : IRequestHandler<SearchLocalizationTemplateQuery, SearchLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserOrganizationRegistry _userOrganizationRegistry;

    public SearchLocalizationTemplateQueryHandler(IDialogDbContext db, IUserOrganizationRegistry userOrganizationRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userOrganizationRegistry = userOrganizationRegistry ?? throw new ArgumentNullException(nameof(userOrganizationRegistry));
    }

    public async Task<SearchLocalizationTemplateResult> Handle(SearchLocalizationTemplateQuery query, CancellationToken cancellationToken)
    {
        query.Org ??= await _userOrganizationRegistry.GetCurrentUserOrgShortNameStrict(cancellationToken);

        return await _db
            .LocalizationTemplateSets
            .Where(x => x.Org == query.Org)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
