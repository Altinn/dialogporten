using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogTransmissionContentDtoValidator : AbstractValidator<TransmissionContentDto>
{
    private static readonly NullabilityInfoContext Context = new();
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName = typeof(TransmissionContentDto)
        .GetProperties()
        .Select(x =>
        {
            var nullabilityInfo = Context.Create(x);
            return new PropertyInfoWithNullability(x, nullabilityInfo);
        })
        .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public CreateDialogDialogTransmissionContentDtoValidator()
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            var contentType = DialogTransmissionContentType.Parse(propertyName);
            var propertySelector = propMetadata.Property;

            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propertySelector.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(new ContentValueDtoValidator(contentType)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propertySelector.GetValue(x) as ContentValueDto)
                        .SetValidator(new ContentValueDtoValidator(contentType)!)
                        .When(x => propertySelector.GetValue(x) is not null);
                    break;
                case NullabilityState.Unknown:
                    break;
                default:
                    break;
            }
        }
    }
}