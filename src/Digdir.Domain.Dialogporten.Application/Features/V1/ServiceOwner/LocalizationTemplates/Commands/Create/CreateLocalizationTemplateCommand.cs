using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using MediatR;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Create;

public sealed class CreateLocalizationTemplateCommand : IRequest<CreateLocalizationTemplateResult>
{
    public required LocalizationTemplateSetDto TemplateSet { get; init; }
}

[GenerateOneOf]
public sealed partial class CreateLocalizationTemplateResult : OneOfBase<Success, DomainError, ValidationError>;

internal sealed class CreateLocalizationTemplateCommandHandler : IRequestHandler<CreateLocalizationTemplateCommand, CreateLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserOrganizationRegistry _userOrganizationRegistry;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLocalizationTemplateCommandHandler(
        IDialogDbContext db,
        IUserOrganizationRegistry userOrganizationRegistry,
        IUnitOfWork unitOfWork)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userOrganizationRegistry = userOrganizationRegistry ?? throw new ArgumentNullException(nameof(userOrganizationRegistry));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateLocalizationTemplateResult> Handle(CreateLocalizationTemplateCommand request, CancellationToken cancellationToken)
    {
        request.TemplateSet.Org ??= await _userOrganizationRegistry.GetCurrentUserOrgShortNameStrict(cancellationToken);
        _db.LocalizationTemplateSets.Add(request.TemplateSet.ToEntity());
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<CreateLocalizationTemplateResult>(
            success => new Success(),
            domainError => domainError,
            concurrencyError => throw new UnreachableException("Should never get a concurrency error when creating a new LocalizationTemplateSet."));
    }
}
