using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.Features.V1.Attachments_AttachmentUrlConsumerType;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Transmissions.Queries;

internal static class TransmissionTestData
{
    public static void AddComplexTransmissions(V1ServiceOwnerDialogsCommandsCreate_Dialog dialog)
    {
        var transmissionId = Guid.CreateVersion7();
        dialog.Transmissions.Clear();
        dialog.AddTransmission(t => t.Id = transmissionId);
        dialog.AddTransmission(t =>
        {
            t.Sender = new()
            {
                ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType.PartyRepresentative,
                ActorId = $"urn:altinn:organization:identifier-no:{E2EConstants.GetDefaultServiceOwnerOrgNr()}"
            };
            t.Content.Summary = new() { Value = [DialogTestData.CreateLocalization("Summary")] };
            t.Content.ContentReference = new()
            {
                MediaType = "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown",
                Value = [DialogTestData.CreateLocalization("https://digdir.com/content-reference")]
            };
            t.RelatedTransmissionId = transmissionId;
            t.ExtendedType = new Uri("https://digdir.com/transmission-type");
            t.IdempotentKey = "idempotent-key";
            t.ExternalReference = "external-reference";
            t.NavigationalActions =
            [
                new()
                {
                    ExpiresAt = DateTime.UtcNow.AddYears(100),
                    Title = [DialogTestData.CreateLocalization("Action title")],
                    Url = new Uri("https://digdir.com/action-url")
                }
            ];
            t.Attachments =
            [
                new()
                {
                    Name = "attachment-name",
                    ExpiresAt = DateTime.UtcNow.AddYears(100),
                    DisplayName = [DialogTestData.CreateLocalization("Attachment display name")],
                    Urls =
                    [
                        new()
                        {
                            Url = new Uri("https://digdir.com/attachment-url"),
                            MediaType = "application/pdf",
                            ConsumerType = Gui
                        }
                    ]
                }
            ];
        });
    }
}
