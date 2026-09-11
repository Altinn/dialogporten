using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.AccessManagement.Queries.GetParties;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL;
using Microsoft.Extensions.DependencyInjection;
using GetDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
using SearchDialogQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogQuery;
using SetSystemLabelCommand = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand;
using BulkSetSystemLabelCommand = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand;
using Dialog = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById.Dialog;
using SearchDialogInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs.SearchDialogInput;
using SearchDialogsPayload = Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs.SearchDialogsPayload;
using DialogLookupModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup.DialogLookup;
using LimitsModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.Limits.Limits;
using AuthorizedParty = Digdir.Domain.Dialogporten.GraphQL.EndUser.Parties.AuthorizedParty;
using SetSystemLabelInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes.SetSystemLabelInput;
using BulkSetSystemLabelInput = Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes.BulkSetSystemLabelInput;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

#pragma warning disable CS0618 // Obsolete DTO members are intentionally exercised by the snapshots

/// <summary>
/// Golden-master snapshots of the current AutoMapper-based GraphQL mappings. These lock in the exact
/// output of every <c>mapper.Map&lt;T&gt;(...)</c> call site in the GraphQL EndUser query/mutation
/// handlers so that the upcoming migration to hand-written static mappers (issue #967) can be proven
/// behaviour-preserving: after the migration the Act step is switched to the static mapper and the
/// verified files must stay byte-for-byte identical.
/// </summary>
public sealed class GraphQlMappingSnapshotTests
{
    private readonly IMapper _mapper;

    public GraphQlMappingSnapshotTests()
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(GraphQLAssemblyMarker.Assembly);
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public Task DialogById_DialogDto_To_Dialog()
    {
        var source = ObjectFiller.Fill<GetDialogDto>();
        var result = _mapper.Map<Dialog>(source);
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task SearchDialogs_Input_To_Query()
    {
        var source = ObjectFiller.Fill<SearchDialogInput>();
        var result = _mapper.Map<SearchDialogQuery>(source);
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

        var result = _mapper.Map<SearchDialogsPayload>(source);
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task DialogLookup_Dto_To_Model()
    {
        var source = ObjectFiller.Fill<EndUserIdentifierLookupDto>();
        var result = _mapper.Map<DialogLookupModel>(source);
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Limits_Dto_To_Model()
    {
        var source = ObjectFiller.Fill<GetLimitsDto>();
        var result = _mapper.Map<LimitsModel>(source);
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

        var result = _mapper.Map<List<AuthorizedParty>>(source);
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Mutation_SetSystemLabelInput_To_Command()
    {
        var source = ObjectFiller.Fill<SetSystemLabelInput>();
        var result = _mapper.Map<SetSystemLabelCommand>(source);
        return Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task Mutation_BulkSetSystemLabelInput_To_Command()
    {
        var source = ObjectFiller.Fill<BulkSetSystemLabelInput>();
        var result = _mapper.Map<BulkSetSystemLabelCommand>(source);
        return Verify(result).UseDirectory("Snapshots");
    }
}
