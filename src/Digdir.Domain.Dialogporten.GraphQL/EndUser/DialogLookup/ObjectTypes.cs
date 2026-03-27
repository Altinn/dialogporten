using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;

[InterfaceType("DialogLookupError")]
public interface IDialogLookupError
{
    string Message { get; set; }
}

public sealed class DialogLookupNotFound : IDialogLookupError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogLookupForbidden : IDialogLookupError
{
    public string Message { get; set; } = "Forbidden";
}

public sealed class DialogLookupValidationError : IDialogLookupError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogLookupPayload
{
    public DialogLookup? Lookup { get; set; }
    public List<IDialogLookupError> Errors { get; set; } = [];
}

public sealed class DialogLookup
{
    public Guid DialogId { get; set; }
    public string InstanceRef { get; set; } = null!;
    public string Party { get; set; } = null!;

    public DialogLookupServiceResource ServiceResource { get; set; } = null!;
    public DialogLookupServiceOwner ServiceOwner { get; set; } = null!;
    public DialogLookupAuthorizationEvidence AuthorizationEvidence { get; set; } = null!;
}

public sealed class DialogLookupServiceResource
{
    public string Id { get; set; } = null!;
    public bool IsDelegable { get; set; }
    public int MinimumAuthenticationLevel { get; set; }
    public List<Localization> Name { get; set; } = [];
}

public sealed class DialogLookupServiceOwner
{
    public string OrgNumber { get; set; } = null!;
    public string Code { get; set; } = null!;
    public List<Localization> Name { get; set; } = [];
}

public sealed class DialogLookupAuthorizationEvidence
{
    public int CurrentAuthenticationLevel { get; set; }
    public bool ViaRole { get; set; }
    public bool ViaAccessPackage { get; set; }
    public bool ViaResourceDelegation { get; set; }
    public bool ViaInstanceDelegation { get; set; }
    public List<DialogLookupAuthorizationEvidenceItem> Evidence { get; set; } = [];
}

public sealed class DialogLookupAuthorizationEvidenceItem
{
    public DialogLookupGrantType GrantType { get; set; }
    public string Subject { get; set; } = null!;
}

public enum DialogLookupGrantType
{
    Role = 1,
    AccessPackage = 2,
    ResourceDelegation = 3,
    InstanceDelegation = 4
}
