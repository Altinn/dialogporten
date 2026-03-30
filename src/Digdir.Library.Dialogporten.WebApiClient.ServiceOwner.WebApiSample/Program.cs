using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var dialogportenSettings = builder.Configuration
    .GetSection("DialogportenSettings")
    .Get<DialogportenSettings>()!;

builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddDialogportenServiceOwnerClient(dialogportenSettings);

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.UseHttpsRedirection();

app.MapPost("/dialogTokenVerify", (
        [FromServices] IDialogTokenValidator dialogTokenVerifier,
        [FromBody] string token)
    => dialogTokenVerifier.Validate(token).IsValid
        ? Results.Ok()
        : Results.Unauthorized());

app.MapGet("/dialog/{dialogId:Guid}", async (
        [FromServices] IServiceOwnerClient client,
        [FromRoute] Guid dialogId,
        CancellationToken cancellationToken) =>
    {
        var response = await client.V1.GetDialogAsync(dialogId, cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode
            ? Results.Ok(response.Content)
            : Results.StatusCode((int)response.StatusCode);
    });

app.MapGet("/dialogs", async (
        [FromServices] IServiceOwnerClient client,
        CancellationToken cancellationToken) =>
    {
        var response = await client.V1.ListDialogsAsync(cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode
            ? Results.Ok(response.Content)
            : Results.StatusCode((int)response.StatusCode);
    });

app.Run();
