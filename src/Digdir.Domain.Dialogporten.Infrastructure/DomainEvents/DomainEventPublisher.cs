﻿using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Infrastructure.DomainEvents;

internal sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly HashSet<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList().AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void Publish(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
