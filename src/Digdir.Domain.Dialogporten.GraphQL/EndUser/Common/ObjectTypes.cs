namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

public sealed class Localization
{
    public string Value { get; set; } = null!;
    public string LanguageCode { get; set; } = null!;
}

public sealed class ContentValue
{
    public List<Localization> Value { get; set; } = [];
    public string MediaType { get; set; } = null!;
}

public sealed class EndUserContext
{
    public Guid Revision { get; set; }
    public List<SystemLabel> SystemLabels { get; set; } = [];
}

public sealed class SeenLog
{
    public Guid Id { get; set; }
    public DateTimeOffset SeenAt { get; set; }

    public Actor SeenBy { get; set; } = null!;

    public bool? IsViaServiceOwner { get; set; }

    public bool IsCurrentEndUser { get; set; }
}

public sealed class Activity
{
    public Guid Id { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public Uri? ExtendedType { get; set; }

    public ActivityType Type { get; set; }

    public Guid? TransmissionId { get; set; }

    public Actor PerformedBy { get; set; } = null!;

    public List<Localization> Description { get; set; } = [];
}

public sealed class Actor
{
    public ActorType? ActorType { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
}

public enum ActorType
{
    PartyRepresentative = 1,
    ServiceOwner = 2
}

public enum ActivityType
{
    [GraphQLDescription("Indicates that a dialog has been created.")]
    DialogCreated = 1,

    [GraphQLDescription("Indicates that a dialog has been closed.")]
    DialogClosed = 2,

    [GraphQLDescription("Information from the service provider, not (directly) related to any transmission.")]
    Information = 3,

    [GraphQLDescription("Indicates that a transmission has been opened.")]
    TransmissionOpened = 4,

    [GraphQLDescription("Indicates that payment has been made.")]
    PaymentMade = 5,

    [GraphQLDescription("Indicates that a signature has been provided.")]
    SignatureProvided = 6,

    [GraphQLDescription("Indicates that a dialog has been opened.")]
    DialogOpened = 7,

    [GraphQLDescription("Indicates that a dialog has been deleted.")]
    DialogDeleted = 8,

    [GraphQLDescription("Indicates that a dialog has been restored.")]
    DialogRestored = 9,

    [GraphQLDescription("Indicates that a dialog has been sent to signing.")]
    SentToSigning = 10,

    [GraphQLDescription("Indicates that a dialog has been sent to form fill.")]
    SentToFormFill = 11,

    [GraphQLDescription("Indicates that a dialog has been sent to send in.")]
    SentToSendIn = 12,

    [GraphQLDescription("Indicates that a dialog has been sent to payment.")]
    SentToPayment = 13,

    [GraphQLDescription("Indicates that a form associated with the dialog has been submitted.")]
    FormSubmitted = 14,

    [GraphQLDescription("Indicates that a form associated with the dialog has been saved.")]
    FormSaved = 15,

    [GraphQLDescription("Indicates that a correspondence has been opened.")]
    CorrespondenceOpened = 16,

    [GraphQLDescription("Indicates that a correspondence has been confirmed.")]
    CorrespondenceConfirmed = 17,
}

public enum DialogStatus
{
    [GraphQLDescription("No explicit status. This is the default.")]
    NotApplicable = 7,

    [GraphQLDescription("Started. In a serial process, this is used to indicate that, for example, a form filling is ongoing.")]
    InProgress = 2,

    [GraphQLDescription("Used to indicate user-initiated dialogs not yet sent.")]
    Draft = 3,

    [GraphQLDescription("Awaiting action by the service owner. Indicates that the dialog is in a state where the party representative has no further tasks, and the responsibility lies with the service owner.")]
    Awaiting = 4,

    [GraphQLDescription("Used to indicate that the dialogue is in progress/under work, but is in a state where the user must do something - for example, correct an error, or other conditions that hinder further processing.")]
    RequiresAttention = 5,

    [GraphQLDescription("The dialogue was completed. This typically means that the dialogue is moved to a GUI archive or similar.")]
    Completed = 6
}

public enum SystemLabel
{
    Default = 1,
    Bin = 2,
    Archive = 3,
    MarkedAsUnopened = 4,
    Sent = 5
}
