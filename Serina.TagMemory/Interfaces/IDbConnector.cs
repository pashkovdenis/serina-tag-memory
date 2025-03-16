namespace Serina.TagMemory.Interfaces
{
    public interface IDbConnector
    {
        Task<IEnumerable<dynamic>> ExecuteQueryAsync(string query);
        Task<bool> TestConnectionAsync();

    }
}
