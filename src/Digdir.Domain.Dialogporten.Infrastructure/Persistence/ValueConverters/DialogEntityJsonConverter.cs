using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.ValueConverters;

internal sealed class DialogEntityJsonConverter : ValueConverter<DialogEntity, string>
{
    public DialogEntityJsonConverter()
        : base(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<DialogEntity>(v, (JsonSerializerOptions?)null)!)
    {
    }
}
