namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;

public sealed class ServiceOwnerLabelResultDto
{
    /// <summary>
    /// A list of labels.
    /// </summary>
    public List<ServiceOwnerLabelDto> Labels { get; set; } = [];

    /// <summary>
    /// The unique identifier for the service owner context revision in UUIDv4 format.
    /// </summary>
    public Guid Revision { get; set; }
}

public sealed class ServiceOwnerLabelDto
{
    /// <summary>
    /// A label value.
    /// </summary>
    public string Value { get; set; } = null!;
}
