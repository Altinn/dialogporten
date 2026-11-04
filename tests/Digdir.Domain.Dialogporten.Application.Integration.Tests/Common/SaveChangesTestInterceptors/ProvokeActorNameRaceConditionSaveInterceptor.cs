using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using MassTransit.Mediator;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.SaveChangesTestInterceptors;

/// <summary>
/// Provokes a race condition (DbConflict) by Inserting all actor-names right before EF saves said actor-names.
/// </summary>
internal sealed class ProvokeActorNameRaceConditionSaveInterceptor : SaveChangesInterceptor, ISaveChangesTestInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _enabled = true;

    public ProvokeActorNameRaceConditionSaveInterceptor(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scopeFactory = scopeFactory;
    }

    public bool HasRaced { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled || HasRaced || eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var pendingActorNames = eventData.Context.ChangeTracker
            .Entries<ActorName>()
            .Where(x => x.State is EntityState.Added)
            .Select(x => x.Entity)
            .ToList();

        if (pendingActorNames.Count == 0)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        await InsertActorNames(pendingActorNames, cancellationToken);

        HasRaced = true;
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task InsertActorNames(List<ActorName> actorName, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var interceptorForOtherUser = scope.ServiceProvider
            .GetServices<ISaveChangesTestInterceptor>()
            .OfType<ProvokeActorNameRaceConditionSaveInterceptor>()
            .Single();

        interceptorForOtherUser._enabled = false;
        await mediator.Send(new CreateDialogCommand
        {
            IsSilentUpdate = false,
            Dto = DialogGenerator.GenerateFakeDialog(
                party: NorwegianPersonIdentifier.PrefixWithSeparator + "04087647795",
                activities: actorName.Select(x => new ActivityDto
                {
                    Id = null,
                    CreatedAt = null,
                    ExtendedType = null,
                    Type = DialogActivityType.Values.Information,
                    TransmissionId = null,
                    PerformedBy = new ActorDto
                    {
                        ActorType = ActorType.Values.PartyRepresentative,
                        ActorId = x.ActorId,
                        ActorName = x.Name
                    },
                    Description =
                    [
                        new LocalizationDto
                        {
                            Value = $"By {nameof(ProvokeActorNameRaceConditionSaveInterceptor)} to provoke a race",
                            LanguageCode = "nb"
                        }
                    ]
                }).ToList()
            )
        }, cancellationToken);
    }
}
