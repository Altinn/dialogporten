using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using System.Collections;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1.ServiceOwner.Dialogs;

public class TransmissionDtoPropertyEquivalenceTests
{
    [Fact]
    public void Transmission_DTOs_Should_Have_Identical_Property_Names()
    {
        var createDialogTransmissionProperties = GetNames(typeof(TransmissionDto)).ToList();
        var createTransmissionProperties = GetNames(typeof(CreateTransmissionDto)).ToList();
        var updateTransmissionProperties = GetNames(typeof(UpdateTransmissionDto)).ToList();

        AssertPropertySetEquality(createDialogTransmissionProperties, createTransmissionProperties,
            nameof(TransmissionDto), nameof(CreateTransmissionDto));

        AssertPropertySetEquality(createDialogTransmissionProperties, updateTransmissionProperties,
            nameof(TransmissionDto), nameof(UpdateTransmissionDto));

        AssertPropertySetEquality(createTransmissionProperties, updateTransmissionProperties,
            nameof(CreateTransmissionDto), nameof(UpdateTransmissionDto));

        AssertNestedPropertySetEquality(typeof(TransmissionDto), typeof(CreateTransmissionDto));
        AssertNestedPropertySetEquality(typeof(TransmissionDto), typeof(UpdateTransmissionDto));
        AssertNestedPropertySetEquality(typeof(CreateTransmissionDto), typeof(UpdateTransmissionDto));
    }

    private static IEnumerable<string> GetNames(Type type) =>
        type.GetProperties().Select(p => p.Name);

    private static void AssertPropertySetEquality(
        List<string> left,
        List<string> right,
        string leftName,
        string rightName)
    {
        var missingFromLeft = right
            .Except(left, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingFromRight = left
            .Except(right, StringComparer.OrdinalIgnoreCase)
            .ToList();

        missingFromLeft.Should().BeEmpty(
            $"Properties missing in {leftName}: " +
            $"{string.Join(", ", missingFromLeft)}");

        missingFromRight.Should().BeEmpty(
            $"Properties missing in {rightName}: " +
            $"{string.Join(", ", missingFromRight)}");
    }

    private static void AssertNestedPropertySetEquality(Type leftRoot, Type rightRoot)
    {
        var leftNamespace = leftRoot.Namespace;
        if (leftNamespace is null)
        {
            return;
        }

        var visited = new HashSet<(Type Left, Type Right)>();
        AssertNestedPropertySetEquality(leftRoot, rightRoot, leftNamespace, $"{leftRoot.Name} vs {rightRoot.Name}", visited);
    }

    private static void AssertNestedPropertySetEquality(
        Type leftType,
        Type rightType,
        string rootNamespace,
        string context,
        HashSet<(Type Left, Type Right)> visited)
    {
        if (!visited.Add((leftType, rightType)))
        {
            return;
        }

        AssertPropertySetEquality(
            GetNames(leftType).ToList(),
            GetNames(rightType).ToList(),
            $"{leftType.FullName ?? leftType.Name} ({context})",
            $"{rightType.FullName ?? rightType.Name} ({context})");

        var leftProperties = leftType.GetProperties();
        var rightProperties = rightType.GetProperties()
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var leftProperty in leftProperties)
        {
            if (!rightProperties.TryGetValue(leftProperty.Name, out var rightProperty))
            {
                continue;
            }

            var leftPropertyType = GetComparableType(leftProperty.PropertyType);
            var rightPropertyType = GetComparableType(rightProperty.PropertyType);

            if (leftPropertyType is null || rightPropertyType is null)
            {
                continue;
            }

            if (!string.Equals(leftPropertyType.Namespace, rootNamespace, StringComparison.Ordinal))
            {
                continue;
            }

            var nestedContext = $"{context} :: {leftType.Name}.{leftProperty.Name}";
            AssertNestedPropertySetEquality(leftPropertyType, rightPropertyType, rootNamespace, nestedContext, visited);
        }
    }

    private static Type? GetComparableType(Type propertyType)
    {
        if (propertyType == typeof(string))
        {
            return null;
        }

        if (propertyType.IsArray)
        {
            return propertyType.GetElementType();
        }

        if (typeof(IEnumerable).IsAssignableFrom(propertyType))
        {
            return propertyType.IsGenericType
                ? propertyType.GetGenericArguments().FirstOrDefault()
                : null;
        }

        return propertyType;
    }
}
