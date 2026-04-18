using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public sealed class OpenApiAudienceFilterOperationProcessor(string audience) : IOperationProcessor
{
    private readonly string _audienceSegment = $"/{audience.Trim('/')}/";

    public bool Process(OperationProcessorContext context)
    {
        var path = $"/{((AspNetCoreOperationProcessorContext)context).ApiDescription.RelativePath?.TrimStart('~').TrimEnd('/')}";
        return path.Contains(_audienceSegment, StringComparison.OrdinalIgnoreCase);
    }
}
