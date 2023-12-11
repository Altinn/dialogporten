using Digdir.Domain.Dialogporten.Domain.Localizations;
using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Content;

public class DialogContentType : AbstractLookupEntity<DialogContentType, DialogContentType.Values>
{
    public DialogContentType(Values id) : base(id) { }
    public enum Values
    {
        Title = 1,
        SenderName = 2,
        Summary = 3,
        AdditionalInfo = 4,
    }

    public bool Required { get; private init; }
    public bool RenderAsHtml { get; private init; }
    public bool OutputInList { get; private init; }
    public int MaxLength { get; private init; }

    public override DialogContentType MapValue(Values id) => id switch
    {
        Values.Title => new(id)
        {
            Required = true,
            RenderAsHtml = false,
            MaxLength = 200,
            OutputInList = true
        },
        Values.SenderName => new(id)
        {
            Required = false,
            RenderAsHtml = false,
            MaxLength = 200,
            OutputInList = true
        },
        Values.Summary => new(id)
        {
            Required = true,
            RenderAsHtml = false,
            MaxLength = 200,
            OutputInList = true
        },
        Values.AdditionalInfo => new(id)
        {
            Required = false,
            RenderAsHtml = true,
            MaxLength = 1023,
            OutputInList = false
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public static readonly Values[] RequiredTypes = GetValues()
        .Where(x => x.Required)
        .Select(x => x.Id)
        .ToArray();
}
