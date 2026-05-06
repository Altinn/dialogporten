using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using UserType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

public sealed class UserTypeValidationInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            var (userType, _) = context.User.GetUserType();
            if (userType is UserType.Unknown)
            {
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage("The request was authenticated, but the user type could not be determined.")
                    .SetCode("AUTH_USER_TYPE_UNKNOWN")
                    .Build());
            }
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
