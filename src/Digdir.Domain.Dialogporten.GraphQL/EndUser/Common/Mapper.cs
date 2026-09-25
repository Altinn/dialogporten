using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

internal static class EnumMapExtensions
{
    /// <summary>
    /// Maps one enum to another by member name, falling back to the numeric value when no name
    /// matches. This replicates AutoMapper's default enum-to-enum conversion, where the domain and
    /// API enums share member names but may use different underlying values (e.g. DialogStatus.Awaiting).
    /// </summary>
    public static TDestination MapByName<TDestination>(this Enum source)
        where TDestination : struct, Enum =>
        Enum.TryParse<TDestination>(source.ToString(), ignoreCase: true, out var destination)
            ? destination
            : (TDestination)Enum.ToObject(typeof(TDestination), Convert.ToInt64(source, CultureInfo.InvariantCulture));
}

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
        ActorType = source.ActorType.MapByName<ActorType>(),
        ActorId = source.ActorId,
        ActorName = source.ActorName
    };
}
