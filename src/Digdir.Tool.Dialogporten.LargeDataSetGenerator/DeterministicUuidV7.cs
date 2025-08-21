using UUIDNext;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

public static class DeterministicUuidV7
{
    public static Guid CreateUuidV7(DateTimeOffset timestamp, string tableName, int tiebreaker = 0)
    {
        var timeBasedEmpty = Guid.Empty.ToVersion7(timestamp);
        var nameBasedUuid = Uuid.NewNameBased(timeBasedEmpty, $"{tableName}{tiebreaker}");
        return nameBasedUuid.ToVersion7(timestamp);
    }

    private static Guid ToVersion7(this Guid guid, DateTimeOffset timestamp)
    {
        // Create a buffer for the UUID (16 bytes)
        Span<byte> uuidBytes = stackalloc byte[16];
        // Copy data from the input GUID into the buffer
        guid.TryWriteBytes(uuidBytes, bigEndian: true, out _);
        // Get the timestamp in milliseconds since Unix epoch
        var unixTimestampMillis = timestamp.ToUnixTimeMilliseconds();

        // Write the timestamp (48 bits) into the UUID buffer
        uuidBytes[0] = (byte)((unixTimestampMillis >> 40) & 0xFF);
        uuidBytes[1] = (byte)((unixTimestampMillis >> 32) & 0xFF);
        uuidBytes[2] = (byte)((unixTimestampMillis >> 24) & 0xFF);
        uuidBytes[3] = (byte)((unixTimestampMillis >> 16) & 0xFF);
        uuidBytes[4] = (byte)((unixTimestampMillis >> 8) & 0xFF);
        uuidBytes[5] = (byte)(unixTimestampMillis & 0xFF);

        // Set the version to 7 (4 high bits of the 7th byte)
        uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x70);

        // Set the variant to RFC 4122 (2 most significant bits of the 9th byte to 10)
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);

        // Construct and return the UUID
        return new Guid(uuidBytes, bigEndian: true);
    }
}
