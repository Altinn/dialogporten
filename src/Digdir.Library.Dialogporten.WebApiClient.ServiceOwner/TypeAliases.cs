// Aliases for generated Refitter DTO types — gives consumers clean names.
// These are assembly-scoped; consumers see them via the facade's public method signatures.

#region Dialog

// Get (full detail)
global using Dialog = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_Dialog;
global using DialogContent = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_Content;
global using DialogTag = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_Tag;
global using DialogAttachment = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogAttachment;
global using DialogAttachmentUrl = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogAttachmentUrl;
global using DialogGuiAction = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogGuiAction;
global using DialogApiAction = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogApiAction;
global using DialogApiActionEndpoint = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogApiActionEndpoint;
global using DialogServiceOwnerContext = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogServiceOwnerContext;
global using DialogServiceOwnerLabel = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogServiceOwnerLabel;
global using DialogEndUserContext = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogEndUserContext;

// Nested sub-entities on the Dialog aggregate
global using DialogTransmission = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogTransmission;
global using DialogTransmissionContent = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogTransmissionContent;
global using DialogTransmissionAttachment = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogTransmissionAttachment;
global using DialogTransmissionAttachmentUrl = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogTransmissionAttachmentUrl;
global using DialogTransmissionNavigationalAction = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogTransmissionNavigationalAction;
global using DialogActivity = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogActivity;
global using DialogSeenLog = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGet_DialogSeenLog;

// Search (list items)
global using DialogSummary = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesSearch_Dialog;
global using PaginatedDialogList = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog;
global using SearchDialogQueryParams = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.DialogsGetQueryParams;

// Create
global using CreateDialogRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsCommandsCreate_Dialog;

// Update
global using UpdateDialogRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsCommandsUpdate_Dialog;

#endregion

#region Transmission (dedicated endpoint)

global using Transmission = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGetTransmission_Transmission;
global using TransmissionSummary = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission;
global using CreateTransmissionRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest;
global using UpdateTransmissionRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest;

#endregion

#region Activity (dedicated endpoint)

global using Activity = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGetActivity_Activity;
global using ActivitySummary = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesSearchActivities_Activity;
global using CreateActivityRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest;

#endregion

#region SeenLog (dedicated endpoint)

global using SeenLog = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog;
global using SeenLogSummary = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog;

#endregion

#region Labels

global using CreateServiceOwnerLabelRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label;

#endregion

#region System Labels

global using SetSystemLabelRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest;
global using BulkSetSystemLabelsRequest = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel;

#endregion

#region End User Context

global using PaginatedEndUserContextList = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem;
global using EndUserContextItem = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem;
global using SearchEndUserContextQueryParams = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.EndusercontextQueryParams;

#endregion

#region Notification Condition

global using NotificationCondition = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition;
global using NotificationConditionQueryParams = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.ShouldSendNotificationQueryParams;

#endregion

#region Dialog Lookup

global using DialogLookup = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup;

#endregion

#region Patch

global using PatchOperation = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.JsonPatchOperations_Operation;

#endregion
