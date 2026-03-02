using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application;

public sealed class ApplicationSettings
{
    public const string ConfigurationSectionName = "Application";

    public required DialogportenSettings Dialogporten { get; init; }
    public FeatureToggle FeatureToggle { get; init; } = new();
    public LimitsSettings Limits { get; init; } = new();
    public BadDataHandling BadDataHandling { get; init; } = BadDataHandling.WarnAndContinue;
}

public sealed class FeatureToggle
{
    public bool UseBranchingLogicForDialogSearch { get; init; }
    public bool UsePartyResourcePruning { get; init; }
    public bool UseAltinnAutoAuthorizedPartiesQueryParameters { get; init; }
    public bool UseCorrectPersonNameOrdering { get; init; }
    public bool UseAccessManagementForAltinnSelfIdentifiedUsers { get; init; }
    public bool UseAccessManagementForIdportenEmailUsers { get; init; }
    public bool UseAccessManagementForFeideUsers { get; init; }
}

public enum BadDataHandling
{
    WarnAndContinue,
    Throw
}

public sealed class DialogportenSettings
{
    public required Uri BaseUri { get; init; }
    public required Ed25519KeyPairs Ed25519KeyPairs { get; init; }
}

public sealed class Ed25519KeyPairs
{
    public required Ed25519KeyPair Primary { get; init; }
    public required Ed25519KeyPair Secondary { get; init; }
}

public sealed class Ed25519KeyPair
{
    public required string Kid { get; init; }
    public required string PrivateComponent { get; init; }
    public required string PublicComponent { get; init; }
}

public sealed class LimitsSettings
{
    public EndUserSearchQueryLimits EndUserSearch { get; init; } = new();
    public ServiceOwnerSearchQueryLimits ServiceOwnerSearch { get; init; } = new();
    public PartyResourcePruningLimits PartyResourcePruning { get; init; } = new();
}

public sealed class PartyResourcePruningLimits
{
    public int MaxPartiesCachingThreshold { get; init; } = 1500; // 0 means no limit.
    public int MinResourcesPruningThreshold { get; init; } = 5;
}

public sealed class EndUserSearchQueryLimits
{
    public int MaxPartyFilterValues { get; init; } = 20;
    public int MaxServiceResourceFilterValues { get; init; } = 20;
    public int MaxOrgFilterValues { get; init; } = 20;
    public int MaxExtendedStatusFilterValues { get; init; } = 20;
}

public sealed class ServiceOwnerSearchQueryLimits
{
    public int MaxPartyFilterValues { get; init; } = 20;
    public int MaxServiceResourceFilterValues { get; init; } = 20;
    public int MaxExtendedStatusFilterValues { get; init; } = 20;
}

internal sealed class ApplicationSettingsValidator : AbstractValidator<ApplicationSettings>
{
    public ApplicationSettingsValidator(
        IValidator<DialogportenSettings> dialogportenSettingsValidator,
        IValidator<LimitsSettings> limitsSettingsValidator)
    {
        RuleFor(x => x.Dialogporten)
            .NotEmpty()
            .SetValidator(dialogportenSettingsValidator);

        RuleFor(x => x.Limits)
            .NotEmpty()
            .SetValidator(limitsSettingsValidator);
    }
}

internal sealed class DialogportenSettingsValidator : AbstractValidator<DialogportenSettings>
{
    public DialogportenSettingsValidator()
    {
        RuleFor(x => x.BaseUri).NotEmpty().IsValidUri();
    }
}

internal sealed class LimitsSettingsValidator : AbstractValidator<LimitsSettings>
{
    public LimitsSettingsValidator(
        IValidator<EndUserSearchQueryLimits> endUserSearchValidator,
        IValidator<ServiceOwnerSearchQueryLimits> serviceOwnerSearchValidator,
        IValidator<PartyResourcePruningLimits> partyResourcePruningValidator)
    {
        RuleFor(x => x.EndUserSearch)
            .NotEmpty()
            .SetValidator(endUserSearchValidator);

        RuleFor(x => x.ServiceOwnerSearch)
            .NotEmpty()
            .SetValidator(serviceOwnerSearchValidator);

        RuleFor(x => x.PartyResourcePruning)
            .NotEmpty()
            .SetValidator(partyResourcePruningValidator);
    }
}

internal sealed class EndUserSearchQueryLimitsValidator : AbstractValidator<EndUserSearchQueryLimits>
{
    public EndUserSearchQueryLimitsValidator()
    {
        RuleFor(x => x.MaxPartyFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.MaxServiceResourceFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.MaxOrgFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.MaxExtendedStatusFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}

internal sealed class ServiceOwnerSearchQueryLimitsValidator : AbstractValidator<ServiceOwnerSearchQueryLimits>
{
    public ServiceOwnerSearchQueryLimitsValidator()
    {
        RuleFor(x => x.MaxPartyFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.MaxServiceResourceFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
        RuleFor(x => x.MaxExtendedStatusFilterValues).GreaterThan(0).LessThanOrEqualTo(1000);
    }
}

internal sealed class PartyResourcePruningLimitsValidator : AbstractValidator<PartyResourcePruningLimits>
{
    public PartyResourcePruningLimitsValidator()
    {
        RuleFor(x => x.MaxPartiesCachingThreshold)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.MinResourcesPruningThreshold)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000);
    }
}
