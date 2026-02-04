using Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

public sealed class GraphQlSettings
{
    public const string SectionName = "GraphQl";

    public required AuthenticationOptions Authentication { get; init; }

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
    }
}
