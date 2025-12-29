using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddDialogportenGraphQlTestClient()
            .ConfigureHttpClient(c =>
                c.BaseAddress = new Uri("http://localhost:5181/graphql"));

        IServiceProvider services = serviceCollection.BuildServiceProvider();

        var client = services.GetRequiredService<IDialogportenGraphQlTestClient>();

        // var dialogId = Guid.Parse("019ad4a0-9df9-71f0-85e1-883d2b551650");
        var dialogId = Guid.NewGuid();

        var foo = await client.GetDialogById.ExecuteAsync(dialogId);

        foo.Data.Should().NotBeNull();
        foo.Data.DialogById.Errors.Should().NotBeNull();
        foo.Data.DialogById.Errors
            .Should().ContainSingle().Which.Message.Should().Contain(dialogId.ToString())
            .And.GetType().Should().BeOfType(typeof(GetDialogById_DialogById_Errors_DialogByIdNotFound));
    }
}
