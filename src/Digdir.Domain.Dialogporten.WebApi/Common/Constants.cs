namespace Digdir.Domain.Dialogporten.WebApi.Common;

internal static class Constants
{
    internal const string IfMatch = "If-Match";
    internal const string ETag = "Etag";
    internal const string Authorization = "Authorization";
    internal const string CurrentTokenIssuer = "CurrentIssuer";
    internal const int MaxRequestBodySize = 200_000;

    internal static class SwaggerSummary
    {
        internal const string GlobalDescription = "Dialogporten API description for both enduser and serviceowner users, as well as open metadata information for public key material.<br><br>All operations* described within this document require authentication and authorization. Read more at <a href=\"https://docs.altinn.studio/dialogporten/user-guides/authenticating/\">https://docs.altinn.studio/dialogporten/user-guides/authenticating/</a><br><br><strong>All GET operations* and POST operations may return or contain, respectively, personal identifiable information (national identity numbers and names).</strong><br><br>For more information about this product, see <a href=\"https://docs.altinn.studio/dialogporten\">https://docs.altinn.studio/dialogporten</a><br><br><em>* Except the metadata APIs";
        internal const string ReturnedResult = "Successfully returned the dialog {0}.";
        internal const string Created = "The UUID of the created dialog {0}. A relative URL to the newly created activity is set in the \"Location\" header.";
        internal const string Deleted = "The dialog {0} was deleted successfully.";
        internal const string Restored = "The dialog {0} was restored successfully.";
        internal const string Updated = "The dialog {0} was updated successfully.";
        internal const string Frozen = "The dialog {0} was frozen successfully.";
        internal const string ValidationError = "Validation error occurred. See problem details for a list of errors.";
        internal const string DomainError = "Domain error occurred. See problem details for a list of errors.";
        internal const string ServiceOwnerAuthenticationFailure = "Missing or invalid authentication token. Requires a Maskinporten-token with the scope \"{0}\".";
        internal const string EndUserAuthenticationFailure = "Missing or invalid authentication token. Requires a Maskinporten-token with the scope \"digdir:dialogporten\".";
        internal const string ServiceOwnerLabelNotFound = "The given dialog or service owner label was not found.";
        internal const string DialogNotFound = "The given dialog ID was not found.";
        internal const string DialogDeleted = $"Entity with the given key(s) is removed.";
        internal const string DialogActivityNotFound = "The specified dialog ID or activity ID was not found.";
        internal const string DialogTransmissionNotFound = "The specified dialog ID or transmission ID was not found.";
        internal const string RevisionMismatch = "The supplied If-Match header did not match the current Revision value for the dialog. The request was not applied.";
        internal const string AccessDeniedToDialog = "Unauthorized to {0} the supplied dialog (not owned by authenticated organization or has additional scope requirements defined in policy).";
        internal const string AccessDeniedToDialogForChildEntity = "Unauthorized to {0} child entity for the given dialog (dialog not owned by authenticated organization or has additional scope requirements defined in service identifiers policy).";
        internal const string DialogCreationNotAllowed = "Unauthorized to create a dialog for the given serviceResource (not owned by authenticated organization or has additional scope requirements defined in policy).";
        internal const string OptimisticConcurrencyNote = "Optimistic concurrency control is implemented using the If-Match header. Supply the Revision value from the GetDialog endpoint to ensure that the dialog is not modified/deleted by another request in the meantime.";
        internal const string OptimisticConcurrencyNoteEnduserContext = "Optimistic concurrency control is implemented using the If-Match header. Supply EnduserContextRevision to ensure that the context is not modified/deleted by another request in the meantime.";
        internal const string IdempotentKeyConflict = "Dialog with IdempotentKey {0} has already been created.";
    }
}
