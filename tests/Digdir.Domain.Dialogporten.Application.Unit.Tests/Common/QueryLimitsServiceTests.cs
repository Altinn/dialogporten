using Digdir.Domain.Dialogporten.Application.Common.QueryLimits;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;

public sealed class QueryLimitsServiceTests
{
    [Fact]
    public void Uses_Default_Limits_When_Not_Configured()
    {
        var snapshot = Substitute.For<IOptionsSnapshot<ApplicationSettings>>();
        snapshot.Value.Returns(CreateSettings());

        var service = new QueryLimitsService(snapshot);

        var endUserLimits = service.GetEndUserSearchDialogLimits();
        var serviceOwnerLimits = service.GetServiceOwnerSearchDialogLimits();

        Assert.Equal(100, endUserLimits.Party);
        Assert.Equal(20, endUserLimits.ServiceResource);
        Assert.Equal(20, endUserLimits.Org);
        Assert.Equal(20, endUserLimits.ExtendedStatus);

        Assert.Equal(20, serviceOwnerLimits.Party);
        Assert.Equal(20, serviceOwnerLimits.ServiceResource);
        Assert.Equal(20, serviceOwnerLimits.ExtendedStatus);
    }

    [Fact]
    public void Uses_Configured_Limits_When_Provided()
    {
        var settings = CreateSettings(new LimitsSettings
        {
            EndUserSearch = new EndUserSearchQueryLimits
            {
                Party = 101,
                ServiceResource = 21,
                Org = 31,
                ExtendedStatus = 41
            },
            ServiceOwnerSearch = new ServiceOwnerSearchQueryLimits
            {
                Party = 11,
                ServiceResource = 12,
                ExtendedStatus = 13
            }
        });

        var snapshot = Substitute.For<IOptionsSnapshot<ApplicationSettings>>();
        snapshot.Value.Returns(settings);

        var service = new QueryLimitsService(snapshot);

        var endUserLimits = service.GetEndUserSearchDialogLimits();
        var serviceOwnerLimits = service.GetServiceOwnerSearchDialogLimits();

        Assert.Equal(101, endUserLimits.Party);
        Assert.Equal(21, endUserLimits.ServiceResource);
        Assert.Equal(31, endUserLimits.Org);
        Assert.Equal(41, endUserLimits.ExtendedStatus);

        Assert.Equal(11, serviceOwnerLimits.Party);
        Assert.Equal(12, serviceOwnerLimits.ServiceResource);
        Assert.Equal(13, serviceOwnerLimits.ExtendedStatus);
    }

    private static ApplicationSettings CreateSettings(LimitsSettings? limits = null) =>
        new()
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://unit.test"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = new Ed25519KeyPair
                    {
                        Kid = "primary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    },
                    Secondary = new Ed25519KeyPair
                    {
                        Kid = "secondary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    }
                }
            },
            Limits = limits ?? new LimitsSettings()
        };
}
