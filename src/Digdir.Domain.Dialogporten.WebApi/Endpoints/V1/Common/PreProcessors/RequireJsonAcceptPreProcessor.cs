using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.PreProcessors;

internal sealed class RequireJsonAcceptPreProcessor : IPreProcessor<EmptyRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<EmptyRequest> context, CancellationToken ct)
    {
        if (AcceptHeaderHelper.AllowsJson(context.HttpContext.Request.Headers.Accept))
        {
            return;
        }

        var failures = new List<ValidationFailure>
        {
            new("Accept", "The request must accept application/json responses.")
        };

        context.HttpContext.Response.StatusCode = StatusCodes.Status406NotAcceptable;
        var response = context.HttpContext.GetResponseOrDefault(context.HttpContext.Response.StatusCode, failures);
        await context.HttpContext.Response.WriteAsJsonAsync(response, response.GetType(), cancellationToken: ct);
        context.HttpContext.MarkResponseStart();
    }
}

internal static class AcceptHeaderHelper
{
    public static bool AllowsJson(StringValues acceptValues)
    {
        if (StringValues.IsNullOrEmpty(acceptValues))
        {
            return true;
        }

        if (!MediaTypeHeaderValue.TryParseList(acceptValues, out var parsedValues))
        {
            return true;
        }

        foreach (var mediaType in parsedValues)
        {
            if (mediaType.Quality is { } and <= 0)
            {
                continue;
            }

            if (mediaType.MatchesAllTypes)
            {
                return true;
            }

            if (!mediaType.Type.HasValue || !mediaType.SubType.HasValue)
            {
                continue;
            }

            var type = mediaType.Type.Value;
            var subtype = mediaType.SubType.Value;

            if (string.Equals(type, "application", StringComparison.OrdinalIgnoreCase) && mediaType.MatchesAllSubTypes)
            {
                return true;
            }

            if (IsJsonSubtype(subtype) &&
                string.Equals(type, "application", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsJsonSubtype(string subtype)
        => subtype.Equals("json", StringComparison.OrdinalIgnoreCase)
           || subtype.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
}
