using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

public abstract class AuthorizationContext : IIdentifiableEntity, ICreatableEntity
{
    public const int MaxNumberOfParties = 3;

    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Overrides the dialog's service resource in the authorization request, e.g. "urn:altinn:resource:some-resource".
    /// </summary>
    public string? ServiceResource { get; set; }

    /// <summary>
    /// An additional resource attribute matched within the effective resource policy,
    /// e.g. "urn:altinn:task:Task_1" or "urn:altinn:subresource:some-subresource".
    /// </summary>
    public string? AdditionalResourceAttribute { get; set; }

    /// <summary>
    /// The parties to evaluate access for. Access is granted if any party permits.
    /// </summary>
    public List<string> Parties { get; set; } = [];

    /// <summary>
    /// Whether the dialog's party is included in the evaluation in addition to <see cref="Parties"/>.
    /// </summary>
    public bool IncludeDialogParty { get; set; }

    /// <summary>
    /// The XACML action to evaluate. Null on attachment/navigational action contexts (always "read"),
    /// optional on transmissions (defaults to read/transmissionread derivation).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// How the carrying entity is presented to end users that are not authorized for this context.
    /// Stored as a smallint; no lookup table (see <see cref="AuthorizationContextUnauthorizedPresentation"/>).
    /// </summary>
    public AuthorizationContextUnauthorizedPresentation.Values UnauthorizedPresentation { get; set; }
}

public sealed class DialogTransmissionAuthorizationContext : AuthorizationContext, IImmutableEntity
{
    public Guid TransmissionId { get; set; }
    public DialogTransmission Transmission { get; set; } = null!;
}

public sealed class DialogApiActionAuthorizationContext : AuthorizationContext
{
    public Guid ApiActionId { get; set; }
    public DialogApiAction ApiAction { get; set; } = null!;
}

public sealed class DialogGuiActionAuthorizationContext : AuthorizationContext
{
    public Guid GuiActionId { get; set; }
    public DialogGuiAction GuiAction { get; set; } = null!;
}

public sealed class AttachmentAuthorizationContext : AuthorizationContext
{
    public Guid AttachmentId { get; set; }
    public Attachment Attachment { get; set; } = null!;
}

public sealed class DialogTransmissionNavigationalActionAuthorizationContext : AuthorizationContext, IImmutableEntity
{
    public Guid NavigationalActionId { get; set; }
    public DialogTransmissionNavigationalAction NavigationalAction { get; set; } = null!;
}
