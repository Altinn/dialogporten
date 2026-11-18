using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class ResourceRegistryClientTests
{
    [Fact]
    public void GetMinimumAuthenticationLevel_Should_Return_Level_Regardless_Of_Obligation_Order()
    {
        // language=xml
        var expressions = $""" 
                           <xacml:ObligationExpressions>
                               {AuthenticationExpression(3)}
                               {SystemUserAuthenticationExpression(4)}
                           </xacml:ObligationExpressions>
                           """;

        var secondExpression = $""" 
                                <xacml:ObligationExpressions>
                                   {SystemUserAuthenticationExpression(4)}
                                   {AuthenticationExpression(3)}
                                </xacml:ObligationExpressions>
                                """;
        var firstPolicy = ParsePolicy(expressions);
        var firstSystemUserAuthLevel = ResourceRegistryClient.GetMinimumSystemUserAuthenticationLevel(firstPolicy);
        var firstAuthLevel = ResourceRegistryClient.GetMinimumAuthenticationLevel(firstPolicy);

        var secondPolicy = ParsePolicy(secondExpression);
        var secondSystemUserAuthLevel = ResourceRegistryClient.GetMinimumSystemUserAuthenticationLevel(secondPolicy);
        var secondAuthLevel = ResourceRegistryClient.GetMinimumAuthenticationLevel(secondPolicy);

        Assert.Equal(4, firstSystemUserAuthLevel);
        Assert.Equal(3, firstAuthLevel);
        Assert.Equal(firstAuthLevel, secondAuthLevel);
        Assert.Equal(firstSystemUserAuthLevel, secondSystemUserAuthLevel);
    }

    [Fact]
    public void GetMinimumSystemUserAuthenticationLevel_Should_Return_Null_When_Obligation_Is_Missing()
    {
        // language=xml
        var expressions = $""" 
                           <xacml:ObligationExpressions>
                               {AuthenticationExpression(3)}
                           </xacml:ObligationExpressions>
                           """;
        var policy = ParsePolicy(expressions);
        var systemUserAuthLevel = ResourceRegistryClient.GetMinimumSystemUserAuthenticationLevel(policy);
        var authLevel = ResourceRegistryClient.GetMinimumAuthenticationLevel(policy);
        Assert.Null(systemUserAuthLevel);
        Assert.Equal(3, authLevel);
    }
    private static XacmlPolicy ParsePolicy(string obligationExpressionsXml)
    {
        //language=xml
        var xml = $"""
                   <xacml:Policy xmlns:xacml="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17"
                                 PolicyId="urn:altinn:policyid:1" Version="1.0"
                                 RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
                     <xacml:Target/>
                     <xacml:Rule RuleId="urn:altinn:rule:1" Effect="Permit">
                       <xacml:Target/>
                     </xacml:Rule>
                     {obligationExpressionsXml}
                   </xacml:Policy>
                   """;

        using var reader = XmlReader.Create(new StringReader(xml));
        return XacmlParser.ParseXacmlPolicy(reader);
    }

    private static string SystemUserAuthenticationExpression(int level) =>
        // language=xml
        $"""
            <xacml:ObligationExpression FulfillOn="Permit" ObligationId="urn:altinn:obligation:3">
                <xacml:AttributeAssignmentExpression AttributeId="urn:altinn:obligation-assignment:3" Category="urn:altinn:minimum-authenticationlevel-systemuser">
                   <xacml:AttributeValue DataType="http://www.w3.org/2001/XMLSchema#integer">{level}</xacml:AttributeValue>
                </xacml:AttributeAssignmentExpression>
            </xacml:ObligationExpression>
         """;

    private static string AuthenticationExpression(int level) =>
        // language=xml
        $"""
            <xacml:ObligationExpression FulfillOn="Permit" ObligationId="urn:altinn:obligation:1">
                <xacml:AttributeAssignmentExpression AttributeId="urn:altinn:obligation-assignment:1" Category="urn:altinn:minimum-authenticationlevel">
                   <xacml:AttributeValue DataType="http://www.w3.org/2001/XMLSchema#integer">{level}</xacml:AttributeValue>
                </xacml:AttributeAssignmentExpression>
            </xacml:ObligationExpression>
         """;
}
