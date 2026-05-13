using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.Logging;
using UserType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

public sealed class DialogportenHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    private readonly ILogger<DialogportenHttpRequestInterceptor> _logger;

    public DialogportenHttpRequestInterceptor(ILogger<DialogportenHttpRequestInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        RejectUnknownAuthenticatedUserType(context);
        SetAcceptLanguageGlobalState(context, requestBuilder);

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }

    private void RejectUnknownAuthenticatedUserType(HttpContext context)
    {
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        var (userType, _) = context.User.GetUserType();
        if (userType is not UserType.Unknown)
        {
            return;
        }

        _logger.LogError(
            "The request was authenticated, but the user type could not be determined. UserType={UserType}, {DiagnosticSummary}",
            userType,
            context.User.GetDiagnosticSummary());

        throw new GraphQLException(ErrorBuilder.New()
            .SetMessage("The request was authenticated, but the user type could not be determined.")
            .SetCode("AUTH_USER_TYPE_UNKNOWN")
            .Build());
    }

    private static void SetAcceptLanguageGlobalState(HttpContext context, OperationRequestBuilder requestBuilder)
    {
        if (context.Request.Headers.TryGetValue(Constants.AcceptLanguage, out var acceptLanguage)
            && AcceptedLanguages.TryParse(acceptLanguage, out var acceptLanguages))
        {
            requestBuilder.SetGlobalState(Constants.AcceptLanguage, acceptLanguages);
        }
    }
}
