using Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;
using Digdir.Library.Utils.AspNet;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

public sealed class GraphQlSettings
{
    public const string SectionName = "GraphQl";

    public required AuthenticationOptions Authentication { get; init; }
    public HealthCheckSettings HealthCheckSettings { get; init; } = new();

    public required GraphQlCorsOptions Cors { get; init; }
}

public sealed class GraphQlCorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "GraphQlCorsPolicy";

    public required List<string> AllowedOrigins { get; init; }
}

internal sealed class GraphQlOptionsValidator : AbstractValidator<GraphQlSettings>
{
    public GraphQlOptionsValidator(
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
