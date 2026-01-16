using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application;

public sealed class ApplicationSettings
{
    public const string ConfigurationSectionName = "Application";

    public required DialogportenSettings Dialogporten { get; init; }
    public FeatureToggle FeatureToggle { get; init; } = new();
}

public sealed class FeatureToggle
{
    public bool UseOptimizedEndUserDialogSearch { get; init; }
    public bool UseOptimizedServiceOwnerDialogSearch { get; init; }
    public bool UseAltinnAutoAuthorizedPartiesQueryParameters { get; init; }
    public bool UseCorrectPersonNameOrdering { get; init; }
    public bool UseAccessManagementForAltinnSelfIdentifiedUsers { get; init; }
    public bool UseAccessManagementForIdportenEmailUsers { get; init; }
    public bool UseAccessManagementForFeideUsers { get; init; }
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

internal sealed class ApplicationSettingsValidator : AbstractValidator<ApplicationSettings>
{
    public ApplicationSettingsValidator(IValidator<DialogportenSettings> dialogportenSettingsValidator)
    {
        RuleFor(x => x.Dialogporten)
            .NotEmpty()
            .SetValidator(dialogportenSettingsValidator);
    }
}

internal sealed class DialogportenSettingsValidator : AbstractValidator<DialogportenSettings>
{
    public DialogportenSettingsValidator()
    {
        RuleFor(x => x.BaseUri).NotEmpty().IsValidUri();
    }
}
