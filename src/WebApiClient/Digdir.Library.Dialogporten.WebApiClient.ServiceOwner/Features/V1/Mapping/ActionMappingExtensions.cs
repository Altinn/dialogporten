using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

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
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
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
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToCreateDialogApiActionEndpoint()).ToList(),
    };

    internal static UpdateDialogApiAction ToUpdateDialogApiAction(this DialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToUpdateDialogApiActionEndpoint()).ToList(),
    };

    internal static UpdateDialogApiAction ToUpdateDialogApiAction(this CreateDialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToUpdateDialogApiActionEndpoint()).ToList(),
    };

    internal static CreateDialogApiAction ToCreateDialogApiAction(this UpdateDialogApiAction source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        Name = source.Name,
        Endpoints = source.Endpoints?.Select(x => x.ToCreateDialogApiActionEndpoint()).ToList(),
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
}
