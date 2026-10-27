using System.Globalization;
using EU = Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;
using SO = Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests;

// Pins the ServiceOwner and EndUser SDK enum numbering against the server's domain enums (1-based). An unset property
// (the C# default, always numeric 0) must never coincide with a named member: the server's IsInEnum() validators then
// reject the unnamed value, so a caller who forgot the field gets a 400 instead of the SDK silently picking the first member.
public class EnumNumberingTests
{
    [Fact]
    public void ServiceOwner_Values_Should_Match_Server_Domain_Enums_And_Default_Should_Be_Unnamed()
    {
        AssertValues<SO.ActorType>(("PartyRepresentative", 1), ("ServiceOwner", 2));
        AssertValues<SO.AttachmentUrlConsumerType>(("Gui", 1), ("Api", 2));
        AssertValues<SO.AuthorizationContextUnauthorizedPresentation>(("Disabled", 1), ("Excluded", 2));

        AssertValues<SO.DialogActivityType>(
            ("DialogCreated", 1), ("DialogClosed", 2), ("Information", 3), ("TransmissionOpened", 4),
            ("PaymentMade", 5), ("SignatureProvided", 6), ("DialogOpened", 7), ("DialogDeleted", 8),
            ("DialogRestored", 9), ("SentToSigning", 10), ("SentToFormFill", 11), ("SentToSendIn", 12),
            ("SentToPayment", 13), ("FormSubmitted", 14), ("FormSaved", 15), ("CorrespondenceOpened", 16),
            ("CorrespondenceConfirmed", 17));

        AssertValues<SO.DialogGuiActionPriority>(("Primary", 1), ("Secondary", 2), ("Tertiary", 3));
        AssertValues<SO.DialogPartyCheckAccessRestriction>(("None", 1), ("DirectAndHierarchy", 2), ("ClientDelegation", 3));

        AssertValues<SO.DialogStatus>(
            ("InProgress", 2), ("Draft", 3), ("RequiresAttention", 5), ("Completed", 6), ("NotApplicable", 7),
            ("Awaiting", 8));

        AssertValues<SO.DialogStatusInput>(
            ("New", 1), ("InProgress", 2), ("Draft", 3), ("Sent", 4), ("RequiresAttention", 5), ("Completed", 6),
            ("NotApplicable", 7), ("Awaiting", 8));

        AssertValues<SO.DialogTransmissionType>(
            ("Information", 1), ("Acceptance", 2), ("Rejection", 3), ("Request", 4), ("Alert", 5), ("Decision", 6),
            ("Submission", 7), ("Correction", 8));

        AssertValues<SO.HttpVerb>(
            ("GET", 1), ("POST", 2), ("PUT", 3), ("PATCH", 4), ("DELETE", 5), ("HEAD", 6), ("OPTIONS", 7),
            ("TRACE", 8), ("CONNECT", 9));

        AssertValues<SO.NotificationConditionType>(("NotExists", 1), ("Exists", 2));
        AssertValues<SO.SystemLabel>(("Default", 1), ("Bin", 2), ("Archive", 3), ("MarkedAsUnopened", 4), ("Sent", 5));
    }

    [Fact]
    public void EndUser_Values_Should_Match_Server_Domain_Enums_And_Default_Should_Be_Unnamed()
    {
        AssertValues<EU.ActorType>(("PartyRepresentative", 1), ("ServiceOwner", 2));
        AssertValues<EU.AttachmentUrlConsumerType>(("Gui", 1), ("Api", 2));

        // Matched by name only; the server's enum is 0-based, so only "no member is 0" matters here.
        AssertValues<EU.AuthorizedPartyType>(("Organization", 1), ("Person", 2));

        AssertValues<EU.DialogActivityType>(
            ("DialogCreated", 1), ("DialogClosed", 2), ("Information", 3), ("TransmissionOpened", 4),
            ("PaymentMade", 5), ("SignatureProvided", 6), ("DialogOpened", 7), ("DialogDeleted", 8),
            ("DialogRestored", 9), ("SentToSigning", 10), ("SentToFormFill", 11), ("SentToSendIn", 12),
            ("SentToPayment", 13), ("FormSubmitted", 14), ("FormSaved", 15), ("CorrespondenceOpened", 16),
            ("CorrespondenceConfirmed", 17));

        AssertValues<EU.DialogGuiActionPriority>(("Primary", 1), ("Secondary", 2), ("Tertiary", 3));

        AssertValues<EU.DialogStatus>(
            ("InProgress", 2), ("Draft", 3), ("RequiresAttention", 5), ("Completed", 6), ("NotApplicable", 7),
            ("Awaiting", 8));

        AssertValues<EU.DialogTransmissionType>(
            ("Information", 1), ("Acceptance", 2), ("Rejection", 3), ("Request", 4), ("Alert", 5), ("Decision", 6),
            ("Submission", 7), ("Correction", 8));

        AssertValues<EU.HttpVerb>(
            ("Get", 1), ("Post", 2), ("Put", 3), ("Patch", 4), ("Delete", 5), ("Head", 6), ("Options", 7),
            ("Trace", 8), ("Connect", 9));

        AssertValues<EU.IdentifierLookupGrantType>(
            ("Role", 1), ("AccessPackage", 2), ("ResourceDelegation", 3), ("InstanceDelegation", 4));

        AssertValues<EU.SystemLabel>(("Default", 1), ("Bin", 2), ("Archive", 3), ("MarkedAsUnopened", 4), ("Sent", 5));
    }

    private static void AssertValues<T>(params (string Name, int Value)[] expected) where T : struct, Enum
    {
        // Check that 0 is not a valid enum value
        Assert.False(Enum.IsDefined(default(T)), $"{typeof(T).Name} has a member numbered 0.");

        Assert.Equal(
            expected.OrderBy(x => x.Name, StringComparer.InvariantCulture),
            Enum.GetValues<T>()
                .Select(v => (v.ToString(), Convert.ToInt32(v, CultureInfo.InvariantCulture)))
                .OrderBy(x => x.Item1, StringComparer.InvariantCulture));
    }
}
