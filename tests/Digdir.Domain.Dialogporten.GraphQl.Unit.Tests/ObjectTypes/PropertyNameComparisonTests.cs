using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using ActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType;
using Attachment = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById.Attachment;
using AttachmentUrl = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById.AttachmentUrl;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using DialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using HttpVerb = Digdir.Domain.Dialogporten.Domain.Http.HttpVerb;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
using SystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class PropertyNameComparisonTests
{
    private sealed class PropertyNameComparisonTestsData : TheoryData<Type, Type, Func<string, bool>?>
    {
        public PropertyNameComparisonTestsData()
        {
            Add(typeof(DialogActivityDto), typeof(Activity), null);

            Add(typeof(DialogEndUserContextDto), typeof(EndUserContext), null);

            Add(typeof(DialogSeenLogDto),
                typeof(SeenLog),
                null);

            Add(typeof(DialogApiActionDto),
                typeof(ApiAction),
                null);

            Add(typeof(DialogApiActionEndpointDto),
                typeof(ApiActionEndpoint),
                null);

            Add(typeof(DialogAttachmentDto),
                typeof(Attachment),
                null);

            Add(typeof(DialogAttachmentUrlDto),
                typeof(AttachmentUrl),
                null);

            Add(typeof(DialogGuiActionDto),
                typeof(GuiAction),
                null);

            Add(typeof(DialogTransmissionDto),
                typeof(Transmission),
                null);

            Add(typeof(DialogTransmissionContentDto),
                typeof(TransmissionContent),
                null);

            Add(typeof(DialogTransmissionAttachmentDto),
                typeof(Attachment),
                null);

            Add(typeof(DialogTransmissionAttachmentUrlDto),
                typeof(AttachmentUrl),
                null);

            // Filtering out deprecated properties in the
            // REST API that are not present in GraphQL
            Add(typeof(DialogDto),
                typeof(Dialog),
                // Delete when dialogDto.SystemLabel is removed
                name => !name.Contains(nameof(DialogDto.SystemLabel)));

            Add(typeof(SearchDialogDto),
                typeof(SearchDialog),
                // Delete when dialogDto.SystemLabel is removed
                name => !name.Contains(nameof(SearchDialogDto.SystemLabel)));

            // ====== Enums =======
            Add(typeof(DialogStatus.Values),
                typeof(GraphQL.EndUser.Common.DialogStatus),
                null);

            Add(typeof(DialogActivityType.Values),
                typeof(ActivityType),
                null);

            Add(typeof(SystemLabel.Values),
                typeof(GraphQL.EndUser.Common.SystemLabel),
                null);

            Add(typeof(ActorType.Values),
                typeof(GraphQL.EndUser.Common.ActorType),
                null);

            Add(typeof(HttpVerb.Values),
                typeof(GraphQL.EndUser.DialogById.HttpVerb),
                null);

            Add(typeof(DialogGuiActionPriority.Values),
                typeof(GuiActionPriority),
                null);

            Add(typeof(DialogTransmissionType.Values),
                typeof(TransmissionType),
                null);

            Add(typeof(AttachmentUrlConsumerType.Values),
                typeof(AttachmentUrlConsumer),
                null);
        }
    }

    [Theory, ClassData(typeof(PropertyNameComparisonTestsData))]
    public void GraphQl_Property_Name_Should_Match_EndUser_Dtos(Type dtoType, Type gqlType, Func<string, bool>? filter = null)
    {
        var dtoProperties = GetNames(dtoType)
            // Filtering out properties that
            // are not present in GraphQL
            .Where(filter ?? (_ => true))
            .ToList();

        var gqlProperties = GetNames(gqlType);

        var missingProperties = dtoProperties.Except(gqlProperties, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(missingProperties.Count == 0,
            $"Properties missing in graphql {gqlType.Name}: {string.Join(", ", missingProperties)}");
    }

    private static IEnumerable<string> GetNames(Type t) =>
        t.IsEnum
            ? Enum.GetNames(t)
            : t.GetProperties().Select(p => p.Name);
}
