using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.AspNetCore.Http.Extensions;

namespace Digdir.Domain.Dialogporten.GraphQL;

public sealed class OpenTelemetryEventListener : ExecutionDiagnosticEventListener
{
    private static readonly ActivitySource ActivitySource = new("Dialogporten.GraphQL");

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var httpContext = GetHttpContextFrom(context);
        if (httpContext == null)
            return EmptyScope;

#if DEBUG
        if (context.Request.OperationName == "IntrospectionQuery")
            return EmptyScope;
#endif

        var operationName = context.Request.OperationName ?? "UnknownOperation";
        var operationPath = $"{operationName} - {context.Request.}";

        var activity = ActivitySource.StartActivity($"GraphQL {operationPath}", ActivityKind.Server);

        if (activity == null)
            return EmptyScope;

        activity.SetTag("graphql.operation_name", operationName);
        activity.SetTag("graphql.query_hash", context.Request.QueryHash);
        activity.SetTag("http.url", httpContext.Request.GetDisplayUrl());
        activity.SetTag("user.id", httpContext.User.Identity?.Name ?? "Not authenticated");
        activity.SetTag("http.method", httpContext.Request.Method);
        activity.SetTag("http.route", httpContext.Request.Path);

        return new ScopeWithEndAction(() => OnEndRequest(context, activity));
    }

    public override void RequestError(IRequestContext context, Exception exception)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            currentActivity.AddException(exception);
            currentActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
        base.RequestError(context, exception);
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            var currentActivity = Activity.Current;
            currentActivity?.AddEvent(new ActivityEvent("ValidationError", default, new ActivityTagsCollection
                {
                    { "message", error.Message }
                }));
        }

        base.ValidationErrors(context, errors);
    }

    private static HttpContext? GetHttpContextFrom(IRequestContext context) =>
        context.ContextData.TryGetValue("HttpContext", out var value) ? value as HttpContext : null;

    private static void OnEndRequest(IRequestContext context, Activity activity)
    {
        var httpContext = GetHttpContextFrom(context);
        if (context.Exception != null)
        {
            activity.AddException(context.Exception);
            activity.SetStatus(ActivityStatusCode.Error, context.Exception.Message);
        }

        if (context.Result is QueryResult { Errors: not null } queryResult)
        {
            foreach (var error in queryResult.Errors)
            {
                if (error.Exception is null)
                {
                    continue;
                }

                activity.AddException(error.Exception);
                activity.SetStatus(ActivityStatusCode.Error, error.Exception.Message);
            }
        }

        if (httpContext != null)
        {
            activity.SetTag("http.status_code", httpContext.Response.StatusCode);
        }

        activity.Dispose();
    }
}

internal sealed class ScopeWithEndAction : IDisposable
{
    private readonly Action _disposeAction;

    public ScopeWithEndAction(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose() => _disposeAction.Invoke();
}
