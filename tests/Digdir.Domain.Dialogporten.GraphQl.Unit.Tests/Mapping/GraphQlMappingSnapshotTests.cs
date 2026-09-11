using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using GetDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
using SearchDialogInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs.SearchDialogInput;
using SetSystemLabelInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes.SetSystemLabelInput;
using BulkSetSystemLabelInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes.BulkSetSystemLabelInput;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

#pragma warning disable CS0618 // Obsolete DTO members are intentionally exercised by the snapshots

/// <summary>
/// Golden-master snapshots of the GraphQL mappings. These lock in the exact output of every mapping
/// call site in the GraphQL EndUser query/mutation handlers. The snapshots were originally captured
/// against the AutoMapper-based mappings and are now produced by the hand-written static mappers
/// (issue #967); the verified files staying byte-for-byte identical proves the migration is
/// behaviour-preserving.
/// </summary>
public sealed class GraphQlMappingSnapshotTests
{
    [Fact]
    public Task DialogById_DialogDto_To_Dialog()
    {
        var source = ObjectFiller.Fill<GetDialogDto>();
        var result = source.ToDialog();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task SearchDialogs_Input_To_Query()
    {
        var source = ObjectFiller.Fill<SearchDialogInput>();
        var result = source.ToSearchDialogQuery();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task SearchDialogs_PaginatedList_To_Payload()
    {
        var filler = new ObjectFiller();
        var source = new PaginatedList<SearchDialogDto>(
            items: [filler.Create<SearchDialogDto>(), filler.Create<SearchDialogDto>()],
            hasNextPage: true,
            @continue: "continuation-token",
            orderBy: "order-by-string");

        var result = source.ToSearchDialogsPayload();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task DialogLookup_Dto_To_Model()
    {
        var source = ObjectFiller.Fill<EndUserIdentifierLookupDto>();
        var result = source.ToDialogLookup();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Limits_Dto_To_Model()
    {
        var source = ObjectFiller.Fill<GetLimitsDto>();
        var result = source.ToLimits();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Parties_Dtos_To_AuthorizedParties()
    {
        var filler = new ObjectFiller();
        var source = new List<AuthorizedPartyDto>
        {
            filler.Create<AuthorizedPartyDto>(),
            filler.Create<AuthorizedPartyDto>()
        };

        var result = source.ToAuthorizedParties();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Mutation_SetSystemLabelInput_To_Command()
    {
        var source = ObjectFiller.Fill<SetSystemLabelInput>();
        var result = source.ToCommand();
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Mutation_BulkSetSystemLabelInput_To_Command()
    {
        var source = ObjectFiller.Fill<BulkSetSystemLabelInput>();
        var result = source.ToCommand();
        return Verify(result).UseDirectory("Snapshots");
    }
}
