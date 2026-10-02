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
        var code = ProblemDetailsConflictCode.ConcurrentOperationRejected;
        _messages.Add($"[{code}](#{code})");
        return this;
    }

    public ConflictMessageBuilder IdempotentKeysExist()
    {
        var code = ProblemDetailsConflictCode.IdempotentKeysExist;
        _messages.Add($"[{code}](#{code})");
        return this;
    }

    public ConflictMessageBuilder DialogIdForIdempotentKeyExists()
    {
        var code = ProblemDetailsConflictCode.DialogIdForIdempotentKeyExists;
        _messages.Add($"[{code}](#{code})");
        return this;
    }

    public string Build()
    {
        if (_messages.Count == 1) return _messages[0];
        var sb = new StringBuilder();

        if (_messages.Count > 1) sb.AppendLine("One of: ");

        sb.Append(string.Join("\n", _messages.Order().Select(m => $"- {m}")));

        return sb.ToString();
    }
}
