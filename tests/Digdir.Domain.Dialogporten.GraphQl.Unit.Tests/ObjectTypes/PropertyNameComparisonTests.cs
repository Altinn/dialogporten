using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class PropertyNameComparisonTests
{
    private sealed class PropertyNameComparisonTestsData : TheoryData<Type, Type, Func<string, bool>?>
    {
        public PropertyNameComparisonTestsData()
        {
            Add(typeof(DialogActivityDto),
                typeof(Activity),
                null);

            Add(typeof(DialogEndUserContextDto),
                typeof(EndUserContext),
                null);

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
        }
    }

    [Theory, ClassData(typeof(PropertyNameComparisonTestsData))]
    public void GraphQl_Property_Name_Should_Match_EndUser_Dtos(Type dtoType, Type gqlType, Func<string, bool>? filter = null)
    {
        var dtoProperties = dtoType.GetProperties()
            .Select(p => p.Name)
            // Filtering out deprecated properties in the
            // REST API that are not present in GraphQL
            .Where(filter ?? (_ => true))
            .ToList();

        var gqlProperties = gqlType.GetProperties().Select(p => p.Name).ToList();

        var missingProperties = dtoProperties.Except(gqlProperties, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(missingProperties.Count == 0,
            $"Properties missing in graphql {gqlType.Name}: {string.Join(", ", missingProperties)}");
    }
}
