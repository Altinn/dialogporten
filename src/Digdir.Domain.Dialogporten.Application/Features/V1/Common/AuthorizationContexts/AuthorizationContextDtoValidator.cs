using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

internal sealed class AuthorizationContextDtoValidator : AbstractValidator<AuthorizationContextDto>
{
    public AuthorizationContextDtoValidator()
    {
        RuleFor(x => x.ServiceResource)
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' must start with '{Constants.ServiceResourcePrefix}'.");

        RuleFor(x => x.AdditionalResourceAttribute)
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || !x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' cannot contain a service resource reference ('{Constants.ServiceResourcePrefix}...'); use 'ServiceResource' instead.")
            .Must(x => x is null || (!ExpandsToAppIdentity(x) && !HasAppPrefix(x)))
            .WithMessage($"'{{PropertyName}}' cannot reference an app (the '{Constants.AppResourcePrefix}' namespace, or a value " +
                         $"expanding into an '{Constants.AppResourceIdPrefix}{{org}}_{{appId}}' identifier); 'ServiceResource' already " +
                         "carries the resource-registry entry for an app, and there is no equivalent per-app override for this field.");

        RuleFor(x => x.Parties)
            .Must(x => x.Count <= AuthorizationContext.MaxNumberOfParties)
            .WithMessage($"'{{PropertyName}}' cannot contain more than {AuthorizationContext.MaxNumberOfParties} parties.");

        RuleFor(x => x.Parties)
            .UniqueBy(x => x);

        RuleFor(x => x.Parties)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must contain at least one party when 'IncludeDialogParty' is false.")
            .When(x => !x.IncludeDialogParty);

        RuleForEach(x => x.Parties)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .IsValidPartyIdentifier();

        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.Action is not null);

        RuleFor(x => x.UnauthorizedPresentation)
            .IsInEnum()
            .WithMessage($"'{{PropertyName}}' is required and must be either " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Disabled}' or " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Excluded}'.");
    }

    // Downstream PDP request construction reinterprets the segment after the last colon as an Altinn app
    // identity whenever it starts with the app id prefix, regardless of the namespace the caller stated (e.g.
    // "urn:altinn:task:app_other_sensitive-app" silently becomes an app/org attribute for a different app).
    // AdditionalResourceAttribute has no legitimate app use case - ServiceResource is the field for
    // referencing an app-backed resource-registry entry - so both the explicit app namespace and any value
    // that would implicitly expand into one are rejected outright.
    private static bool ExpandsToAppIdentity(string value)
    {
        var lastColonIndex = value.LastIndexOf(':');
        var tail = lastColonIndex == -1 ? value : value[(lastColonIndex + 1)..];
        return tail.StartsWith(Constants.AppResourceIdPrefix, StringComparison.Ordinal);
    }

    private static bool HasAppPrefix(string value) =>
        value.StartsWith(Constants.AppResourcePrefix, StringComparison.OrdinalIgnoreCase);
}
