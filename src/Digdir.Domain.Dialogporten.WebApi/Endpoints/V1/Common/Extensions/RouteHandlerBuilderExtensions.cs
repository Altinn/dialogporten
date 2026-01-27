using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;

internal static class RouteHandlerBuilderExtensions
{
    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder OperationId(string operationId)
        {
            builder.Add(b => b.Metadata.Add(new EndpointNameMetadata(operationId)));
            return builder;
        }

        public RouteHandlerBuilder ProducesOneOf(params int[] statusCodes)
        {
            builder.ClearDefaultProduces(StatusCodes.Status200OK);
            foreach (var statusCode in statusCodes)
            {
                switch (statusCode)
                {
                    case StatusCodes.Status201Created:
                        builder.Produces<string>(statusCode, "application/json");
                        break;
                    case StatusCodes.Status400BadRequest:
                    case StatusCodes.Status412PreconditionFailed:
                    case StatusCodes.Status404NotFound:
                    case StatusCodes.Status422UnprocessableEntity:
                        builder.ProducesProblemDetails(statusCode);
                        break;
                    default:
                        builder.Produces(statusCode);
                        break;
                }
            }

            return builder;
        }

        public RouteHandlerBuilder ProducesOneOf<T>(params int[] statusCodes)
            where T : class
        {
            builder.ProducesOneOf(statusCodes);
            builder.Produces<T>(statusCodes.First());
            return builder;
        }
    }
}
