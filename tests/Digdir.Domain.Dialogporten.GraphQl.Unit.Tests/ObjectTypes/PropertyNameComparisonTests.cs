using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class PropertyNameComparisonTests
{
    private sealed class PropertyNameComparisonTestsData : TheoryData<Type, Type>
    {
        public PropertyNameComparisonTestsData()
        {
            // DialogDto for detail and search queries have their own tests since they have
            // properties that are deprecated in the REST API but not present in the GraphQL API.
            // See DialogDtoTests.cs and DialogSearchDtoTests.cs

            Add(typeof(DialogActivityDto),
                typeof(Activity));

            Add(typeof(DialogEndUserContextDto),
                typeof(EndUserContext));

            Add(typeof(DialogSeenLogDto),
                typeof(SeenLog));

            Add(typeof(DialogApiActionDto),
                typeof(ApiAction));

            Add(typeof(DialogApiActionEndpointDto),
                typeof(ApiActionEndpoint));

            Add(typeof(DialogAttachmentDto),
                typeof(Attachment));

            Add(typeof(DialogAttachmentUrlDto),
                typeof(AttachmentUrl));

            Add(typeof(DialogGuiActionDto),
                typeof(GuiAction));

            Add(typeof(DialogTransmissionDto),
                typeof(Transmission));

            Add(typeof(DialogTransmissionContentDto),
                typeof(TransmissionContent));

            Add(typeof(DialogTransmissionAttachmentDto),
                typeof(Attachment));

            Add(typeof(DialogTransmissionAttachmentUrlDto),
                typeof(AttachmentUrl));
        }
    }

    [Theory, ClassData(typeof(PropertyNameComparisonTestsData))]
    public void GraphQl_Property_Name_Should_Match_EndUser_Dtos(Type dtoType, Type gqlType)
    {
        var dtoProperties = dtoType.GetProperties().Select(p => p.Name).ToList();
        var gqlProperties = gqlType.GetProperties().Select(p => p.Name).ToList();

        var missingProperties = dtoProperties.Except(gqlProperties, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(missingProperties.Count == 0,
            $"Properties missing in graphql {gqlType.Name}: {string.Join(", ", missingProperties)}");
    }
}
