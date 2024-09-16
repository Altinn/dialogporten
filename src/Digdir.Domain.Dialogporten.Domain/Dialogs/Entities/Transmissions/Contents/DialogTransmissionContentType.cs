using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

public sealed class DialogTransmissionContentType : AbstractLookupEntity<DialogTransmissionContentType, DialogTransmissionContentType.Values>
{
    public DialogTransmissionContentType(Values id) : base(id) { }
    public enum Values
    {
        Title = 1,
        Summary = 2
    }

    public bool Required { get; private init; }
    public int MaxLength { get; private init; }

    public string[] AllowedMediaTypes { get; private init; } = [];

    public override DialogTransmissionContentType MapValue(Values id) => id switch
    {
        Values.Title => new(id)
        {
            Required = true,
            MaxLength = Constants.DefaultMaxStringLength,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.Summary => new(id)
        {
            Required = true,
            MaxLength = Constants.DefaultMaxStringLength,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public static readonly Values[] RequiredTypes = GetValues()
        .Where(x => x.Required)
        .Select(x => x.Id)
        .ToArray();
}
