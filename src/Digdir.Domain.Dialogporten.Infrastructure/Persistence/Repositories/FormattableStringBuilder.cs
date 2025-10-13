using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
public class FormattableStringBuilder
{
    private readonly StringBuilder _format = new();
    private readonly Dictionary<object, int> _argNumberByArg = [];

    public FormattableString ToFormattableString() =>
        FormattableStringFactory.Create(_format.ToString(), _argNumberByArg
            .OrderBy(x => x.Value)
            .Select(x => x.Key)
            .ToArray());

    public FormattableStringBuilder Append(string value)
    {
        _format.Append(value.Replace("{", "{{").Replace("}", "}}"));
        return this;
    }

    public FormattableStringBuilder AppendIf(bool condition, string value)
    {
        if (condition)
        {
            _format.Append(value.Replace("{", "{{").Replace("}", "}}"));
        }

        return this;
    }

    public FormattableStringBuilder Append(
        [InterpolatedStringHandlerArgument("")]
        ref FormattableStringHandler handler) => this;

    public FormattableStringBuilder AppendIf(
        bool condition,
        [InterpolatedStringHandlerArgument("", "condition")]
        ref FormattableStringHandler handler) => this;

    [InterpolatedStringHandler]
    public readonly ref struct FormattableStringHandler
    {
        private readonly FormattableStringBuilder _builder;

        public FormattableStringHandler(int literalLength, int formattedCount, FormattableStringBuilder builder)
        {
            _builder = builder;
        }

        public FormattableStringHandler(int literalLength, int formattedCount, FormattableStringBuilder builder, bool condition, out bool shouldAppend)
        {
            shouldAppend = condition;
            _builder = builder;
        }

        public void AppendLiteral(string value) => _builder._format.Append(value.Replace("{", "{{").Replace("}", "}}"));

        public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        {
            const string @null = "null";

            if (!_builder._argNumberByArg.TryGetValue(value ?? @null, out var argNumber))
            {
                argNumber = _builder._argNumberByArg[value ?? @null] = _builder._argNumberByArg.Count;
            }

            _builder._format.Append('{').Append(argNumber);
            if (alignment != 0) _builder._format.Append(',').Append(alignment);
            if (format is not null) _builder._format.Append(':').Append(format);
            _builder._format.Append('}');
        }
    }
}

public class PostgresFormattableStringBuilder : FormattableStringBuilder
{
    public new FormattableStringBuilder Append(
        [InterpolatedStringHandlerArgument(""), StringSyntax("PostgreSQL")]
        ref FormattableStringHandler handler) => base.Append(ref handler);

    public new FormattableStringBuilder AppendIf(
        bool condition,
        [InterpolatedStringHandlerArgument("", "condition"), StringSyntax("PostgreSQL")]
        ref FormattableStringHandler handler) => base.AppendIf(condition, ref handler);
}
