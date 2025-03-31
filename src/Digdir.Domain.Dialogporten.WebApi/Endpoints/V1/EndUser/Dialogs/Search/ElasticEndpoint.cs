using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Dialogs.Search;

public sealed class ElasticEndpoint : Endpoint<Test>
{
    private readonly ISender _sender;

    // private static readonly List<string> Parties = File.ReadAllLines("./parties").ToList();
    public ElasticEndpoint(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public override void Configure()
    {
        Get("dialogfacets");
        Policies(AuthorizationPolicy.EndUser);
        Group<EndUserGroup>();
        Description(d => d.ClearDefaultProduces(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(Test req, CancellationToken ct)
    {
        // var random = new Random();
        // var selectedParties = Parties
        //     .OrderBy(_ => random.Next())
        //     .Take(req.NumOfParti)
        //     .ToList();

        List<string> selectedParties = ["urn:altinn:organization:identifier-no:000382310"];

        var result = await _sender.Send(new ElasticRequest
        {
            Party = selectedParties,
            ServiceResource = []
        }, ct);
        await result.Match(
            elapsedTimeInMs => SendOkAsync(elapsedTimeInMs, ct));
    }
}

public sealed class Test
{
    public int NumOfParti { get; set; }
}
