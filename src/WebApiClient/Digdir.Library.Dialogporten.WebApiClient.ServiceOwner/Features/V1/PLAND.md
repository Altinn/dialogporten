# PLAN: Dialog mapper (Create ⇄ Update ⇄ Get)

## Goal

Provide a mapper that converts between the three parallel Dialog model
hierarchies in `Features/V1`:

- **Get** (`Get.Dialog` + `Get.Dialog*`) — the response DTO returned by the API.
- **Create** (`Create.CreateDialog` + `Create.CreateDialog*`) — the POST body.
- **Update** (`Update.UpdateDialog` + `Update.UpdateDialog*`) — the PUT body.

Typical use cases the mapper must support:

- `Get.Dialog` → `Update.UpdateDialog` — fetch a dialog, mutate, PUT it back.
- `Get.Dialog` → `Create.CreateDialog` — clone/duplicate an existing dialog.
- `Create.CreateDialog` → `Update.UpdateDialog` — reuse a create payload for an update.
- `Update.UpdateDialog` → `Create.CreateDialog` — reverse of the above.

## Current state (findings)

- No mapping library is referenced (no AutoMapper / Mapster / Mapperly). Package
  refs today: Refit, Refit.HttpClientFactory, Maskinporten, NSec, Hosting.Abstractions.
- The project multi-targets `net10.0;net9.0;net8.0` and ships as a public NuGet
  library, so adding runtime dependencies is undesirable.
- The three hierarchies are structurally near-identical trees but are **not** the
  same types. Each has its own `*Content`, `*Transmission`, `*Attachment`,
  `*GuiAction`, `*ApiAction`, `*Activity`, etc.
- Leaf/value types in `Features/V1/Common` are **shared** across all three
  (`ContentValue`, `Actor`, `Localization`, `NotificationCondition`) — these need
  no mapping, just reference reuse.

### Field-shape differences that make this non-trivial

| Concern | Create | Update | Get |
|---|---|---|---|
| Identity: `Id`, `IdempotentKey` | ✅ | ❌ | ✅ |
| `ServiceResource`, `Party` | ✅ | ❌ (immutable) | ✅ |
| `VisibleFrom`, `CreatedAt`, `UpdatedAt` (overridable) | ✅ | ❌ | ✅ (read-only actuals) |
| `SystemLabel` | ✅ | ❌ | ✅ (obsolete) |
| Status enum type | `DialogStatusInput?` | `DialogStatusInput` | `DialogStatus` |
| Read-only server fields (`Org`, `Revision`, `ServiceResourceType`, `DeletedAt`, `ContentUpdatedAt`, `*TransmissionsCount`, `HasUnopenedContent`, `IsContentSeen`, `SeenSinceLast*`, `EndUserContext`) | ❌ | ❌ | ✅ |
| Content type | `CreateDialogContent` (8 fields incl. `MainContentReference`) | `UpdateDialogContent` (8 fields) | `Content` (8) / `DialogContentSummary` (7, no `MainContentReference`/`AdditionalInfo`) |

**Enum gap (must be handled explicitly):** `DialogStatus` (output) and
`DialogStatusInput` (input) do not share ordinal values.
- `DialogStatusInput` has `New`, `Sent` which `DialogStatus` lacks.
- Map by **name**, not by ordinal. `Get → Create/Update` maps
  `InProgress/Draft/RequiresAttention/Completed/NotApplicable/Awaiting` 1:1;
  the reverse drops `New`/`Sent` (decision needed — see Open questions).

## Approach

**Recommended: hand-written static extension methods, zero new dependencies.**

Rationale: this is a published client library, the models are stable
Refitter-generated POCOs, and source-gen mappers (Mapperly) would add a build
dependency and still require manual config for every field-shape mismatch above.
Hand-written extensions are explicit about the lossy conversions, which is exactly
where the risk lives.

> Alternative considered: `Riok.Mapperly` (compile-time, no runtime dep). Viable,
> but every table row above becomes a `[MapperIgnoreSource]`/`[MapProperty]`
> attribute, so it saves little over explicit code here. Fall back to it only if
> the hierarchy grows a lot.

### Structure

Create `Features/V1/Mapping/` with one file per direction group:

```
Mapping/
  DialogMappingExtensions.cs      // top-level Dialog ⇄ CreateDialog ⇄ UpdateDialog
  TransmissionMappingExtensions.cs
  AttachmentMappingExtensions.cs
  ActionMappingExtensions.cs      // GuiAction / ApiAction / endpoints
  ActivityMappingExtensions.cs
  ContentMappingExtensions.cs
  DialogStatusMapping.cs          // explicit enum name-based conversion
```

Public API shape (extension methods, discoverable via IntelliSense):

```csharp
public static class DialogMappingExtensions
{
    public static UpdateDialog ToUpdateDialog(this Get.Dialog source);
    public static CreateDialog ToCreateDialog(this Get.Dialog source);
    public static UpdateDialog ToUpdateDialog(this CreateDialog source);
    public static CreateDialog ToCreateDialog(this UpdateDialog source);
}
```

Nested mappers are `internal static` extension methods on the nested types,
called by the parents. Collections map with `.Select(x => x.ToXxx()).ToList()`,
null-propagating (`source.Items?.Select(...).ToList()`).

## Implementation steps

1. **Scaffold** `Features/V1/Mapping/` and the files above.
2. **Enum mapping** (`DialogStatusMapping.cs`): two methods,
   `ToDialogStatusInput(this DialogStatus)` and `ToDialogStatus(this DialogStatusInput)`,
   switching on name. Decide the `New`/`Sent` fallback (Open question 1).
3. **Leaf/content mappers** (`ContentMappingExtensions.cs`): map
   `Content`/`DialogContentSummary` ⇄ `CreateDialogContent` ⇄ `UpdateDialogContent`.
   `ContentValue` is shared → assign by reference. Note `DialogContentSummary`
   lacks `MainContentReference`/`AdditionalInfo`.
4. **Child-entity mappers**: Transmission, Attachment (+ `Url`), GuiAction,
   ApiAction (+ `Endpoint`), Activity, Tag families. Work bottom-up so parents
   can call children.
5. **Top-level `DialogMappingExtensions.cs`**: wire the four public conversions.
   For fields absent on the target, drop them; for read-only Get-only fields,
   ignore on the way in. For `Get → Create`, decide whether `Id` is carried
   (idempotent re-create) or nulled (Open question 2).
6. **Tests**: add a test file (mirror existing test project layout — locate the
   `*.Tests` project for this client) covering:
   - round-trip `Create → Update → Create` preserves overlapping fields.
   - `Get → Update` maps status + content + all child collections.
   - enum name mapping incl. the lossy `New`/`Sent` case.
   - null collections stay null / empty stay empty (match existing default of `[]`).
7. **Docs**: short section in the project `README.md` showing `.ToUpdateDialog()` usage.

## Open questions

1. **Lossy status reverse mapping** (`DialogStatusInput.New`/`Sent` → `DialogStatus`
   which has neither): throw, or map to nearest (`New`→`Draft`, `Sent`→`InProgress`)?
2. **`Get → Create` identity**: carry `Id` (enables idempotent re-create) or null
   it (true "new dialog" clone)? Suggest an optional `bool preserveId = false` param.
3. **Scope of "convert to each other"**: is `Create ⇄ Update` actually needed, or
   only the `Get → Create`/`Get → Update` read-modify-write flows? Trimming reduces
   surface area.
4. **Round-trip fidelity**: Get-only fields (revision, counts, contexts) are
   unrecoverable after a Create/Update round-trip — confirm that's acceptable.
