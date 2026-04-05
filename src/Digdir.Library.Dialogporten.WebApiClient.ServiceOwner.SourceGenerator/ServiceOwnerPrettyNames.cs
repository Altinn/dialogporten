namespace Digdir.Library.Dialogporten.WebApiClient.ServiceOwner.SourceGenerator;

internal static class ServiceOwnerPrettyNames
{
    public static bool TryGetPrettyName(string transportTypeName, out string? prettyName)
    {
        prettyName = transportTypeName switch
        {
            "Actors_ActorType" => "ActorType",
            "Attachments_AttachmentUrlConsumerType" => "AttachmentUrlConsumerType",
            "ContinuationTokenSetOfTOrderDefinitionAndTTarget" => "ContinuationTokenSet",
            "DialogEndUserContextsEntities_SystemLabel" => "SystemLabel",
            "DialogsEntitiesActivities_DialogActivityType" => "DialogActivityType",
            "DialogsEntitiesActions_DialogGuiActionPriority" => "DialogGuiActionPriority",
            "DialogsEntitiesTransmissions_DialogTransmissionType" => "DialogTransmissionType",
            "DialogsEntities_DialogStatus" => "DialogStatus",
            "DialogsGetQueryParams" => "SearchDialogQueryParams",
            "EndusercontextQueryParams" => "SearchEndUserContextQueryParams",
            "Http_HttpVerb" => "HttpVerb",
            "JsonPatchOperations_Operation" => "PatchOperation",
            "JsonPatchOperations_OperationType" => "PatchOperationType",
            "OrderSetOfTOrderDefinitionAndTTarget" => "OrderSet",
            "PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog" => "PaginatedDialogList",
            "PaginatedListOfV1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem" => "PaginatedEndUserContextList",
            "ShouldSendNotificationQueryParams" => "NotificationConditionQueryParams",
            "V1CommonContent_ContentValue" => "ContentValue",
            "V1CommonIdentifierLookup_IdentifierLookupServiceOwner" => "DialogLookupServiceOwner",
            "V1CommonIdentifierLookup_IdentifierLookupServiceResource" => "DialogLookupServiceResource",
            "V1CommonIdentifierLookup_ServiceOwnerIdentifierLookup" => "DialogLookup",
            "V1CommonLocalizations_Localization" => "Localization",
            "V1Common_DeletedFilter" => "DeletedFilter",
            "V1EndUserCommon_AcceptedLanguage" => "AcceptedLanguage",
            "V1EndUserCommon_AcceptedLanguages" => "AcceptedLanguages",
            "V1ServiceOwnerCommonActors_Actor" => "Actor",
            "V1ServiceOwnerCommonDialogStatuses_DialogStatusInput" => "DialogStatusInput",
            "V1ServiceOwnerDialogsQueriesSearchEndUserContext_DialogEndUserContextItem" => "EndUserContextItem",
            "V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationConditionType" => "NotificationConditionType",
            "V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_BulkSetSystemLabel" => "BulkSetSystemLabelsRequest",
            "V1ServiceOwnerEndUserContextCommandsBulkSetSystemLabels_DialogRevision" => "BulkSetSystemLabelsDialogRevision",
            "V1ServiceOwnerEndUserContextCommandsSetSystemLabel_SetDialogSystemLabelRequest" => "SetSystemLabelRequest",
            "V1ServiceOwnerServiceOwnerContextCommandsCreateServiceOwnerLabel_Label" => "CreateServiceOwnerLabelRequest",
            _ => TryGetPatternPrettyName(transportTypeName)
        };

        return prettyName is not null;
    }

    private static string? TryGetPatternPrettyName(string transportTypeName)
    {
        if (TryMatchDialogQueryGet(transportTypeName, out var prettyName) ||
            TryMatchDialogQuerySearch(transportTypeName, out prettyName) ||
            TryMatchCreateDialog(transportTypeName, out prettyName) ||
            TryMatchUpdateDialog(transportTypeName, out prettyName) ||
            TryMatchGetTransmission(transportTypeName, out prettyName) ||
            TryMatchSearchTransmissions(transportTypeName, out prettyName) ||
            TryMatchCreateTransmission(transportTypeName, out prettyName) ||
            TryMatchUpdateTransmission(transportTypeName, out prettyName) ||
            TryMatchActivity(transportTypeName, out prettyName) ||
            TryMatchSeenLog(transportTypeName, out prettyName) ||
            TryMatchNotificationCondition(transportTypeName, out prettyName))
        {
            return prettyName;
        }

        return null;
    }

    private static bool TryMatchDialogQueryGet(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsQueriesGet_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Dialog" => "Dialog",
            "Content" => "DialogContent",
            "Tag" => "DialogTag",
            _ when suffix.StartsWith("Dialog", StringComparison.Ordinal) => suffix,
            _ => $"Dialog{suffix}"
        };

        return true;
    }

    private static bool TryMatchDialogQuerySearch(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsQueriesSearch_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Dialog" => "DialogSummary",
            "Content" => "DialogSummaryContent",
            _ => $"DialogSummary{suffix}"
        };

        return true;
    }

    private static bool TryMatchCreateDialog(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsCommandsCreate_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Dialog" => "CreateDialogRequest",
            _ when suffix.StartsWith("Dialog", StringComparison.Ordinal) => $"Create{suffix}",
            _ => $"CreateDialog{suffix}"
        };

        return true;
    }

    private static bool TryMatchUpdateDialog(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsCommandsUpdate_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Dialog" => "UpdateDialogRequest",
            _ when suffix.StartsWith("Dialog", StringComparison.Ordinal) => $"Update{suffix}",
            _ => $"UpdateDialog{suffix}"
        };

        return true;
    }

    private static bool TryMatchGetTransmission(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsQueriesGetTransmission_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Transmission" => "Transmission",
            _ when suffix.StartsWith("Transmission", StringComparison.Ordinal) => suffix,
            _ => $"Transmission{suffix}"
        };

        return true;
    }

    private static bool TryMatchSearchTransmissions(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsQueriesSearchTransmissions_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "Transmission" => "TransmissionSummary",
            _ when suffix.StartsWith("Transmission", StringComparison.Ordinal) => $"TransmissionSummary{suffix["Transmission".Length..]}",
            _ => $"TransmissionSummary{suffix}"
        };

        return true;
    }

    private static bool TryMatchCreateTransmission(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsCommandsCreateTransmission_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "TransmissionRequest" => "CreateTransmissionRequest",
            _ when suffix.StartsWith("Transmission", StringComparison.Ordinal) => $"Create{suffix}",
            _ => $"CreateTransmission{suffix}"
        };

        return true;
    }

    private static bool TryMatchUpdateTransmission(string transportTypeName, out string? prettyName)
    {
        const string prefix = "V1ServiceOwnerDialogsCommandsUpdateTransmission_";
        if (!transportTypeName.StartsWith(prefix, StringComparison.Ordinal))
        {
            prettyName = null;
            return false;
        }

        var suffix = transportTypeName[prefix.Length..];
        prettyName = suffix switch
        {
            "TransmissionRequest" => "UpdateTransmissionRequest",
            _ when suffix.StartsWith("Transmission", StringComparison.Ordinal) => $"Update{suffix}",
            _ => $"UpdateTransmission{suffix}"
        };

        return true;
    }

    private static bool TryMatchActivity(string transportTypeName, out string? prettyName)
    {
        prettyName = transportTypeName switch
        {
            "V1ServiceOwnerDialogsQueriesGetActivity_Activity" => "Activity",
            "V1ServiceOwnerDialogsQueriesSearchActivities_Activity" => "ActivitySummary",
            "V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest" => "CreateActivityRequest",
            _ => null
        };

        return prettyName is not null;
    }

    private static bool TryMatchSeenLog(string transportTypeName, out string? prettyName)
    {
        prettyName = transportTypeName switch
        {
            "V1ServiceOwnerDialogsQueriesGetSeenLog_SeenLog" => "SeenLog",
            "V1ServiceOwnerDialogsQueriesSearchSeenLogs_SeenLog" => "SeenLogSummary",
            _ => null
        };

        return prettyName is not null;
    }

    private static bool TryMatchNotificationCondition(string transportTypeName, out string? prettyName)
    {
        prettyName = transportTypeName switch
        {
            "V1ServiceOwnerDialogsQueriesNotificationCondition_NotificationCondition" => "NotificationCondition",
            _ => null
        };

        return prettyName is not null;
    }
}
