namespace Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

internal static class SystemLabelService
{
    extension(List<SystemLabel.Values> next)
    {
        public List<SystemLabel.Values> RemoveSystemLabels(IEnumerable<SystemLabel.Values> labelsToRemove)
        {
            foreach (var labelToRemove in labelsToRemove.Distinct())
            {
                next.RemoveSystemLabel(labelToRemove);
            }

            next.EnsureRequiredLabels();
            return next;
        }

        public List<SystemLabel.Values> AddSystemLabels(IEnumerable<SystemLabel.Values> labelsToAdd)
        {
            foreach (var labelToAdd in labelsToAdd.Distinct())
            {
                next.AddSystemLabel(labelToAdd);
            }

            next.EnsureRequiredLabels();
            return next;
        }

        private void XorDefaultArchiveBinGroup(SystemLabel.Values labelToAdd)
        {
            next.RemoveAll(SystemLabel.IsDefaultArchiveBinGroup);
            next.Add(labelToAdd);
        }

        private void RemoveSystemLabel(SystemLabel.Values labelToRemove)
        {
            if (SystemLabel.IsDefaultArchiveBinGroup(labelToRemove) && next.Contains(labelToRemove))
            {
                next.XorDefaultArchiveBinGroup(SystemLabel.Values.Default);
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

        private void AddSystemLabel(SystemLabel.Values labelToAdd)
        {
            if (SystemLabel.IsDefaultArchiveBinGroup(labelToAdd))
            {
                next.XorDefaultArchiveBinGroup(labelToAdd);
                return;
            }

            if (labelToAdd == SystemLabel.Values.MarkedAsUnopened && !next.Contains(labelToAdd))
            {
                next.Add(labelToAdd);
                return;
            }

            if (labelToAdd == SystemLabel.Values.Sent && !next.Contains(labelToAdd))
            {
                next.Add(labelToAdd);
            }
        }

        private void EnsureRequiredLabels()
        {
            if (!next.Any(SystemLabel.IsDefaultArchiveBinGroup))
            {
                next.Add(SystemLabel.Values.Default);
            }
        }
    }
}
