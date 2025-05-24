using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogContentDtoValidator : AbstractValidator<ContentDto?>
{
    private static readonly NullabilityInfoContext Context = new();
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName = typeof(ContentDto)
        .GetProperties()
        .Select(x =>
        {
            var nullabilityInfo = Context.Create(x);
            return new PropertyInfoWithNullability(x, nullabilityInfo);
        })
        .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public CreateDialogContentDtoValidator(IUser? user)
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            var contentType = DialogContentType.Parse(propertyName);
            var propertySelector = propMetadata.Property;

            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propertySelector.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(new ContentValueDtoValidator(contentType, user)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .SetValidator(new ContentValueDtoValidator(contentType, user)!)
                        .When(x => propMetadata.Property.GetValue(x) is not null);
                    break;
                case NullabilityState.Unknown:
                    break;
                default:
                    break;
            }
        }
    }
}