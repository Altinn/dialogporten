using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
internal partial class FormattableStringBuilder
{
    // Matches a literal "{{" escape (group 1 empty) or a placeholder index "{<digits>" (group 1 = digits).
    // The "{{" branch is matched so an escaped brace followed by digits (e.g. "{{1}}") isn't renumbered as
    // a placeholder. The trailing part of a placeholder (alignment/format, e.g. ",5:yyyy}") is left untouched.
    [GeneratedRegex(@"\{\{|\{(\d+)")]
    private static partial Regex PlaceholderRegex();

    // Renumber every placeholder in one pass so multi-digit indices (e.g. "{10}") are rewritten
    // atomically rather than through a prefix-matching Replace that would also corrupt them.
    private static string RenumberPlaceholders(string srcFormat, Dictionary<int, int> argNumberBySrcArgNumber) =>
        PlaceholderRegex().Replace(srcFormat, match =>
            match.Groups[1].Success
                ? "{" + argNumberBySrcArgNumber[int.Parse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture)]
                    .ToString(CultureInfo.InvariantCulture)
                : match.Value);

    private const string Null = "!!__null__!!";
    private readonly StringBuilder _format = new();
    private readonly Dictionary<object, int> _argNumberByArg = [];
    private readonly Dictionary<object, string> _consolidatedFormatByFormattableSource = [];

    public FormattableString ToFormattableString() =>
        FormattableStringFactory.Create(_format.ToString(), _argNumberByArg
            .OrderBy(x => x.Value)
            .Select(x => Equals(x.Key, Null) ? GetNullValue() : x.Key)
            .ToArray());

    public (string Sql, DynamicParameters Parameters) ToDynamicParameters()
    {
        var parameters = _argNumberByArg
            .OrderBy(x => x.Value)
            .Aggregate(new DynamicParameters(), (parameters, pair) =>
            {
                parameters.Add($"p{pair.Value}", Equals(pair.Key, Null) ? GetNullValue() : pair.Key);
                return parameters;
            });
        return (Sql: _format.ToString().Replace("{", "@p").Replace("}", ""), Parameters: parameters);
    }

    protected virtual object? GetNullValue() => null;
    protected virtual object? SomethingValue(object? value) => value;

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
    internal readonly ref struct FormattableStringHandler
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
            // Build the src -> consolidated argument-number map in a single pass. GetArgumentNumber has
            // side effects (registering args in insertion order), so srcArgs must be enumerated in order.
            var argNumberBySrcArgNumber = new Dictionary<int, int>();
            foreach (var (srcValue, srcArgNumber) in srcArgs)
            {
                argNumberBySrcArgNumber[srcArgNumber] = GetArgumentNumber(srcValue);
            }

            return RenumberPlaceholders(srcFormat, argNumberBySrcArgNumber);
        }

        private int GetArgumentNumber(object? value)
        {
            value = _builder.SomethingValue(value);
            return !_builder._argNumberByArg.TryGetValue(value ?? Null, out var argumentNumber)
                ? _builder._argNumberByArg[value ?? Null] = _builder._argNumberByArg.Count
                : argumentNumber;
        }
    }
}

internal sealed class PostgresFormattableStringBuilder : FormattableStringBuilder
{
    private static readonly NpgsqlParameter NullValue = new NpgsqlParameter<string?>("__null__", null);
    protected override object GetNullValue() => NullValue;
    protected override object? SomethingValue(object? value) => value switch
    {
        DateTime dateTime => (DateTimeOffset)dateTime.ToUniversalTime(),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime(),
        _ => base.SomethingValue(value)
    };

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
