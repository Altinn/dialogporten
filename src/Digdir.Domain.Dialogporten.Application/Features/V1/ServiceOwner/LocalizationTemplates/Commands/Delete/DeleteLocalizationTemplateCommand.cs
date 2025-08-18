using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Delete;

public sealed class DeleteLocalizationTemplateCommand : IRequest<DeleteLocalizationTemplateResult>
{
    public required string Org { get; init; }
    public required string Id { get; init; }
    public Guid? IfMatchRevision { get; init; }
}

[GenerateOneOf]
public sealed partial class DeleteLocalizationTemplateResult : OneOfBase<Success, EntityNotFound, ConcurrencyError>;

internal sealed class DeleteLocalizationTemplateCommandHandler : IRequestHandler<DeleteLocalizationTemplateCommand, DeleteLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLocalizationTemplateCommandHandler(IDialogDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DeleteLocalizationTemplateResult> Handle(DeleteLocalizationTemplateCommand command, CancellationToken cancellationToken)
    {
        var set = await _db.LocalizationTemplateSets.FindAsync([command.Org, command.Id], cancellationToken);

        if (set is null)
        {
            return new EntityNotFound<LocalizationTemplateSet>([command.Org, command.Id]);
        }

        _db.LocalizationTemplateSets.Remove(set);

        var saveResult = await _unitOfWork
            .EnableConcurrencyCheck(set, command.IfMatchRevision)
            .SaveChangesAsync(cancellationToken);

        return saveResult.Match<DeleteLocalizationTemplateResult>(
            success => success,
            domainError => throw new UnreachableException("Should never get a domain error when deleting a LocalizationTemplateSet."),
            concurrencyError => concurrencyError);
    }
}
