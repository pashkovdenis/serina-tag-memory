using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using Serina.TagMemory.Plugin;

namespace Serina.TagMemory
{
    public interface ITagMemoryFactory
    {
        SqlMemoryPlugin BuildPlugin();
        Task<ITagMemoryFactory> WithDbConnector(IDbConnector connector);
        ITagMemoryFactory WithMemoryConfig(MemoryConfig memoryConfig);
    }
}
