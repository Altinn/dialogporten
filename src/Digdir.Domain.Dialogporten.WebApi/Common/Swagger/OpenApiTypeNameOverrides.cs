using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

// Temporary bridge for SDK-facing schema names until endpoint families are migrated to
// WebApi-owned request/response contract types with their own OpenAPI type names.
internal static class OpenApiTypeNameOverrides
{
    private static readonly FrozenDictionary<string, string> CommonOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Actors_ActorType"] = "ActorType",
        ["Attachments_AttachmentUrlConsumerType"] = "AttachmentUrlConsumerType",
        ["DialogEndUserContextsEntities_SystemLabel"] = "SystemLabel",
        ["DialogsEntities_DialogStatus"] = "DialogStatus",
        ["DialogsEntitiesActions_DialogGuiActionPriority"] = "DialogGuiActionPriority",
        ["DialogsEntitiesActivities_DialogActivityType"] = "DialogActivityType",
        ["DialogsEntitiesTransmissions_DialogTransmissionType"] = "DialogTransmissionType",
        ["Http_HttpVerb"] = "HttpVerb",
        ["ProblemDetails_Error"] = "ProblemDetailsError",
        ["V1CommonContent_ContentValue"] = "ContentValue",
        ["V1CommonLocalizations_Localization"] = "Localization",
        ["V1EndUserCommon_AcceptedLanguage"] = "AcceptedLanguage",
        ["V1EndUserCommon_AcceptedLanguages"] = "AcceptedLanguages"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> EndUserOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["PaginatedListOfV1EndUserDialogsQueriesSearch_Dialog"] = "PaginatedListOfDialogListItem",
        ["V1AccessManagementQueriesGetParties_AuthorizedParty"] = "AuthorizedParty",
        ["V1AccessManagementQueriesGetParties_Parties"] = "Parties",
        ["V1CommonIdentifierLookup_EndUserIdentifierLookup"] = "EndUserIdentifierLookup",
        ["V1CommonIdentifierLookup_IdentifierLookupAuthorizationEvidence"] = "IdentifierLookupAuthorizationEvidence",
        ["V1CommonIdentifierLookup_IdentifierLookupAuthorizationEvidenceItem"] = "IdentifierLookupAuthorizationEvidenceItem",
        ["V1CommonIdentifierLookup_IdentifierLookupGrantType"] = "IdentifierLookupGrantType",
        ["V1CommonIdentifierLookup_IdentifierLookupServiceOwner"] = "IdentifierLookupServiceOwner",
        ["V1CommonIdentifierLookup_IdentifierLookupServiceResource"] = "IdentifierLookupServiceResource",
        ["V1EndUserCommonActors_Actor"] = "Actor",
        ["V1EndUserDialogLookupQueriesGet_DialogLookupRequest"] = "GetDialogLookupRequest",
        ["V1EndUserDialogsQueriesGet_Content"] = "Content",
        ["V1EndUserDialogsQueriesGet_Dialog"] = "Dialog",
        ["V1EndUserDialogsQueriesGet_DialogActivity"] = "DialogActivity",
        ["V1EndUserDialogsQueriesGet_DialogApiAction"] = "DialogApiAction",
        ["V1EndUserDialogsQueriesGet_DialogApiActionEndpoint"] = "DialogApiActionEndpoint",
        ["V1EndUserDialogsQueriesGet_DialogAttachment"] = "DialogAttachment",
        ["V1EndUserDialogsQueriesGet_DialogAttachmentUrl"] = "DialogAttachmentUrl",
        ["V1EndUserDialogsQueriesGet_DialogEndUserContext"] = "DialogEndUserContext",
        ["V1EndUserDialogsQueriesGet_DialogGuiAction"] = "DialogGuiAction",
        ["V1EndUserDialogsQueriesGet_DialogRequest"] = "GetDialogRequest",
        ["V1EndUserDialogsQueriesGet_DialogSeenLog"] = "DialogSeenLog",
        ["V1EndUserDialogsQueriesGet_DialogTransmission"] = "DialogTransmission",
        ["V1EndUserDialogsQueriesGet_DialogTransmissionAttachment"] = "DialogTransmissionAttachment",
        ["V1EndUserDialogsQueriesGet_DialogTransmissionAttachmentUrl"] = "DialogTransmissionAttachmentUrl",
        ["V1EndUserDialogsQueriesGet_DialogTransmissionContent"] = "DialogTransmissionContent",
        ["V1EndUserDialogsQueriesGet_DialogTransmissionNavigationalAction"] = "DialogTransmissionNavigationalAction",
        ["V1EndUserDialogsQueriesGetActivity_Activity"] = "DialogActivityDetails",
        ["V1EndUserDialogsQueriesGetActivity_ActivityRequest"] = "GetDialogActivityRequest",
        ["V1EndUserDialogsQueriesGetSeenLog_SeenLog"] = "DialogSeenLogDetails",
        ["V1EndUserDialogsQueriesGetTransmission_Attachment"] = "DialogTransmissionAttachmentDetails",
        ["V1EndUserDialogsQueriesGetTransmission_AttachmentUrl"] = "DialogTransmissionAttachmentUrlDetails",
        ["V1EndUserDialogsQueriesGetTransmission_Content"] = "DialogTransmissionContentDetails",
        ["V1EndUserDialogsQueriesGetTransmission_NavigationalAction"] = "DialogTransmissionNavigationalActionDetails",
        ["V1EndUserDialogsQueriesGetTransmission_Transmission"] = "DialogTransmissionDetails",
        ["V1EndUserDialogsQueriesGetTransmission_TransmissionRequest"] = "GetDialogTransmissionRequest",
        ["V1EndUserDialogsQueriesSearch_Content"] = "DialogContentSummary",
        ["V1EndUserDialogsQueriesSearch_Dialog"] = "DialogListItem",
        ["V1EndUserDialogsQueriesSearch_DialogActivity"] = "DialogActivityListItem",
        ["V1EndUserDialogsQueriesSearch_DialogEndUserContext"] = "DialogEndUserContextListItem",
        ["V1EndUserDialogsQueriesSearch_DialogRequest"] = "SearchDialogsRequest",
        ["V1EndUserDialogsQueriesSearch_DialogSeenLog"] = "DialogSeenLogListItem",
        ["V1EndUserDialogsQueriesSearchActivities_Activity"] = "DialogActivitySearchItem",
        ["V1EndUserDialogsQueriesSearchActivities_ActivityRequest"] = "SearchDialogActivitiesRequest",
        ["V1EndUserDialogsQueriesSearchSeenLogs_SeenLog"] = "DialogSeenLogSearchItem",
        ["V1EndUserDialogsQueriesSearchTransmissions_Attachment"] = "DialogTransmissionSearchAttachment",
        ["V1EndUserDialogsQueriesSearchTransmissions_AttachmentUrl"] = "DialogTransmissionSearchAttachmentUrl",
        ["V1EndUserDialogsQueriesSearchTransmissions_Content"] = "DialogTransmissionSearchContent",
        ["V1EndUserDialogsQueriesSearchTransmissions_NavigationalAction"] = "DialogTransmissionSearchNavigationalAction",
        ["V1EndUserDialogsQueriesSearchTransmissions_Transmission"] = "DialogTransmissionSearchItem",
        ["V1EndUserDialogsQueriesSearchTransmissions_TransmissionRequest"] = "SearchDialogTransmissionsRequest",
        ["V1EndUserEndUserContextCommandsBulkSetSystemLabels_BulkSetDialogSystemLabelsRequest"] = "BulkSetDialogSystemLabelsRequest",
        ["V1EndUserEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel"] = "BulkSetSystemLabel",
        ["V1EndUserEndUserContextCommandsBulkSetSystemLabels_DialogRevision"] = "DialogRevision",
        ["V1EndUserEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest"] = "SetDialogSystemLabelRequest",
        ["V1EndUserEndUserContextQueriesSearchLabelAssignmentLog_LabelAssignmentLog"] = "LabelAssignmentLog"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> ServiceOwnerOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["JsonPatchOperations_Operation"] = "JsonPatchOperation",
        ["JsonPatchOperations_OperationType"] = "JsonPatchOperationType",
        ["PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog"] = "PaginatedListOfDialogListItem",
        ["PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem"] = "PaginatedListOfDialogEndUserContextItem",
        ["V1Common_DeletedFilter"] = "DeletedFilter",
        ["V1CommonIdentifierLookup_IdentifierLookupServiceOwner"] = "IdentifierLookupServiceOwner",
        ["V1CommonIdentifierLookup_IdentifierLookupServiceResource"] = "IdentifierLookupServiceResource",
        ["V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup"] = "ServiceOwnerIdentifierLookup",
        ["V1ServiceOwnerCommonActors_Actor"] = "Actor",
        ["V1ServiceOwnerCommonDialogStatuses_DialogStatusInput"] = "DialogStatusInput",
        ["V1ServiceOwnerDialogLookupQueriesGet_DialogLookupRequest"] = "GetDialogLookupRequest",
        ["V1ServiceOwnerDialogsCommandsCreate_Activity"] = "CreateDialogActivity",
        ["V1ServiceOwnerDialogsCommandsCreate_ApiAction"] = "CreateDialogApiAction",
        ["V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint"] = "CreateDialogApiActionEndpoint",
        ["V1ServiceOwnerDialogsCommandsCreate_Attachment"] = "CreateDialogAttachment",
        ["V1ServiceOwnerDialogsCommandsCreate_AttachmentUrl"] = "CreateDialogAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsCreate_Content"] = "CreateDialogContent",
        ["V1ServiceOwnerDialogsCommandsCreate_Dialog"] = "CreateDialog",
        ["V1ServiceOwnerDialogsCommandsCreate_DialogRequest"] = "CreateDialogRequest",
        ["V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext"] = "CreateDialogServiceOwnerContext",
        ["V1ServiceOwnerDialogsCommandsCreate_GuiAction"] = "CreateDialogGuiAction",
        ["V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel"] = "CreateDialogServiceOwnerLabel",
        ["V1ServiceOwnerDialogsCommandsCreate_Tag"] = "CreateDialogTag",
        ["V1ServiceOwnerDialogsCommandsCreate_Transmission"] = "CreateDialogTransmission",
        ["V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachment"] = "CreateDialogTransmissionAttachment",
        ["V1ServiceOwnerDialogsCommandsCreate_TransmissionAttachmentUrl"] = "CreateDialogTransmissionAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsCreate_TransmissionContent"] = "CreateDialogTransmissionContent",
        ["V1ServiceOwnerDialogsCommandsCreate_TransmissionNavigationalAction"] = "CreateDialogTransmissionNavigationalAction",
        ["V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest"] = "CreateDialogActivityRequest",
        ["V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachment"] = "CreateTransmissionAttachment",
        ["V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionAttachmentUrl"] = "CreateTransmissionAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionContent"] = "CreateTransmissionContent",
        ["V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionNavigationalAction"] = "CreateTransmissionNavigationalAction",
        ["V1ServiceOwnerDialogsCommandsCreateTransmission_TransmissionRequest"] = "CreateTransmissionRequest",
        ["V1ServiceOwnerDialogsCommandsUpdate_Activity"] = "UpdateDialogActivity",
        ["V1ServiceOwnerDialogsCommandsUpdate_ApiAction"] = "UpdateDialogApiAction",
        ["V1ServiceOwnerDialogsCommandsUpdate_ApiActionEndpoint"] = "UpdateDialogApiActionEndpoint",
        ["V1ServiceOwnerDialogsCommandsUpdate_Attachment"] = "UpdateDialogAttachment",
        ["V1ServiceOwnerDialogsCommandsUpdate_AttachmentUrl"] = "UpdateDialogAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsUpdate_Content"] = "UpdateDialogContent",
        ["V1ServiceOwnerDialogsCommandsUpdate_Dialog"] = "UpdateDialog",
        ["V1ServiceOwnerDialogsCommandsUpdate_DialogRequest"] = "UpdateDialogRequest",
        ["V1ServiceOwnerDialogsCommandsUpdate_GuiAction"] = "UpdateDialogGuiAction",
        ["V1ServiceOwnerDialogsCommandsUpdate_Tag"] = "UpdateDialogTag",
        ["V1ServiceOwnerDialogsCommandsUpdate_Transmission"] = "UpdateDialogTransmission",
        ["V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachment"] = "UpdateDialogTransmissionAttachment",
        ["V1ServiceOwnerDialogsCommandsUpdate_TransmissionAttachmentUrl"] = "UpdateDialogTransmissionAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsUpdate_TransmissionContent"] = "UpdateDialogTransmissionContent",
        ["V1ServiceOwnerDialogsCommandsUpdate_TransmissionNavigationalAction"] = "UpdateDialogTransmissionNavigationalAction",
        ["V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionAttachment"] = "UpdateTransmissionAttachment",
        ["V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionAttachmentUrl"] = "UpdateTransmissionAttachmentUrl",
        ["V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionContent"] = "UpdateTransmissionContent",
        ["V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionNavigationalAction"] = "UpdateTransmissionNavigationalAction",
        ["V1ServiceOwnerDialogsCommandsUpdateTransmission_TransmissionRequest"] = "UpdateTransmissionRequest",
        ["V1ServiceOwnerDialogsQueriesGet_Content"] = "Content",
        ["V1ServiceOwnerDialogsQueriesGet_Dialog"] = "Dialog",
        ["V1ServiceOwnerDialogsQueriesGet_DialogActivity"] = "DialogActivity",
        ["V1ServiceOwnerDialogsQueriesGet_DialogApiAction"] = "DialogApiAction",
        ["V1ServiceOwnerDialogsQueriesGet_DialogApiActionEndpoint"] = "DialogApiActionEndpoint",
        ["V1ServiceOwnerDialogsQueriesGet_DialogAttachment"] = "DialogAttachment",
        ["V1ServiceOwnerDialogsQueriesGet_DialogAttachmentUrl"] = "DialogAttachmentUrl",
        ["V1ServiceOwnerDialogsQueriesGet_DialogEndUserContext"] = "DialogEndUserContext",
        ["V1ServiceOwnerDialogsQueriesGet_DialogGuiAction"] = "DialogGuiAction",
        ["V1ServiceOwnerDialogsQueriesGet_DialogSeenLog"] = "DialogSeenLog",
        ["V1ServiceOwnerDialogsQueriesGet_DialogServiceOwnerContext"] = "DialogServiceOwnerContext",
        ["V1ServiceOwnerDialogsQueriesGet_DialogServiceOwnerLabel"] = "DialogServiceOwnerLabel",
        ["V1ServiceOwnerDialogsQueriesGet_DialogTransmission"] = "DialogTransmission",
        ["V1ServiceOwnerDialogsQueriesGet_DialogTransmissionAttachment"] = "DialogTransmissionAttachment",
        ["V1ServiceOwnerDialogsQueriesGet_DialogTransmissionAttachmentUrl"] = "DialogTransmissionAttachmentUrl",
        ["V1ServiceOwnerDialogsQueriesGet_DialogTransmissionContent"] = "DialogTransmissionContent",
        ["V1ServiceOwnerDialogsQueriesGet_DialogTransmissionNavigationalAction"] = "DialogTransmissionNavigationalAction",
        ["V1ServiceOwnerDialogsQueriesGet_Tag"] = "DialogTag",
        ["V1ServiceOwnerDialogsQueriesGetActivity_Activity"] = "DialogActivityDetails",
        ["V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog"] = "DialogSeenLogDetails",
        ["V1ServiceOwnerDialogsQueriesGetTransmission_Attachment"] = "DialogTransmissionAttachmentDetails",
        ["V1ServiceOwnerDialogsQueriesGetTransmission_AttachmentUrl"] = "DialogTransmissionAttachmentUrlDetails",
        ["V1ServiceOwnerDialogsQueriesGetTransmission_Content"] = "DialogTransmissionContentDetails",
        ["V1ServiceOwnerDialogsQueriesGetTransmission_NavigationalAction"] = "DialogTransmissionNavigationalActionDetails",
        ["V1ServiceOwnerDialogsQueriesGetTransmission_Transmission"] = "DialogTransmissionDetails",
        ["V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition"] = "NotificationCondition",
        ["V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationConditionType"] = "NotificationConditionType",
        ["V1ServiceOwnerDialogsQueriesSearch_Content"] = "DialogContentSummary",
        ["V1ServiceOwnerDialogsQueriesSearch_Dialog"] = "DialogListItem",
        ["V1ServiceOwnerDialogsQueriesSearch_DialogActivity"] = "DialogActivityListItem",
        ["V1ServiceOwnerDialogsQueriesSearch_DialogEndUserContext"] = "DialogListItemEndUserContext",
        ["V1ServiceOwnerDialogsQueriesSearch_DialogSeenLog"] = "DialogSeenLogListItem",
        ["V1ServiceOwnerDialogsQueriesSearch_DialogServiceOwnerContext"] = "DialogServiceOwnerContextListItem",
        ["V1ServiceOwnerDialogsQueriesSearch_ServiceOwnerLabel"] = "DialogServiceOwnerLabelListItem",
        ["V1ServiceOwnerDialogsQueriesSearchActivities_Activity"] = "DialogActivitySearchItem",
        ["V1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem"] = "DialogEndUserContextItem",
        ["V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog"] = "DialogSeenLogSearchItem",
        ["V1ServiceOwnerDialogsQueriesSearchTransmissions_Attachment"] = "DialogTransmissionSearchAttachment",
        ["V1ServiceOwnerDialogsQueriesSearchTransmissions_AttachmentUrl"] = "DialogTransmissionSearchAttachmentUrl",
        ["V1ServiceOwnerDialogsQueriesSearchTransmissions_Content"] = "DialogTransmissionSearchContent",
        ["V1ServiceOwnerDialogsQueriesSearchTransmissions_NavigationalAction"] = "DialogTransmissionSearchNavigationalAction",
        ["V1ServiceOwnerDialogsQueriesSearchTransmissions_Transmission"] = "DialogTransmissionSearchItem",
        ["V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetDialogSystemLabelsRequest"] = "BulkSetDialogSystemLabelsRequest",
        ["V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel"] = "BulkSetSystemLabel",
        ["V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_DialogRevision"] = "DialogRevision",
        ["V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest"] = "SetDialogSystemLabelRequest",
        ["V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label"] = "CreateServiceOwnerLabel",
        ["V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_ServiceOwnerLabelRequest"] = "CreateServiceOwnerLabelRequest",
        ["V1ServiceOwnerServiceOwnerContextQueriesGetServiceOwnerLabels_ServiceOwnerLabel"] = "ServiceOwnerLabel"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool TryGetOverride(string documentName, string currentName, [NotNullWhen(true)] out string? overrideName)
    {
        if (documentName is not ("v1.enduser" or "v1.serviceowner"))
        {
            overrideName = null;
            return false;
        }

        if (CommonOverrides.TryGetValue(currentName, out overrideName))
        {
            return true;
        }

        var documentOverrides = documentName switch
        {
            "v1.enduser" => EndUserOverrides,
            "v1.serviceowner" => ServiceOwnerOverrides,
            _ => null
        };

        if (documentOverrides is not null && documentOverrides.TryGetValue(currentName, out overrideName))
        {
            return true;
        }

        overrideName = null;
        return false;
    }
}
