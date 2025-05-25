using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogContentDtoValidator : AbstractValidator<ContentDto>
{
    private static readonly NullabilityInfoContext Context = new();
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName =
        typeof(ContentDto).GetProperties()
            .Select(x =>
            {
                var nullabilityInfo = Context.Create(x);
                return new PropertyInfoWithNullability(x, nullabilityInfo);
            })
            .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public UpdateDialogContentDtoValidator(IUser? user)
    {
        foreach (var (propertyName, (propertyInfo, nullabilityInfo)) in SourcePropertyMetaDataByName)
        {
            switch (nullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propertyInfo.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(
                            new ContentValueDtoValidator(DialogContentType.Parse(propertyName), user)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propertyInfo.GetValue(x) as ContentValueDto)
                        .SetValidator(
                            new ContentValueDtoValidator(DialogContentType.Parse(propertyName), user)!)
                        .When(x => propertyInfo.GetValue(x) is not null);
                    break;
                case NullabilityState.Unknown:
                default:
                    break;
            }
        }
    }
}
