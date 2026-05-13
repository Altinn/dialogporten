using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Npgsql;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogSearchRepositoryTests
{
    [Fact]
    public void CreateUpsertFreeTextSearchIndexCommand_Should_Apply_Configured_Command_Timeout()
    {
        using var connection = new NpgsqlConnection();

        using var command = DialogSearchRepository.CreateUpsertFreeTextSearchIndexCommand(
            connection,
            Guid.CreateVersion7(),
            commandTimeoutSeconds: 17);

        command.CommandTimeout.Should().Be(17);
    }

    [Fact]
    public void CreateUpsertFreeTextSearchIndexCommand_Should_Keep_Provider_Default_Command_Timeout_When_Configured_Timeout_Is_0()
    {
        using var connection = new NpgsqlConnection();
        using var defaultCommand = new NpgsqlCommand();

        using var command = DialogSearchRepository.CreateUpsertFreeTextSearchIndexCommand(
            connection,
            Guid.CreateVersion7(),
            commandTimeoutSeconds: 0);

        command.CommandTimeout.Should().Be(defaultCommand.CommandTimeout);
    }

    [Fact]
    public void CreateUpsertFreeTextSearchIndexCommand_Should_Keep_Provider_Default_Command_Timeout_When_Configured_Timeout_Is_Null()
    {
        using var connection = new NpgsqlConnection();
        using var defaultCommand = new NpgsqlCommand();

        using var command = DialogSearchRepository.CreateUpsertFreeTextSearchIndexCommand(
            connection,
            Guid.CreateVersion7(),
            commandTimeoutSeconds: null);

        command.CommandTimeout.Should().Be(defaultCommand.CommandTimeout);
    }
}
