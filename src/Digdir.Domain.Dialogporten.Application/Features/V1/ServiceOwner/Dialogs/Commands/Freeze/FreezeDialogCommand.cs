using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using MediatR;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;

public sealed class FreezeDialogCommand : IRequest<FreezeDialogResult>
{
    public Guid Id { get; set; }

    public Guid? IfMatchDialogRevision { get; set; }
}

[GenerateOneOf]
public sealed partial class FreezeDialogResult : OneOfBase<Success, EntityNotFound, Forbidden, ConcurrencyError>;
