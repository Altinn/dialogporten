using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

internal interface ITransmissionContentDto
{
    ContentValueDto Title { get; set; }
    ContentValueDto? Summary { get; set; }
    ContentValueDto? ContentReference { get; set; }
}
