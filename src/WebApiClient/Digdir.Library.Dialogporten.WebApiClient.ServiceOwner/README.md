# Altinn.ApiClients.Dialogporten.ServiceOwner

.NET SDK for the [Dialogporten](https://github.com/altinn/dialogporten) ServiceOwner API. Provides a typed HTTP client for creating and managing dialogs on behalf of a service owner, backed by Maskinporten authentication and automatic EdDSA key caching for dialog token validation.

Sample projects are available at https://github.com/Altinn/dialogporten-samples.

## Installation

```
dotnet add package Altinn.ApiClients.Dialogporten.ServiceOwner
```

Targets net8.0, net9.0, and net10.0.

## Setup

Register the client in your DI container using `AddDialogportenClient`. The `BaseUri` must point to the Dialogporten root, excluding `/api/v...`.

| Environment | BaseUri |
|---|---|
| Production | `https://platform.altinn.no/dialogporten` |
| TT02 | `https://platform.tt02.altinn.no/dialogporten` |

### Built-in Maskinporten authentication

Provide Maskinporten settings and the SDK handles token acquisition automatically. The primary scope is `digdir:dialogporten.serviceprovider`; list/search operations also require `digdir:dialogporten.serviceprovider.search`.

```csharp
builder.Services.AddDialogportenClient(options =>
{
    options.BaseUri = "https://platform.altinn.no/dialogporten";
    options.Maskinporten = new MaskinportenSettings
    {
        Authority = "https://maskinporten.no/",
        ClientId = "your-client-id",
        Scope = "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search",
        // Supply either EncodedJwk or a certificate
        EncodedJwk = "..."
    };
});
```

### Custom authentication

If you manage Maskinporten tokens yourself (e.g. via a shared client definition), use the overload that accepts an `IHttpClientBuilder` delegate:

```csharp
builder.Services.AddDialogportenClient(
    options => options.BaseUri = "https://platform.altinn.no/dialogporten",
    httpClientBuilder => httpClientBuilder.AddMaskinportenHttpMessageHandler<MyClientDefinition>("my-key")
);
```

## Using the client

Inject `IServiceOwnerApi` and call methods on the `V1` property:

```csharp
public class MyService(IServiceOwnerApi dialogporten)
{
    public async Task<string> CreateDialogAsync(CreateDialog dto, CancellationToken ct)
    {
        var response = await dialogporten.V1.CreateDialog(dto, ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed: {response.StatusCode}");

        // response.Content contains the new dialog ID
        return response.Content!;
    }
}
```

All methods return `IApiResponse<T>` (or `IApiResponse` for void responses) from [Refit](https://github.com/reactiveui/refit), giving you access to the status code, headers, and deserialized content.

## Available operations

### Dialogs (CRUD)

| Method | Description |
|---|---|
| `SearchDialogs(queryParams, ct)` | Paginated list of dialogs owned by the authenticated service owner. Use `continuationToken` from the response to page through results. |
| `CreateDialog(dto, ct)` | Create a new dialog. Returns the new dialog ID. |
| `GetDialog(dialogId, endUserId?, ct)` | Single dialog aggregate. Can return deleted dialogs (check `DeletedAt`). |
| `UpdateDialog(dialogId, dto, ifMatch, ct)` | Full replace of a dialog. Supply `Revision` as `ifMatch` for optimistic concurrency. |
| `PatchDialog(dialogId, patchDocument, etag, ct)` | Partial update via [RFC 6902 JSON Patch](https://tools.ietf.org/html/rfc6902). |
| `DeleteDialog(dialogId, ifMatch, ct)` | Soft-delete a dialog. End users get 410 Gone; service owner can still read via `GetDialog`. |
| `RestoreDialog(dialogId, ifMatch, ct)` | Restore a soft-deleted dialog. |
| `PurgeDialog(dialogId, ifMatch, ct)` | Permanently delete a dialog (hard delete). |
| `FreezeDialog(dialogId, ifMatch, ct)` | Freeze a dialog to prevent further modification (admin scope required to unfreeze). |
| `GetDialogLookup(instanceRef, acceptLanguage, ct)` | Resolve dialog metadata by an external instance reference. |

**Search filter highlights** (`SearchDialogsQueryParams`):
- `ServiceResource`, `Party`, `EndUserId` — filter by resource, receiving party, or a specific end user
- `Status` — `New`, `InProgress`, `Waiting`, `Signing`, `Cancelled`, `Completed`
- `Deleted` — `Include`, `Exclude` (default), or `Only`
- `SystemLabel`, `ServiceOwnerLabels` — filter by system or custom service-owner labels (prefix matching with `*` supported)
- `CreatedAfter/Before`, `UpdatedAfter/Before`, `ContentUpdatedAfter/Before`, `DueAfter/Before`, `VisibleAfter/Before`
- `IsContentSeen` — filter by seen/unseen content
- `Search` / `SearchLanguageCode` — free-text fuzzy search
- `ContinuationToken`, `Limit` (1–1000, default 100)

Dates must include an explicit time zone, e.g. `2024-01-15T10:00:00Z`.

### Transmissions

| Method | Description |
|---|---|
| `SearchDialogTransmissions(dialogId, ct)` | All transmissions for a dialog. |
| `CreateDialogTransmission(dialogId, dto, ifMatch, ct)` | Add a transmission. Returns the new transmission ID. |
| `GetDialogTransmission(dialogId, transmissionId, ct)` | Single transmission. |
| `UpdateDialogTransmission(dialogId, transmissionId, dto, ifMatch, ct)` | Full replace of a transmission. |

### Activities

| Method | Description |
|---|---|
| `SearchDialogActivities(dialogId, ct)` | All activities for a dialog. |
| `CreateDialogActivity(dialogId, dto, ifMatch, ct)` | Add an activity to a dialog's history. Returns the new activity ID. |
| `GetDialogActivity(dialogId, activityId, ct)` | Single activity. |

### Seen log

| Method | Description |
|---|---|
| `SearchDialogSeenLogs(dialogId, ct)` | All seen-log records for a dialog. |
| `GetDialogSeenLog(dialogId, seenLogId, ct)` | Single seen-log record. |

### Service owner labels

| Method | Description |
|---|---|
| `GetServiceOwnerLabels(dialogId, ct)` | All service-owner labels for a dialog. |
| `CreateServiceOwnerLabel(dialogId, dto, ifMatch, ct)` | Add a label. Supply `Revision` as `ifMatch`. |
| `DeleteServiceOwnerLabel(dialogId, label, ifMatch, ct)` | Remove a label. |

### System labels (end user context)

| Method | Description |
|---|---|
| `SetDialogSystemLabels(dialogId, request, enduserId?, ifMatch, ct)` | Set system label(s) on a single dialog for a given end user. |
| `BulkSetDialogSystemLabels(dto, enduserId?, ct)` | Set system labels on multiple dialogs in one call. |
| `SearchDialogEndUserContexts(queryParams, ct)` | Paginated list of dialog end-user context labels for given parties. |

### Other

| Method | Description |
|---|---|
| `CheckNotificationCondition(dialogId, queryParams, ct)` | Check whether a notification condition is met (used by Altinn Notification). |

## Optimistic concurrency

Write operations accept an optional `ifMatch` parameter (maps to `If-Match` header). Pass the `Revision` GUID from a prior `GetDialog` response to prevent overwriting concurrent changes. A mismatched revision returns `412 Precondition Failed`.

## Dialog token validation

Dialogporten issues short-lived EdDSA-signed dialog tokens when an end user accesses a dialog. Your backend can validate these tokens using the injected `IDialogTokenValidator`:

```csharp
public class MyController(IDialogTokenValidator validator)
{
    [HttpGet("resource/{dialogId}")]
    public IActionResult Get(Guid dialogId, [FromHeader] string dialogToken)
    {
        var result = validator.Validate(dialogToken, dialogId: dialogId, requiredActions: ["read"]);

        if (!result.IsValid)
            return Forbid();

        // result.ClaimsPrincipal is non-null here
        return Ok();
    }
}
```

The validator caches public keys fetched from the Dialogporten `.well-known` endpoint (via a background hosted service). By default it throws on startup if keys cannot be fetched; set `ThrowOnPublicKeyFetchInit = false` in `DialogportenSettings` to make startup tolerant of transient failures.

**`DialogTokenValidationParameters`** lets you override defaults globally or per-call:

```csharp
// Per-call override with extra clock skew
var result = validator.Validate(token, options: new DialogTokenValidationParameters
{
    ClockSkew = TimeSpan.FromSeconds(30)
});
```

### Authorization contexts

An entity with an `authorizationContext` (a transmission, attachment, action or navigational action) can be granted through a party or resource other than the dialog's own, so its grant is not expressed by the actions claim. Instead the dialog token's `e` claim lists, for every such entity the end user is authorized for, the entity's id or the `tokenRef` the service owner supplied on the context. For a request scoped to such an entity, pass the reference and the validation fails unless it is listed:

```csharp
var result = validator.Validate(dialogToken, dialogId: dialogId, requiredEntityReference: transmissionId.ToString());
// or, when the context was created with a tokenRef:
var result = validator.Validate(dialogToken, dialogId: dialogId, requiredEntityReference: "my-own-reference");

// The listed references are also available directly:
var authorizedEntities = result.ClaimsPrincipal?.GetAuthorizedEntityReferences();
```

## Settings reference

```json
{
  "Dialogporten": {
    "BaseUri": "https://platform.altinn.no/dialogporten",
    "ThrowOnPublicKeyFetchInit": true,
    "Maskinporten": {
      "Authority": "https://maskinporten.no/",
      "ClientId": "your-client-id",
      "Scope": "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search",
      "EncodedJwk": "..."
    }
  }
}
```

Bind and register:

```csharp
var settings = builder.Configuration
    .GetSection("Dialogporten")
    .Get<DialogportenSettings>()!;

builder.Services.AddDialogportenClient(settings);
```

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
