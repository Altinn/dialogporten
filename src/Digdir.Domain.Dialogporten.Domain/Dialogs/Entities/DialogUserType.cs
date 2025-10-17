using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

public sealed class DialogUserType : AbstractLookupEntity<DialogUserType, DialogUserType.Values>
{
    public DialogUserType(Values id) : base(id) { }
    public override DialogUserType MapValue(Values id) => new(id);

    public enum Values
    {
        /// <summary>
        /// Unknown user type (undeterminable)
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// ID-porten authenticated (has "pid" claim)
        /// </summary>
        Person = 1,

        /// <summary>
        /// Altinn 3 system user
        /// </summary>
        SystemUser = 2,

        /// <summary>
        /// Maskinporten authenticated service owner
        /// </summary>
        ServiceOwner = 3,

        /// <summary>
        /// Like "Person", but via a service owner system
        /// </summary>
        ServiceOwnerOnBehalfOfPerson = 4,

        /// <summary>
        /// Self-identified user
        /// </summary>
        SelfIdentifiedUser = 5
    }
}
