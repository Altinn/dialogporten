using Digdir.Domain.Dialogporten.Application;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authentication;

internal sealed class DialogportenOpenApiOptionsValidator : AbstractValidator<OpenApiSettings>
{
    public DialogportenOpenApiOptionsValidator()
    {
        RuleFor(x => x.IdportenLogoutUrl)
            .NotEmpty()
            .WithMessage($"{nameof(OpenApiSettings.IdportenLogoutUrl)} must not be null or empty");
        RuleFor(x => x.IdportenAuthorizationUrl)
            .NotEmpty()
            .WithMessage($"{nameof(OpenApiSettings.IdportenAuthorizationUrl)} must not be null or empty");
        RuleFor(x => x.IdportenTokenUrl)
            .NotEmpty()
            .WithMessage($"{nameof(OpenApiSettings.IdportenTokenUrl)} must not be null or empty");
        RuleFor(x => x.MaskinportenTokenUrl)
            .NotEmpty()
            .WithMessage($"{nameof(OpenApiSettings.MaskinportenTokenUrl)} must not be null or empty");
    }
}
