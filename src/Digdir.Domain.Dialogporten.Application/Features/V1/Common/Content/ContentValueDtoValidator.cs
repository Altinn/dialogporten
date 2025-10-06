using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

// DialogContentValueDtoValidator has constructor parameter input, and can't be registered in DI assembly scan
// IIgnoreOnAssemblyScan is used to ignore the class when scanning for validators
// The validator is manually created in the Create and Update validators
internal sealed class ContentValueDtoValidator : AbstractValidator<ContentValueDto>, IIgnoreOnAssemblyScan
{
    private const string CorrespondenceScope = "altinn.dialogporten.correspondence";
    public ContentValueDtoValidator(DialogTransmissionContentType contentType, IUser? user = null)
    {
        var allowedMediaTypes = GetAllowedMediaTypes(contentType, user);
        RuleFor(x => x.MediaType)
            .NotEmpty()
            .Must(value => value is not null && allowedMediaTypes.Contains(value))
            .WithMessage($"{{PropertyName}} '{{PropertyValue}}' is not allowed for content type {contentType.Name}. " +
                         $"Allowed media types are {string.Join(", ", allowedMediaTypes
                             // Removing the deprecated values from the list of allowed media types in the error message
                             .Where(x => !x.Equals(MediaTypes.EmbeddableMarkdownDeprecated, StringComparison.Ordinal) &&
                                         !x.Equals(MediaTypes.LegacyEmbeddableHtmlDeprecated, StringComparison.Ordinal))
                             .Select(x => $"'{x}'"))}");

        When(x =>
            x.MediaType is not null
            && x.MediaType.StartsWith(MediaTypes.EmbeddablePrefix, StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleForEach(x => x.Value)
                    .ChildRules(x => x
                        .RuleFor(x => x.Value)
                        .IsValidHttpsUrl());
            });

        RuleFor(x => x.Value)
            .NotEmpty()
            .SetValidator(_ =>
            {
                var maxLength = user!.GetPrincipal().HasScope(CorrespondenceScope)
                    ? contentType.CorrespondenceMaxLength
                    : contentType.MaxLength;
                return new LocalizationDtosValidator(maxLength);
            });

        RuleForEach(x => x.Value)
            .ContainsValidHtml()
            .When(x => x.MediaType is MediaTypes.LegacyHtml);
    }

    public ContentValueDtoValidator(DialogContentType contentType, IUser? user = null)
    {

        var allowedMediaTypes = GetAllowedMediaTypes(contentType, user);
        RuleFor(x => x.MediaType)
            .NotEmpty()
            .Must(value => value is not null && allowedMediaTypes.Contains(value))
            .WithMessage($"{{PropertyName}} '{{PropertyValue}}' is not allowed for content type {contentType.Name}. " +
                         $"Allowed media types are {string.Join(", ", allowedMediaTypes
                             // Removing the deprecated values from the list of allowed media types in the error message
                             .Where(x => !x.Equals(MediaTypes.EmbeddableMarkdownDeprecated, StringComparison.Ordinal) &&
                                         !x.Equals(MediaTypes.LegacyEmbeddableHtmlDeprecated, StringComparison.Ordinal))
                             .Select(x => $"'{x}'"))}");

        When(x =>
                x.MediaType is not null
                && x.MediaType.StartsWith(MediaTypes.EmbeddablePrefix, StringComparison.InvariantCultureIgnoreCase),
            () =>
            {
                RuleForEach(x => x.Value)
                    .ChildRules(x => x
                        .RuleFor(x => x.Value)
                        .IsValidHttpsUrl());
            });

        RuleFor(x => x.Value)
            .NotEmpty()
            .SetValidator(_ =>
            {
                var maxLength = user!.GetPrincipal().HasScope(CorrespondenceScope)
                    ? contentType.CorrespondenceMaxLength
                    : contentType.MaxLength;
                return new LocalizationDtosValidator(maxLength);
            });

        RuleForEach(x => x.Value)
            .ContainsValidHtml()
            .When(x => x.MediaType is MediaTypes.LegacyHtml);
    }

    [SuppressMessage("Style", "IDE0072:Add missing cases")]
    private static string[] GetAllowedMediaTypes(DialogContentType contentType, IUser? user)
        => contentType.Id switch
        {
            DialogContentType.Values.AdditionalInfo when UserHasLegacyHtmlScope(user)
                => contentType.AllowedMediaTypes.Append(MediaTypes.LegacyHtml).ToArray(),
            DialogContentType.Values.MainContentReference when UserHasLegacyHtmlScope(user)
                => contentType.AllowedMediaTypes
                    .Append(MediaTypes.LegacyEmbeddableHtml)
                    // Manually adding this for backwards compatibility
                    // until correspondence is updated and deployed
                    // TODO: https://github.com/Altinn/dialogporten/issues/1782
                    .Append(MediaTypes.EmbeddableMarkdownDeprecated)
                    .Append(MediaTypes.LegacyEmbeddableHtmlDeprecated)
                    .ToArray(),
            DialogContentType.Values.MainContentReference
                => contentType.AllowedMediaTypes
                    .Append(MediaTypes.EmbeddableMarkdownDeprecated)
                    .ToArray(),
            _ => contentType.AllowedMediaTypes
        };

    [SuppressMessage("Style", "IDE0072:Add missing cases")]
    private static string[] GetAllowedMediaTypes(DialogTransmissionContentType contentType, IUser? user)
        => contentType.Id switch
        {
            DialogTransmissionContentType.Values.ContentReference when UserHasLegacyHtmlScope(user)
                => contentType.AllowedMediaTypes
                    .Append(MediaTypes.LegacyEmbeddableHtml)
                    // Manually adding this for backwards compatibility
                    // until correspondence is updated and deployed
                    // TODO: https://github.com/Altinn/dialogporten/issues/1782
                    .Append(MediaTypes.EmbeddableMarkdownDeprecated)
                    .Append(MediaTypes.LegacyEmbeddableHtmlDeprecated)
                    .ToArray(),
            DialogTransmissionContentType.Values.ContentReference
                => contentType.AllowedMediaTypes
                    .Append(MediaTypes.EmbeddableMarkdownDeprecated)
                    .ToArray(),
            _ => contentType.AllowedMediaTypes
        };

    private static bool UserHasLegacyHtmlScope(IUser? user)
        => user is not null && user.GetPrincipal().HasScope(AuthorizationScope.LegacyHtmlScope);
}
