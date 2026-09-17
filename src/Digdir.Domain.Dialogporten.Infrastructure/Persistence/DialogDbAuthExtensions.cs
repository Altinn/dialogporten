using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence;

/// <summary>
/// Wires the dialog database data source for the authentication mode selected in <see cref="DialogDbAuthSettings"/>.
/// In <see cref="DialogDbAuthMode.EntraToken"/> mode the data source connects as the configured PostgreSQL role with a
/// rotating access token supplied by <see cref="DefaultAzureCredential"/> instead of a password.
/// </summary>
internal static partial class DialogDbAuthExtensions
{
    private const string OssRdbmsScope = "https://ossrdbms-aad.database.windows.net/.default";

    private static readonly TimeSpan SuccessRefreshInterval = TimeSpan.FromMinutes(50);
    private static readonly TimeSpan FailureRefreshInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The periodic password provider callback is only cancelled when the data source is disposed, so the token
    /// request gets its own timeout.
    /// </summary>
    private static readonly TimeSpan TokenAcquisitionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns the connection string the data source should be built from. In
    /// <see cref="DialogDbAuthMode.EntraToken"/> mode the password is removed and the user name is replaced by the
    /// PostgreSQL role the access token is issued for. All other options are kept as-is. In
    /// <see cref="DialogDbAuthMode.Password"/> mode the connection string is returned unchanged.
    /// </summary>
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
            Username = auth.Username
        };

        return builder.ConnectionString;
    }

    extension(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        /// <summary>
        /// Configures a periodic access token provider when <see cref="DialogDbAuthMode.EntraToken"/> is selected.
        /// Does nothing in <see cref="DialogDbAuthMode.Password"/> mode.
        /// </summary>
        internal NpgsqlDataSourceBuilder UseDialogDbAuth(DialogDbAuthSettings auth, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(auth);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            if (auth.Mode is not DialogDbAuthMode.EntraToken)
            {
                return dataSourceBuilder;
            }

            var logger = loggerFactory.CreateLogger(typeof(DialogDbAuthExtensions));
            var role = dataSourceBuilder.ConnectionStringBuilder.Username ?? string.Empty;
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext([OssRdbmsScope]);
            var acquisitionLogged = 0;

            return dataSourceBuilder.UsePeriodicPasswordProvider(
                async (_, cancellationToken) =>
                {
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
                },
                successRefreshInterval: SuccessRefreshInterval,
                failureRefreshInterval: FailureRefreshInterval);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Acquired an access token for the dialog database role {PostgresRole}.")]
    private static partial void AccessTokenAcquired(ILogger logger, string postgresRole);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Failed to acquire an access token for the dialog database role {PostgresRole}.")]
    private static partial void AccessTokenAcquisitionFailed(ILogger logger, string postgresRole, Exception exception);
}
