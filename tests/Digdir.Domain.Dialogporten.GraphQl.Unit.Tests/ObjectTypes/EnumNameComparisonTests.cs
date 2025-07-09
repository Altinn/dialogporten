using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class EnumNameComparisonTests
{
    private sealed class EnumNameComparisonTestsData : TheoryData<Type, Type>
    {

        public EnumNameComparisonTestsData()
        {
            // DialogContentType.Values has its own test
            // since it is mapped from an enum to a class
            // with properties. See ContentTypeTests.cs

            Add(typeof(DialogStatus.Values),
                typeof(GraphQL.EndUser.Common.DialogStatus));

            Add(typeof(DialogActivityType.Values),
                typeof(GraphQL.EndUser.Common.ActivityType));

            Add(typeof(SystemLabel.Values),
                typeof(GraphQL.EndUser.Common.SystemLabel));

            Add(typeof(ActorType.Values),
                typeof(GraphQL.EndUser.Common.ActorType));

            Add(typeof(HttpVerb.Values),
                typeof(GraphQL.EndUser.DialogById.HttpVerb));

            Add(typeof(DialogGuiActionPriority.Values),
                typeof(GraphQL.EndUser.DialogById.GuiActionPriority));

            Add(typeof(DialogTransmissionType.Values),
                typeof(GraphQL.EndUser.DialogById.TransmissionType));

            Add(typeof(AttachmentUrlConsumerType.Values),
                typeof(GraphQL.EndUser.DialogById.AttachmentUrlConsumer));
        }
    }

    [Theory, ClassData(typeof(EnumNameComparisonTestsData))]
    public void GraphQl_Enum_Names_Should_Match_Domain_Enum_Names(Type domainEnum, Type graphQlEnum)
    {
        var domainEnumValues = Enum.GetNames(domainEnum);
        var graphQlEnumValues = Enum.GetNames(graphQlEnum);

        var missingProperties = domainEnumValues
            .Except(graphQlEnumValues, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Assert
        Assert.True(missingProperties.Count == 0,
            $"Properties missing in graphql enum {graphQlEnum.Name}: " +
            $"{string.Join(", ", missingProperties)}");
    }
}
