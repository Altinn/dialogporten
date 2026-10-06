using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.EndUser;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var dialogportenSettings = builder.Configuration
    .GetSection("DialogportenSettings")
    .Get<DialogportenSettings>();
builder.Services.AddDialogportenClient(dialogportenSettings ?? throw new InvalidOperationException("No Dialogporten settings found"));

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
        [FromServices] IEndUserApi endUserApi,
        [FromRoute] Guid dialogId,
        CancellationToken cancellationToken) =>
{
    var response = await endUserApi.V1.GetDialog(dialogId, cancellationToken: cancellationToken);
    return response.IsSuccessful
        ? Results.Ok(response.Content)
        : Results.StatusCode((int)response.StatusCode);
});

app.Run();
