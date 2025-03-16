namespace Serina.TagMemory.Interfaces
{
    public interface IAiService
    {
        Task<string> TransformInputToSqlAsync(string input);
    }
}
