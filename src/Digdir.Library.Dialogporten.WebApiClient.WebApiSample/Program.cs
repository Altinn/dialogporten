using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var dialogportenSettings = builder.Configuration
    .GetSection("DialogportenSettings")
    .Get<DialogportenSettings>()!;
builder.Services.AddDialogportenClient(dialogportenSettings);

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

app.MapGet("/dialog/{dialogId:Guid}", (
        [FromServices] IServiceownerApi serviceOwnerApi,
        [FromRoute] Guid dialogId,
        CancellationToken cancellationToken)
    => Results.Ok(serviceOwnerApi.V1ServiceOwnerDialogsGetGetDialog(dialogId, null!, cancellationToken)));

app.Run();
