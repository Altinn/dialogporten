using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

internal static class CommonMapExtensions
{
    public static List<Localization> ToGraphQlLocalizations(this List<LocalizationDto> source) =>
        source.Select(ToGraphQlLocalization).ToList();

    public static Localization ToGraphQlLocalization(this LocalizationDto source) => new()
    {
        Value = source.Value,
        LanguageCode = source.LanguageCode
    };

    public static ContentValue ToGraphQlContentValue(this ContentValueDto source) => new()
    {
        Value = source.Value.ToGraphQlLocalizations(),
        MediaType = source.MediaType,
        IsAuthorized = source.IsAuthorized
    };

    public static ContentValue? ToGraphQlContentValueOrNull(this ContentValueDto? source) =>
        source?.ToGraphQlContentValue();

    public static Actor ToGraphQlActor(this ActorDto source) => new()
    {
        ActorType = source.ActorType.ToGraphQl(),
        ActorId = source.ActorId,
        ActorName = source.ActorName
    };
}
