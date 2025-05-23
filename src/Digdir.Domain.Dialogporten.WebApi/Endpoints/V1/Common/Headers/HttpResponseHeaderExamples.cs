using Digdir.Domain.Dialogporten.WebApi.Common;
using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Headers;

public static class HttpResponseHeaderExamples
{
    public static ResponseHeader NewDialogETagHeader(int statusCode)
        => new(statusCode, Constants.ETag)
        {
            Description = "The new UUID ETag of the dialog",
            Example = "123e4567-e89b-12d3-a456-426614174000"
        };

    public static ResponseHeader NewServiceOwnerContextETagHeader(int statusCode)
        => new(statusCode, Constants.ETag)
        {
            Description = "The new UUID ETag of the ServiceOwner Context",
            Example = "123e4567-e89b-12d3-a456-426614174000"
        };
}
