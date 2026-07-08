# .NET SDK for Dialogporten ServiceOwner API

Simple overview
TODO

## Mapping between dialog models

The API uses three parallel dialog model families: `Dialog` (the GET response), `CreateDialog` (the POST
body) and `UpdateDialog` (the PUT body). The `Features.V1.Mapping` namespace provides extension methods to
convert between them for read-modify-write and clone flows:

```csharp
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

// Fetch a dialog, mutate it and PUT it back:
var dialog = (await client.GetDialog(dialogId)).Content!;
var update = dialog.ToUpdateDialog();
update.Progress = 100;
update.Status = DialogStatusInput.Completed;
await client.UpdateDialog(dialogId, update);

// Clone an existing dialog into a new one (Id/IdempotentKey are dropped by default):
var clone = dialog.ToCreateDialog();

// Pass preserveId: true to carry over Id/IdempotentKey for an idempotent re-create:
var idempotent = dialog.ToCreateDialog(preserveId: true);

// Reuse a create payload as an update, or vice versa:
UpdateDialog asUpdate = createDialog.ToUpdateDialog();
CreateDialog asCreate = updateDialog.ToCreateDialog(); // remember to set ServiceResource and Party
```

These conversions are intentionally lossy: fields that have no target on the destination model are dropped
(for example, identity, party and visibility fields are not part of an `UpdateDialog`), and read-only server
fields on `Dialog` (revision, counts, contexts, seen-log) cannot be recovered after a round-trip. The output
status enum `DialogStatus` and the input enum `DialogStatusInput` are mapped by name; the input-only values
`New` and `Sent` map to `NotApplicable` and `Awaiting` respectively via
`DialogStatusMapping.ToDialogStatus`.
