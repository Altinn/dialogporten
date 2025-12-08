using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using FluentValidation;
using static Digdir.Domain.Dialogporten.Application.Features.V1.Common.ValidationErrorStrings;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchNew;

internal sealed class SearchDialogQueryValidator : AbstractValidator<SearchDialogQuery>
{
    public SearchDialogQueryValidator()
    {
        Include(new PaginationParameterValidator<SearchDialogQueryOrderDefinition, DialogEntity>());

        RuleForEach(x => x.ServiceOwnerLabels)
            .MinimumLength(Constants.MinSearchStringLength)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Search)
            .MinimumLength(Constants.MinSearchStringLength)
            .When(x => x.Search is not null);

        RuleFor(x => x.SearchLanguageCode)
            .Must(x => x is null || Localization.IsValidCultureCode(x))
            .WithMessage(searchQuery =>
                (searchQuery.SearchLanguageCode == "no"
                    ? LocalizationValidatorConstants.InvalidCultureCodeErrorMessageWithNorwegianHint
                    : LocalizationValidatorConstants.InvalidCultureCodeErrorMessage) +
                LocalizationValidatorConstants.NormalizationErrorMessage);

        RuleFor(x => x.EndUserId)
            .Must(x => PartyIdentifier.TryParse(x, out var id) && id is NorwegianPersonIdentifier or SystemUserIdentifier)
            .WithMessage($"{{PropertyName}} must be a valid end user identifier. It must match the format " +
                         $"'{NorwegianPersonIdentifier.PrefixWithSeparator}{{norwegian f-nr/d-nr}}' or '{SystemUserIdentifier.PrefixWithSeparator}{{uuid}}'.")
            .Must((x, _) => !x.ServiceResource.IsNullOrEmpty() || !x.Party.IsNullOrEmpty())
            .WithMessage($"Either '{nameof(SearchDialogQuery.ServiceResource)}' or '{nameof(SearchDialogQuery.Party)}' " +
                         $"must be specified if '{nameof(SearchDialogQuery.EndUserId)}' is provided.")
            .When(x => x.EndUserId is not null);

        RuleFor(x => x.Search)
            .Must((x, _) => x.EndUserId is not null)
            .WithMessage($"'{nameof(SearchDialogQuery.EndUserId)}' must be specified if {{PropertyName}} is provided.")
            .When(x => x.Search is not null);

        RuleForEach(x => x.Party)
            .IsValidPartyIdentifier();

        RuleFor(x => x.ServiceResource!.Count)
            .LessThanOrEqualTo(20)
            .When(x => x.ServiceResource is not null);

        RuleFor(x => x.Party!.Count)
            .LessThanOrEqualTo(20)
            .When(x => x.Party is not null);

        RuleFor(x => x.ExtendedStatus!.Count)
            .LessThanOrEqualTo(20)
            .When(x => x.ExtendedStatus is not null);

        RuleForEach(x => x.Status).IsInEnum();

        RuleForEach(x => x.SystemLabel).IsInEnum();
        RuleFor(x => x.Process)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.Process is not null);

        RuleFor(x => x.CreatedAfter)
            .LessThanOrEqualTo(x => x.CreatedBefore)
            .When(x => x.CreatedAfter is not null && x.CreatedBefore is not null)
            .WithMessage(PropertyNameMustBeLessThanOrEqualToComparisonProperty);

        RuleFor(x => x.DueAfter)
            .LessThanOrEqualTo(x => x.DueBefore)
            .When(x => x.DueAfter is not null && x.DueBefore is not null)
            .WithMessage(PropertyNameMustBeLessThanOrEqualToComparisonProperty);

        RuleFor(x => x.UpdatedAfter)
            .LessThanOrEqualTo(x => x.UpdatedBefore)
            .When(x => x.UpdatedAfter is not null && x.UpdatedBefore is not null)
            .WithMessage(PropertyNameMustBeLessThanOrEqualToComparisonProperty);

        RuleFor(x => x.ContentUpdatedAfter)
            .LessThanOrEqualTo(x => x.ContentUpdatedBefore)
            .When(x => x.ContentUpdatedAfter is not null && x.ContentUpdatedBefore is not null)
            .WithMessage(PropertyNameMustBeLessThanOrEqualToComparisonProperty);

        RuleFor(x => x.VisibleAfter)
            .LessThanOrEqualTo(x => x.VisibleBefore)
            .When(x => x.VisibleAfter is not null && x.VisibleBefore is not null)
            .WithMessage(PropertyNameMustBeLessThanOrEqualToComparisonProperty);
    }
}
