using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.ServiceResourceMetadata;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class ServiceResourceMetadataPruneExtensionsTests
{
    [Fact]
    public void PrunedCopy_Prunes_To_Language_Without_Mutating_The_Shared_Source()
    {
        var source = CreateItem();

        var copy = source.PrunedCopy([new AcceptedLanguage("en", 1)]);

        // The copy is pruned to the requested language across every localized field...
        copy.ServiceResource.Name.Should().ContainSingle().Which.LanguageCode.Should().Be("en");
        copy.ServiceOwner.Name.Should().ContainSingle().Which.LanguageCode.Should().Be("en");
        copy.Roles.Should().ContainSingle().Which.Name.Should().ContainSingle().Which.LanguageCode.Should().Be("en");
        copy.AccessPackages.Should().ContainSingle().Which.Name.Should().ContainSingle().Which.LanguageCode.Should().Be("en");

        // ...while the shared, cached source item retains all languages (no in-place mutation).
        source.ServiceResource.Name.Should().HaveCount(2);
        source.ServiceOwner.Name.Should().HaveCount(2);
        source.Roles[0].Name.Should().HaveCount(2);
        source.AccessPackages[0].Name.Should().HaveCount(2);
        copy.Should().NotBeSameAs(source);
    }

    [Fact]
    public void PrunedCopy_With_Null_Keeps_All_Languages()
    {
        var copy = CreateItem().PrunedCopy(null);
        copy.ServiceResource.Name.Should().HaveCount(2);
    }

    [Fact]
    public void PrunedCopy_With_Null_Preserves_Every_Field()
    {
        // Guard against PrunedCopy (a hand-written field-by-field clone) silently dropping a field when the DTO
        // gains one: with no language pruning the copy must be deep-equal to the fully-populated source. Since
        // the cached catalogue is the only path to both query handlers, a dropped field would vanish from every
        // response with no compiler error.
        var source = CreateItem();

        var copy = source.PrunedCopy(null);

        copy.Should().BeEquivalentTo(source);
    }

    [Fact]
    public void PrunedCopy_ReSorts_Roles_By_The_Pruned_Language()
    {
        // roleX and roleY sort in opposite orders depending on the language, so the pruned-language ordering is
        // observable. The catalogue is built/sorted nb-first, so without per-request re-sorting an en request
        // would keep the nb order.
        var item = CreateItemWithTwoRoles();

        var nbOrder = item.PrunedCopy([new AcceptedLanguage("nb", 1)]).Roles.Select(r => r.Urn).ToList();
        var enOrder = item.PrunedCopy([new AcceptedLanguage("en", 1)]).Roles.Select(r => r.Urn).ToList();

        // nb: "Alpha"(y) before "Beta"(x); en: "Alpha"(x) before "Beta"(y).
        nbOrder.Should().Equal("urn:role:y", "urn:role:x");
        enOrder.Should().Equal("urn:role:x", "urn:role:y");
    }

    [Fact]
    public void GetSortName_Prefers_Nb_Then_En_Then_First()
    {
        ServiceResourceMetadataPruneExtensions.GetSortName(
            [new() { LanguageCode = "en", Value = "Eng" }, new() { LanguageCode = "nb", Value = "Bok" }])
            .Should().Be("Bok");
        ServiceResourceMetadataPruneExtensions.GetSortName(
            [new() { LanguageCode = "se", Value = "Sami" }, new() { LanguageCode = "en", Value = "Eng" }])
            .Should().Be("Eng");
        ServiceResourceMetadataPruneExtensions.GetSortName(
            [new() { LanguageCode = "se", Value = "Sami" }])
            .Should().Be("Sami");
        ServiceResourceMetadataPruneExtensions.GetSortName([]).Should().BeEmpty();
    }

    private static List<LocalizationDto> Names() =>
    [
        new() { LanguageCode = "nb", Value = "Tjeneste" },
        new() { LanguageCode = "en", Value = "Service" }
    ];

    private static ServiceResourceMetadataItemDto CreateItemWithTwoRoles()
    {
        var item = CreateItem();
        item.Roles =
        [
            new ServiceResourceMetadataRoleDto
            {
                Urn = "urn:role:x",
                Name = [new() { LanguageCode = "nb", Value = "Beta" }, new() { LanguageCode = "en", Value = "Alpha" }],
                Links = new LinkDto { Metadata = "https://example/role-x" }
            },
            new ServiceResourceMetadataRoleDto
            {
                Urn = "urn:role:y",
                Name = [new() { LanguageCode = "nb", Value = "Alpha" }, new() { LanguageCode = "en", Value = "Beta" }],
                Links = new LinkDto { Metadata = "https://example/role-y" }
            }
        ];
        return item;
    }

    private static ServiceResourceMetadataItemDto CreateItem() =>
        new()
        {
            ServiceResource = new ServiceResourceMetadataServiceResourceDto
            {
                Id = "some-service",
                ResourceType = "GenericAccessResource",
                Status = "Active",
                IsDelegable = true,
                MinimumAuthenticationLevel = 3,
                Name = Names(),
                Links = new LinkDto { Metadata = "https://example/meta" }
            },
            Roles = [new ServiceResourceMetadataRoleDto { Urn = "urn:role:a", Name = Names(), Links = new LinkDto { Metadata = "https://example/role" } }],
            AccessPackages = [new ServiceResourceMetadataAccessPackageDto { Urn = "urn:ap:a", Name = Names(), Links = new LinkDto { Metadata = "https://example/ap" } }],
            ServiceOwner = new ServiceResourceMetadataServiceOwnerDto
            {
                OrgNumber = "111111111",
                Code = "ORG",
                Name = Names()
            }
        };
}
