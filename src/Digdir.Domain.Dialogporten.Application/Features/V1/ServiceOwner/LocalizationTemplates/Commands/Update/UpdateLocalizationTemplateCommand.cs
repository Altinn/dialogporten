using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Update;

public sealed class UpdateLocalizationTemplateCommand : IRequest<UpdateLocalizationTemplateResult>
{
    public Guid? IfMatchRevision { get; init; }
    public required TemplateSetDto TemplateSet { get; init; }
}

[GenerateOneOf]
public sealed partial class UpdateLocalizationTemplateResult : OneOfBase<Success<Guid>, DomainError, EntityNotFound, ValidationError, ConcurrencyError>;

public sealed class UpdateLocalizationTemplateCommandHandler : IRequestHandler<UpdateLocalizationTemplateCommand, UpdateLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserOrganizationRegistry _userOrganizationRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLocalizationTemplateCommandHandler(
        IDialogDbContext db,
        IUserOrganizationRegistry userOrganizationRegistry,
        IUnitOfWork unitOfWork)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userOrganizationRegistry = userOrganizationRegistry ?? throw new ArgumentNullException(nameof(userOrganizationRegistry));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateLocalizationTemplateResult> Handle(UpdateLocalizationTemplateCommand command, CancellationToken cancellationToken)
    {
        var setDto = command.TemplateSet;
        setDto.Org ??= await _userOrganizationRegistry.GetCurrentUserOrgShortNameStrict(cancellationToken);

        var setEntity = await _db.LocalizationTemplateSets
            .FirstOrDefaultAsync(x => x.Org == setDto.Org && x.Id == setDto.Id, cancellationToken);

        if (setEntity is null)
        {
            return new EntityNotFound<LocalizationTemplateSet>([setDto.Org, setDto.Id]);
        }

        var languageCodesForRemoval = setEntity.Templates
            .ExceptBy(setDto.Templates.Select(x => x.LanguageCode), x => x.LanguageCode)
            .Select(x => x.LanguageCode);

        foreach (var code in languageCodesForRemoval)
        {
            setEntity.RemoveTemplate(code);
        }

        foreach (var templateDto in setDto.Templates)
        {
            setEntity.AddOrUpdateTemplate(templateDto.LanguageCode, templateDto.Template);
        }

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(setEntity, command.IfMatchRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<UpdateLocalizationTemplateResult>(
            success => new Success<Guid>(setEntity.Revision),
            domainError => domainError,
            concurrencyError => concurrencyError);
    }
}
