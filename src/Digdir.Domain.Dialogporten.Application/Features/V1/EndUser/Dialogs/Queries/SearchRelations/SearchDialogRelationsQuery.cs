using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Relations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations;

public sealed class SearchDialogRelationsQuery : IRequest<SearchDialogRelationsResult>, IFeatureMetricServiceResourceThroughDialogIdRequest, IDialogRelationPagination
{
    public Guid DialogId { get; set; }

    /// <summary>
    /// Only return relations of this type, as seen from the dialog.
    /// </summary>
    public DialogRelationType.Values? Type { get; init; }

    /// <summary>
    /// Filter by one or more parties owning the related dialogs.
    /// </summary>
    public List<string>? Party { get; init; }

    /// <summary>
    /// Filter by one or more service resources of the related dialogs.
    /// </summary>
    public List<string>? ServiceResource { get; init; }

    /// <summary>
    /// Filter by one or more statuses of the related dialogs.
    /// </summary>
    public List<DialogStatus.Values>? Status { get; init; }

    /// <summary>
    /// If set to true, related dialogs with isApiOnly set to true are left out.
    /// </summary>
    public bool? ExcludeApiOnly { get; init; }

    /// <summary>
    /// Continuation token for pagination.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Limit the number of results returned. Defaults to 100, maximum 1000.
    /// </summary>
    public int? Limit { get; init; }

    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }
}

public sealed class DialogRelationDto
{
    /// <summary>
    /// The type of the relation, as seen from the dialog in the path. "Parent" means that the related dialog is
    /// the parent of that dialog, and "Child" that it is one of its children.
    /// </summary>
    public DialogRelationType.Values RelationType { get; set; }

    /// <summary>
    /// The related dialog.
    /// </summary>
    public RelatedDialogDto Dialog { get; set; } = null!;
}

/// <summary>
/// A subset of the dialog search list item.
/// </summary>
public sealed class RelatedDialogDto
{
    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The service owner code representing the organization (service owner) related to this dialog.
    /// </summary>
    /// <example>ske</example>
    public string Org { get; set; } = null!;

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// </summary>
    /// <example>urn:altinn:resource:some-service-identifier</example>
    public string ServiceResource { get; set; } = null!;

    /// <summary>
    /// The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).
    /// </summary>
    public string ServiceResourceType { get; set; } = null!;

    /// <summary>
    /// The party code representing the organization or person that the dialog belongs to in URN format.
    /// </summary>
    /// <example>urn:altinn:person:identifier-no:01125512345</example>
    public string Party { get; set; } = null!;

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    public DialogStatus.Values Status { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// the dialog to further specify the "status" enum.
    /// </summary>
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog content was last updated.
    /// </summary>
    public DateTimeOffset ContentUpdatedAt { get; set; }

    /// <summary>
    /// The due date for the dialog. This is the last date when the dialog is expected to be completed.
    /// </summary>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// </summary>
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// Indicates whether the related dialog has relations of its own.
    /// </summary>
    public bool HasRelations { get; set; }

    /// <summary>
    /// The content of the dialog in search results.
    /// </summary>
    public ContentDto Content { get; set; } = null!;
}

[GenerateOneOf]
public sealed partial class SearchDialogRelationsResult : OneOfBase<PaginatedList<DialogRelationDto>, EntityNotFound, ValidationError>;

internal sealed class SearchDialogRelationsQueryValidator : AbstractValidator<SearchDialogRelationsQuery>
{
    public SearchDialogRelationsQueryValidator()
    {
        RuleFor(x => x.DialogId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleForEach(x => x.Status).IsInEnum();
        RuleFor(x => x.Party).Must(x => x is null || x.Count <= 100).WithMessage("At most 100 parties can be given.");
        RuleFor(x => x.ServiceResource).Must(x => x is null || x.Count <= 20).WithMessage("At most 20 service resources can be given.");
        Include(new DialogRelationPaginationValidator());
    }
}

internal sealed class SearchDialogRelationsQueryHandler : IRequestHandler<SearchDialogRelationsQuery, SearchDialogRelationsResult>
{
    private readonly IDialogDbContext _db;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly IDialogRelationRepository _relationRepository;
    private readonly IDialogSearchRepository _searchRepository;
    private readonly IUser _user;
    private readonly IClock _clock;

    public SearchDialogRelationsQueryHandler(
        IDialogDbContext db,
        IAltinnAuthorization altinnAuthorization,
        IDialogRelationRepository relationRepository,
        IDialogSearchRepository searchRepository,
        IUser user,
        IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
        _relationRepository = relationRepository ?? throw new ArgumentNullException(nameof(relationRepository));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<SearchDialogRelationsResult> Handle(SearchDialogRelationsQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNowOffset;

        // The anchor dialog must be visible to the user under list semantics, as in search. No PDP calls are made.
        var anchor = await _db.Dialogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DialogId, cancellationToken);

        if (anchor is null
            || anchor.VisibleFrom > now
            || anchor.ExpiresAt <= now
            || !await _altinnAuthorization.HasListAuthorizationForDialog(anchor, cancellationToken))
        {
            return new EntityNotFound<DialogEntity>(request.DialogId);
        }

        var limit = request.GetLimit();
        if (!anchor.HasRelations)
        {
            return DialogRelationPagination.ToPage(Array.Empty<DialogRelationDto>(), limit, x => x.Dialog.Id, x => x);
        }

        var parties = request.Party?.Select(x => x.ToLowerInvariant()).ToList() ?? [];
        var serviceResources = request.ServiceResource ?? [];
        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            parties,
            serviceResources,
            cancellationToken: cancellationToken);

        if (authorizedResources.HasNoAuthorizations)
        {
            return DialogRelationPagination.ToPage(Array.Empty<DialogRelationDto>(), limit, x => x.Dialog.Id, x => x);
        }

        var relatedDialogs = await _relationRepository.SearchAsEndUser(
            new EndUserDialogRelationsQuery(
                anchor.Id,
                authorizedResources,
                now,
                request.Type,
                parties,
                serviceResources,
                request.Status ?? [],
                request.ExcludeApiOnly ?? false,
                request.GetAfter(),
                limit + 1),
            cancellationToken);

        var contentByDialogId = await FetchContentByDialogId(
            relatedDialogs.Take(limit).Select(x => x.RelatedDialogId).ToArray(),
            request.AcceptedLanguages,
            cancellationToken);

        return DialogRelationPagination.ToPage(
            relatedDialogs,
            limit,
            x => x.RelatedDialogId,
            x => new DialogRelationDto
            {
                RelationType = (DialogRelationType.Values)x.TypeId,
                Dialog = new RelatedDialogDto
                {
                    Id = x.RelatedDialogId,
                    Org = x.Org,
                    ServiceResource = x.ServiceResource,
                    ServiceResourceType = x.ServiceResourceType,
                    Party = x.Party,
                    Status = (DialogStatus.Values)x.StatusId,
                    ExtendedStatus = x.ExtendedStatus,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    ContentUpdatedAt = x.ContentUpdatedAt,
                    DueAt = x.DueAt,
                    IsApiOnly = x.IsApiOnly,
                    HasRelations = x.HasRelations,
                    Content = contentByDialogId[x.RelatedDialogId]
                }
            });
    }

    private async Task<Dictionary<Guid, ContentDto>> FetchContentByDialogId(
        Guid[] dialogIds,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken)
    {
        if (dialogIds.Length == 0)
        {
            return [];
        }

        // The same content as in search, including the non-sensitive fallback when the authentication level is too low.
        var userAuthLevel = _user.GetPrincipal().GetAuthenticationLevel();
        var result = await _searchRepository.FetchContentByDialogId(dialogIds, userAuthLevel, cancellationToken);
        return result.ToDictionary(x => x.Key, x =>
        {
            var content = new ContentDto
            {
                Title = ToContentValueDto(x.Value.Title)!,
                Summary = ToContentValueDto(x.Value.Summary),
                ExtendedStatus = ToContentValueDto(x.Value.ExtendedStatus),
                SenderName = ToContentValueDto(x.Value.SenderName),
            };

            foreach (var localizations in new[] { content.Title, content.Summary, content.ExtendedStatus, content.SenderName }
                         .OfType<ContentValueDto>()
                         .Select(v => v.Value))
            {
                localizations.PruneLocalizations(acceptedLanguages);
            }

            return content;
        });

        static ContentValueDto? ToContentValueDto(DataContentValueDto? dataContent)
        {
            return dataContent is null
                ? null
                : new ContentValueDto
                {
                    MediaType = dataContent.MediaType,
                    Value = dataContent.Value
                        .Select(l => new LocalizationDto { LanguageCode = l.LanguageCode, Value = l.Value })
                        .ToList()
                };
        }
    }
}
