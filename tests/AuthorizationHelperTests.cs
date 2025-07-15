using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using FluentAssertions;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization.Tests;

public class AuthorizationHelperTests
{
    [Fact]
    public async Task CollapseSubjectResources_WithEmptyAuthorizedParties_ReturnsEmptyResult()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult { AuthorizedParties = new List<AuthorizedParty>() };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.ResourcesByParties.Should().BeEmpty();
        result.AltinnAppInstanceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithNoConstraints_ProcessesAllParties()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                },
                new AuthorizedParty
                {
                    Party = "party2",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role2" }
                }
            }
        };
        var constraintParties = new List<string>(); // Empty list = no constraints
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" },
            new SubjectResource { Subject = "role2", Resource = "resource2" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(2);
        result.ResourcesByParties["party1"].Should().Contain("resource1");
        result.ResourcesByParties["party2"].Should().Contain("resource2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithConstraintParties_FiltersPartiesCorrectly()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                },
                new AuthorizedParty
                {
                    Party = "party2",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role2" }
                }
            }
        };
        var constraintParties = new List<string> { "party1" }; // Only party1 should be processed
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" },
            new SubjectResource { Subject = "role2", Resource = "resource2" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties.Should().ContainKey("party1");
        result.ResourcesByParties.Should().NotContainKey("party2");
        result.ResourcesByParties["party1"].Should().Contain("resource1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithConstraintResources_FiltersResourcesCorrectly()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string> { "resource1" }; // Only resource1 should be included
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" },
            new SubjectResource { Subject = "role1", Resource = "resource2" } // This should be filtered out
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().Contain("resource1");
        result.ResourcesByParties["party1"].Should().NotContain("resource2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithPartiesWithoutRoles_SkipsThoseParties()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                },
                new AuthorizedParty
                {
                    Party = "party2",
                    AuthorizedRolesAndAccessPackages = new List<string>() // Empty roles
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties.Should().ContainKey("party1");
        result.ResourcesByParties.Should().NotContainKey("party2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithMultipleRolesPerParty_ConsolidatesResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1", "role2" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" },
            new SubjectResource { Subject = "role1", Resource = "resource2" },
            new SubjectResource { Subject = "role2", Resource = "resource2" }, // Duplicate resource
            new SubjectResource { Subject = "role2", Resource = "resource3" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(3); // Duplicates should be removed
        result.ResourcesByParties["party1"].Should().Contain("resource1");
        result.ResourcesByParties["party1"].Should().Contain("resource2");
        result.ResourcesByParties["party1"].Should().Contain("resource3");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithDirectResourceAuthorizations_AddsDirectResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedResources = new List<string> { "directResource1", "directResource2" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(2);
        result.ResourcesByParties["party1"].Should().Contain("directResource1");
        result.ResourcesByParties["party1"].Should().Contain("directResource2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithDirectResourcesAndConstraints_FiltersDirectResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedResources = new List<string> { "directResource1", "directResource2" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string> { "directResource1" }; // Only directResource1 should be included
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().Contain("directResource1");
        result.ResourcesByParties["party1"].Should().NotContain("directResource2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithBothRoleAndDirectResources_CombinesResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedResources = new List<string> { "directResource1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "roleResource1" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(2);
        result.ResourcesByParties["party1"].Should().Contain("roleResource1");
        result.ResourcesByParties["party1"].Should().Contain("directResource1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithAltinnAppInstances_AddsInstanceIds()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    PartyId = "12345",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app1",
                            InstanceId = "instance1"
                        }
                    }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.AltinnAppInstanceIds.Should().HaveCount(1);
        result.AltinnAppInstanceIds.Should().Contain($"{Constants.ServiceContextInstanceIdPrefix}12345/instance1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithNonAltinnAppInstances_SkipsInstances()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    PartyId = "12345",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = "non-altinn-app-resource",
                            InstanceId = "instance1"
                        }
                    }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.AltinnAppInstanceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithConstrainedAltinnAppInstances_FiltersInstances()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    PartyId = "12345",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app1",
                            InstanceId = "instance1"
                        },
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app2",
                            InstanceId = "instance2"
                        }
                    }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string> { $"{Constants.ServiceResourcePrefix}{Constants.AppResourceIdPrefix}app1" };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.AltinnAppInstanceIds.Should().HaveCount(1);
        result.AltinnAppInstanceIds.Should().Contain($"{Constants.ServiceContextInstanceIdPrefix}12345/instance1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithNoMatchingSubjects_ReturnsEmptyResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "differentRole", Resource = "resource1" } // No matching subject
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().BeEmpty();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithCancellationToken_PassesTokenToGetAllSubjectResources()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var cancellationToken = new CancellationToken(true); // Already cancelled
        var getAllSubjectResourcesCalled = false;
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(token =>
        {
            getAllSubjectResourcesCalled = true;
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new List<SubjectResource>());
        });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AuthorizationHelper.CollapseSubjectResources(
                authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken));

        getAllSubjectResourcesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithLargeDataSet_HandlesPerformanceEfficiently()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = Enumerable.Range(1, 100).Select(i => new AuthorizedParty
            {
                Party = $"party{i}",
                AuthorizedRolesAndAccessPackages = new List<string> { $"role{i}" }
            }).ToList()
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = Enumerable.Range(1, 100).Select(i => new SubjectResource
        {
            Subject = $"role{i}",
            Resource = $"resource{i}"
        }).ToList();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(100);
        foreach (var i in Enumerable.Range(1, 100))
        {
            result.ResourcesByParties[$"party{i}"].Should().Contain($"resource{i}");
        }
    }

    [Fact]
    public async Task CollapseSubjectResources_WithDuplicateSubjects_DeduplicatesCorrectly()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1", "role1" } // Duplicate role
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" },
            new SubjectResource { Subject = "role1", Resource = "resource2" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(2);
        result.ResourcesByParties["party1"].Should().Contain("resource1");
        result.ResourcesByParties["party1"].Should().Contain("resource2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithMixedScenario_HandlesComplexCase()
    {
        // Arrange - Complex scenario with multiple parties, roles, direct resources, and instances
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    PartyId = "12345",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1", "role2" },
                    AuthorizedResources = new List<string> { "directResource1" },
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app1",
                            InstanceId = "instance1"
                        }
                    }
                },
                new AuthorizedParty
                {
                    Party = "party2",
                    PartyId = "67890",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role3" },
                    AuthorizedResources = new List<string> { "directResource2" },
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = "non-altinn-resource",
                            InstanceId = "instance2"
                        }
                    }
                }
            }
        };
        var constraintParties = new List<string> { "party1" }; // Only party1 should be processed
        var constraintResources = new List<string> { "roleResource1", "directResource1", $"{Constants.ServiceResourcePrefix}{Constants.AppResourceIdPrefix}app1" };
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "roleResource1" },
            new SubjectResource { Subject = "role1", Resource = "roleResource2" }, // Will be filtered out
            new SubjectResource { Subject = "role2", Resource = "roleResource3" }, // Will be filtered out
            new SubjectResource { Subject = "role3", Resource = "roleResource4" } // Party2 filtered out
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties.Should().ContainKey("party1");
        result.ResourcesByParties["party1"].Should().HaveCount(2); // roleResource1 + directResource1
        result.ResourcesByParties["party1"].Should().Contain("roleResource1");
        result.ResourcesByParties["party1"].Should().Contain("directResource1");
        result.AltinnAppInstanceIds.Should().HaveCount(1);
        result.AltinnAppInstanceIds.Should().Contain($"{Constants.ServiceContextInstanceIdPrefix}12345/instance1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithMultipleInstancesForSameParty_AddsAllValidInstances()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    PartyId = "12345",
                    AuthorizedRolesAndAccessPackages = new List<string>(),
                    AuthorizedInstances = new List<AuthorizedInstance>
                    {
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app1",
                            InstanceId = "instance1"
                        },
                        new AuthorizedInstance
                        {
                            ResourceId = $"{Constants.AppResourceIdPrefix}app2",
                            InstanceId = "instance2"
                        },
                        new AuthorizedInstance
                        {
                            ResourceId = "non-altinn-resource",
                            InstanceId = "instance3"
                        }
                    }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.AltinnAppInstanceIds.Should().HaveCount(2);
        result.AltinnAppInstanceIds.Should().Contain($"{Constants.ServiceContextInstanceIdPrefix}12345/instance1");
        result.AltinnAppInstanceIds.Should().Contain($"{Constants.ServiceContextInstanceIdPrefix}12345/instance2");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithEmptyGetAllSubjectResourcesResult_HandlesGracefully()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(new List<SubjectResource>()));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().BeEmpty();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithOverlappingDirectAndRoleResources_DeduplicatesCorrectly()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedResources = new List<string> { "resource1" } // Same resource from both role and direct
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" } // Same resource as direct
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1); // Should not duplicate
        result.ResourcesByParties["party1"].Should().Contain("resource1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithNullAuthorizedResources_HandlesGracefully()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedResources = null // Test null handling
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().Contain("resource1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithNullAuthorizedInstances_HandlesGracefully()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedInstances = null // Test null handling
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" }
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().Contain("resource1");
        result.AltinnAppInstanceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CollapseSubjectResources_WithCaseSensitiveResourceMatching_HandlesCorrectly()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string> { "Resource1" }; // Capital R
        var subjectResources = new List<SubjectResource>
        {
            new SubjectResource { Subject = "role1", Resource = "resource1" }, // lowercase r
            new SubjectResource { Subject = "role1", Resource = "Resource1" } // Capital R
        };
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => Task.FromResult(subjectResources));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await AuthorizationHelper.CollapseSubjectResources(
            authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken);

        // Assert
        result.ResourcesByParties.Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().HaveCount(1);
        result.ResourcesByParties["party1"].Should().Contain("Resource1");
        result.ResourcesByParties["party1"].Should().NotContain("resource1");
    }

    [Fact]
    public async Task CollapseSubjectResources_WithExceptionInGetAllSubjectResources_PropagatesException()
    {
        // Arrange
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" }
                }
            }
        };
        var constraintParties = new List<string>();
        var constraintResources = new List<string>();
        var getAllSubjectResources = new Func<CancellationToken, Task<List<SubjectResource>>>(_ => 
            Task.FromException<List<SubjectResource>>(new InvalidOperationException("Test exception")));
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AuthorizationHelper.CollapseSubjectResources(
                authorizedParties, constraintParties, constraintResources, getAllSubjectResources, cancellationToken));
    }
}