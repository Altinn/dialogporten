using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

public sealed class DialogStatus : AbstractLookupEntity<DialogStatus, DialogStatus.Values>
{
    public DialogStatus(Values id) : base(id) { }
    public override DialogStatus MapValue(Values id) => new(id);

    public enum Values
    {
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
}
