using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;

public sealed class DialogTransmissionContentType : AbstractLookupEntity<DialogTransmissionContentType, DialogTransmissionContentType.Values>
{
    public DialogTransmissionContentType(Values id) : base(id) { }
    public enum Values
    {
        Title = 1,
        Summary = 2,
        ContentReference = 3
    }

    public bool Required { get; private init; }
    public int MaxLength { get; private init; }

    public int CorrespondenceMaxLength { get; private init; }

    public string[] AllowedMediaTypes { get; private init; } = [];

    public override DialogTransmissionContentType MapValue(Values id) => id switch
    {
        Values.Title => new(id)
        {
            Required = true,
            MaxLength = Constants.DefaultMaxStringLength,
            CorrespondenceMaxLength = Constants.CorrespondenceMaxStringLength,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.Summary => new(id)
        {
            Required = false,
            MaxLength = Constants.DefaultMaxStringLength,
            CorrespondenceMaxLength = Constants.CorrespondenceMaxStringLength,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.ContentReference => new(id)
        {
            Required = false,
            MaxLength = 1023,
            CorrespondenceMaxLength = 1023,
            AllowedMediaTypes = [MediaTypes.EmbeddableMarkdown]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public static readonly Values[] RequiredTypes = GetValues()
        .Where(x => x.Required)
        .Select(x => x.Id)
        .ToArray();
}
