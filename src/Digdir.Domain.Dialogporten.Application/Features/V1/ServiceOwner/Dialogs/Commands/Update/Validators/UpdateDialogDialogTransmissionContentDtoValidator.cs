using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDialogTransmissionContentDtoValidator : AbstractValidator<TransmissionContentDto>
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

    public UpdateDialogDialogTransmissionContentDtoValidator(IUser? user)
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(
                            new ContentValueDtoValidator(DialogTransmissionContentType.Parse(propertyName), user)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .SetValidator(
                            new ContentValueDtoValidator(DialogTransmissionContentType.Parse(propertyName), user)!)
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
