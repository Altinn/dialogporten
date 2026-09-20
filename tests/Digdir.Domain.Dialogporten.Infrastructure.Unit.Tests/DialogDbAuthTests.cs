using AwesomeAssertions;
using Azure.Core;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogDbAuthTests
{
    private const string ConnectionString =
        "Host=dialogporten.postgres.database.azure.com;Port=5432;Database=dialogporten;" +
        "Username=dialogportenUser;Password=supersecret;Ssl Mode=Require;Include Error Detail=True;" +
        "Maximum Pool Size=80;Command Timeout=30";

    [Fact]
    public void BuildConnectionString_Should_Return_Connection_String_Unchanged_In_Password_Mode()
    {
        var auth = new DialogDbAuthSettings();

        var result = DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth);

        result.Should().Be(ConnectionString);
    }

    [Fact]
    public void BuildConnectionString_Should_Return_Connection_String_Unchanged_In_Password_Mode_With_Username()
    {
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.Password,
            Username = "dialogporten-identity"
        };

        var result = DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth);

        result.Should().Be(ConnectionString);
    }

    [Fact]
    public void BuildConnectionString_Should_Remove_Password_In_EntraToken_Mode()
    {
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };

        var result = new NpgsqlConnectionStringBuilder(
            DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth));

        result.Password.Should().BeNull();
        result.ConnectionString.Should().NotContain("supersecret");
    }

    [Fact]
    public void BuildConnectionString_Should_Set_Configured_Username_In_EntraToken_Mode()
    {
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };

        var result = new NpgsqlConnectionStringBuilder(
            DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth));

        result.Username.Should().Be("dialogporten-identity");
    }

    [Fact]
    public void BuildConnectionString_Should_Preserve_Other_Options_In_EntraToken_Mode()
    {
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };
        var original = new NpgsqlConnectionStringBuilder(ConnectionString);

        var result = new NpgsqlConnectionStringBuilder(
            DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth));

        result.Host.Should().Be(original.Host);
        result.Port.Should().Be(original.Port);
        result.Database.Should().Be(original.Database);
        result.IncludeErrorDetail.Should().Be(original.IncludeErrorDetail);
        result.MaxPoolSize.Should().Be(original.MaxPoolSize);
        result.CommandTimeout.Should().Be(original.CommandTimeout);
    }

    [Theory]
    [InlineData(SslMode.Disable)]
    [InlineData(SslMode.Allow)]
    [InlineData(SslMode.Prefer)]
    [InlineData(SslMode.Require)]
    [InlineData(SslMode.VerifyCA)]
    [InlineData(SslMode.VerifyFull)]
    public void BuildConnectionString_Should_Require_Validated_Tls_For_Entra_Tokens(SslMode sslMode)
    {
        var original = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            SslMode = sslMode,
            RootCertificate = "/certificates/postgres-root.pem"
        };
        original["Trust Server Certificate"] = true;
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };

        var result = new NpgsqlConnectionStringBuilder(
            DialogDbAuthExtensions.BuildConnectionString(original.ConnectionString, auth));

        result.SslMode.Should().Be(SslMode.VerifyFull);
        result.RootCertificate.Should().Be(original.RootCertificate);
        result.ConnectionString.Should().NotContain("Trust Server Certificate");
    }

    [Fact]
    public void BuildConnectionString_Should_Produce_A_Connection_String_A_Password_Provider_Can_Be_Built_On()
    {
        // Npgsql refuses to build a data source that has both a password provider and a password, so the password
        // removal above is load-bearing rather than cosmetic.
        var auth = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };

        var builder = new NpgsqlDataSourceBuilder(
            DialogDbAuthExtensions.BuildConnectionString(ConnectionString, auth));
        builder.UseDialogDbAuth(auth, NullLoggerFactory.Instance, new RecordingTokenCredential());

        var build = () => builder.Build().Dispose();

        build.Should().NotThrow();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseDialogDbAuth_Should_Read_Current_Token_For_Each_Physical_Connection(bool asynchronous)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = new PostgreSqlAuthenticationStub();
        var credential = new RecordingTokenCredential();
        await using var dataSource = CreateDataSource(server, DialogDbAuthMode.EntraToken, credential);

        var firstPassword = server.ReceivePasswordAsync(timeout.Token);
        await OpenUntilAuthenticationResponse(dataSource, asynchronous, timeout.Token);
        (await firstPassword).Should().Be("initial-token");

        // A new physical connection must consult the credential again after its cached token changes.
        credential.Token = "refreshed-token";
        var secondPassword = server.ReceivePasswordAsync(timeout.Token);
        await OpenUntilAuthenticationResponse(dataSource, asynchronous, timeout.Token);
        (await secondPassword).Should().Be("refreshed-token");
        credential.AsynchronousRequests.Should().Equal(asynchronous, asynchronous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseDialogDbAuth_Should_Keep_Password_Authentication_Without_Requesting_A_Token(bool asynchronous)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = new PostgreSqlAuthenticationStub();
        var credential = new RecordingTokenCredential();
        await using var dataSource = CreateDataSource(server, DialogDbAuthMode.Password, credential);

        var password = server.ReceivePasswordAsync(timeout.Token);
        await OpenUntilAuthenticationResponse(dataSource, asynchronous, timeout.Token);

        (await password).Should().Be("static-password");
        credential.AsynchronousRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task UseDialogDbAuth_Should_Cancel_Token_Acquisition_When_Connection_Open_Is_Cancelled()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource();
        using var server = new PostgreSqlAuthenticationStub();
        var credential = new RecordingTokenCredential { WaitForCancellation = true };
        await using var dataSource = CreateDataSource(server, DialogDbAuthMode.EntraToken, credential);
        await using var connection = dataSource.CreateConnection();

        var password = server.ReceivePasswordAsync(timeout.Token);
        var opening = connection.OpenAsync(cancellation.Token);
        await credential.RequestStarted.Task.WaitAsync(timeout.Token);
        await cancellation.CancelAsync();

        var exception = await Assert.ThrowsAsync<NpgsqlException>(() => opening.WaitAsync(timeout.Token));
        exception.InnerException.Should().BeAssignableTo<OperationCanceledException>();
        credential.LastCancellationToken.IsCancellationRequested.Should().BeTrue();
        (await password).Should().BeNull();
    }

    [Fact]
    public void DialogDbAuthSettings_Should_Default_To_Password_Mode()
    {
        var settings = new DialogDbAuthSettings();

        settings.Mode.Should().Be(DialogDbAuthMode.Password);
        settings.Username.Should().BeNull();
    }

    [Fact]
    public void DialogDbAuthSettingsValidator_Should_Accept_Default_Settings()
    {
        var result = new DialogDbAuthSettingsValidator().Validate(new DialogDbAuthSettings());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void DialogDbAuthSettingsValidator_Should_Reject_EntraToken_Mode_Without_Username(string? username)
    {
        var settings = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = username
        };

        var result = new DialogDbAuthSettingsValidator().Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(DialogDbAuthSettings.Username));
    }

    [Fact]
    public void DialogDbAuthSettings_Should_Bind_Mode_From_Configuration_By_Name()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:DialogDbAuth:Mode"] = "EntraToken",
                ["Infrastructure:DialogDbAuth:Username"] = "dialogporten-identity"
            })
            .Build();

        var settings = configuration
            .GetSection("Infrastructure:DialogDbAuth")
            .Get<DialogDbAuthSettings>();

        settings.Should().NotBeNull();
        settings.Mode.Should().Be(DialogDbAuthMode.EntraToken);
        settings.Username.Should().Be("dialogporten-identity");
    }

    [Fact]
    public void DialogDbAuthSettingsValidator_Should_Accept_EntraToken_Mode_With_Username()
    {
        var settings = new DialogDbAuthSettings
        {
            Mode = DialogDbAuthMode.EntraToken,
            Username = "dialogporten-identity"
        };

        var result = new DialogDbAuthSettingsValidator().Validate(settings);

        result.IsValid.Should().BeTrue();
    }

    private static NpgsqlDataSource CreateDataSource(
        PostgreSqlAuthenticationStub server,
        DialogDbAuthMode mode,
        TokenCredential credential)
    {
        var auth = new DialogDbAuthSettings { Mode = mode, Username = "dialogporten-identity" };
        var connectionString = new NpgsqlConnectionStringBuilder(
            DialogDbAuthExtensions.BuildConnectionString(server.ConnectionString, auth))
        {
            // This loopback stub implements only PostgreSQL authentication. TLS validation has separate tests above.
            SslMode = SslMode.Disable
        };
        return new NpgsqlDataSourceBuilder(connectionString.ConnectionString)
            .UseDialogDbAuth(auth, NullLoggerFactory.Instance, credential)
            .Build();
    }

    private static async Task OpenUntilAuthenticationResponse(
        NpgsqlDataSource dataSource,
        bool asynchronous,
        CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();
        // The stub rejects authentication after capturing the password, avoiding an unrelated database protocol mock.
        var exception = asynchronous
            ? await Assert.ThrowsAsync<PostgresException>(() => connection.OpenAsync(cancellationToken))
            : Assert.Throws<PostgresException>(connection.Open);
        exception.SqlState.Should().Be(PostgresErrorCodes.InvalidPassword);
    }

    private sealed class RecordingTokenCredential : TokenCredential
    {
        public string Token { get; set; } = "initial-token";
        public bool WaitForCancellation { get; init; }
        public List<bool> AsynchronousRequests { get; } = [];
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken LastCancellationToken { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => RecordRequest(requestContext, asynchronous: false, cancellationToken);

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var token = RecordRequest(requestContext, asynchronous: true, cancellationToken);
            if (WaitForCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            return token;
        }

        private AccessToken RecordRequest(TokenRequestContext requestContext, bool asynchronous, CancellationToken cancellationToken)
        {
            requestContext.Scopes.Should().Equal("https://ossrdbms-aad.database.windows.net/.default");
            cancellationToken.CanBeCanceled.Should().BeTrue();
            LastCancellationToken = cancellationToken;
            AsynchronousRequests.Add(asynchronous);
            RequestStarted.TrySetResult();
            return new AccessToken(Token, DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
