using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;

internal static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder OperationId(this RouteHandlerBuilder builder, string operationId)
    {
        builder.Add(b => b.Metadata.Add(new EndpointNameMetadata(operationId)));
        return builder;
    }

    public static RouteHandlerBuilder ProducesOneOf(this RouteHandlerBuilder builder, params int[] statusCodes)
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
                    builder.ProducesProblemFE<ProblemDetails>(statusCode);
                    break;
                default:
                    builder.Produces(statusCode);
                    break;
            }
        }

        return builder;
    }

    public static RouteHandlerBuilder ProducesOneOf<T>(this RouteHandlerBuilder builder, params int[] statusCodes)
        where T : class
    {
        builder.ProducesOneOf(statusCodes);
        builder.Produces<T>(statusCodes.First());
        return builder;
    }
}
