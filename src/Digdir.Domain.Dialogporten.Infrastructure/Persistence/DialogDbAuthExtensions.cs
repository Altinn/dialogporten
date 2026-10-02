using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence;

/// <summary>
/// Wires the dialog database data source for the authentication mode selected in <see cref="DialogDbAuthSettings"/>.
/// In <see cref="DialogDbAuthMode.EntraToken"/> mode the data source connects as the configured PostgreSQL role with an
/// access token from <see cref="DefaultAzureCredential"/> instead of a password. The token is read from the credential
/// cache every time a physical connection is opened, so its lifetime is governed by Azure Identity, which refreshes
/// proactively ahead of expiry and keeps serving the still-valid cached token while a refresh is failing.
/// </summary>
internal static partial class DialogDbAuthExtensions
{
    private const string OssRdbmsScope = "https://ossrdbms-aad.database.windows.net/.default";

    /// <summary>
    /// Upper bound on a single credential call, so a slow token endpoint cannot hold a connection open indefinitely.
    /// </summary>
    private static readonly TimeSpan TokenAcquisitionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns the connection string the data source should be built from. In
    /// <see cref="DialogDbAuthMode.EntraToken"/> mode the password is removed and the user name is replaced by the
    /// PostgreSQL role the access token is issued for. TLS validates the server certificate and host name. In
    /// <see cref="DialogDbAuthMode.Password"/> mode the connection string is returned unchanged.
    /// </summary>
    /// <remarks>
    /// Removing the password is required, not cosmetic: Npgsql refuses to build a data source that has both a
    /// password provider and a password in its connection string.
    /// </remarks>
    internal static string BuildConnectionString(string connectionString, DialogDbAuthSettings auth)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(auth);

        if (auth.Mode is not DialogDbAuthMode.EntraToken)
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Password = null,
            Username = auth.Username,
            SslMode = SslMode.VerifyFull
        };

        // Older connection strings can carry this legacy bypass option. Entra tokens require server validation.
        builder.Remove("Trust Server Certificate");

        return builder.ConnectionString;
    }

    extension(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        /// <summary>
        /// Supplies an access token as the password for every physical connection when
        /// <see cref="DialogDbAuthMode.EntraToken"/> is selected. Does nothing in
        /// <see cref="DialogDbAuthMode.Password"/> mode.
        /// </summary>
        internal NpgsqlDataSourceBuilder UseDialogDbAuth(
            DialogDbAuthSettings auth,
            ILoggerFactory loggerFactory,
            TokenCredential? credential = null)
        {
            ArgumentNullException.ThrowIfNull(auth);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            if (auth.Mode is not DialogDbAuthMode.EntraToken)
            {
                return dataSourceBuilder;
            }

            var logger = loggerFactory.CreateLogger(typeof(DialogDbAuthExtensions));
            var role = dataSourceBuilder.ConnectionStringBuilder.Username ?? string.Empty;
            credential ??= new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext([OssRdbmsScope]);
            var acquisitionLogged = 0;

            // Both callbacks are mandatory: Npgsql rejects the registration unless a sync and an async provider are
            // supplied, and picks the one matching how the connection is opened. Each call is a credential cache read
            // in the common case, which is what keeps connection opening fast.
            return dataSourceBuilder.UsePasswordProvider(
                _ =>
                {
                    using var timeout = new CancellationTokenSource(TokenAcquisitionTimeout);

                    try
                    {
                        var token = credential.GetToken(tokenRequestContext, timeout.Token);

                        if (Interlocked.Exchange(ref acquisitionLogged, 1) == 0)
                        {
                            AccessTokenAcquired(logger, role);
                        }

                        return token.Token;
                    }
                    catch (Exception exception)
                    {
                        AccessTokenAcquisitionFailed(logger, role, exception);
                        throw;
                    }
                },
                async (_, cancellationToken) =>
                {
                    // The supplied token cancels when the connection open is cancelled; the linked source adds the
                    // independent bound on the credential call itself.
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TokenAcquisitionTimeout);

                    try
                    {
                        var token = await credential.GetTokenAsync(tokenRequestContext, timeout.Token);

                        if (Interlocked.Exchange(ref acquisitionLogged, 1) == 0)
                        {
                            AccessTokenAcquired(logger, role);
                        }

                        return token.Token;
                    }
                    catch (Exception exception)
                    {
                        AccessTokenAcquisitionFailed(logger, role, exception);
                        throw;
                    }
                });
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Acquired an access token for the dialog database role {PostgresRole}.")]
    private static partial void AccessTokenAcquired(ILogger logger, string postgresRole);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Failed to acquire an access token for the dialog database role {PostgresRole}.")]
    private static partial void AccessTokenAcquisitionFailed(ILogger logger, string postgresRole, Exception exception);
}
