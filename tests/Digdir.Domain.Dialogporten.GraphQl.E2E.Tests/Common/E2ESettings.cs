namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public sealed record E2ESettings
{
    public required string DialogportenBaseUri { get; init; }
    public int WebAPiPort { get; init; } = -1;
    public int GraphQlPort { get; init; } = -1;
    public required string TokenGeneratorUser { get; init; }
    public required string TokenGeneratorPassword { get; init; }
}
