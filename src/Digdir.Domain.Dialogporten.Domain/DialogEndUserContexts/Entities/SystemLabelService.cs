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

    public static void XorDefaultArchiveBinGroup(this List<SystemLabel.Values> next, SystemLabel.Values labelToAdd)
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

        // If mark as unopened, remove it
        // If "sent", ignore, do not remove
    }

    private static void AddSystemLabel(this List<SystemLabel.Values> next, SystemLabel.Values labelToAdd)
    {
        if (SystemLabel.IsDefaultArchiveBinGroup(labelToAdd))
        {
            XorDefaultArchiveBinGroup(next, labelToAdd);
        }
    }
}
