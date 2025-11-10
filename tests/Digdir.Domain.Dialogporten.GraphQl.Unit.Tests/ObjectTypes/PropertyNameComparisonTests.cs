using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using Activity = Digdir.Domain.Dialogporten.GraphQL.EndUser.Common.Activity;
using ActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType;
using Attachment = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById.Attachment;
using AttachmentUrl = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById.AttachmentUrl;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using DialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using HttpVerb = Digdir.Domain.Dialogporten.Domain.Http.HttpVerb;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld.DialogDto;
using SystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;
using SearchDialogQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld.SearchDialogQuery;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class PropertyNameComparisonTests
{
    [Theory, ClassData(typeof(PropertyNameComparisonTestsData))]
    public void GraphQl_Contract_Objects_And_Enums_Should_Match_EndUser(Type dtoType, Type gqlType, Func<string, bool>? filter = null)
    {
        var dtoProperties = GetNames(dtoType)
            .Where(filter ?? (_ => true))
            .ToList();

        var gqlProperties = GetNames(gqlType);

        var missingProperties = dtoProperties
            .Except(gqlProperties, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(missingProperties.Count == 0,
            $"Properties missing in graphql {gqlType.Name}: {string.Join(", ", missingProperties)}");
    }

    private sealed class PropertyNameComparisonTestsData : TheoryData<Type, Type, Func<string, bool>?>
    {
        public PropertyNameComparisonTestsData()
        {
            // ====== Input ======
            Add(typeof(SearchDialogQuery), typeof(SearchDialogInput), IgnoreAcceptedLanguage);

            // ====== Dtos =======
            Add(typeof(DialogActivityDto), typeof(Activity), null);
            Add(typeof(DialogEndUserContextDto), typeof(EndUserContext), null);
            Add(typeof(DialogSeenLogDto), typeof(SeenLog), null);
            Add(typeof(DialogApiActionDto), typeof(ApiAction), null);
            Add(typeof(DialogApiActionEndpointDto), typeof(ApiActionEndpoint), null);
            Add(typeof(DialogAttachmentDto), typeof(Attachment), null);
            Add(typeof(DialogAttachmentUrlDto), typeof(AttachmentUrl), null);
            Add(typeof(DialogGuiActionDto), typeof(GuiAction), null);
            Add(typeof(DialogTransmissionDto), typeof(Transmission), null);
            Add(typeof(DialogTransmissionContentDto), typeof(TransmissionContent), null);
            Add(typeof(DialogTransmissionAttachmentDto), typeof(Attachment), null);
            Add(typeof(DialogTransmissionAttachmentUrlDto), typeof(AttachmentUrl), null);

            // ====== Enums =======
            Add(typeof(DialogStatus.Values), typeof(GraphQL.EndUser.Common.DialogStatus), null);
            Add(typeof(DialogActivityType.Values), typeof(ActivityType), null);
            Add(typeof(SystemLabel.Values), typeof(GraphQL.EndUser.Common.SystemLabel), null);
            Add(typeof(ActorType.Values), typeof(GraphQL.EndUser.Common.ActorType), null);
            Add(typeof(HttpVerb.Values), typeof(GraphQL.EndUser.DialogById.HttpVerb), null);
            Add(typeof(DialogGuiActionPriority.Values), typeof(GuiActionPriority), null);
            Add(typeof(DialogTransmissionType.Values), typeof(TransmissionType), null);
            Add(typeof(AttachmentUrlConsumerType.Values), typeof(AttachmentUrlConsumer), null);

            // ====== Special cases =======

            // Filtering out deprecated properties in the
            // REST API that are not present in GraphQL
            // Delete filter when dialogDto.SystemLabel is removed
            Add(typeof(DialogDto), typeof(Dialog), ExcludeSystemLabel);
            Add(typeof(SearchDialogDto), typeof(SearchDialog), ExcludeSystemLabel);

            // Filtering out properties that are not present in GraphQL
            // These properties are mapped to
            // Title and Summary in the application layer
            Add(typeof(DialogContentType.Values), typeof(Content),
                ExcludeNonSensitiveContentTypes);

            Add(typeof(DialogContentType.Values), typeof(SearchContent),
                OutputInListAndExcludeNonSensitive);
        }
    }

    private static bool OutputInListAndExcludeNonSensitive(string name)
    {
        var value = DialogContentType
            .GetValues()
            .FirstOrDefault(x => x.Name
                .Equals(name, StringComparison.OrdinalIgnoreCase));

        return value!.OutputInList
               && ExcludeNonSensitiveContentTypes(name);
    }

    private static bool IgnoreAcceptedLanguage(string name) =>
        !name.Equals(nameof(SearchDialogQuery.AcceptedLanguages),
            StringComparison.OrdinalIgnoreCase);

    private static bool ExcludeNonSensitiveContentTypes(string name) =>
        !name.Equals(nameof(DialogContentType.Values.NonSensitiveTitle), StringComparison.OrdinalIgnoreCase)
        && !name.Equals(nameof(DialogContentType.Values.NonSensitiveSummary), StringComparison.OrdinalIgnoreCase);

    private static bool ExcludeSystemLabel(string name) =>
        !name.Equals(nameof(SearchDialogDto.SystemLabel), StringComparison.Ordinal);

    private static IEnumerable<string> GetNames(Type t) =>
        t.IsEnum
            ? Enum.GetNames(t)
            : t.GetProperties().Select(p => p.Name);
}
