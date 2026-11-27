using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflicts;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;

internal static class TransmissionIdempotentKeyExtensions
{
    extension(IDialogDbContext db)
    {
        /// <summary>
        /// Finds idempotent keys on new transmissions that are repeated among them, or already used
        /// by a stored transmission on the dialog, without loading the dialog's transmissions.
        /// </summary>
        public async Task<Conflict?> FindIdempotentKeyConflict(
            Guid dialogId,
            IEnumerable<DialogTransmission> newTransmissions,
            CancellationToken cancellationToken)
        {
            var newIdempotentKeys = newTransmissions
                .Select(x => x.IdempotentKey)
                .OfType<string>()
                .ToList();

            if (newIdempotentKeys.Count == 0)
            {
                return null;
            }

            var existingIdempotentKeys = await db.DialogTransmissions
                .Where(x => x.DialogId == dialogId)
                .Select(x => x.IdempotentKey)
                .OfType<string>()
                .Where(x => newIdempotentKeys.Contains(x))
                .ToListAsync(cancellationToken);

            var duplicatedIdempotentKeys = newIdempotentKeys
                .Concat(existingIdempotentKeys)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicatedIdempotentKeys.Count == 0)
            {
                return null;
            }

            var conflictingKeys = string.Join(", ", duplicatedIdempotentKeys.Select(x => $"'{x}'"));
            return new Conflict(
                nameof(DialogTransmission.IdempotentKey),
                $"Duplicate IdempotentKey detected in dialog transmissions. Conflicting keys: {conflictingKeys}.",
                new IdempotentKeysExist
                {
                    IdempotentKeys = duplicatedIdempotentKeys
                }
            );
        }
    }
}
