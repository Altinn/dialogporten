﻿using System.Diagnostics;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common.Services;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Elements;
using FluentValidation.Results;
using MediatR;
using OneOf;
using OneOf.Types;
using Constants = Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

public sealed class CreateDialogCommand : CreateDialogDto, IRequest<CreateDialogResult>;

[GenerateOneOf]
public partial class CreateDialogResult : OneOfBase<Success<Guid>, DomainError, ValidationError, Forbidden>;

internal sealed class CreateDialogCommandHandler : IRequestHandler<CreateDialogCommand, CreateDialogResult>
{
    private readonly IDialogDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainContext _domainContext;
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IUserRegistry _userRegistry;
    private readonly IDialogActivityService _dialogActivityService;

    private readonly ValidationFailure _progressValidationFailure = new(nameof(CreateDialogCommand.Progress), "Progress cannot be set for correspondence dialogs.");

    public CreateDialogCommandHandler(
        IDialogDbContext db,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainContext domainContext,
        IUserResourceRegistry userResourceRegistry,
        IUserRegistry userRegistry,
        IDialogActivityService dialogActivityService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _dialogActivityService = dialogActivityService ?? throw new ArgumentNullException(nameof(dialogActivityService));
    }

    public async Task<CreateDialogResult> Handle(CreateDialogCommand request, CancellationToken cancellationToken)
    {
        if (!await _userResourceRegistry.CurrentUserIsOwner(request.ServiceResource, cancellationToken))
        {
            return new Forbidden($"Not owner of {request.ServiceResource}.");
        }

        var serviceResourceType = await _userResourceRegistry.GetResourceType(request.ServiceResource, cancellationToken);

        if (!_userResourceRegistry.UserCanModifyResourceType(serviceResourceType))
        {
            return new Forbidden($"User cannot modify resource type {serviceResourceType}.");
        }

        if (serviceResourceType == Constants.Correspondence)
        {
            if (request.Progress is not null)
                return new ValidationError(_progressValidationFailure);
        }

        var dialog = _mapper.Map<DialogEntity>(request);

        var userInfo = await _userRegistry.GetCurrentUserInformation(cancellationToken);

        dialog.Org = userInfo.ServiceOwnerShortName ?? string.Empty;
        dialog.ServiceResourceType = serviceResourceType;

        if (string.IsNullOrWhiteSpace(dialog.Org))
        {
            _domainContext.AddError(new DomainFailure(nameof(DialogEntity.Org),
                "Cannot find service owner organization shortname for current user. Please ensure that you are logged in as a service owner."));
        }

        var existingDialogIds = await _db.GetExistingIds(new[] { dialog }, cancellationToken);
        if (existingDialogIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogEntity>(existingDialogIds));
        }

        var existingActivityIds = await _db.GetExistingIds(dialog.Activities, cancellationToken);
        if (existingActivityIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogActivity>(existingActivityIds));
        }

        var existingElementIds = await _db.GetExistingIds(dialog.Elements, cancellationToken);
        if (existingElementIds.Count != 0)
        {
            _domainContext.AddError(DomainFailure.EntityExists<DialogElement>(existingElementIds));
        }

        await _dialogActivityService.EnsurePerformedByIsSetForActivities(dialog.Activities, cancellationToken);

        await _db.Dialogs.AddAsync(dialog, cancellationToken);

        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.Match<CreateDialogResult>(
            success => new Success<Guid>(dialog.Id),
            domainError => domainError,
            concurrencyError => throw new UnreachableException("Should never get a concurrency error when creating a new dialog"));
    }
}
