﻿using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Infrastructure.GraphQl;
using HotChocolate.Subscriptions;
using MassTransit;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Constants = Digdir.Domain.Dialogporten.Infrastructure.GraphQl.GraphQlSubscriptionConstants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Interceptors;

internal sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private readonly ITransactionTime _transactionTime;
    private readonly Lazy<ITopicEventSender> _topicEventSender;
    private readonly ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> _logger;
    private readonly Lazy<IPublishEndpoint> _publishEndpoint;
    private readonly IDomainEventContext _domainEventContext;

    private List<IDomainEvent> _domainEvents = [];

    public ConvertDomainEventsToOutboxMessagesInterceptor(
        ITransactionTime transactionTime,
        Lazy<ITopicEventSender> topicEventSender,
        ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> logger,
        Lazy<IPublishEndpoint> publishEndpoint,
        IDomainEventContext domainEventContext)
    {
        _transactionTime = transactionTime ?? throw new ArgumentNullException(nameof(transactionTime));
        _topicEventSender = topicEventSender ?? throw new ArgumentNullException(nameof(topicEventSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _domainEventContext = domainEventContext;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureLazyLoadedServices();

        var dbContext = eventData.Context;

        if (dbContext is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        _domainEvents = dbContext.ChangeTracker.Entries()
            .SelectMany(x =>
                x.Entity is IEventPublisher publisher
                    ? publisher.PopDomainEvents()
                    : [])
            .ToList();

        if (_domainEvents.Count == 0)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        EnsureLazyLoadedServices();
        foreach (var domainEvent in _domainEvents)
        {
            domainEvent.Metadata = _domainEventContext.Metadata;
            domainEvent.OccuredAt = _transactionTime.Value;
        }

        await Task.WhenAll(_domainEvents
            .Select(x => _publishEndpoint.Value
                .Publish(x, x.GetType(), cancellationToken)));

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        if (_domainEvents.Count == 0)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        try
        {
            var tasks = _domainEvents
                .Select(x => x switch
                {
                    DialogUpdatedDomainEvent dialogUpdatedDomainEvent => new DialogEventPayload
                    {
                        Id = dialogUpdatedDomainEvent.DialogId,
                        Type = DialogEventType.DialogUpdated
                    },
                    DialogDeletedDomainEvent dialogDeletedDomainEvent => new DialogEventPayload
                    {
                        Id = dialogDeletedDomainEvent.DialogId,
                        Type = DialogEventType.DialogDeleted
                    },
                    _ => (DialogEventPayload?)null
                })
                .Where(x => x is not null)
                .Cast<DialogEventPayload>()
                .Select(x => _topicEventSender.Value.SendAsync(
                        $"{Constants.DialogEventsTopic}{x.Id}",
                        x,
                        cancellationToken)
                    .AsTask());
            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send domain events to graphQL subscription");
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void EnsureLazyLoadedServices()
    {
        try
        {
            _ = _topicEventSender.Value;
            _ = _publishEndpoint.Value;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to ensure lazy-loaded services. Is the presentation layer registered with publish capabilities?", e);
        }
    }
}
