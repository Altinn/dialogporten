using Refit;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Infrastructure;

internal interface IResourceRegistry
{
    [Get("/resourceregistry/api/v1/resource/resourcelist")]
    Task<List<ResourceDto>> GetResources(CancellationToken cancellationToken = default);
}

internal sealed record ResourceDto(string Identifier, HasCompetentAuthority HasCompetentAuthority, string ResourceType)
{
    public string Identifier { get; } = $"{Domain.Dialogporten.Domain.Common.Constants.ServiceResourcePrefix}{Identifier}";
}

internal sealed record HasCompetentAuthority(string Orgcode);

