using System.ComponentModel;
using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

public sealed class SystemLabel : AbstractLookupEntity<SystemLabel, SystemLabel.Values>
{
    public const string Prefix = "systemlabel";
    public const string PrefixWithSeparator = Prefix + ":";

    public enum Values
    {
        Default = 1,
        Bin = 2,
        Archive = 3,
        MarkedAsUnopened = 4,
    }

    public SystemLabel(Values id) : base(id) { }

    public List<DialogEndUserContext> DialogEndUserContexts { get; set; } = [];
    public override SystemLabel MapValue(Values id) => new(id);
}

public static class SystemLabelExtensions
{
    public static string ToNamespacedName(this SystemLabel.Values label) => label switch
    {
        SystemLabel.Values.Default => SystemLabel.PrefixWithSeparator + label,
        SystemLabel.Values.Bin => SystemLabel.PrefixWithSeparator + label,
        SystemLabel.Values.Archive => SystemLabel.PrefixWithSeparator + label,
        SystemLabel.Values.MarkedAsUnopened => SystemLabel.PrefixWithSeparator + label,
        _ => throw new InvalidEnumArgumentException(nameof(label), (int)label, typeof(SystemLabel.Values))
    };

    public static bool IsExclusive(this SystemLabel.Values label) => label switch
    {
        SystemLabel.Values.Default => true,
        SystemLabel.Values.Bin => true,
        SystemLabel.Values.Archive => true,
        SystemLabel.Values.MarkedAsUnopened => false,
        _ => throw new InvalidEnumArgumentException(nameof(label), (int)label, typeof(SystemLabel.Values))
    };
}
