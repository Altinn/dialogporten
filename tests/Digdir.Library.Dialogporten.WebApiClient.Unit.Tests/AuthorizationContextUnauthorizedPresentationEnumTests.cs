using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceOwnerPresentation = Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums.AuthorizationContextUnauthorizedPresentation;
using LegacyPresentation = Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests;

// Pins the SDK enum numbering against the server's domain enum (Disabled = 1, Excluded = 2). An unset property
// (the C# default, always numeric 0) must never coincide with a named member here: the server's IsInEnum()
// validator rejects the unnamed value it deserializes into, forcing a caller who omitted the field to get a
// 400 instead of the SDK silently selecting "Disabled" - the less restrictive presentation - on their behalf.
public class AuthorizationContextUnauthorizedPresentationEnumTests
{
    private static readonly JsonSerializerOptions ServiceOwnerOptions = CreateOptions<ServiceOwnerPresentation>();
    private static readonly JsonSerializerOptions LegacyOptions = CreateOptions<LegacyPresentation>();

    [Fact]
    public void ServiceOwnerClient_Unset_Value_Should_Serialize_As_Unnamed_Numeric() =>
        Assert.Equal("0", JsonSerializer.Serialize(default(ServiceOwnerPresentation), ServiceOwnerOptions));

    [Theory]
    [InlineData(ServiceOwnerPresentation.Disabled, "\"Disabled\"")]
    [InlineData(ServiceOwnerPresentation.Excluded, "\"Excluded\"")]
    public void ServiceOwnerClient_Named_Values_Should_Serialize_By_Name(ServiceOwnerPresentation value, string expected) =>
        Assert.Equal(expected, JsonSerializer.Serialize(value, ServiceOwnerOptions));

    [Fact]
    public void LegacyClient_Unset_Value_Should_Serialize_As_Unnamed_Numeric() =>
        Assert.Equal("0", JsonSerializer.Serialize(default(LegacyPresentation), LegacyOptions));

    [Theory]
    [InlineData(LegacyPresentation.Disabled, "\"Disabled\"")]
    [InlineData(LegacyPresentation.Excluded, "\"Excluded\"")]
    public void LegacyClient_Named_Values_Should_Serialize_By_Name(LegacyPresentation value, string expected) =>
        Assert.Equal(expected, JsonSerializer.Serialize(value, LegacyOptions));

    private static JsonSerializerOptions CreateOptions<T>() where T : struct, Enum
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter<T>());
        return options;
    }
}
