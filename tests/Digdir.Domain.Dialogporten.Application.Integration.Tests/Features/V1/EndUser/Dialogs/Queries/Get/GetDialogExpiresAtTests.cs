using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogExpiresAtTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_NotFound_For_Expired_Dialog()
    {
        var expiresAt = DialogApplication.Clock.UtcNowOffset.AddDays(1);

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ExpiresAt = expiresAt)
            .OverrideUtc(expiresAt.AddSeconds(1))
            .GetEndUserDialog()
            .ExecuteAndAssert<EntityExpired<DialogEntity>>();
    }
}
