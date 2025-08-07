using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common.Authorization;
using Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using FastEndpoints;
using MediatR;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner.Dialogs.Queries.Get;

public sealed class GetDialogEndpoint : Endpoint<GetDialogQuery, DialogDto>
{
    private readonly ISender _sender;
    private readonly ICostManagementMetricsService _metricsService;
    private readonly IServiceIdentifierExtractor _serviceExtractor;

    public GetDialogEndpoint(ISender sender, ICostManagementMetricsService metricsService, IServiceIdentifierExtractor serviceExtractor)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _serviceExtractor = serviceExtractor ?? throw new ArgumentNullException(nameof(serviceExtractor));
    }

    public override void Configure()
    {
        Get("dialogs/{dialogId}");
        Policies(AuthorizationPolicy.ServiceProvider);
        Group<ServiceOwnerGroup>();

        Description(b => b.ProducesOneOf<DialogDto>(
            StatusCodes.Status200OK,
            StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetDialogQuery req, CancellationToken ct)
    {
        var result = await _sender.Send(req, ct);
        await result.Match(
            dto =>
            {
                // Record successful transaction manually (complementing middleware)
                var orgIdentifier = _serviceExtractor.ExtractServiceIdentifier(HttpContext);
                _metricsService.RecordTransaction(
                    TransactionType.GetDialogServiceOwner,
                    StatusCodes.Status200OK,
                    orgIdentifier);

                HttpContext.Response.Headers.ETag = dto.Revision.ToString();
                return SendOkAsync(dto, ct);
            },
            notFound => this.NotFoundAsync(notFound, ct),
            validationError => this.BadRequestAsync(validationError, ct));
    }
}
