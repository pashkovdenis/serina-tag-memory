using Dapper;
using Npgsql;
using Serina.TagMemory.Interfaces;
using System.Data;

namespace Serina.TagMemory.Connectors
{
    public class PostgresConnector : IDbConnector
    {
        private readonly string _connectionString;

        public PostgresConnector(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<IDbConnection> CreateConnectionAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string query)
        {
            using var connection = await CreateConnectionAsync();
           
            return await connection.QueryAsync(query);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await CreateConnectionAsync();
                return connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }

}
