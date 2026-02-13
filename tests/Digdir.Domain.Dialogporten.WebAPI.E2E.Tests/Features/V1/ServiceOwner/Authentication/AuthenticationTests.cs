using System.Net;
using System.Reflection;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Refit;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authentication;

[Collection(nameof(WebApiTestCollectionFixture))]
public class AuthenticationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<AuthScenario, EndpointScenario> AuthenticationCases => BuildAuthenticationCases();

    [E2ETheory]
    [MemberData(nameof(AuthenticationCases))]
    public async Task Should_Return_401_With_Expected_WwwAuthenticate_Header(
        AuthScenario authScenario,
        EndpointScenario endpointScenario)
    {
        using var _ = Fixture.UseServiceOwnerTokenOverrides(tokenOverride: authScenario.TokenOverride);

        var response = await endpointScenario.Call(Fixture.ServiceownerApi, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        response.Headers.Should().NotBeNull();
        var hasAuthenticateHeader = response.Headers.TryGetValues("WWW-Authenticate", out var authenticateHeaders);
        hasAuthenticateHeader.Should().BeTrue();

        var authenticateHeaderValue = string.Join(',', authenticateHeaders ?? []);
        authenticateHeaderValue.Should().Contain("Bearer");
        authenticateHeaderValue.Should().Contain(authScenario.ExpectedAuthenticateHeaderFragment);
    }

    private static TheoryData<AuthScenario, EndpointScenario> BuildAuthenticationCases()
    {
        var authScenarios = new[]
        {
            new AuthScenario(
                Name: "missing token",
                TokenOverride: string.Empty,
                ExpectedAuthenticateHeaderFragment: "Bearer"),
            new AuthScenario(
                Name: "malformed token",
                TokenOverride: "thisisnotavalidtoken",
                ExpectedAuthenticateHeaderFragment: "error=\"invalid_token\"")
        };

        var endpointScenarios = typeof(IServiceownerApi)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsRefitHttpMethod)
            .OrderBy(method => method.Name)
            .Select(method => new EndpointScenario(
                Name: method.Name,
                Call: (api, cancellationToken) =>
                    InvokeEndpointAsync(api, method, cancellationToken)))
            .ToArray();

        var theoryData = new TheoryData<AuthScenario, EndpointScenario>();

        foreach (var authScenario in authScenarios)
        {
            foreach (var endpointScenario in endpointScenarios)
            {
                theoryData.Add(authScenario, endpointScenario);
            }
        }

        return theoryData;
    }

    private static async Task<IApiResponse> InvokeEndpointAsync(
        IServiceownerApi serviceownerApi,
        MethodInfo method,
        CancellationToken cancellationToken)
    {
        var arguments = method.GetParameters()
            .Select(parameter => CreateArgument(parameter.ParameterType, [], cancellationToken))
            .ToArray();

        var invocationResult = method.Invoke(serviceownerApi, arguments)
            ?? throw new InvalidOperationException($"Invocation returned null for method '{method.Name}'.");

        if (invocationResult is not Task task)
        {
            throw new InvalidOperationException($"Method '{method.Name}' did not return a Task.");
        }

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty?.GetValue(task) is not IApiResponse response)
        {
            throw new InvalidOperationException($"Method '{method.Name}' did not return IApiResponse.");
        }

        return response;
    }

    private static bool IsRefitHttpMethod(MethodInfo method) =>
        method.GetCustomAttributes(inherit: true)
            .Any(attribute => attribute is
                GetAttribute or
                PostAttribute or
                PutAttribute or
                PatchAttribute or
                DeleteAttribute or
                HeadAttribute or
                OptionsAttribute);

    private static object? CreateArgument(
        Type parameterType,
        HashSet<Type> creationStack,
        CancellationToken cancellationToken)
    {
        if (!creationStack.Add(parameterType))
        {
            return parameterType.IsValueType
                ? Activator.CreateInstance(parameterType)
                : null;
        }

        try
        {
            if (parameterType == typeof(CancellationToken))
            {
                return cancellationToken;
            }

            if (parameterType == typeof(Guid))
            {
                return Guid.NewGuid();
            }

            if (Nullable.GetUnderlyingType(parameterType) is not null)
            {
                return null;
            }

            if (parameterType == typeof(string))
            {
                return "autogenerated";
            }

            if (parameterType.IsEnum || parameterType.IsValueType)
            {
                return Activator.CreateInstance(parameterType);
            }

            if (parameterType.IsArray)
            {
                var elementType = parameterType.GetElementType()
                    ?? throw new InvalidOperationException($"Unable to determine element type for '{parameterType}'.");
                return Array.CreateInstance(elementType, 0);
            }

            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = parameterType.GetGenericArguments()[0];
                return Array.CreateInstance(elementType, 0);
            }

            var defaultCtor = parameterType.GetConstructor(Type.EmptyTypes);
            if (defaultCtor is not null)
            {
                return Activator.CreateInstance(parameterType)
                    ?? throw new InvalidOperationException($"Unable to instantiate argument of type '{parameterType}'.");
            }

            var constructor = parameterType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(ctor => ctor.GetParameters().Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Unable to instantiate argument of type '{parameterType}' because no public constructors were found.");

            var constructorArguments = constructor.GetParameters()
                .Select(parameter => CreateArgument(parameter.ParameterType, creationStack, cancellationToken))
                .ToArray();

            return constructor.Invoke(constructorArguments);
        }
        finally
        {
            _ = creationStack.Remove(parameterType);
        }
    }

    public sealed record AuthScenario(
        string Name,
        string TokenOverride,
        string ExpectedAuthenticateHeaderFragment)
    {
        public override string ToString() => Name;
    }

    public sealed record EndpointScenario(
        string Name,
        Func<IServiceownerApi, CancellationToken, Task<IApiResponse>> Call)
    {
        public override string ToString() => Name;
    }
}
