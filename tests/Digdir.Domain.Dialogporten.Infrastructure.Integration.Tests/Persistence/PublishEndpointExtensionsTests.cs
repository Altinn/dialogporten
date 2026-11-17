using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Domain.Actors.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;
using MassTransit;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Infrastructure.Integration.Tests.Persistence;

public sealed class PublishEndpointExtensionsTests
{
    [Fact]
    public async Task PublishDomainEvent_Should_Set_Transport_Delay_For_Delayed_Event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        IPipe<PublishContext>? capturedPipe = null;
        publishEndpoint
            .When(x => x.Publish(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<IPipe<PublishContext>>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedPipe = x.ArgAt<IPipe<PublishContext>>(2));
        var domainEvent = new ResyncActorNameEvent(Guid.NewGuid(), "reason") { DeliveryDelay = TimeSpan.FromSeconds(30) };

        // Act
        await publishEndpoint.PublishDomainEvent(domainEvent, ct);

        // Assert
        await publishEndpoint.Received(1).Publish(
            domainEvent,
            Arg.Is<Type>(x => x == typeof(ResyncActorNameEvent)),
            Arg.Any<IPipe<PublishContext>>(),
            ct);
        capturedPipe.Should().NotBeNull();

        var publishContext = Substitute.For<PublishContext>();
        await capturedPipe.Send(publishContext);
        publishContext.Delay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PublishDomainEvent_Should_Publish_Without_Pipe_For_Undelayed_Event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var domainEvent = new ResyncActorNameEvent(Guid.NewGuid(), "reason");

        // Act
        await publishEndpoint.PublishDomainEvent(domainEvent, ct);

        // Assert
        await publishEndpoint.Received(1).Publish(domainEvent, Arg.Is<Type>(x => x == typeof(ResyncActorNameEvent)), ct);
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<object>(),
            Arg.Any<Type>(),
            Arg.Any<IPipe<PublishContext>>(),
            Arg.Any<CancellationToken>());
    }
}
