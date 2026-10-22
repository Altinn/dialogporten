using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using DialogRelationTypeValues = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations.DialogRelationType.Values;
using AppDialogRelationDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations.DialogRelationDto;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogRelations;

internal static class Mapper
{
    internal static DialogRelationsPayload ToPayload(this PaginatedList<AppDialogRelationDto> source) => new()
    {
        Items = source.Items.Select(ToDialogRelation).ToList(),
        HasNextPage = source.HasNextPage,
        ContinuationToken = source.ContinuationToken
    };

    private static DialogRelation ToDialogRelation(AppDialogRelationDto source) => new()
    {
        RelationType = source.RelationType.ToGraphQl(),
        Dialog = new RelatedDialog
        {
            Id = source.Dialog.Id,
            Org = source.Dialog.Org,
            ServiceResource = source.Dialog.ServiceResource,
            ServiceResourceType = source.Dialog.ServiceResourceType,
            Party = source.Dialog.Party,
            Status = source.Dialog.Status.ToGraphQl(),
            ExtendedStatus = source.Dialog.ExtendedStatus,
            CreatedAt = source.Dialog.CreatedAt,
            UpdatedAt = source.Dialog.UpdatedAt,
            ContentUpdatedAt = source.Dialog.ContentUpdatedAt,
            DueAt = source.Dialog.DueAt,
            IsApiOnly = source.Dialog.IsApiOnly,
            HasRelations = source.Dialog.HasRelations,
            Content = SearchDialogsMapExtensions.ToSearchContent(source.Dialog.Content)
        }
    };

    internal static DialogRelationType ToGraphQl(this DialogRelationTypeValues source) => source switch
    {
        DialogRelationTypeValues.Parent => DialogRelationType.Parent,
        DialogRelationTypeValues.Child => DialogRelationType.Child,
        DialogRelationTypeValues.Related => DialogRelationType.Related,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    internal static DialogRelationTypeValues ToDomain(this DialogRelationType source) => source switch
    {
        DialogRelationType.Parent => DialogRelationTypeValues.Parent,
        DialogRelationType.Child => DialogRelationTypeValues.Child,
        DialogRelationType.Related => DialogRelationTypeValues.Related,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };
}
