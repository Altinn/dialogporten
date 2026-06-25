using Refit;

namespace Altinn.ApiClients.Dialogporten.Infrastructure;

internal interface IInternalDialogportenApi
{
    [Get("/api/v1/.well-known/jwks.json")]
    Task<DialogportenJwks> GetJwks(CancellationToken cancellationToken);
}

internal sealed class DialogportenJwks
{
    public required List<JsonWebKey> Keys { get; init; }

    internal sealed class JsonWebKey
    {
        public required string Kty { get; init; }
        public required string Use { get; init; }
        public required string Kid { get; init; }
        public required string Crv { get; init; }
        public required string X { get; init; }
        public required string Alg { get; init; }
    }
}
