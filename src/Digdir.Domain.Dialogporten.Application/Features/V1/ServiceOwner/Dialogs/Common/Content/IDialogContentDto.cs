using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Content;

internal interface IDialogContentDto
{
    ContentValueDto Title { get; set; }
    ContentValueDto? NonSensitiveTitle { get; set; }
    ContentValueDto? Summary { get; set; }
    ContentValueDto? NonSensitiveSummary { get; set; }
    ContentValueDto? SenderName { get; set; }
    ContentValueDto? AdditionalInfo { get; set; }
    ContentValueDto? ExtendedStatus { get; set; }
    ContentValueDto? MainContentReference { get; set; }
}
