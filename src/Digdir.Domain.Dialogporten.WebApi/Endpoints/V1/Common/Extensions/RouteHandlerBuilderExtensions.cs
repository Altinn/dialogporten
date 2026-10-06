using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;

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

                builder.Produces(status, problem.GetType(), "application/problem+json");
            }

            return builder;
        }
    }
}
