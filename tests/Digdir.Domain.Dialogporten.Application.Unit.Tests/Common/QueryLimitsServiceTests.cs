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

        Assert.Equal(20, endUserLimits.MaxPartyFilterValues);
        Assert.Equal(20, endUserLimits.MaxServiceResourceFilterValues);
        Assert.Equal(20, endUserLimits.MaxOrgFilterValues);
        Assert.Equal(20, endUserLimits.MaxExtendedStatusFilterValues);

        Assert.Equal(20, serviceOwnerLimits.MaxPartyFilterValues);
        Assert.Equal(20, serviceOwnerLimits.MaxServiceResourceFilterValues);
        Assert.Equal(20, serviceOwnerLimits.MaxExtendedStatusFilterValues);
    }

    [Fact]
    public void Uses_Configured_Limits_When_Provided()
    {
        var settings = CreateSettings(new LimitsSettings
        {
            EndUserSearch = new EndUserSearchQueryLimits
            {
                MaxPartyFilterValues = 101,
                MaxServiceResourceFilterValues = 21,
                MaxOrgFilterValues = 31,
                MaxExtendedStatusFilterValues = 41
            },
            ServiceOwnerSearch = new ServiceOwnerSearchQueryLimits
            {
                MaxPartyFilterValues = 11,
                MaxServiceResourceFilterValues = 12,
                MaxExtendedStatusFilterValues = 13
            }
        });

        var snapshot = Substitute.For<IOptionsSnapshot<ApplicationSettings>>();
        snapshot.Value.Returns(settings);

        var service = new QueryLimitsService(snapshot);

        var endUserLimits = service.GetEndUserSearchDialogLimits();
        var serviceOwnerLimits = service.GetServiceOwnerSearchDialogLimits();

        Assert.Equal(101, endUserLimits.MaxPartyFilterValues);
        Assert.Equal(21, endUserLimits.MaxServiceResourceFilterValues);
        Assert.Equal(31, endUserLimits.MaxOrgFilterValues);
        Assert.Equal(41, endUserLimits.MaxExtendedStatusFilterValues);

        Assert.Equal(11, serviceOwnerLimits.MaxPartyFilterValues);
        Assert.Equal(12, serviceOwnerLimits.MaxServiceResourceFilterValues);
        Assert.Equal(13, serviceOwnerLimits.MaxExtendedStatusFilterValues);
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
