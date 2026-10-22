using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;

public sealed class DialogRelationType : AbstractLookupEntity<DialogRelationType, DialogRelationType.Values>
{
    public DialogRelationType(Values id) : base(id) { }
    public override DialogRelationType MapValue(Values id) => new(id);

    /// <summary>
    /// The type of a relation, always expressed from the perspective of the dialog it is read from.
    /// </summary>
    public enum Values
    {
        /// <summary>
        /// The related dialog is the parent of this dialog. A dialog has at most one parent.
        /// </summary>
        Parent = 1,

        /// <summary>
        /// The related dialog is a child of this dialog.
        /// </summary>
        Child = 2,

        /// <summary>
        /// The dialogs are related, without a hierarchy. Symmetric.
        /// </summary>
        Related = 3
    }
}

public static class DialogRelationTypeExtensions
{
    extension(DialogRelationType.Values type)
    {
        /// <summary>
        /// The same relation as seen from the other dialog.
        /// </summary>
        public DialogRelationType.Values Invert() => type switch
        {
            DialogRelationType.Values.Parent => DialogRelationType.Values.Child,
            DialogRelationType.Values.Child => DialogRelationType.Values.Parent,
            DialogRelationType.Values.Related => DialogRelationType.Values.Related,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
