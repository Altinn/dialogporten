using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;
using FastEndpoints;
using ProblemDetails = Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;

internal static class RouteHandlerBuilderExtensions
{
    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder ProducesDpProblemFor(params int[] statusCodes)
        {
            foreach (var status in statusCodes)
            {
                var fakeCtx = new DefaultHttpContext { Response = { StatusCode = status } };
                var problem = ProblemDetailsRules.TryCreateProblemDetails(fakeCtx, [])
                              ?? throw new ArgumentException($"Missing ProblemDetails for status: {status}");

                _ = problem switch
                {
                    ProblemDetails => builder.ProducesProblemFE<ProblemDetails>(status),

                    _ => throw new ArgumentException("Unknown problem for status: ", nameof(status))
                };
            }

            return builder;
        }
    }
}
