using System.Globalization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests;

// Pins the ServiceOwner SDK enum numbering against the server's domain enums (1-based). An unset property (the C#
// default, always numeric 0) must never coincide with a named member: the server's IsInEnum() validators then reject
// the unnamed value, so a caller who forgot the field gets a 400 instead of the SDK silently picking the first member.
public class ServiceOwnerEnumNumberingTests
{

    [Fact]
    public void Values_Should_Match_Server_Domain_Enums_And_Default_Should_Be_Unnamed()
    {
        AssertValues<ActorType>(("PartyRepresentative", 1), ("ServiceOwner", 2));
        AssertValues<AttachmentUrlConsumerType>(("Gui", 1), ("Api", 2));
        AssertValues<AuthorizationContextUnauthorizedPresentation>(("Disabled", 1), ("Excluded", 2));

        AssertValues<DialogActivityType>(
            ("DialogCreated", 1), ("DialogClosed", 2), ("Information", 3), ("TransmissionOpened", 4),
            ("PaymentMade", 5), ("SignatureProvided", 6), ("DialogOpened", 7), ("DialogDeleted", 8),
            ("DialogRestored", 9), ("SentToSigning", 10), ("SentToFormFill", 11), ("SentToSendIn", 12),
            ("SentToPayment", 13), ("FormSubmitted", 14), ("FormSaved", 15), ("CorrespondenceOpened", 16),
            ("CorrespondenceConfirmed", 17));

        AssertValues<DialogGuiActionPriority>(("Primary", 1), ("Secondary", 2), ("Tertiary", 3));
        AssertValues<DialogPartyCheckAccessRestriction>(("None", 1), ("DirectAndHierarchy", 2), ("ClientDelegation", 3));

        AssertValues<DialogStatus>(
            ("InProgress", 2), ("Draft", 3), ("RequiresAttention", 5), ("Completed", 6), ("NotApplicable", 7),
            ("Awaiting", 8));

        AssertValues<DialogStatusInput>(
            ("New", 1), ("InProgress", 2), ("Draft", 3), ("Sent", 4), ("RequiresAttention", 5), ("Completed", 6),
            ("NotApplicable", 7), ("Awaiting", 8));

        AssertValues<DialogTransmissionType>(
            ("Information", 1), ("Acceptance", 2), ("Rejection", 3), ("Request", 4), ("Alert", 5), ("Decision", 6),
            ("Submission", 7), ("Correction", 8));

        AssertValues<HttpVerb>(
            ("GET", 1), ("POST", 2), ("PUT", 3), ("PATCH", 4), ("DELETE", 5), ("HEAD", 6), ("OPTIONS", 7),
            ("TRACE", 8), ("CONNECT", 9));

        AssertValues<NotificationConditionType>(("NotExists", 1), ("Exists", 2));
        AssertValues<SystemLabel>(("Default", 1), ("Bin", 2), ("Archive", 3), ("MarkedAsUnopened", 4), ("Sent", 5));
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
