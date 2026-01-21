using Dapper;
using Npgsql;
using System.Data;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal readonly struct PostgresArray<T>(T[] value) : SqlMapper.ICustomQueryParameter
{
    public void AddParameter(IDbCommand command, string name)
    {
        var param = new NpgsqlParameter(name, value);
        command.Parameters.Add(param);
    }
}
