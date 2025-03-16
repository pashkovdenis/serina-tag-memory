using Dapper;
using Microsoft.Data.SqlClient;
using Serina.TagMemory.Interfaces;
using System.Data;

namespace Serina.TagMemory.Connectors
{
    public class SqlServerConnector : IDbConnector
    {
        private readonly string _connectionString;

        public SqlServerConnector(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<IDbConnection> CreateConnectionAsync()
        {
            var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        public async Task<IEnumerable<dynamic>> ExecuteQueryAsync(string query)
        {
            using var connection = await CreateConnectionAsync();
            Console.WriteLine(query);
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
