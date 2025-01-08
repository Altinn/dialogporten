﻿using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

public sealed record DialogUpdatedDomainEvent(
    Guid DialogId,
    string ServiceResource,
    string Party,
    string? Process,
    string? PrecedingProcess) : DomainEvent, IProcessEvent;
