﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

public sealed class SearchDialogQuery : SortablePaginationParameter<SearchDialogQueryOrderDefinition, IntermediateSearchDialogDto>, IRequest<SearchDialogResult>
{
    private string? _searchLanguageCode;

    /// <summary>
    /// Filter by one or more service resources
    /// </summary>
    public List<string>? ServiceResource { get; init; }

    /// <summary>
    /// Filter by one or more owning parties
    /// </summary>
    public List<string>? Party { get; init; }

    /// <summary>
    /// Filter by end user id
    /// </summary>
    public string? EndUserId { get; init; }

    /// <summary>
    /// Filter by one or more extended statuses
    /// </summary>
    public List<string>? ExtendedStatus { get; init; }

    /// <summary>
    /// Filter by external reference
    /// </summary>
    public string? ExternalReference { get; init; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public List<DialogStatus.Values>? Status { get; init; }

    /// <summary>
    /// Only return dialogs created after this date
    /// </summary>
    public DateTimeOffset? CreatedAfter { get; init; }

    /// <summary>
    /// Only return dialogs created before this date
    /// </summary>
    public DateTimeOffset? CreatedBefore { get; init; }
    /// <summary>
    /// Only return dialogs updated after this date
    /// </summary>
    public DateTimeOffset? UpdatedAfter { get; init; }

    /// <summary>
    /// Only return dialogs updated before this date
    /// </summary>
    public DateTimeOffset? UpdatedBefore { get; init; }

    /// <summary>
    /// Only return dialogs with due date after this date
    /// </summary>
    public DateTimeOffset? DueAfter { get; init; }

    /// <summary>
    /// Only return dialogs with due date before this date
    /// </summary>
    public DateTimeOffset? DueBefore { get; init; }

    /// <summary>
    /// Only return dialogs with visible-from date after this date
    /// </summary>
    public DateTimeOffset? VisibleAfter { get; init; }

    /// <summary>
    /// Only return dialogs with visible-from date before this date
    /// </summary>
    public DateTimeOffset? VisibleBefore { get; init; }

    /// <summary>
    /// Filter by process
    /// </summary>
    public string? Process { get; init; }

    /// <summary>
    /// Filter by Display state 
    /// </summary>
    public List<SystemLabel.Values>? SystemLabel { get; set; }

    /// <summary>
    /// Search string for free text search. Will attempt to fuzzily match in all free text fields in the aggregate
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Limit free text search to texts with this language code, e.g. 'no', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes
    /// </summary>
    public string? SearchLanguageCode
    {
        get => _searchLanguageCode;
        init => _searchLanguageCode = Localization.NormalizeCultureCode(value);
    }
}

public sealed class SearchDialogQueryOrderDefinition : IOrderDefinition<IntermediateSearchDialogDto>
{
    public static IOrderOptions<IntermediateSearchDialogDto> Configure(IOrderOptionsBuilder<IntermediateSearchDialogDto> options) =>
        options.AddId(x => x.Id)
            .AddDefault("createdAt", x => x.CreatedAt)
            .AddOption("updatedAt", x => x.UpdatedAt)
            .AddOption("dueAt", x => x.DueAt)
            .Build();
}

[GenerateOneOf]
public sealed partial class SearchDialogResult : OneOfBase<PaginatedList<SearchDialogDto>, ValidationError>;

internal sealed class SearchDialogQueryHandler : IRequestHandler<SearchDialogQuery, SearchDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public SearchDialogQueryHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUserResourceRegistry userResourceRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _altinnAuthorization = altinnAuthorization;
    }

    public async Task<SearchDialogResult> Handle(SearchDialogQuery request, CancellationToken cancellationToken)
    {
        var resourceIds = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        var searchExpression = Expressions.LocalizedSearchExpression(request.Search, request.SearchLanguageCode);

        var dialogQuery = _db.Dialogs.AsQueryable();

        // If the service owner impersonates an end user, we need to filter the dialogs
        // based on the end user's authorization, not the service owner's (which is
        // allowed to see everything about every service resource they own).
        if (request.EndUserId is not null)
        {
            var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
                request.Party ?? [],
                request.ServiceResource ?? [],
                request.EndUserId,
                cancellationToken);
            dialogQuery = _db.Dialogs.PrefilterAuthorizedDialogs(authorizedResources);
        }

        var paginatedList = await dialogQuery
            .Include(x => x.Content)
            .ThenInclude(x => x.Value.Localizations)
            .WhereIf(!request.ServiceResource.IsNullOrEmpty(),
                x => request.ServiceResource!.Contains(x.ServiceResource))
            .WhereIf(!request.Party.IsNullOrEmpty(), x => request.Party!.Contains(x.Party))
            .WhereIf(!request.ExtendedStatus.IsNullOrEmpty(),
                x => x.ExtendedStatus != null && request.ExtendedStatus!.Contains(x.ExtendedStatus))
            .WhereIf(!string.IsNullOrWhiteSpace(request.ExternalReference),
                x => x.ExternalReference != null && request.ExternalReference == x.ExternalReference)
            .WhereIf(!request.Status.IsNullOrEmpty(), x => request.Status!.Contains(x.StatusId))
            .WhereIf(request.CreatedAfter.HasValue, x => request.CreatedAfter <= x.CreatedAt)
            .WhereIf(request.CreatedBefore.HasValue, x => x.CreatedAt <= request.CreatedBefore)
            .WhereIf(request.UpdatedAfter.HasValue, x => request.UpdatedAfter <= x.UpdatedAt)
            .WhereIf(request.UpdatedBefore.HasValue, x => x.UpdatedAt <= request.UpdatedBefore)
            .WhereIf(request.DueAfter.HasValue, x => request.DueAfter <= x.DueAt)
            .WhereIf(request.DueBefore.HasValue, x => x.DueAt <= request.DueBefore)
            .WhereIf(request.Process is not null, x => EF.Functions.ILike(x.Process!, request.Process!))
            .WhereIf(request.VisibleAfter.HasValue, x => request.VisibleAfter <= x.VisibleFrom)
            .WhereIf(request.VisibleBefore.HasValue, x => x.VisibleFrom <= request.VisibleBefore)
            .WhereIf(request.SystemLabel.IsNullOrEmpty(), x => request.SystemLabel!.Contains(x.DialogEndUserContext.SystemLabelId))
            .WhereIf(request.Search is not null, x =>
                x.Content.Any(x => x.Value.Localizations.AsQueryable().Any(searchExpression)) ||
                x.SearchTags.Any(x => EF.Functions.ILike(x.Value, request.Search!))
            )
            .Where(x => resourceIds.Contains(x.ServiceResource))
            .ProjectTo<IntermediateSearchDialogDto>(_mapper.ConfigurationProvider)
            .ToPaginatedListAsync(request, cancellationToken: cancellationToken);

        if (request.EndUserId is not null)
        {
            foreach (var seenRecord in paginatedList.Items.SelectMany(x => x.SeenSinceLastUpdate))
            {
                seenRecord.IsCurrentEndUser = seenRecord.SeenBy.ActorId == request.EndUserId;
            }
        }

        return paginatedList.ConvertTo(_mapper.Map<SearchDialogDto>);
    }
}
