using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class DialogportenHttpRequestInterceptorTests
{
    private const string AuthenticationType = "Bearer";

    private static DefaultHttpContext CreateHttpContext(
        ClaimsPrincipal user,
        ILogger<DialogportenHttpRequestInterceptor> logger) =>
        new()
        {
            User = user,
            RequestServices = new ServiceCollection()
                .AddSingleton(logger)
                .BuildServiceProvider()
        };

    [Fact]
    public async Task Authenticated_user_with_unknown_user_type_throws_graphql_exception()
    {
        var logger = new TestLogger<DialogportenHttpRequestInterceptor>();
        var interceptor = new DialogportenHttpRequestInterceptor();
        var context = CreateHttpContext(
            new ClaimsPrincipal(new ClaimsIdentity(
                claims: [new Claim("custom-claim", "custom-value")],
                AuthenticationType)),
            logger);

        var act = async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<GraphQLException>();
        ex.Which.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("AUTH_USER_TYPE_UNKNOWN");
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("UserType=Unknown")
            && entry.Message.Contains("custom-claim: custom-value"));
    }

    [Fact]
    public async Task Authenticated_user_with_known_user_type_passes_through()
    {
        var logger = new TestLogger<DialogportenHttpRequestInterceptor>();
        var interceptor = new DialogportenHttpRequestInterceptor();
        var context = CreateHttpContext(
            new ClaimsPrincipal(new ClaimsIdentity(
                claims: [new Claim("pid", "22834498646")],
                AuthenticationType)),
            logger);

        var act = async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Authenticated_dialog_token_passes_through()
    {
        var logger = new TestLogger<DialogportenHttpRequestInterceptor>();
        var interceptor = new DialogportenHttpRequestInterceptor();
        var context = CreateHttpContext(
            new ClaimsPrincipal(new ClaimsIdentity(
                claims:
                [
                    new Claim(DialogTokenClaimTypes.DialogId, Guid.NewGuid().ToString())
                ],
                AuthenticationType)),
            logger);

        var act = async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        logger.Entries.Should().BeEmpty();
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
            Func<TState, Exception?, string> formatter) => Entries.Add(new(logLevel, formatter(state, exception)));

        internal sealed record LogEntry(LogLevel Level, string Message);

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
