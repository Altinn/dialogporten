using Digdir.Domain.Dialogporten.Application.Common;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.Utils;

public class ApplicationEventHandlerUtilsTests
{
    [Fact]
    public void Can_Not_Have_Duplicate_Endpoint_Names_On_Domain_Event_Handlers()
    {
        // Act
        var sameNameEndpoints = ApplicationEventHandlerUtils
            .GetHandlerEventMaps()
            .Select(x => EndpointNameAttribute.GetName(x.HandlerType, x.EventType))
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        // Assert
        Assert.Empty(sameNameEndpoints);
    }

    /// <summary>
    /// We should use caution when modifying endpoints, as this will change the target queue.
    /// Scenarios on deployment of new code where snapshot test fails:
    /// <list type="bullet">
    ///     <item>
    ///         Endpoint added to snapshot: A new queue is created. This is considered safe.
    ///     </item>
    ///     <item>
    ///         Endpoint removed from snapshot: The queue is NOT removed automatically and
    ///         possibly contains unconsumed messages.
    ///     </item>
    ///     <item>
    ///         Endpoint renamed in snapshot: See both of the above bullet points.
    ///     </item>
    /// </list>
    /// Required actions upon accepting snapshot changes:
    /// <list type="bullet">
    ///     <item>
    ///         Unconsumed messages: Move to new queue or delete (cautiously) through manual intervention.
    ///     </item>
    ///     <item>
    ///         Ghost queue: Delete through manual intervention when above is done.
    ///     </item>
    /// </list>
    /// </summary>
    /// <remarks>
    ///     Did you only intend to rename the handler or event type? If so, consider using the
    ///     <see cref="EndpointNameAttribute"/> to keep the endpoint name static.
    /// </remarks>
    [Fact]
    public async Task Developer_Should_Use_Caution_When_Modifying_Endpoints()
    {
        // Act
        var map = ApplicationEventHandlerUtils
            .GetHandlerEventMaps()
            .Select(x => EndpointNameAttribute.GetName(x.HandlerType, x.EventType))
            .Order()
            .ToArray();

        // Assert
        await Verify(map);
    }

    /// <summary>
    /// We should use caution when modifying events, as this will change the target topic, and possibly
    /// the message schema. Scenarios on deployment of new code where snapshot test fails:
    /// <list type="bullet">
    ///     <item>
    ///         Event added to snapshot: A new topic is created. This is considered safe.
    ///     </item>
    ///     <item>
    ///         Event removed from snapshot: Handlers whose queues were bound to this topic mey contain
    ///         unprocessable messages due to old schema format, and the topic is NOT removed
    ///         automatically.
    ///     </item>
    ///     <item>
    ///         Event renamed in snapshot: See both of the above bullet points.
    ///     </item>
    /// </list>
    /// Required actions upon accepting snapshot changes:
    /// <list type="bullet">
    ///     <item>
    ///         Unprocessable messages: Will end up in the dead-letter queue. From there they can
    ///         be altered, re-queued or deleted through manual intervention.
    ///     </item>
    ///     <item>Ghost topic: Delete through manual intervention.</item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task Developer_Should_Use_Caution_When_Modifying_Events()
    {
        // Act
        var map = ApplicationEventHandlerUtils
            .GetHandlerEventMaps()
            .Select(x => x.EventType.FullName)
            .Order()
            .Distinct()
            .ToArray();

        // Assert
        await Verify(map);
    }
}
