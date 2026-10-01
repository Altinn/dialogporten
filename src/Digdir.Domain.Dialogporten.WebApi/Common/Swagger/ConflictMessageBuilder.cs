using System.Text;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Mappers;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public sealed class ConflictMessageBuilder
{
    private readonly List<string> _messages;

    public ConflictMessageBuilder()
    {
        _messages = new List<string>();
    }

    public ConflictMessageBuilder ConcurrentOperationRejected()
    {
        _messages.Add($"[{ProblemDetailsConflictMapper.ConcurrentOperationRejectedProblemCode}](#Conflicts)");
        return this;
    }

    public ConflictMessageBuilder IdempotentKeysExist()
    {
        _messages.Add($"[{ProblemDetailsConflictMapper.IdempotentKeysExistProblemCode}](#Conflicts)");
        return this;
    }

    public ConflictMessageBuilder DialogIdForIdempotentKeyExists()
    {
        _messages.Add($"[{ProblemDetailsConflictMapper.DialogIdForIdempotentKeyExistsProblemCode}](#Conflicts)");
        return this;
    }

    public string Build()
    {
        if (_messages.Count == 1) return _messages[0];
        var sb = new StringBuilder();

        if (_messages.Count > 1) sb.AppendLine("One of: ");

        sb.Append(string.Join("\n", _messages.Select(m => $"- {m}")));

        return sb.ToString();
    }
}
