using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

public abstract class AuthorizationContext : IIdentifiableEntity, ICreatableEntity
{
    /// <summary>
    /// The maximum number of <see cref="Parties"/> and <see cref="DialogPartyChecks"/> combined.
    /// </summary>
    public const int MaxNumberOfEntries = 4;
    public const int MaxTokenReferenceLength = 50;

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
    /// Other parties than the dialog's to evaluate access for, without access restriction. Access is granted if
    /// any party, or any of <see cref="DialogPartyChecks"/>, permits.
    /// </summary>
    public List<string> Parties { get; set; } = [];

    /// <summary>
    /// Checks evaluated on behalf of the dialog's own party, each with an access restriction and optionally its
    /// own service resource and/or action. Access is granted if any check, or any entry in <see cref="Parties"/>,
    /// permits. Stored as a JSON column.
    /// </summary>
    public List<DialogPartyCheck> DialogPartyChecks { get; set; } = [];

    /// <summary>
    /// The XACML action to evaluate. Optional; null means "read".
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// An optional service owner supplied reference that identifies this context in the dialog token's
    /// authorized entities ("e") claim in place of the carrying entity's id. Sharing a value between entities
    /// forms an OR-group: authorization for any member adds the shared value to the claim.
    /// </summary>
    public string? TokenReference { get; set; }

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
