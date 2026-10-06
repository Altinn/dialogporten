using System.Text;
using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;
using Base64Url = System.Buffers.Text.Base64Url;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class CompactJwsGeneratorTests
{
    [Fact]
    public void ValidJwsIsGenerated()
    {
        // Arrange
        var generator = new Ed25519Generator(new OptionsMock<ApplicationSettings>(GetSettings()));

        var payload = new Dictionary<string, object?>
        {
            { "sub", "1234567890" },
            { "name", "John Doe" },
            { "iat", 1516239022 }
        };

        // Act
        var jws = generator.GetCompactJws(payload, DialogTokenTypes.DialogToken);

        // Assert
        Assert.True(generator.VerifyCompactJws(jws));
    }

    [Fact]
    public void HeaderCarriesTheDialogTokenType()
    {
        // Arrange
        var generator = new Ed25519Generator(new OptionsMock<ApplicationSettings>(GetSettings()));

        // Act
        var jws = generator.GetCompactJws([], DialogTokenTypes.DialogToken);

        // Assert
        var header = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(jws.Split('.')[0]));
        Assert.Equal(DialogTokenTypes.DialogToken, JsonDocument.Parse(header).RootElement.GetProperty("typ").GetString());
    }

    private static ApplicationSettings GetSettings()
    {
        return new ApplicationSettings
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://unittest"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = new Ed25519KeyPair
                    {
                        Kid = "unittestkeypair1",
                        PrivateComponent = "ns9Mgams90E5bCNGg9iSXONvRvASFcWF_Nb_JJ8oAEA",
                        PublicComponent = "qIn67qFQUBiwW2kv7J-5CdUCdR67CzOSnwXPBunh0d0"
                    },
                    Secondary = new Ed25519KeyPair
                    {
                        Kid = "unittestkeypair2",
                        PrivateComponent = "",
                        PublicComponent = ""
                    }
                }
            }
        };
    }
}
