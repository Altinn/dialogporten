using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using FluentValidation;
using AuthorizationConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

internal sealed class AuthorizationContextDtoValidator : AbstractValidator<AuthorizationContextDto>
{
    public AuthorizationContextDtoValidator()
    {
        RuleFor(x => x.ServiceResource)
            .IsValidContextServiceResource();

        RuleFor(x => x.AdditionalResourceAttribute)
            .IsValidAuthorizationAttribute()
            .Must(x => x is null || !x.StartsWith(Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' cannot contain a service resource reference ('{Constants.ServiceResourcePrefix}...'); use 'ServiceResource' instead.")
            .Must(x => x is null || (!ExpandsToAppIdentity(x) && !HasAppPrefix(x)))
            .WithMessage($"'{{PropertyName}}' cannot reference an app (the '{Constants.AppResourcePrefix}' namespace, or a value " +
                         $"expanding into an '{Constants.AppResourceIdPrefix}{{org}}_{{appId}}' identifier); 'ServiceResource' already " +
                         "carries the resource-registry entry for an app, and there is no equivalent per-app override for this field.")
            // The org namespace is rejected for the same reason as the app namespace above: it is the other
            // half of an app identity. An app-backed service resource already renders as an urn:altinn:app +
            // urn:altinn:org pair, so a caller-supplied org would put a second, caller-chosen org value in the
            // same resource category, where any-of attribute matching lets it satisfy a policy target
            // belonging to another organization.
            .Must(x => x is null || !x.StartsWith(Constants.OrgResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"'{{PropertyName}}' cannot reference an organization (the '{Constants.OrgResourcePrefix}' namespace); " +
                         "the organization owning the effective resource is derived from the resource itself.");

        RuleFor(x => x)
            .Must(x => x.Parties.Count + x.DialogPartyChecks.Count is >= 1 and <= AuthorizationContext.MaxNumberOfEntries)
            .OverridePropertyName(nameof(AuthorizationContextDto.Parties))
            .WithMessage($"'{nameof(AuthorizationContextDto.Parties)}' and '{nameof(AuthorizationContextDto.DialogPartyChecks)}' " +
                         $"must together contain at least 1 and at most {AuthorizationContext.MaxNumberOfEntries} entries.");

        RuleFor(x => x.Parties)
            .UniqueBy(x => x);

        RuleForEach(x => x.Parties)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .IsValidPartyIdentifier();

        // Null entries are rejected here; the aggregate rules below skip them so that they report this failure
        // instead of throwing.
        RuleForEach(x => x.DialogPartyChecks)
            .NotNull()
            .SetValidator(DialogPartyCheckValidator);

        RuleFor(x => x)
            .Must(x => x.DialogPartyChecks
                .OfType<DialogPartyCheckDto>()
                .Select(check => GetNormalizedIdentity(x, check))
                .Distinct()
                .Count() == x.DialogPartyChecks.OfType<DialogPartyCheckDto>().Count())
            .OverridePropertyName(nameof(AuthorizationContextDto.DialogPartyChecks))
            .WithMessage($"'{nameof(AuthorizationContextDto.DialogPartyChecks)}' cannot contain duplicate checks " +
                         "(same access restriction, via party, effective service resource and effective action).");

        // A None check already permits everything a restricted check for the same right would.
        RuleFor(x => x)
            .Must(x => !x.DialogPartyChecks
                .OfType<DialogPartyCheckDto>()
                .Where(check => check.AccessRestriction
                    is DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy
                    or DialogPartyCheckAccessRestriction.Values.ClientDelegation)
                .Any(check => x.DialogPartyChecks.OfType<DialogPartyCheckDto>().Any(other =>
                    other.AccessRestriction is DialogPartyCheckAccessRestriction.Values.None
                    && GetNormalizedRight(x, other) == GetNormalizedRight(x, check))))
            .OverridePropertyName(nameof(AuthorizationContextDto.DialogPartyChecks))
            .WithMessage($"'{nameof(AuthorizationContextDto.DialogPartyChecks)}' cannot contain a " +
                         $"'{DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy}' or " +
                         $"'{DialogPartyCheckAccessRestriction.Values.ClientDelegation}' check for the same effective service " +
                         $"resource and action as a '{DialogPartyCheckAccessRestriction.Values.None}' check, which already covers it.");

        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.Action is not null);

        RuleFor(x => x.TokenRef)
            .NotEmpty()
            .MaximumLength(AuthorizationContext.MaxTokenReferenceLength)
            .When(x => x.TokenRef is not null);

        RuleFor(x => x.UnauthorizedPresentation)
            .IsInEnum()
            .WithMessage($"'{{PropertyName}}' is required and must be either " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Disabled}' or " +
                         $"'{AuthorizationContextUnauthorizedPresentation.Values.Excluded}'.");
    }

    private static readonly DialogPartyCheckDtoValidator DialogPartyCheckValidator = new();

    // The dialog's own service resource is deliberately not resolved here: a check explicitly naming it is
    // rendered without the dialog's instance reference, so it is a different PDP request than one inheriting it.
    private static (string? ServiceResource, string Action) GetNormalizedRight(
        AuthorizationContextDto context, DialogPartyCheckDto check) =>
        (check.ServiceResource ?? context.ServiceResource,
            check.Action ?? context.Action ?? AuthorizationConstants.ReadAction);

    private static (DialogPartyCheckAccessRestriction.Values, string?, (string?, string)) GetNormalizedIdentity(
        AuthorizationContextDto context, DialogPartyCheckDto check) =>
        (check.AccessRestriction, check.ViaParty, GetNormalizedRight(context, check));

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
