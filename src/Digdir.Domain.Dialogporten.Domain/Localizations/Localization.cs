﻿using Digdir.Library.Entity.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Localizations;

public class Localization : IJoinEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public string Value { get; set; } = null!;
    public string CultureCode { get; set; } = null!;

    // === Dependent relationships ===
    public long LocalizationSetId { get; set; }
    public LocalizationSet LocalizationSet { get; set; } = null!;
}
