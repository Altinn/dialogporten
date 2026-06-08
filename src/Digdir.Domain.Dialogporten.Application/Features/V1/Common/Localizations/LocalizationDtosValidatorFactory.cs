using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

internal sealed class LocalizationDtosValidatorFactory
{
    private readonly IUser _user;

    public LocalizationDtosValidatorFactory(IUser user)
    {
        _user = user;
    }

    public LocalizationDtosValidator CreateActivityDescriptionLocalizationDtosValidator()
    {
        return _user.GetPrincipal().IsCorrespondence()
            ? new LocalizationDtosValidator(Constants.CorrespondenceActivityDescriptionMaxLength)
            : new LocalizationDtosValidator();
    }
}
