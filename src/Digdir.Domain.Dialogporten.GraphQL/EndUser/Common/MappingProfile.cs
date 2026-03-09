using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using ApplicationDialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using GetDialogActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogActivityDto;
using GetDialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogSeenLogDto;
using SearchDialogActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogActivityDto;
using SearchDialogSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogSeenLogDto;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

internal static class GraphQlMapper
{
    extension(LocalizationDto source)
    {
        public Localization ToGraphQl() => new()
        {
            Value = source.Value,
            LanguageCode = source.LanguageCode
        };
    }

    extension(ContentValueDto source)
    {
        public ContentValue ToGraphQl() => new()
        {
            Value = source.Value?.Select(localization => localization.ToGraphQl()).ToList() ?? [],
            MediaType = source.MediaType
        };
    }

    extension(GetDialogSeenLogDto source)
    {
        public SeenLog ToGraphQl() => new()
        {
            Id = source.Id,
            SeenAt = source.SeenAt,
            SeenBy = source.SeenBy.ToGraphQl(),
            IsViaServiceOwner = source.IsViaServiceOwner,
            IsCurrentEndUser = source.IsCurrentEndUser
        };
    }

    extension(SearchDialogSeenLogDto source)
    {
        public SeenLog ToGraphQl() => new()
        {
            Id = source.Id,
            SeenAt = source.SeenAt,
            SeenBy = source.SeenBy.ToGraphQl(),
            IsViaServiceOwner = source.IsViaServiceOwner,
            IsCurrentEndUser = source.IsCurrentEndUser
        };
    }

    extension(GetDialogActivityDto source)
    {
        public Activity ToGraphQl() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = (ActivityType)source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToGraphQl(),
            Description = source.Description?.Select(localization => localization.ToGraphQl()).ToList() ?? []
        };
    }

    extension(SearchDialogActivityDto source)
    {
        public Activity ToGraphQl() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = (ActivityType)source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToGraphQl(),
            Description = source.Description?.Select(localization => localization.ToGraphQl()).ToList() ?? []
        };
    }

    extension(ActorDto source)
    {
        public Actor ToGraphQl() => new()
        {
            ActorType = (ActorType)source.ActorType,
            ActorId = source.ActorId,
            ActorName = source.ActorName
        };
    }

    extension(ApplicationDialogStatus.Values source)
    {
        public DialogStatus ToGraphQl() => source switch
        {
            ApplicationDialogStatus.Values.NotApplicable => DialogStatus.NotApplicable,
            ApplicationDialogStatus.Values.InProgress => DialogStatus.InProgress,
            ApplicationDialogStatus.Values.Draft => DialogStatus.Draft,
            ApplicationDialogStatus.Values.Awaiting => DialogStatus.Awaiting,
            ApplicationDialogStatus.Values.RequiresAttention => DialogStatus.RequiresAttention,
            ApplicationDialogStatus.Values.Completed => DialogStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
