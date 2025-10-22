using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
public class FormattableStringBuilder
{
    private const string Null = "!!__null__!!";
    private readonly StringBuilder _format = new();
    private readonly Dictionary<object, int> _argNumberByArg = [];
    private readonly Dictionary<object, string> _consolidatedFormatByFormattableSource = [];

    public FormattableString ToFormattableString() =>
        FormattableStringFactory.Create(_format.ToString(), _argNumberByArg
            .OrderBy(x => x.Value)
            .Select(x => Equals(x.Key, Null) ? null : x.Key)
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
            if (value is FormattableString or FormattableStringBuilder)
            {
                AppendFormattableSource(value);
                return;
            }

            _builder._format.Append('{').Append(GetArgumentNumber(value));
            if (alignment != 0) _builder._format.Append(',').Append(alignment);
            if (format is not null) _builder._format.Append(':').Append(format);
            _builder._format.Append('}');
        }

        private void AppendFormattableSource(object value)
        {
            if (!_builder._consolidatedFormatByFormattableSource.TryGetValue(value, out var consolidatedFormat))
            {
                _builder._consolidatedFormatByFormattableSource[value] = consolidatedFormat = value switch
                {
                    FormattableString fs => ConsolidateFormat(
                        srcFormat: fs.Format,
                        srcArgs: fs.GetArguments().Select((x, i) => (x ?? Null, i))),
                    FormattableStringBuilder fsBuilder => ConsolidateFormat(
                        srcFormat: fsBuilder._format.ToString(),
                        srcArgs: fsBuilder._argNumberByArg.Select(x => (x.Key, x.Value))),
                    _ => throw new InvalidOperationException("Unsupported formattable source.")
                };
            }

            _builder._format.Append(consolidatedFormat);
        }

        private string ConsolidateFormat(string srcFormat, IEnumerable<(object, int)> srcArgs)
        {
            foreach (var (srcValue, srcArgNumber) in srcArgs)
            {
                var argNumber = GetArgumentNumber(srcValue);
                if (srcArgNumber == argNumber) continue;
                srcFormat = srcFormat.Replace("{" + srcArgNumber, "{" + argNumber);
            }
            return srcFormat;
        }

        private int GetArgumentNumber(object? value) =>
            !_builder._argNumberByArg.TryGetValue(value ?? Null, out var argumentNumber)
                ? _builder._argNumberByArg[value ?? Null] = _builder._argNumberByArg.Count
                : argumentNumber;
    }
}

public class PostgresFormattableStringBuilder : FormattableStringBuilder
{
    public new PostgresFormattableStringBuilder Append(
        [InterpolatedStringHandlerArgument(""), StringSyntax("PostgreSQL")]
        ref FormattableStringHandler handler) => (PostgresFormattableStringBuilder)base.Append(ref handler);

    public new PostgresFormattableStringBuilder AppendIf(
        bool condition,
        [InterpolatedStringHandlerArgument("", "condition"), StringSyntax("PostgreSQL")]
        ref FormattableStringHandler handler) => (PostgresFormattableStringBuilder)base.AppendIf(condition, ref handler);

    public new PostgresFormattableStringBuilder Append([StringSyntax("PostgreSQL")] string value) =>
        (PostgresFormattableStringBuilder)base.Append(value);

    public new PostgresFormattableStringBuilder AppendIf(bool condition, [StringSyntax("PostgreSQL")] string value) =>
        (PostgresFormattableStringBuilder)base.AppendIf(condition, value);
}
