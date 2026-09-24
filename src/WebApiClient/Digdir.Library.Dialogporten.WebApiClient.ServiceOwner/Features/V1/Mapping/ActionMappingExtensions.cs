using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

// The legacy authorizationAttribute/action members carried by these maps are [Obsolete] in favour of
// authorizationContext, but a mapping layer has to keep round-tripping them for as long as the server
// still returns and accepts them.
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable DPEXP001 // authorizationContext is experimental

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the GUI action and API action (+ endpoint) hierarchies. Get-only server fields such as
/// <c>IsAuthorized</c> have no target on the input models and are dropped. Localized titles/prompts are
/// shared <c>Localization</c> collections and are reused by reference.
/// </summary>
internal static class ActionMappingExtensions
{
    // GUI actions

    internal static CreateDialogGuiAction ToCreateDialogGuiAction(this DialogGuiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    internal static UpdateDialogGuiAction ToUpdateDialogGuiAction(this DialogGuiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    internal static UpdateDialogGuiAction ToUpdateDialogGuiAction(this CreateDialogGuiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    internal static CreateDialogGuiAction ToCreateDialogGuiAction(this UpdateDialogGuiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    // Create/Update -> Get. The Get-only IsAuthorized field has no source and is left null. Id and HttpMethod are
    // optional on the input models and are defaulted (Guid.Empty / default HttpVerb) when absent.

    internal static DialogGuiAction ToDialogGuiAction(this CreateDialogGuiAction source) => new()
    {
        Id = source.Id ?? default,
        // The server returns the empty-string sentinel, not null, when the action came from a context.
        Action = source.Action ?? string.Empty,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod ?? default,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    internal static DialogGuiAction ToDialogGuiAction(this UpdateDialogGuiAction source) => new()
    {
        Id = source.Id ?? default,
        // The server returns the empty-string sentinel, not null, when the action came from a context.
        Action = source.Action ?? string.Empty,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod ?? default,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt,
    };

    // API actions

    internal static CreateDialogApiAction ToCreateDialogApiAction(this DialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToCreateDialogApiActionEndpoint()).ToList() ?? [],
    };

    internal static UpdateDialogApiAction ToUpdateDialogApiAction(this DialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToUpdateDialogApiActionEndpoint()).ToList() ?? [],
    };

    internal static UpdateDialogApiAction ToUpdateDialogApiAction(this CreateDialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToUpdateDialogApiActionEndpoint()).ToList() ?? [],
    };

    internal static CreateDialogApiAction ToCreateDialogApiAction(this UpdateDialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToCreateDialogApiActionEndpoint()).ToList() ?? [],
    };

    internal static DialogApiAction ToDialogApiAction(this CreateDialogApiAction source) => new()
    {
        Id = source.Id ?? default,
        // The server returns the empty-string sentinel, not null, when the action came from a context.
        Action = source.Action ?? string.Empty,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToDialogApiActionEndpoint()).ToList() ?? [],
    };

    internal static DialogApiAction ToDialogApiAction(this UpdateDialogApiAction source) => new()
    {
        Id = source.Id ?? default,
        // The server returns the empty-string sentinel, not null, when the action came from a context.
        Action = source.Action ?? string.Empty,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToDialogApiActionEndpoint()).ToList() ?? [],
    };

    // API action endpoints

    private static CreateDialogApiActionEndpoint ToCreateDialogApiActionEndpoint(this DialogApiActionEndpoint source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };

    private static UpdateDialogApiActionEndpoint ToUpdateDialogApiActionEndpoint(this DialogApiActionEndpoint source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };

    private static UpdateDialogApiActionEndpoint ToUpdateDialogApiActionEndpoint(this CreateDialogApiActionEndpoint source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };

    private static CreateDialogApiActionEndpoint ToCreateDialogApiActionEndpoint(this UpdateDialogApiActionEndpoint source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };

    private static DialogApiActionEndpoint ToDialogApiActionEndpoint(this CreateDialogApiActionEndpoint source) => new()
    {
        Id = source.Id ?? default,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };

    private static DialogApiActionEndpoint ToDialogApiActionEndpoint(this UpdateDialogApiActionEndpoint source) => new()
    {
        Id = source.Id ?? default,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt,
    };
}

#pragma warning restore DPEXP001
#pragma warning restore CS0618
