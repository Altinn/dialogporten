namespace Digdir.Library.Dialogporten.E2E.Common;

public sealed record E2ESettings
{
    /// <summary>
    /// The WebAPI up to and including its path base, e.g. <c>https://localhost:7215/dialogporten</c>.
    /// </summary>
    public required Uri WebApiBaseUri { get; init; }

    /// <summary>
    /// The GraphQL endpoint itself, e.g. <c>http://localhost:5180/graphql</c>.
    /// </summary>
    public required Uri GraphQlUri { get; init; }

    public required string TokenGeneratorUser { get; init; }
    public required string TokenGeneratorPassword { get; init; }
}
