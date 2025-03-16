using Serina.TagMemory.Interfaces;

namespace Serina.TagMemory.SqlBuilder
{
    public class SchemaScanner : ISchemaScanner
    {
        private readonly IDbConnector _dbConnector;

        public string DefaultSchema { get; set; } = "public"; 

        public SchemaScanner(IDbConnector dbConnector)
        {
            _dbConnector = dbConnector;
        }

        public async Task<IEnumerable<dynamic>> GetTablesAsync()
        {
            string query = $"SELECT table_name FROM information_schema.tables ";
            return await _dbConnector.ExecuteQueryAsync(query) ;
        }

        public async Task<IEnumerable<dynamic>> GetColumnsAsync(string tableName)
        {
            string query = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}';";
            return await _dbConnector.ExecuteQueryAsync(query);
        }
    }
}
