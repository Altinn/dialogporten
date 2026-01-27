using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.Validators;

public class ValidateReferenceHierarchyTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void Cannot_Create_Hierarchy_With_Depth_Violations(int maxDepth, int numberOfViolations)
    {
        // Arrange
        var violatingDepth = maxDepth + 1;
        var elements = Enumerable
            .Range(1, numberOfViolations)
            .Aggregate(
                HierarchyTestNodeBuilder.CreateNewHierarchy(violatingDepth),
                (current, _) => current.CreateNewHierarchy(violatingDepth))
            .Build();

        // Act
        var domainFailures = Sut(elements, maxDepth: maxDepth, maxWidth: 1);

        // Assert
        var domainFailure = Assert.Single(domainFailures);
        Assert.Contains("depth violation", domainFailure.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void Cannot_Create_Hierarchy_With_Width_Violations(int maxWidth, int numberOfViolations)
    {
        // Arrange
        var violatingWidth = maxWidth + 1;
        var elements = Enumerable
            .Range(1, numberOfViolations)
            .Aggregate(
                HierarchyTestNodeBuilder.CreateNewHierarchy(1).AddWidth(violatingWidth),
                (current, _) => current.CreateNewHierarchy(1).AddWidth(violatingWidth))
            .Build();

        // Act
        var domainFailures = Sut(elements, maxDepth: 2, maxWidth: maxWidth);

        // Assert
        var domainFailure = Assert.Single(domainFailures);
        Assert.Contains("width violation", domainFailure.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void Cannot_Create_Hierarchy_With_Circular_References(int cycleLength, int numberOfViolations)
    {
        // Arrange
        var elements = Enumerable
            .Range(1, numberOfViolations)
            .Aggregate(HierarchyTestNodeBuilder.CreateNewCyclicHierarchy(cycleLength),
                (current, _) => current.CreateNewCyclicHierarchy(cycleLength))
            .Build();

        // Act
        var domainFailures = Sut(elements, maxDepth: cycleLength, maxWidth: 1);

        // Assert
        var domainFailure = Assert.Single(domainFailures);
        Assert.Contains("cyclic reference", domainFailure.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 10)]
    [InlineData(100, 100, 100)]
    public void Cannot_Create_Hierarchy_With_Multiple_Violations(int maxDepth, int maxWidth, int cycleLength)
    {
        // Arrange
        var violatingDepth = maxDepth + 1;
        var violatingWidth = maxWidth + 1;

        var elements = Enumerable
            .Range(1, maxDepth)
            .Aggregate(
                HierarchyTestNodeBuilder.CreateNewHierarchy(violatingDepth),
                (current, _) => current.CreateNewHierarchy(violatingDepth))
            .AddWidth(violatingWidth)
            .CreateNewCyclicHierarchy(cycleLength)
            .Build();

        // Act
        var domainFailures = Sut(elements, maxDepth: maxDepth, maxWidth: maxWidth);

        // Assert
        Assert.True(domainFailures.Count == 3);
        Assert.Single(domainFailures, x => x.ErrorMessage.Contains("depth violation"));
        Assert.Single(domainFailures, x => x.ErrorMessage.Contains("width violation"));
        Assert.Single(domainFailures, x => x.ErrorMessage.Contains("cyclic reference"));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(10, 10, 10)]
    [InlineData(100, 100, 100)]
    public void Can_Create_Valid_Complex_Hierarchy(int numberOfSegments, int maxDepth, int maxWidth)
    {
        // Arrange
        var elements = Enumerable
            .Range(1, numberOfSegments)
            .Aggregate(
                HierarchyTestNodeBuilder.CreateNewHierarchy(maxDepth).AddWidth(maxWidth - 1),
                (current, _) => current.CreateNewHierarchy(maxDepth))
            .Build();

        // Act
        var domainFailures = Sut(elements, maxDepth: maxDepth, maxWidth: maxWidth);

        // Assert
        Assert.Empty(domainFailures);
    }

    [Fact]
    public void Cannot_Create_Node_Referencing_Non_Existent_Parent()
    {
        // Arrange
        var unknownParent = HierarchyTestNode.Create();
        var node = HierarchyTestNode.Create(parent: unknownParent);

        // Act
        var domainFailures = Sut([node], maxDepth: 1, maxWidth: 1);

        // Assert
        var domainFailure = Assert.Single(domainFailures);
        Assert.True(node.ParentId.HasValue);
        var parentId = node.ParentId.Value;
        Assert.Contains(parentId.ToString(), domainFailure.ErrorMessage);
        Assert.Contains("reference violation", domainFailure.ErrorMessage);
    }

    [Fact]
    public void Cannot_Create_Node_With_Self_Reference()
    {
        // Arrange
        var id = Guid.NewGuid();
        var nodes = HierarchyTestNodeBuilder
            .CreateNewCyclicHierarchy(depth: 1, id)
            .Build();

        // Act
        var domainFailures = Sut(nodes, maxDepth: 1, maxWidth: 1);

        // Assert
        var domainFailure = Assert.Single(domainFailures);
        Assert.Contains(id.ToString(), domainFailure.ErrorMessage);
        Assert.Contains("cyclic reference", domainFailure.ErrorMessage);
    }

    [Fact]
    public void Sut_Should_Throw_Exception_For_Node_With_Default_Id()
    {
        // Arrange
        var node = HierarchyTestNode.Create(Guid.Empty);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => Sut([node], maxDepth: 1, maxWidth: 1));

        // Assert
        Assert.Contains("non-default", exception.Message);
    }

    [Fact]
    public void Empty_Hierarchy_Should_Not_Fail()
    {
        // Arrange/Act
        var domainFailures = Sut([], maxDepth: 1, maxWidth: 1);

        // Assert
        Assert.Empty(domainFailures);
    }

    private static List<DomainFailure> Sut(IReadOnlyCollection<HierarchyTestNode> nodes, int maxDepth, int maxWidth)
        => nodes.ValidateReferenceHierarchy(
            keySelector: x => x.Id,
            parentKeySelector: x => x.ParentId,
            propertyName: "Reference",
            maxDepth: maxDepth,
            maxWidth: maxWidth);
}
