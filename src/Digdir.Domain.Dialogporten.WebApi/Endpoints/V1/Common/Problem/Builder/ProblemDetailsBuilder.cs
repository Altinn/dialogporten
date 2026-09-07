using System.Diagnostics;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Builder;

public interface IProblemDetailsBuilder
{
    IProblemDetailsBuilder ForContext(HttpContext context);
    IDialogportenProblemDetails Build();
}

public sealed class ProblemDetailsBuilder<TProblemDetails> : IProblemDetailsBuilder
    where TProblemDetails : class, IDialogportenProblemDetails
{
    private readonly TProblemDetails _problem;

    public ProblemDetailsBuilder(TProblemDetails problem)
    {
        _problem = problem;
    }

    IProblemDetailsBuilder IProblemDetailsBuilder.ForContext(HttpContext context) => ForContext(context);

    public ProblemDetailsBuilder<TProblemDetails> ForContext(HttpContext context)
    {
        _problem.Instance = context.Request.Path;
        _problem.TraceId = Activity.Current?.Id ?? context.TraceIdentifier;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithTitle(string title)
    {
        _problem.Title = title;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithStatusCode(int statusCode)
    {
        _problem.Status = statusCode;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithDetail(string detail)
    {
        _problem.Detail = detail;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithType(string type)
    {
        _problem.Type = type;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithErrors(Dictionary<string, string[]> errors)
    {
        _problem.Errors = errors;
        return this;
    }

    public ProblemDetailsBuilder<TProblemDetails> WithErrors(List<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray());
        return WithErrors(errors);
    }

    public ProblemDetailsBuilder<TProblemDetails> Modify(Action<TProblemDetails> modify)
    {
        modify.Invoke(_problem);
        return this;
    }

    public IDialogportenProblemDetails Build()
    {
        return _problem;
    }
}
