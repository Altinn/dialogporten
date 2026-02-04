using Medo;

namespace Digdir.Library.Entity.Abstractions.Features.Identifiable;

/// <summary>
/// Provides extension methods for <see cref="IIdentifiableEntity"/>.
/// </summary>
public static class IdentifiableExtensions
{
    /// <summary>
    /// Populates <see cref="IIdentifiableEntity.Id"/> with a new guid for each entity if the id is <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="entities">The collection of <see cref="IIdentifiableEntity"/> to update.</param>
    public static void EnsureIds(this IEnumerable<IIdentifiableEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.EnsureId();
        }
    }

    /// <summary>
    /// Populates <see cref="IIdentifiableEntity.Id"/> with a new guid if the id is <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="identifiable">The <see cref="IIdentifiableEntity"/> to update.</param>
    public static Guid EnsureId(this IIdentifiableEntity identifiable)
        => identifiable.Id = CreateVersion7IfDefault(identifiable.Id);

    /// <summary>
    /// Creates a new version 7 UUID if the value is null or <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Guid CreateVersion7IfDefault(this Guid? value) =>
        value?.CreateVersion7IfDefault() ?? CreateVersion7();

    /// <summary>
    /// Creates a new version 7 UUID if the value is <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to check and potentially update.</param>
    /// <returns>A new version 7 UUID if the value is <see cref="Guid.Empty"/>, otherwise the original value.</returns>
    public static Guid CreateVersion7IfDefault(this Guid value) =>
        value == default ? CreateVersion7() : value;

    /// <summary>
    /// Creates a new version 7 UUID.
    ///
    /// </summary>
    /// <returns>A new version 7 UUID in big endian format.</returns>
    // We want Guids in big endian text representation.
    // The default behavior of Medo is bigEndian text representation,
    // Setting bigEndian to false for two reasons:
    // 1. Use the parameter name explicitly in case the Medo API changes
    // 2. Make it clear that we want big endian text representation, which means little endian byte order
    public static Guid CreateVersion7(DateTimeOffset? timeStamp = null) => timeStamp is null
        ? Uuid7.NewUuid7().ToGuid(bigEndian: false)
        : Uuid7.NewUuid7(timeStamp.Value).ToGuid(bigEndian: false);
}
