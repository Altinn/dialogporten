using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Digdir.Library.Utils.AspNet;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public sealed class WebApiSettings
{
    public const string SectionName = "WebApi";

    public required AuthenticationOptions Authentication { get; init; }
    public HealthCheckSettings HealthCheckSettings { get; init; } = new();
}

internal sealed class WebApiOptionsValidator : AbstractValidator<WebApiSettings>
{
    public WebApiOptionsValidator(
               IValidator<AuthenticationOptions> authenticationOptionsValidator)
    {
        RuleFor(x => x.Authentication)
            .SetValidator(authenticationOptionsValidator);

        RuleForEach(x => x.HealthCheckSettings.HttpGetEndpointsToCheck)
            .ChildRules(endpoint =>
            {
                endpoint.RuleFor(x => x.Name).NotEmpty();
                endpoint.RuleFor(x => x)
                    .Must(HaveExactlyOneEndpointAddress)
                    .WithMessage("Exactly one of 'Url' or 'AltinnPlatformRelativePath' must be set.");
                endpoint.When(x => !string.IsNullOrWhiteSpace(x.Url), () =>
                    endpoint.RuleFor(x => x.Url)
                        .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                        .WithMessage("'{PropertyName}' must be a valid absolute URL."));
                endpoint.When(x => !string.IsNullOrWhiteSpace(x.AltinnPlatformRelativePath), () =>
                    endpoint.RuleFor(x => x.AltinnPlatformRelativePath)
                        .NotEmpty()
                        .Must(path => !Uri.TryCreate(path, UriKind.Absolute, out _))
                        .WithMessage("'{PropertyName}' must be a relative path."));
            });
    }

    private static bool HaveExactlyOneEndpointAddress(HttpGetEndpointToCheck endpoint) =>
        !string.IsNullOrWhiteSpace(endpoint.Url) ^ !string.IsNullOrWhiteSpace(endpoint.AltinnPlatformRelativePath);
}
