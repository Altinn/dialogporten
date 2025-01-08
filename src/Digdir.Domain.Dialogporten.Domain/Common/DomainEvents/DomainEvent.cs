﻿using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public abstract record DomainEvent : IDomainEvent
{
    [JsonInclude]
    public Guid EventId { get; private set; } = Guid.NewGuid();

    [JsonInclude]
    public DateTimeOffset OccuredAt { get; set; }

    [JsonInclude]
    public FrozenDictionary<string, string> Metadata { get; set; } = FrozenDictionary<string, string>.Empty;
}
