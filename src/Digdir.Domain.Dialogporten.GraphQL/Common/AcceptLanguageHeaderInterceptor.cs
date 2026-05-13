using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

public sealed class AcceptLanguageHeaderInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.TryGetValue(Constants.AcceptLanguage, out var acceptLanguage))
        {
            if (AcceptedLanguages.TryParse(acceptLanguage, out var acceptLanguages))
            {
                requestBuilder.SetGlobalState(Constants.AcceptLanguage, acceptLanguages);
            }
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
