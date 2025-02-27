using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

public sealed class DialogContentType : AbstractLookupEntity<DialogContentType, DialogContentType.Values>
{
    public DialogContentType(Values id) : base(id) { }
    public enum Values
    {
        Title = 1,
        SenderName = 2,
        Summary = 3,
        AdditionalInfo = 4,
        ExtendedStatus = 5,
        MainContentReference = 6,
        NonSensitiveTitle = 7,
        NonSensitiveSummary = 8,
    }

    public bool Required { get; private init; }
    public bool OutputInList { get; private init; }

    public int MaxLength { get; private init; }

    public string[] AllowedMediaTypes { get; private init; } = [];

    public override DialogContentType MapValue(Values id) => id switch
    {
        Values.Title => new(id)
        {
            Required = true,
            MaxLength = Constants.DefaultMaxStringLength,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.SenderName => new(id)
        {
            Required = false,
            MaxLength = Constants.DefaultMaxStringLength,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.Summary => new(id)
        {
            Required = true,
            MaxLength = Constants.DefaultMaxStringLength,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.AdditionalInfo => new(id)
        {
            Required = false,
            MaxLength = 1023,
            OutputInList = false,
            AllowedMediaTypes = [MediaTypes.PlainText, MediaTypes.Markdown]
        },
        Values.ExtendedStatus => new(id)
        {
            Required = false,
            MaxLength = 20,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.MainContentReference => new(id)
        {
            Required = false,
            MaxLength = 1023,
            OutputInList = false,
            AllowedMediaTypes = [MediaTypes.EmbeddableMarkdown]
        },
        Values.NonSensitiveTitle => new(id)
        {
            Required = false,
            MaxLength = Constants.DefaultMaxStringLength,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        Values.NonSensitiveSummary => new(id)
        {
            Required = false,
            MaxLength = Constants.DefaultMaxStringLength,
            OutputInList = true,
            AllowedMediaTypes = [MediaTypes.PlainText]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public static readonly Values[] RequiredTypes = GetValues()
        .Where(x => x.Required)
        .Select(x => x.Id)
        .ToArray();
}
