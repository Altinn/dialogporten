using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

internal sealed class GuidArrayTypeHandler : SqlMapper.TypeHandler<Guid[]>
{
    public override void SetValue(IDbDataParameter parameter, Guid[]? value)
    {
        var npgsqlParameter = (NpgsqlParameter)parameter;
        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Uuid;
        npgsqlParameter.Value = value;
    }

    public override Guid[] Parse(object value) => (Guid[])value;
}
