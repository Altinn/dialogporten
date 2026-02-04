using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Json;

internal sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const int OffsetLength = 6;
    private const string OffsetRequiredErrorMessage =
        "The JSON value could not be converted to System.DateTimeOffset. " +
        "Expected explicit offset postfix. For example '+00:00', '-05:00', 'Z'.";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTimeOffset = reader.GetDateTimeOffset();
        // Validate that the last 6 bytes includes a valid offset
        return !HasValidOffset(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan)
            ? throw new JsonException($"{OffsetRequiredErrorMessage} Got '{reader.GetString()}'.")
            : dateTimeOffset;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var utf8Date = new byte[33];
        Utf8Formatter.TryFormat(value.UtcDateTime, utf8Date, out var bytesWritten, new StandardFormat('O'));
        Array.Resize(ref utf8Date, bytesWritten);
        writer.WriteStringValue(utf8Date);
    }

    private static bool HasValidOffset(ReadOnlySpan<byte> dateTimeOffsetSpan)
    {
        if (dateTimeOffsetSpan.Length < OffsetLength)
        {
            return false;
        }

        var offsetSpan = dateTimeOffsetSpan[^OffsetLength..];

        if (IsZ(offsetSpan[^1]))
        {
            return true;
        }

        for (var i = 0; i < offsetSpan.Length; i++)
        {
            var value = offsetSpan[i];
            var isValid = i switch
            {
                0 => IsPlusOrMinus(value),
                1 => IsNumber(value),
                2 => IsNumber(value),
                3 => IsColon(value),
                4 => IsNumber(value),
                5 => IsNumber(value),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!isValid)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumber(byte value) => value is >= 48 and <= 57;
    private static bool IsZ(byte value) => value is 90;
    private static bool IsPlusOrMinus(byte value) => value is 43 or 45;
    private static bool IsColon(byte value) => value is 58;
}
