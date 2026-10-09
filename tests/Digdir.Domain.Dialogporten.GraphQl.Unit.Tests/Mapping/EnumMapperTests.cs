using AwesomeAssertions;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;
using DomainActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType;
using DomainDialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using DomainActivityType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivityType;
using DomainTransmissionType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmissionType;
using DomainGuiActionPriority = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiActionPriority;
using DomainHttpVerb = Digdir.Domain.Dialogporten.Domain.Http.HttpVerb;
using DomainAttachmentUrlConsumerType = Digdir.Domain.Dialogporten.Domain.Attachments.AttachmentUrlConsumerType;
using DomainSystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;
using IdentifierLookupGrantType = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.IdentifierLookupGrantType;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

/// <summary>
/// Guards every explicit enum mapper (ToGraphQl / ToDomain). Mapping is value-driven via
/// <see cref="Enum.GetValues{TEnum}()"/>, so adding a member to either side of a mapped enum without
/// updating the mapper fails here: a missing case throws <see cref="ArgumentOutOfRangeException"/>, and a
/// new member on the opposite enum breaks the completeness/round-trip assertion.
/// </summary>
public class EnumMapperTests
{
    // Bidirectional pairs: every value must map both ways and round-trip back to itself.
    [Fact]
    public void DialogStatus_Mappers_Are_Complete_And_Consistent() =>
        AssertRoundTrip<DomainDialogStatus.Values, DialogStatus>(v => v.ToGraphQl(), v => v.ToDomain());

    [Fact]
    public void SystemLabel_Mappers_Are_Complete_And_Consistent() =>
        AssertRoundTrip<DomainSystemLabel.Values, SystemLabel>(v => v.ToGraphQl(), v => v.ToDomain());

    // One-directional (domain -> GraphQL): every domain value must map, and every GraphQL value must be produced.
    [Fact]
    public void ActorType_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainActorType.Values, ActorType>(v => v.ToGraphQl());

    [Fact]
    public void ActivityType_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainActivityType.Values, ActivityType>(v => v.ToGraphQl());

    [Fact]
    public void TransmissionType_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainTransmissionType.Values, TransmissionType>(v => v.ToGraphQl());

    [Fact]
    public void GuiActionPriority_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainGuiActionPriority.Values, GuiActionPriority>(v => v.ToGraphQl());

    [Fact]
    public void HttpVerb_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainHttpVerb.Values, HttpVerb>(v => v.ToGraphQl());

    [Fact]
    public void AttachmentUrlConsumer_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<DomainAttachmentUrlConsumerType.Values, AttachmentUrlConsumer>(v => v.ToGraphQl());

    [Fact]
    public void DialogLookupGrantType_ToGraphQl_Is_Complete() =>
        AssertToGraphQlComplete<IdentifierLookupGrantType, DialogLookupGrantType>(v => v.ToGraphQl());

    private static void AssertToGraphQlComplete<TSource, TTarget>(Func<TSource, TTarget> toGraphQl)
        where TSource : struct, Enum
        where TTarget : struct, Enum
    {
        var produced = new HashSet<TTarget>();
        foreach (var value in Enum.GetValues<TSource>())
        {
            TTarget mapped = default;
            var act = () => mapped = toGraphQl(value);
            act.Should().NotThrow($"'{typeof(TSource).Name}.{value}' must have an explicit mapping");
            produced.Add(mapped);
        }

        foreach (var target in Enum.GetValues<TTarget>())
        {
            produced.Should().Contain(target,
                $"GraphQL '{typeof(TTarget).Name}.{target}' must be produced by some {typeof(TSource).Name} value");
        }
    }

    private static void AssertRoundTrip<TDomain, TGraphQl>(
        Func<TDomain, TGraphQl> toGraphQl,
        Func<TGraphQl, TDomain> toDomain)
        where TDomain : struct, Enum
        where TGraphQl : struct, Enum
    {
        foreach (var value in Enum.GetValues<TDomain>())
        {
            TGraphQl graphQl = default;
            var act = () => graphQl = toGraphQl(value);
            act.Should().NotThrow($"'{typeof(TDomain).Name}.{value}' must map to {typeof(TGraphQl).Name}");
            toDomain(graphQl).Should().Be(value, "the mapping should round-trip back to the domain value");
        }

        foreach (var value in Enum.GetValues<TGraphQl>())
        {
            TDomain domain = default;
            var act = () => domain = toDomain(value);
            act.Should().NotThrow($"'{typeof(TGraphQl).Name}.{value}' must map to {typeof(TDomain).Name}");
            toGraphQl(domain).Should().Be(value, "the mapping should round-trip back to the GraphQL value");
        }
    }
}
