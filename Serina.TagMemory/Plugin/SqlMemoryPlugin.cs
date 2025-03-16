using Microsoft.SemanticKernel;
using Serina.TagMemory.Interfaces;
using System.ComponentModel;

namespace Serina.TagMemory.Plugin
{
    /// <summary>
    /// Semantic Kernel Memory plugin for sql databases 
    /// </summary>
    public sealed class SqlMemoryPlugin
    {
        private readonly IAiService _aiService;  

        public SqlMemoryPlugin(  IAiService aiService)
        {
            _aiService = aiService; 
        }

        [KernelFunction, Description("Get information from sql database.")]
        public async Task<string> ProcessQueryAsync(
             [Description("The user-provided natural language query that needs to be converted into SQL.")] string input)
        {
            var response = await _aiService.TransformInputToSqlAsync(input);

            return response;
        }

    }
}
