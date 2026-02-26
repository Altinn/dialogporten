using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.PartyResourceReferences;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.Events.PartyResourceReferences;

public class PartyResourceReferenceCacheInvalidatorTests
{
    [Fact]
    public async Task Handle_WhenDialogCreated_ShouldInvalidateCachedReferencesForParty()
    {
        var repo = Substitute.For<IPartyResourceReferenceRepository>();
        var sut = new PartyResourceReferenceCacheInvalidator(repo);
        var party = "urn:altinn:organization:identifier-no:313130983";

        await sut.Handle(
            new DialogCreatedDomainEvent(
                Guid.NewGuid(),
                "urn:altinn:resource:resource-a",
                party,
                Process: null,
                PrecedingProcess: null),
            CancellationToken.None);

        await repo.Received(1).InvalidateCachedReferencesForParty(party, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenDialogDeleted_ShouldInvalidateCachedReferencesForParty()
    {
        var repo = Substitute.For<IPartyResourceReferenceRepository>();
        var sut = new PartyResourceReferenceCacheInvalidator(repo);
        var party = "urn:altinn:person:identifier-no:19895597581";

        await sut.Handle(
            new DialogDeletedDomainEvent(
                Guid.NewGuid(),
                "urn:altinn:resource:resource-a",
                party,
                Process: null,
                PrecedingProcess: null),
            CancellationToken.None);

        await repo.Received(1).InvalidateCachedReferencesForParty(party, CancellationToken.None);
    }
}
