namespace Digdir.Library.Dialogporten.WebApiClient.Config;

public class DialogportenSettings
{
    public string Environment { get; set; } = null!;
    public MaskinportenSettings Maskinporten { get; set; } = null!;
}

public record Ed25519Keys
{
    public Ed25519Key Primary { get; set; } = null!;
    public Ed25519Key Secondary { get; set; } = null!;
}

public record Ed25519Key
{
    public string Kid { get; set; } = null!;
    public string PublicComponent { get; set; } = null!;
}
