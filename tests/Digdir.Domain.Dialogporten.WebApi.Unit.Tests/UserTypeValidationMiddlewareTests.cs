using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class UserTypeValidationMiddlewareTests
{
    private const string AuthenticationType = "Bearer";

    [Fact]
    public async Task Authenticated_user_with_unknown_user_type_logs_diagnostic_summary()
    {
        var logger = new TestLogger<UserTypeValidationMiddleware>();
        var middleware = new UserTypeValidationMiddleware(_ => Task.CompletedTask, logger);
        var context = CreateContext(
            "enduser",
            [new Claim("custom-claim", "custom-value")]);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("UserType=Unknown")
            && entry.Message.Contains("custom-claim: custom-value"));
    }

    [Fact]
    public async Task Authenticated_user_with_known_user_type_that_is_invalid_for_endpoint_does_not_log()
    {
        var logger = new TestLogger<UserTypeValidationMiddleware>();
        var middleware = new UserTypeValidationMiddleware(_ => Task.CompletedTask, logger);
        var context = CreateContext(
            "serviceprovider",
            [new Claim(ClaimsPrincipalExtensions.PidClaim, "22834498646")]);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        logger.Entries.Should().BeEmpty();
    }

    private static DefaultHttpContext CreateContext(string policy, List<Claim> claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType))
        };

        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute(policy)),
            "Test endpoint"));

        return context;
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];
        private static readonly IDisposable NoOpScope = new NoopDisposable();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpScope;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new(logLevel, formatter(state, exception)));
        }

        internal sealed record LogEntry(LogLevel Level, string Message);

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
