using DeterministicGuids;

namespace Digdir.Domain.Dialogporten.Domain.Common;

public static class GuidExtensions
{
    /// <param name="parentV7Id"></param>
    extension(Guid parentV7Id)
    {
        /// <summary>
        /// Creates a deterministic UUID v7 by first creating a UUID v5 based
        /// on the parent UUID (namespace) and name, then converting that to
        /// a UUID v7 by copying the UUID v7 parts from the parent UUID.
        /// </summary>
        /// <remarks>
        /// Assumes that parentV7Id is on UUID v7 format.
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public Guid CreateDeterministicSubUuidV7(string name)
            => DeterministicGuid.Create(parentV7Id, name).CopyUuidV7PartsFrom(parentV7Id);

        private Guid CopyUuidV7PartsFrom(Guid source)
        {
            // Create buffers for the source and target GUIDs (16 bytes each)
            Span<byte> sourceBytes = stackalloc byte[16];
            Span<byte> targetBytes = stackalloc byte[16];

            // Copy data from the source and target GUIDs into the buffers
            source.TryWriteBytes(sourceBytes, bigEndian: true, out _);
            parentV7Id.TryWriteBytes(targetBytes, bigEndian: true, out _);

            // Copy the first 48 bits (6 bytes) from the source to the target (timestamp)
            sourceBytes[..6].CopyTo(targetBytes[..6]);

            // Copy only the four most significant bits of the 7th byte (version) from the source to the target
            targetBytes[6] = (byte)((targetBytes[6] & 0x0F) | (sourceBytes[6] & 0xF0));

            // Copy only the two most significant bits of the 9th byte (variant) from the source to the target
            targetBytes[8] = (byte)((targetBytes[8] & 0x3F) | (sourceBytes[8] & 0xC0));

            // Construct and return the new target GUID
            return new Guid(targetBytes, bigEndian: true);
        }
    }

}
