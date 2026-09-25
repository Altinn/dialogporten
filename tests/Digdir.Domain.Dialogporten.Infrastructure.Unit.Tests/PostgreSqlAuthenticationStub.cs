using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

/// <summary>
/// Exercises Npgsql's real password-provider callbacks without Azure or a database server. It handles only the
/// startup/password exchange, then deliberately rejects authentication so no query or type-loading protocol is needed.
/// </summary>
internal sealed class PostgreSqlAuthenticationStub : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    public PostgreSqlAuthenticationStub()
    {
        _listener.Start();
    }

    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = IPAddress.Loopback.ToString(),
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port,
        Database = "dialogporten",
        Username = "dialogportenUser",
        Password = "static-password",
        SslMode = SslMode.Disable,
        GssEncryptionMode = GssEncryptionMode.Disable,
        Pooling = false,
        Timeout = 5
    }.ConnectionString;

    public async Task<string?> ReceivePasswordAsync(CancellationToken cancellationToken)
    {
        using var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        await using var stream = client.GetStream();
        _ = await ReadPayloadAsync(stream, cancellationToken).ConfigureAwait(false); // StartupMessage has no type byte.

        // AuthenticationCleartextPassword: type 'R', message length 8, authentication code 3.
        byte[] authenticationRequest = [(byte)'R', 0, 0, 0, 8, 0, 0, 0, 3];
        await stream.WriteAsync(authenticationRequest, cancellationToken).ConfigureAwait(false);

        var messageType = new byte[1];
        if (await stream.ReadAsync(messageType, cancellationToken).ConfigureAwait(false) == 0)
        {
            return null; // A cancelled token request closes the connection without sending a password.
        }

        messageType[0].Should().Be((byte)'p');
        var payload = await ReadPayloadAsync(stream, cancellationToken).ConfigureAwait(false);
        payload[^1].Should().Be(0);
        var password = Encoding.UTF8.GetString(payload.AsSpan(0, payload.Length - 1));

        var errorPayload = Encoding.UTF8.GetBytes("SERROR\0C28P01\0MAuthentication stub completed\0\0");
        var error = new byte[5 + errorPayload.Length];
        error[0] = (byte)'E';
        BinaryPrimitives.WriteInt32BigEndian(error.AsSpan(1), errorPayload.Length + 4);
        errorPayload.CopyTo(error, 5);
        await stream.WriteAsync(error, cancellationToken).ConfigureAwait(false);
        return password;
    }

    private static async Task<byte[]> ReadPayloadAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
        length.Should().BeInRange(5, 4096);
        var payload = new byte[length - 4];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return payload;
    }

    public void Dispose() => _listener.Stop();
}
