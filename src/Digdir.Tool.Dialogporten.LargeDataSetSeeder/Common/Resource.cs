namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal sealed record Resource(string Identifier, string Type, string OrgCode)
{
    public bool HasDagl { get; }
    public bool HasPriv { get; }
    public string OrgCode { get; } = OrgCode.ToLowerInvariant();
    public Resource(string Identifier, string Type, string OrgCode, IEnumerable<string> subjects)
        : this(Identifier, Type, OrgCode)
    {
        using var e = subjects.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current == "urn:altinn:rolecode:dagl") HasDagl = true;
            if (e.Current == "urn:altinn:rolecode:priv") HasPriv = true;
        }
    }
}
