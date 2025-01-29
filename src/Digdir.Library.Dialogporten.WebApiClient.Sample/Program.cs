using System.Diagnostics;
using Digdir.Library.Dialogporten.WebApiClient.Extensions;
using Digdir.Library.Dialogporten.WebApiClient.Features.V1;
using Digdir.Library.Dialogporten.WebApiClient.Sample;
using Digdir.Library.Dialogporten.WebApiClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddDialogportenClient();
services.AddDialogTokenVerifer();


var serviceProvider = services.BuildServiceProvider();
var now = DateTime.UtcNow;

var dialogportenClient = serviceProvider.GetRequiredService<IServiceownerApi>();

var verifier = serviceProvider.GetRequiredService<DialogTokenVerifier>();

// Usage
// Test token 
var token =
    "eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCIsImtpZCI6ImRldi1wcmltYXJ5LXNpZ25pbmcta2V5In0.eyJqdGkiOiIzNGZhMGViNS0xZGVmLTQxMDYtYWY4YS0xMjljYjNiNTliNDYiLCJjIjoidXJuOmFsdGlubjpwZXJzb246aWRlbnRpZmllci1ubzowODg5NTY5OTY4NCIsImwiOjMsInAiOiJ1cm46YWx0aW5uOnBlcnNvbjppZGVudGlmaWVyLW5vOjA4ODk1Njk5Njg0IiwicyI6InVybjphbHRpbm46cmVzb3VyY2U6c3VwZXItc2ltcGxlLXNlcnZpY2UiLCJpIjoiMDE5MzI1MzgtMzEzZC03NGI1LTg1ZWMtMWI5MGIxMjYzNWRjIiwiYSI6InJlYWQiLCJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo3MjE0L2FwaS92MSIsImlhdCI6MTczMTU3ODk5OCwibmJmIjoxNzMxNTc4OTk4LCJleHAiOjE3MzE1Nzk1OTh9.fL-rpDsXqwOSVk5zMizLZRaFugaz2VfVNf0CjOxIhSdwrkAhh1UfRu5RcD2OK4ddnRrCuz8iKKJyadkek9UGAg";
Console.WriteLine(verifier.Verify(token));
var dict = DialogTokenVerifier.GetDialogTokenClaims(token);
Console.WriteLine(dict);
Console.WriteLine(dict[DialogTokenClaimTypes.Actions]);


Console.WriteLine("== Start Create Dialog ==");
var createDialogCommand = CreateCommand();
var response = await dialogportenClient.V1ServiceOwnerDialogsCreateDialog(createDialogCommand);
Console.WriteLine(response.StatusCode);
Console.WriteLine(response.Content);
Console.WriteLine("== End Create Dialog ==");


// Get single dialog SO
Console.WriteLine("==Start Get Single Dialog==");
var guid = Guid.Parse(response.Content!.Replace("\"", "").Trim());
var dialog = dialogportenClient.V1ServiceOwnerDialogsGetGetDialog(guid, null!).Result.Content;
Debug.Assert(dialog != null, nameof(dialog) + " != null");
Dialogs.PrintGetDialog(dialog);
Console.WriteLine("==End Get Single Dialog==");


Console.WriteLine("==Start Search Dialogs==");
var param = new V1ServiceOwnerDialogsSearchSearchDialogQueryParams()
{
    SystemLabel = [DialogEndUserContextsEntities_SystemLabel.Default]
};
var result = await dialogportenClient.V1ServiceOwnerDialogsSearchSearchDialog(param);
Console.WriteLine(now);
Console.WriteLine(result.Content!.Items.Count);
Console.WriteLine(result.Content.Items.First().Org);
Console.WriteLine("==End Search Dialogs==");


Console.WriteLine("== Start Patch Dialog ==");
List<JsonPatchOperations_Operation> patchDocument =
[
    new()
    {
        Op = "replace",
        OperationType = JsonPatchOperations_OperationType.Replace,
        Path = "/progress",
        Value = 50
    }
];
var patchResponse = await dialogportenClient.V1ServiceOwnerDialogsPatchDialog(guid, patchDocument, null);
Console.WriteLine(patchResponse.IsSuccessStatusCode);
Console.WriteLine(patchResponse.StatusCode);
Console.WriteLine("== End Patch Dialog ==");


Console.WriteLine("== Start update Dialog ==");
var updateCommand = UpdateCommand();
var updateResponse = await dialogportenClient.V1ServiceOwnerDialogsUpdateDialog(guid, updateCommand, null, CancellationToken.None);
Console.WriteLine(updateResponse.StatusCode);
Console.WriteLine(updateResponse.Error?.Content);
Console.WriteLine("== End Update Dialog ==");


Console.WriteLine("== Start Delete Dialog ==");
var deleteResponse = await dialogportenClient.V1ServiceOwnerDialogsDeleteDialog(guid, null);
Console.WriteLine(deleteResponse.IsSuccessStatusCode);
Console.WriteLine("== End Delete Dialog ==");


Console.WriteLine("==Start Get Single Dialog==");
dialog = dialogportenClient.V1ServiceOwnerDialogsGetGetDialog(guid, null!).Result.Content;
Debug.Assert(dialog != null, nameof(dialog) + " != null");
Dialogs.PrintGetDialog(dialog);
Console.WriteLine("==End Get Single Dialog==");


result = await dialogportenClient.V1ServiceOwnerDialogsSearchSearchDialog(param);
Debug.Assert(result.Content != null, nameof(result) + " != null");
Console.WriteLine(result.Content!.Items.Count);


Console.WriteLine("== Start Purge Dialog == ");
var purgeResponse = await dialogportenClient.V1ServiceOwnerDialogsPurgePurgeDialog(guid, dialog.Revision);
Console.WriteLine($"Purge response status code: {response.StatusCode}");
Console.WriteLine($"Purge Response: {response.StatusCode}");

Console.WriteLine($"Purge response status code: {purgeResponse.StatusCode}");
if (purgeResponse.IsSuccessStatusCode)
{
    var dialogAfterPurge = await dialogportenClient.V1ServiceOwnerDialogsGetGetDialog(guid, null!);
    Console.WriteLine(dialogAfterPurge.StatusCode);
}
Console.WriteLine("== End Purge Dialog ==");
return;

static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateCommand()
{
    return new V1ServiceOwnerDialogsCommandsCreate_Dialog
    {
        Activities = [],
        ApiActions = [],
        GuiActions = [],
        Attachments = [],
        SearchTags = [],
        ServiceResource = "urn:altinn:resource:super-simple-service",
        Party = "urn:altinn:person:identifier-no:14886498226",
        SystemLabel = DialogEndUserContextsEntities_SystemLabel.Default,
        Status = DialogsEntities_DialogStatus.New,
        Progress = 2,
        Content = new V1ServiceOwnerDialogsCommandsCreate_Content
        {
            Title = new V1CommonContent_ContentValue
            {
                Value =
                [
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "nb",
                        Value = "Hoved"
                    },
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "en",
                        Value = "Main"
                    }
                ],
                MediaType = "text/plain"
            },
            Summary = new V1CommonContent_ContentValue
            {
                Value =
                [
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "nb",
                        Value = "Hoved Summary"
                    },
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "en",
                        Value = "Main Summary"
                    }
                ],
                MediaType = "text/plain"
            }


        },
        Id = null,
        Transmissions =
        [
            new V1ServiceOwnerDialogsCommandsCreate_Transmission
            {
                Attachments =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment
                    {
                        DisplayName =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Hoved misson"
                            }
                        ],
                        Urls =
                        [
                            new V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl
                            {
                                ConsumerType = Attachments_AttachmentUrlConsumerType.Gui,
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url")
                            }
                        ]

                    }
                ],
                Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
                {
                    Summary = new V1CommonContent_ContentValue
                    {
                        MediaType = "text/plain",
                        Value =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Tranmission summary"
                            }
                        ]
                    },
                    Title = new V1CommonContent_ContentValue
                    {
                        MediaType = "text/plain",
                        Value =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Tranmission Title"
                            }
                        ]
                    }
                },
                Sender = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.ServiceOwner
                },
                Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information
            }
        ]
    };
}

static V1ServiceOwnerDialogsCommandsUpdate_Dialog UpdateCommand()
{
    return new V1ServiceOwnerDialogsCommandsUpdate_Dialog
    {
        Activities = [],
        ApiActions = [],
        Attachments = [],
        SearchTags = [],
        Status = DialogsEntities_DialogStatus.New,
        Progress = 60,
        Content = new V1ServiceOwnerDialogsCommandsUpdate_Content
        {
            Title = new V1CommonContent_ContentValue
            {
                Value =
                [
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "nb",
                        Value = "Hoved"
                    },
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "en",
                        Value = "Main"
                    }
                ],
                MediaType = "text/plain"
            },
            Summary = new V1CommonContent_ContentValue
            {
                Value =
                [
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "nb",
                        Value = "Hoved Summary"
                    },
                    new V1CommonLocalizations_Localization
                    {
                        LanguageCode = "en",
                        Value = "Main Summary"
                    }
                ],
                MediaType = "text/plain"
            }


        },
        GuiActions = [],
        Transmissions =
        [
            new V1ServiceOwnerDialogsCommandsUpdate_Transmission
            {
                Attachments =
                [
                    new V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachment
                    {
                        DisplayName =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Hoved mission"
                            }
                        ],
                        Urls =
                        [
                            new V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachmentUrl
                            {
                                ConsumerType = Attachments_AttachmentUrlConsumerType.Gui,
                                Url = new Uri("https://digdir.apps.tt02.altinn.no/some-other-url")
                            }
                        ]

                    }
                ],
                Content = new V1ServiceOwnerDialogsCommandsUpdate_TransmissionContent
                {
                    Summary = new V1CommonContent_ContentValue
                    {
                        MediaType = "text/plain",
                        Value =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Transmission summary"
                            }
                        ]
                    },
                    Title = new V1CommonContent_ContentValue
                    {
                        MediaType = "text/plain",
                        Value =
                        [
                            new V1CommonLocalizations_Localization
                            {
                                LanguageCode = "nb",
                                Value = "Transmission Title"
                            }
                        ]
                    }
                },
                Sender = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.ServiceOwner
                },
                Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information
            }
        ],
        VisibleFrom = null
    };
}
