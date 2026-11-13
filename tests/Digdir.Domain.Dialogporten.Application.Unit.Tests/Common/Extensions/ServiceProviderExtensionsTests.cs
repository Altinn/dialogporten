using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Extensions;

public sealed class ServiceProviderExtensionsTests
{
    private static readonly DateTime OuterTime = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime InnerTime = new(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc);

    [Fact]
    public async Task CreateInheritingAsyncScope_Should_Inherit_Transaction_Time()
    {
        // Arrange
        await using var serviceProvider = CreateServiceProvider();
        await using var outerScope = serviceProvider.CreateAsyncScope();
        var outerTransactionTime = outerScope.ServiceProvider.GetRequiredService<ITransactionTime>();
        _ = outerTransactionTime.Value;

        // Act
        await using var innerScope = outerScope.ServiceProvider.CreateInheritingAsyncScope();

        // Assert
        var innerTransactionTime = innerScope.ServiceProvider.GetRequiredService<ITransactionTime>();
        innerTransactionTime.Should().NotBeSameAs(outerTransactionTime);
        innerTransactionTime.Value.Should().Be(OuterTime);
    }

    [Fact]
    public async Task CreateInheritingAsyncScope_Should_Copy_Application_Context_Metadata()
    {
        // Arrange
        await using var serviceProvider = CreateServiceProvider();
        await using var outerScope = serviceProvider.CreateAsyncScope();
        var outerApplicationContext = outerScope.ServiceProvider.GetRequiredService<IApplicationContext>();
        outerApplicationContext.AddMetadata("key", "value");

        // Act
        await using var innerScope = outerScope.ServiceProvider.CreateInheritingAsyncScope();

        // Assert
        var innerApplicationContext = innerScope.ServiceProvider.GetRequiredService<IApplicationContext>();
        innerApplicationContext.Should().NotBeSameAs(outerApplicationContext);
        innerApplicationContext.Metadata.Should().Equal(outerApplicationContext.Metadata);

        innerApplicationContext.AddMetadata("inner", "only");
        outerApplicationContext.Metadata.Should().NotContainKey("inner");
    }

    [Fact]
    public void Inherit_Should_Throw_When_Transaction_Time_Has_Already_Been_Used()
    {
        // Arrange
        var transactionTime = new TransactionTime(CreateClock());
        _ = transactionTime.Value;

        // Act
        var act = () => transactionTime.Inherit(OuterTime);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    private static ServiceProvider CreateServiceProvider() => new ServiceCollection()
        .AddSingleton(CreateClock())
        .AddScoped<ITransactionTime, TransactionTime>()
        .AddScoped<IApplicationContext, ApplicationContext>()
        .BuildServiceProvider(validateScopes: true);

    // The first read returns the outer time and any later read a different time, so an inner
    // scope that read the clock itself instead of inheriting would be detected.
    private static IClock CreateClock()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(OuterTime, InnerTime);
        return clock;
    }
}
