using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabel;

public sealed class GetServiceOwnerLabelEndpoint(ISender sender) : Endpoint<GetServiceOwnerLabelsQuery, List<ServiceOwnerLabelDto>>
{
    private readonly ISender _sender = sender ?? throw new ArgumentNullException(nameof(sender));

    public override void Configure()
    {
        Get("dialogs/{dialogId}/context/labels");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetServiceOwnerLabelsQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            success =>
            {
                HttpContext.Response.Headers.Append(Constants.ETag, success.Revision.ToString());
                return SendOkAsync(success.Labels, ct);
            },
            notFound => this.NotFoundAsync(notFound, ct));
    }
}
