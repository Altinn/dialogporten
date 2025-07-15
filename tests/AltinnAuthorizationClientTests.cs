using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AltinnAuthorizationClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IFusionCacheProvider> _cacheProviderMock;
    private readonly Mock<IFusionCache> _pdpCacheMock;
    private readonly Mock<IFusionCache> _partiesCacheMock;
    private readonly Mock<IFusionCache> _subjectResourcesCacheMock;
    private readonly Mock<IUser> _userMock;
    private readonly Mock<IDialogDbContext> _dbContextMock;
    private readonly Mock<ILogger<AltinnAuthorizationClient>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<DialogDbContext> _scopedDbContextMock;
    private readonly Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private readonly HttpClient _httpClient;
    private readonly AltinnAuthorizationClient _sut;

    public AltinnAuthorizationClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _cacheProviderMock = new Mock<IFusionCacheProvider>();
        _pdpCacheMock = new Mock<IFusionCache>();
        _partiesCacheMock = new Mock<IFusionCache>();
        _subjectResourcesCacheMock = new Mock<IFusionCache>();
        _userMock = new Mock<IUser>();
        _dbContextMock = new Mock<IDialogDbContext>();
        _loggerMock = new Mock<ILogger<AltinnAuthorizationClient>>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopedDbContextMock = new Mock<DialogDbContext>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();

        _cacheProviderMock.Setup(x => x.GetCache(nameof(Authorization))).Returns(_pdpCacheMock.Object);
        _cacheProviderMock.Setup(x => x.GetCache(nameof(AuthorizedPartiesResult))).Returns(_partiesCacheMock.Object);
        _cacheProviderMock.Setup(x => x.GetCache(nameof(SubjectResource))).Returns(_subjectResourcesCacheMock.Object);
        
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetRequiredService<DialogDbContext>()).Returns(_scopedDbContextMock.Object);

        _userMock.Setup(x => x.GetPrincipal()).Returns(_claimsPrincipalMock.Object);

        _sut = new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            null!,
            _cacheProviderMock.Object,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("client", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCacheProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            null!,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("cacheProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullUser_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            null!,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("user", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDb_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            _userMock.Object,
            null!,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("db", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            _userMock.Object,
            _dbContextMock.Object,
            null!,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullServiceScopeFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            null!));

        Assert.Equal("serviceScopeFactory", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCacheFromProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var cacheProvider = new Mock<IFusionCacheProvider>();
        cacheProvider.Setup(x => x.GetCache(nameof(Authorization))).Returns((IFusionCache)null!);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AltinnAuthorizationClient(
            _httpClient,
            cacheProvider.Object,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object));

        Assert.Equal("cacheProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        // Act
        var client = new AltinnAuthorizationClient(
            _httpClient,
            _cacheProviderMock.Object,
            _userMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object);

        // Assert
        Assert.NotNull(client);
    }

    #endregion

    #region GetDialogDetailsAuthorization Tests

    [Fact]
    public async Task GetDialogDetailsAuthorization_WithValidDialog_ReturnsAuthorizationResult()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
            Id = Guid.NewGuid(),
            ServiceResource = "test-resource",
            Party = "test-party"
        };
        var expectedResult = new DialogDetailsAuthorizationResult();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
        
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _pdpCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<DialogDetailsAuthorizationResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetDialogDetailsAuthorization(dialogEntity);

        // Assert
        Assert.Equal(expectedResult, result);
        _pdpCacheMock.Verify(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<DialogDetailsAuthorizationResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDialogDetailsAuthorization_WithCancellationToken_PassesTokenToCache()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
            Id = Guid.NewGuid(),
            ServiceResource = "test-resource",
            Party = "test-party"
        };
        var cancellationToken = new CancellationToken();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
        
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _pdpCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<DialogDetailsAuthorizationResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DialogDetailsAuthorizationResult());

        // Act
        await _sut.GetDialogDetailsAuthorization(dialogEntity, cancellationToken);

        // Assert
        _pdpCacheMock.Verify(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<DialogDetailsAuthorizationResult>>>(),
            It.IsAny<TimeSpan?>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDialogDetailsAuthorization_WithNullDialog_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _sut.GetDialogDetailsAuthorization(null!));
    }

    [Fact]
    public async Task GetDialogDetailsAuthorization_CacheThrowsException_PropagatesException()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
            Id = Guid.NewGuid(),
            ServiceResource = "test-resource",
            Party = "test-party"
        };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
        
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _pdpCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<DialogDetailsAuthorizationResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.GetDialogDetailsAuthorization(dialogEntity));
        
        Assert.Equal("Cache error", exception.Message);
    }

    #endregion

    #region GetAuthorizedResourcesForSearch Tests

    [Fact]
    public async Task GetAuthorizedResourcesForSearch_WithValidParameters_ReturnsAuthorizationResult()
    {
        // Arrange
        var constraintParties = new List<string> { "party1", "party2" };
        var serviceResources = new List<string> { "resource1", "resource2" };
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims);

        // Act
        var result = await _sut.GetAuthorizedResourcesForSearch(constraintParties, serviceResources);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ResourcesByParties);
        Assert.NotNull(result.DialogIds);
        Assert.NotNull(result.AltinnAppInstanceIds);
    }

    [Fact]
    public async Task GetAuthorizedResourcesForSearch_WithEmptyConstraints_ReturnsEmptyResult()
    {
        // Arrange
        var constraintParties = new List<string>();
        var serviceResources = new List<string>();
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims);

        // Act
        var result = await _sut.GetAuthorizedResourcesForSearch(constraintParties, serviceResources);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ResourcesByParties);
        Assert.Empty(result.DialogIds);
        Assert.Empty(result.AltinnAppInstanceIds);
    }

    [Fact]
    public async Task GetAuthorizedResourcesForSearch_WithNullConstraintParties_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.GetAuthorizedResourcesForSearch(null!, new List<string>()));
    }

    [Fact]
    public async Task GetAuthorizedResourcesForSearch_WithNullServiceResources_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.GetAuthorizedResourcesForSearch(new List<string>(), null!));
    }

    [Fact]
    public async Task GetAuthorizedResourcesForSearch_WithCancellationToken_PassesTokenThroughChain()
    {
        // Arrange
        var constraintParties = new List<string> { "party1" };
        var serviceResources = new List<string> { "resource1" };
        var cancellationToken = new CancellationToken();
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims);

        // Act
        await _sut.GetAuthorizedResourcesForSearch(constraintParties, serviceResources, cancellationToken);

        // Assert
        _partiesCacheMock.Verify(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region GetAuthorizedParties Tests

    [Fact]
    public async Task GetAuthorizedParties_WithValidParty_ReturnsAuthorizedParties()
    {
        // Arrange
        var partyIdentifier = Mock.Of<IPartyIdentifier>();
        var expectedResult = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "test-party",
                    PartyId = 123,
                    AuthorizedResources = new List<string> { "resource1" },
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedInstances = new List<string> { "instance1" },
                    SubParties = new List<AuthorizedParty>()
                }
            }
        };

        _partiesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.GetAuthorizedParties(partyIdentifier);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Single(result.AuthorizedParties);
        Assert.Equal("test-party", result.AuthorizedParties.First().Party);
    }

    [Fact]
    public async Task GetAuthorizedParties_WithFlattenTrue_ReturnsFlattennedParties()
    {
        // Arrange
        var partyIdentifier = Mock.Of<IPartyIdentifier>();
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "parent-party",
                    PartyId = 123,
                    AuthorizedResources = new List<string> { "resource1" },
                    AuthorizedRolesAndAccessPackages = new List<string> { "role1" },
                    AuthorizedInstances = new List<string> { "instance1" },
                    SubParties = new List<AuthorizedParty>
                    {
                        new AuthorizedParty
                        {
                            Party = "sub-party",
                            PartyId = 456,
                            AuthorizedResources = new List<string> { "resource2" },
                            AuthorizedRolesAndAccessPackages = new List<string> { "role2" },
                            AuthorizedInstances = new List<string> { "instance2" }
                        }
                    }
                }
            }
        };

        _partiesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorizedParties);

        // Act
        var result = await _sut.GetAuthorizedParties(partyIdentifier, flatten: true);

        // Assert
        Assert.Equal(2, result.AuthorizedParties.Count);
        Assert.Contains(result.AuthorizedParties, p => p.Party == "parent-party" && p.ParentParty == null);
        Assert.Contains(result.AuthorizedParties, p => p.Party == "sub-party" && p.ParentParty == "parent-party");
        Assert.All(result.AuthorizedParties, p => Assert.Empty(p.SubParties));
    }

    [Fact]
    public async Task GetAuthorizedParties_WithFlattenFalse_ReturnsHierarchicalParties()
    {
        // Arrange
        var partyIdentifier = Mock.Of<IPartyIdentifier>();
        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>
            {
                new AuthorizedParty
                {
                    Party = "parent-party",
                    PartyId = 123,
                    SubParties = new List<AuthorizedParty>
                    {
                        new AuthorizedParty { Party = "sub-party", PartyId = 456 }
                    }
                }
            }
        };

        _partiesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorizedParties);

        // Act
        var result = await _sut.GetAuthorizedParties(partyIdentifier, flatten: false);

        // Assert
        Assert.Single(result.AuthorizedParties);
        Assert.Single(result.AuthorizedParties.First().SubParties);
        Assert.Equal("sub-party", result.AuthorizedParties.First().SubParties.First().Party);
    }

    [Fact]
    public async Task GetAuthorizedParties_WithNullPartyIdentifier_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.GetAuthorizedParties(null!));
    }

    [Fact]
    public async Task GetAuthorizedParties_WithCancellationToken_PassesTokenToCache()
    {
        // Arrange
        var partyIdentifier = Mock.Of<IPartyIdentifier>();
        var cancellationToken = new CancellationToken();
        var expectedResult = new AuthorizedPartiesResult
        {
            AuthorizedParties = new List<AuthorizedParty>()
        };

        _partiesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _sut.GetAuthorizedParties(partyIdentifier, cancellationToken: cancellationToken);

        // Assert
        _partiesCacheMock.Verify(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region HasListAuthorizationForDialog Tests

    [Fact]
    public async Task HasListAuthorizationForDialog_WithAuthorizedResource_ReturnsTrue()
    {
        // Arrange
        var dialogId = Guid.NewGuid();
        var dialog = new DialogEntity
        {
            Id = dialogId,
            Party = "test-party",
            ServiceResource = "test-resource"
        };
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims, hasResourceAuthorization: true);

        // Act
        var result = await _sut.HasListAuthorizationForDialog(dialog);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasListAuthorizationForDialog_WithSpecificDialogId_ReturnsTrue()
    {
        // Arrange
        var dialogId = Guid.NewGuid();
        var dialog = new DialogEntity
        {
            Id = dialogId,
            Party = "test-party",
            ServiceResource = "test-resource"
        };
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims, hasDialogIdAuthorization: true, targetDialogId: dialogId);

        // Act
        var result = await _sut.HasListAuthorizationForDialog(dialog);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasListAuthorizationForDialog_WithoutAuthorization_ReturnsFalse()
    {
        // Arrange
        var dialogId = Guid.NewGuid();
        var dialog = new DialogEntity
        {
            Id = dialogId,
            Party = "test-party",
            ServiceResource = "test-resource"
        };
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims, hasResourceAuthorization: false);

        // Act
        var result = await _sut.HasListAuthorizationForDialog(dialog);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasListAuthorizationForDialog_WithNullDialog_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.HasListAuthorizationForDialog(null!));
    }

    [Fact]
    public async Task HasListAuthorizationForDialog_WithCancellationToken_PassesTokenThroughChain()
    {
        // Arrange
        var dialog = new DialogEntity
        {
            Id = Guid.NewGuid(),
            Party = "test-party",
            ServiceResource = "test-resource"
        };
        var cancellationToken = new CancellationToken();
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, "test-user"),
            new("urn:altinn:party", "12345")
        };
        
        SetupAuthorizationMocks(claims);

        // Act
        await _sut.HasListAuthorizationForDialog(dialog, cancellationToken);

        // Assert
        _partiesCacheMock.Verify(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region UserHasRequiredAuthLevel Tests

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, true)]
    [InlineData(2, 1, false)]
    [InlineData(3, 3, true)]
    [InlineData(4, 5, true)]
    [InlineData(10, 2, false)]
    public void UserHasRequiredAuthLevel_WithDifferentLevels_ReturnsExpectedResult(
        int minimumLevel, int userLevel, bool expected)
    {
        // Arrange
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(userLevel);

        // Act
        var result = _sut.UserHasRequiredAuthLevel(minimumLevel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UserHasRequiredAuthLevel_WithNegativeMinimumLevel_ReturnsTrue()
    {
        // Arrange
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(0);

        // Act
        var result = _sut.UserHasRequiredAuthLevel(-1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithServiceResource_ReturnsCorrectResult()
    {
        // Arrange
        var serviceResource = "test-resource";
        var minimumAuthLevel = 2;
        var userAuthLevel = 3;
        
        SetupResourcePolicyMock(serviceResource, minimumAuthLevel);
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(userAuthLevel);

        // Act
        var result = await _sut.UserHasRequiredAuthLevel(serviceResource);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithServiceResourceNotFound_ReturnsTrue()
    {
        // Arrange
        var serviceResource = "non-existent-resource";
        var userAuthLevel = 1;
        
        SetupResourcePolicyMock(serviceResource, null); // No policy found
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(userAuthLevel);

        // Act
        var result = await _sut.UserHasRequiredAuthLevel(serviceResource);

        // Assert
        Assert.True(result); // Default minimum auth level 0
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithServiceResourceInsufficientLevel_ReturnsFalse()
    {
        // Arrange
        var serviceResource = "test-resource";
        var minimumAuthLevel = 4;
        var userAuthLevel = 2;
        
        SetupResourcePolicyMock(serviceResource, minimumAuthLevel);
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(userAuthLevel);

        // Act
        var result = await _sut.UserHasRequiredAuthLevel(serviceResource);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithNullServiceResource_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sut.UserHasRequiredAuthLevel(null!));
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithEmptyServiceResource_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sut.UserHasRequiredAuthLevel(string.Empty));
    }

    [Fact]
    public async Task UserHasRequiredAuthLevel_WithCancellationToken_PassesTokenToDatabase()
    {
        // Arrange
        var serviceResource = "test-resource";
        var cancellationToken = new CancellationToken();
        
        SetupResourcePolicyMock(serviceResource, 2);
        _claimsPrincipalMock.Setup(x => x.GetAuthenticationLevel()).Returns(3);

        // Act
        await _sut.UserHasRequiredAuthLevel(serviceResource, cancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.ResourcePolicyInformation, Times.Once);
    }

    #endregion

    #region GetFlattenedAuthorizedParties Tests

    [Fact]
    public void GetFlattenedAuthorizedParties_ShouldFlattenHierarchy()
    {
        // Arrange
        var input = new AuthorizedPartiesResult
        {
            AuthorizedParties =
            [
                new()
                {
                    Party = "parent",
                    AuthorizedRolesAndAccessPackages = ["role1"],
                    AuthorizedResources = ["resource1", "resource2"],
                    SubParties =
                    [
                        new()
                        {
                            Party = "child1",
                            AuthorizedRolesAndAccessPackages = ["role2"],
                            AuthorizedResources = ["resource3"],
                        },
                        new()
                        {
                            Party = "child2",
                            AuthorizedRolesAndAccessPackages = []
                        }
                    ]
                },
                new()
                {
                    Party = "independent",
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedResources = ["resource4"],
                }
            ]
        };

        // Act
        var method = typeof(AltinnAuthorizationClient).GetMethod(
            "GetFlattenedAuthorizedParties",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (AuthorizedPartiesResult)method!.Invoke(null, [input])!;

        // Assert
        Assert.Equal(4, result.AuthorizedParties.Count);
        Assert.Single(result.AuthorizedParties.Where(x => x.Party == "parent"));
        Assert.Single(result.AuthorizedParties.Where(x => x.Party == "child1"));
        Assert.Single(result.AuthorizedParties.Where(x => x.Party == "child2"));
        Assert.Single(result.AuthorizedParties.Where(x => x.Party == "independent"));
        
        // Verify parent party properties
        var parent = result.AuthorizedParties.First(x => x.Party == "parent");
        Assert.Null(parent.ParentParty);
        Assert.Empty(parent.SubParties);
        
        // Verify child parties have parent reference
        var child1 = result.AuthorizedParties.First(x => x.Party == "child1");
        Assert.Equal("parent", child1.ParentParty);
        Assert.Empty(child1.SubParties);
        
        var child2 = result.AuthorizedParties.First(x => x.Party == "child2");
        Assert.Equal("parent", child2.ParentParty);
        Assert.Empty(child2.SubParties);
        
        // Verify independent party
        var independent = result.AuthorizedParties.First(x => x.Party == "independent");
        Assert.Null(independent.ParentParty);
        Assert.Empty(independent.SubParties);
    }

    [Fact]
    public void GetFlattenedAuthorizedParties_WithEmptyInput_ReturnsEmptyResult()
    {
        // Arrange
        var input = new AuthorizedPartiesResult
        {
            AuthorizedParties = []
        };

        // Act
        var method = typeof(AltinnAuthorizationClient).GetMethod(
            "GetFlattenedAuthorizedParties",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (AuthorizedPartiesResult)method!.Invoke(null, [input])!;

        // Assert
        Assert.Empty(result.AuthorizedParties);
    }

    [Fact]
    public void GetFlattenedAuthorizedParties_WithNoSubParties_ReturnsOriginalStructure()
    {
        // Arrange
        var input = new AuthorizedPartiesResult
        {
            AuthorizedParties =
            [
                new()
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = ["role1"],
                    AuthorizedResources = ["resource1"],
                    SubParties = []
                },
                new()
                {
                    Party = "party2",
                    AuthorizedRolesAndAccessPackages = ["role2"],
                    AuthorizedResources = ["resource2"],
                    SubParties = []
                }
            ]
        };

        // Act
        var method = typeof(AltinnAuthorizationClient).GetMethod(
            "GetFlattenedAuthorizedParties",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (AuthorizedPartiesResult)method!.Invoke(null, [input])!;

        // Assert
        Assert.Equal(2, result.AuthorizedParties.Count);
        Assert.All(result.AuthorizedParties, party => Assert.Null(party.ParentParty));
        Assert.All(result.AuthorizedParties, party => Assert.Empty(party.SubParties));
    }

    [Fact]
    public void GetFlattenedAuthorizedParties_WithEmptyCollections_UsesCachedEmptyLists()
    {
        // Arrange
        var input = new AuthorizedPartiesResult
        {
            AuthorizedParties =
            [
                new()
                {
                    Party = "party1",
                    AuthorizedRolesAndAccessPackages = [],
                    AuthorizedResources = [],
                    AuthorizedInstances = [],
                    SubParties = []
                }
            ]
        };

        // Act
        var method = typeof(AltinnAuthorizationClient).GetMethod(
            "GetFlattenedAuthorizedParties",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (AuthorizedPartiesResult)method!.Invoke(null, [input])!;

        // Assert
        Assert.Single(result.AuthorizedParties);
        var party = result.AuthorizedParties.First();
        Assert.Empty(party.AuthorizedRolesAndAccessPackages);
        Assert.Empty(party.AuthorizedResources);
        Assert.Empty(party.AuthorizedInstances);
        Assert.Empty(party.SubParties);
    }

    #endregion

    #region Private Helper Methods

    private void SetupAuthorizationMocks(List<Claim> claims, bool hasResourceAuthorization = false, 
        bool hasDialogIdAuthorization = false, Guid? targetDialogId = null)
    {
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        var authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = hasResourceAuthorization ? 
                new List<AuthorizedParty>
                {
                    new AuthorizedParty
                    {
                        Party = "test-party",
                        AuthorizedResources = new List<string> { "test-resource" }
                    }
                } : new List<AuthorizedParty>()
        };

        _partiesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<AuthorizedPartiesResult>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorizedParties);

        SetupSubjectResourcesMock();
        SetupDialogLabelsMock(hasDialogIdAuthorization, targetDialogId);
    }

    private void SetupSubjectResourcesMock()
    {
        var mockDbSet = new Mock<DbSet<SubjectResource>>();
        var subjectResources = new List<SubjectResource>().AsQueryable();
        mockDbSet.As<IQueryable<SubjectResource>>().Setup(m => m.Provider).Returns(subjectResources.Provider);
        mockDbSet.As<IQueryable<SubjectResource>>().Setup(m => m.Expression).Returns(subjectResources.Expression);
        mockDbSet.As<IQueryable<SubjectResource>>().Setup(m => m.ElementType).Returns(subjectResources.ElementType);
        mockDbSet.As<IQueryable<SubjectResource>>().Setup(m => m.GetEnumerator()).Returns(subjectResources.GetEnumerator());

        _scopedDbContextMock.Setup(x => x.SubjectResources).Returns(mockDbSet.Object);
        _subjectResourcesCacheMock.Setup(x => x.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<List<SubjectResource>>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectResource>());
    }

    private void SetupDialogLabelsMock(bool hasDialogIdAuthorization = false, Guid? targetDialogId = null)
    {
        var mockDialogLabelsDbSet = new Mock<DbSet<DialogServiceOwnerLabel>>();
        var dialogLabels = hasDialogIdAuthorization && targetDialogId.HasValue ?
            new List<DialogServiceOwnerLabel>
            {
                new DialogServiceOwnerLabel
                {
                    Value = "test-instance-id",
                    DialogServiceOwnerContext = new DialogServiceOwnerContext
                    {
                        DialogId = targetDialogId.Value
                    }
                }
            }.AsQueryable() :
            new List<DialogServiceOwnerLabel>().AsQueryable();

        mockDialogLabelsDbSet.As<IQueryable<DialogServiceOwnerLabel>>().Setup(m => m.Provider).Returns(dialogLabels.Provider);
        mockDialogLabelsDbSet.As<IQueryable<DialogServiceOwnerLabel>>().Setup(m => m.Expression).Returns(dialogLabels.Expression);
        mockDialogLabelsDbSet.As<IQueryable<DialogServiceOwnerLabel>>().Setup(m => m.ElementType).Returns(dialogLabels.ElementType);
        mockDialogLabelsDbSet.As<IQueryable<DialogServiceOwnerLabel>>().Setup(m => m.GetEnumerator()).Returns(dialogLabels.GetEnumerator());

        _dbContextMock.Setup(x => x.DialogServiceOwnerLabels).Returns(mockDialogLabelsDbSet.Object);
    }

    private void SetupResourcePolicyMock(string serviceResource, int? minimumAuthLevel)
    {
        var mockDbSet = new Mock<DbSet<ResourcePolicyInformation>>();
        var resourcePolicyInfo = minimumAuthLevel.HasValue ?
            new List<ResourcePolicyInformation>
            {
                new ResourcePolicyInformation
                {
                    Resource = serviceResource,
                    MinimumAuthenticationLevel = minimumAuthLevel.Value
                }
            }.AsQueryable() :
            new List<ResourcePolicyInformation>().AsQueryable();

        mockDbSet.As<IQueryable<ResourcePolicyInformation>>().Setup(m => m.Provider).Returns(resourcePolicyInfo.Provider);
        mockDbSet.As<IQueryable<ResourcePolicyInformation>>().Setup(m => m.Expression).Returns(resourcePolicyInfo.Expression);
        mockDbSet.As<IQueryable<ResourcePolicyInformation>>().Setup(m => m.ElementType).Returns(resourcePolicyInfo.ElementType);
        mockDbSet.As<IQueryable<ResourcePolicyInformation>>().Setup(m => m.GetEnumerator()).Returns(resourcePolicyInfo.GetEnumerator());

        _dbContextMock.Setup(x => x.ResourcePolicyInformation).Returns(mockDbSet.Object);
    }

    #endregion
}

// Helper classes for testing
public class ResourcePolicyInformation
{
    public string Resource { get; set; } = string.Empty;
    public int MinimumAuthenticationLevel { get; set; }
}

public class DialogServiceOwnerLabel
{
    public string Value { get; set; } = string.Empty;
    public DialogServiceOwnerContext DialogServiceOwnerContext { get; set; } = new();
}

public class DialogServiceOwnerContext
{
    public Guid DialogId { get; set; }
}