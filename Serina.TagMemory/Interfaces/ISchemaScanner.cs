namespace Serina.TagMemory.Interfaces
{
    public interface ISchemaScanner
    {
        Task<IEnumerable<dynamic>> GetTablesAsync();
        Task<IEnumerable<dynamic>> GetColumnsAsync(string tableName);
    }
}
