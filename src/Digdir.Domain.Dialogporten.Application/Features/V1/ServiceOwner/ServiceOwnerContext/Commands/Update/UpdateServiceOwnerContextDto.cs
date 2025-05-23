namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

public sealed class UpdateServiceOwnerContextDto
{
    public List<ServiceOwnerLabelDto> ServiceOwnerLabels { get; set; } = [];
}

public sealed class ServiceOwnerLabelDto
{
    /// <summary>
    /// A label value.
    /// </summary>
    public string Value { get; set; } = null!;
}
