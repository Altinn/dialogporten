﻿using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Interfaces;
using Digdir.Domain.Dialogporten.Domain;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Dialogues.Commands.Create;

public class CreateDialogueCommand : CreateDialogueDto, IRequest<Guid> { }

internal sealed class CreateDialogueCommandHandler : IRequestHandler<CreateDialogueCommand, Guid>
{
    private readonly IDialogueDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDialogueCommandHandler(IDialogueDbContext db, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> Handle(CreateDialogueCommand request, CancellationToken cancellationToken)
    {
        var dialogue = _mapper.Map<DialogueEntity>(request);
        await _db.Dialogues.AddAsync(dialogue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return dialogue.Id;
    }
}
