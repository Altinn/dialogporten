namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

internal static class SystemLabelService
{
    public static List<SystemLabel.Values> RemoveSystemLabels(this List<SystemLabel.Values> next,
        IEnumerable<SystemLabel.Values> labelsToRemove)
    {
        foreach (var labelToRemove in labelsToRemove.Distinct())
        {
            next.RemoveSystemLabel(labelToRemove);
        }

        return next;
    }

    public static List<SystemLabel.Values> AddSystemLabels(this List<SystemLabel.Values> next,
        IEnumerable<SystemLabel.Values> labelsToAdd)
    {
        foreach (var labelToAdd in labelsToAdd.Distinct())
        {
            next.AddSystemLabel(labelToAdd);
        }

        return next;
    }

    private static void XorDefaultArchiveBinGroup(this List<SystemLabel.Values> next, SystemLabel.Values labelToAdd)
    {
        next.RemoveAll(SystemLabel.IsDefaultArchiveBinGroup);
        next.Add(labelToAdd);
    }

    private static void RemoveSystemLabel(this List<SystemLabel.Values> next, SystemLabel.Values labelToRemove)
    {
        if (SystemLabel.IsDefaultArchiveBinGroup(labelToRemove) && next.Contains(labelToRemove))
        {
            XorDefaultArchiveBinGroup(next, SystemLabel.Values.Default);
        }

        if (labelToRemove == SystemLabel.Values.MarkedAsUnopened && next.Contains(labelToRemove))
        {
            next.Remove(labelToRemove);
        }

        if (labelToRemove == SystemLabel.Values.Sent)
        {
            // This should have been caught in the validation layer
            throw new InvalidOperationException("Cannot remove 'Sent' system label.");
        }
    }

    private static void AddSystemLabel(this List<SystemLabel.Values> next, SystemLabel.Values labelToAdd)
    {
        if (SystemLabel.IsDefaultArchiveBinGroup(labelToAdd))
        {
            XorDefaultArchiveBinGroup(next, labelToAdd);
            return;
        }

        if (labelToAdd == SystemLabel.Values.MarkedAsUnopened && !next.Contains(labelToAdd))
        {
            next.Add(labelToAdd);
        }

        if (labelToAdd == SystemLabel.Values.Sent && !next.Contains(labelToAdd))
        {
            next.Add(labelToAdd);
        }
    }
}
