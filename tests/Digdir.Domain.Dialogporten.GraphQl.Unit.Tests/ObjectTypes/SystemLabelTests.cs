using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using DomainSystemLabel = Digdir.Domain.Dialogporten.Domain
    .DialogEndUserContexts.Entities.SystemLabel.Values;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class SystemLabelTests
{
    [Fact]
    public void SystemLabel_Types_In_GraphQl_Must_Match_Domain_Types()
    {
        // Arrange
        var domainTypes = Enum.GetValues(typeof(DomainSystemLabel)).Cast<DomainSystemLabel>().ToList();
        var graphQlTypes = Enum.GetValues(typeof(SystemLabel)).Cast<SystemLabel>().ToList();

        // Assert
        Assert.Equal(domainTypes.Count, graphQlTypes.Count);

        for (var i = 0; i < domainTypes.Count; i++)
        {
            Assert.Equal(domainTypes[i].ToString(), graphQlTypes[i].ToString());
        }
    }
}
