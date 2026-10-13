using DomainTransmissionType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmissionType;
using DomainGuiActionPriority = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiActionPriority;
using DomainHttpVerb = Digdir.Domain.Dialogporten.Domain.Http.HttpVerb;
using DomainAttachmentUrlConsumerType = Digdir.Domain.Dialogporten.Domain.Attachments.AttachmentUrlConsumerType;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

// Explicit enum-to-enum mappers. Each member is mapped by hand (rather than by a generic name/value
// converter) so that only intentionally related enums can be converted and unexpected values fail loudly.
internal static class DialogByIdEnumMapExtensions
{
    public static TransmissionType ToGraphQl(this DomainTransmissionType.Values source) => source switch
    {
        DomainTransmissionType.Values.Information => TransmissionType.Information,
        DomainTransmissionType.Values.Acceptance => TransmissionType.Acceptance,
        DomainTransmissionType.Values.Rejection => TransmissionType.Rejection,
        DomainTransmissionType.Values.Request => TransmissionType.Request,
        DomainTransmissionType.Values.Alert => TransmissionType.Alert,
        DomainTransmissionType.Values.Decision => TransmissionType.Decision,
        DomainTransmissionType.Values.Submission => TransmissionType.Submission,
        DomainTransmissionType.Values.Correction => TransmissionType.Correction,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static GuiActionPriority ToGraphQl(this DomainGuiActionPriority.Values source) => source switch
    {
        DomainGuiActionPriority.Values.Primary => GuiActionPriority.Primary,
        DomainGuiActionPriority.Values.Secondary => GuiActionPriority.Secondary,
        DomainGuiActionPriority.Values.Tertiary => GuiActionPriority.Tertiary,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static HttpVerb ToGraphQl(this DomainHttpVerb.Values source) => source switch
    {
        DomainHttpVerb.Values.GET => HttpVerb.GET,
        DomainHttpVerb.Values.POST => HttpVerb.POST,
        DomainHttpVerb.Values.PUT => HttpVerb.PUT,
        DomainHttpVerb.Values.PATCH => HttpVerb.PATCH,
        DomainHttpVerb.Values.DELETE => HttpVerb.DELETE,
        DomainHttpVerb.Values.HEAD => HttpVerb.HEAD,
        DomainHttpVerb.Values.OPTIONS => HttpVerb.OPTIONS,
        DomainHttpVerb.Values.TRACE => HttpVerb.TRACE,
        DomainHttpVerb.Values.CONNECT => HttpVerb.CONNECT,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static AttachmentUrlConsumer ToGraphQl(this DomainAttachmentUrlConsumerType.Values source) => source switch
    {
        DomainAttachmentUrlConsumerType.Values.Gui => AttachmentUrlConsumer.Gui,
        DomainAttachmentUrlConsumerType.Values.Api => AttachmentUrlConsumer.Api,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };
}
