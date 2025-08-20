namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;

public enum DialogStatusInput
{
    /// <summary>
    /// The dialogue is considered new. Typically used for simple messages that do not require any interaction,
    /// or as an initial step for dialogues. This is the default.
    /// </summary>
    [Obsolete($"Use {nameof(NotApplicable)} instead.")]
    New = 1,

    /// <summary>
    /// Sent by the service owner. In a serial process, this is used after a submission is made.
    /// </summary>
    [Obsolete($"Use {nameof(Awaiting)} instead.")]
    Sent = 4,

    /// <summary>
    /// No explicit status. This is the default.
    /// </summary>
    NotApplicable = 7,

    /// <summary>
    /// Started. In a serial process, this is used to indicate that, for example, a form filling is ongoing.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Used to indicate user-initiated dialogs not yet sent.
    /// </summary>
    Draft = 3,

    /// <summary>
    /// Awaiting action by the service owner. Indicates that the dialog is in a state where the party representative has no further tasks, and the responsibility lies with the service owner.
    /// </summary>
    Awaiting = 8,

    /// <summary>
    /// Used to indicate that the dialogue is in progress/under work, but is in a state where the user must do something - for example, correct an error, or other conditions that hinder further processing.
    /// </summary>
    RequiresAttention = 5,

    /// <summary>
    /// The dialogue was completed. This typically means that the dialogue is moved to a GUI archive or similar.
    /// </summary>
    Completed = 6
}
