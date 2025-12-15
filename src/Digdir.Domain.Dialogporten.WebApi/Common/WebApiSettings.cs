using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Library.Utils.AspNet;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public sealed class WebApiSettings
{
    public const string SectionName = "WebApi";

    public required AuthenticationOptions Authentication { get; init; }
}

internal sealed class WebApiOptionsValidator : AbstractValidator<WebApiSettings>
{
    public WebApiOptionsValidator(
               IValidator<AuthenticationOptions> authenticationOptionsValidator)
    {
        RuleFor(x => x.Authentication)
            .SetValidator(authenticationOptionsValidator);
    }
}
