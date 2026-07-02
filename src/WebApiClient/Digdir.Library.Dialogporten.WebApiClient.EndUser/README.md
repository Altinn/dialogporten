# Altinn.ApiClients.Dialogporten.EndUser

.NET SDK for the [Dialogporten](https://github.com/altinn/dialogporten) EndUser API. Provides a typed HTTP client for reading dialogs and related resources on behalf of end users, backed by Maskinporten authentication and automatic EdDSA key caching for dialog token validation.

Sample projects are available at https://github.com/Altinn/dialogporten-samples.

## Installation

```
dotnet add package Altinn.ApiClients.Dialogporten.EndUser
```

Targets net8.0, net9.0, and net10.0.

## Setup

Register the client in your DI container using `AddDialogportenClient`. The `BaseUri` must point to the Dialogporten root, excluding `/api/v...`.

| Environment | BaseUri |
|---|---|
| Production | `https://platform.altinn.no/dialogporten` |
| TT02 (staging) | `https://platform.tt02.altinn.no/dialogporten` |
| AT23 (test) | `https://platform.at23.altinn.cloud/dialogporten` |

### Built-in Maskinporten authentication

Provide Maskinporten settings and the SDK handles token acquisition automatically. The required scope is `digdir:dialogporten`.

```csharp
builder.Services.AddDialogportenClient(options =>
{
    options.BaseUri = "https://platform.altinn.no/dialogporten";
    options.Maskinporten = new MaskinportenSettings
    {
        Authority = "https://maskinporten.no/",
        ClientId = "your-client-id",
        Scope = "digdir:dialogporten",
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

Inject `IEndUserApi` and call methods on the `V1` property:

```csharp
public class MyService(IEndUserApi dialogporten)
{
    public async Task<ICollection<DialogListItem>> GetMyDialogsAsync(CancellationToken ct)
    {
        var response = await dialogporten.V1.SearchDialogs(
            new SearchDialogsQueryParams { Status = [DialogStatus.InProgress] },
            cancellationToken: ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed: {response.StatusCode}");

        return response.Content!.Items;
    }
}
```

All methods return `IApiResponse<T>` (or `IApiResponse` for void responses) from [Refit](https://github.com/reactiveui/refit), giving you access to the status code, headers, and deserialized content.

## Available operations

### Dialogs

| Method | Description |
|---|---|
| `SearchDialogs(queryParams, acceptLanguage, ct)` | Paginated list of dialogs. Use `continuationToken` from the response to page through results. |
| `GetDialog(dialogId, acceptLanguage, ct)` | Single dialog aggregate. Accessing this endpoint marks the dialog content as seen. |
| `GetDialogLookup(instanceRef, acceptLanguage, ct)` | Resolve dialog metadata by an external instance reference. |

**Search filter highlights** (`SearchDialogsQueryParams`):
- `Org`, `ServiceResource`, `Party` — filter by org, resource, or owning party
- `Status` — `New`, `InProgress`, `Waiting`, `Signing`, `Cancelled`, `Completed`
- `SystemLabel` — `Default`, `Bin`, `Archive`
- `CreatedAfter/Before`, `UpdatedAfter/Before`, `DueAfter/Before`
- `IsContentSeen` — filter by seen/unseen content
- `Search` / `SearchLanguageCode` — free-text fuzzy search
- `ContinuationToken`, `Limit` (1–1000, default 100)

Dates must include an explicit time zone, e.g. `2024-01-15T10:00:00Z`.

### Transmissions

| Method | Description |
|---|---|
| `SearchDialogTransmissions(dialogId, acceptLanguage, ct)` | All transmissions for a dialog. |
| `GetDialogTransmission(dialogId, transmissionId, acceptLanguage, ct)` | Single transmission. |

### Activities

| Method | Description |
|---|---|
| `SearchDialogActivities(dialogId, acceptLanguage, ct)` | All activities for a dialog. |
| `GetDialogActivity(dialogId, activityId, acceptLanguage, ct)` | Single activity. |

### Seen log

| Method | Description |
|---|---|
| `SearchDialogSeenLogs(dialogId, ct)` | All seen-log records for a dialog. |
| `GetDialogSeenLog(dialogId, seenLogId, ct)` | Single seen-log record. |

### System labels

| Method | Description |
|---|---|
| `SetDialogSystemLabels(dialogId, request, ifMatch, ct)` | Set system label(s) on a single dialog. Supply `EnduserContextRevision` as `ifMatch` for optimistic concurrency. |
| `BulkSetDialogSystemLabels(dto, ct)` | Set system labels on multiple dialogs in one call. |
| `SearchDialogLabelAssignmentLogs(dialogId, ct)` | History of label assignment changes for a dialog. |

### Parties and resources

| Method | Description |
|---|---|
| `GetParties(ct)` | Authorized parties available to the authenticated end user. |
| `SearchAuthorizedServiceResources(party, acceptLanguage, ct)` | Service resources the end user is authorized to access, optionally filtered by party. |

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
    // Disable lifetime validation (useful in tests)
    ValidateLifetime = false,
    ClockSkew = TimeSpan.FromSeconds(30)
});
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
      "Scope": "digdir:dialogporten",
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
