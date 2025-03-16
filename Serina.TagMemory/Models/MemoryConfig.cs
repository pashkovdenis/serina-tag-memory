using Serina.Semantic.Ai.Pipelines.SemanticKernel;

namespace Serina.TagMemory.Models
{
    public sealed class MemoryConfig
    {
        public string ModelName { get; set; } = ""; 

        public string Endpoint { get; set; } = "";

        public string Key { get; set; } = "1234567890";

        public List<string> Examples { get; set; } = new();

        public EngineType EngineType { get; set; } = EngineType.Ollama;

        public bool ScanSchema { get; set; } = false;

        public string EngineTypeDescription { get; set; }


        public void Validate()
        {
            if (EngineType == EngineType.Ollama && string.IsNullOrEmpty(Endpoint))
            {
                throw new ArgumentException("Endpoint is requreid for local models", nameof(Endpoint));
            }
            else if (string.IsNullOrEmpty(Key) && EngineType != EngineType.Azure)
            {
                throw new ArgumentException("Endpoint is requreid for local models", nameof(Key));
            }

            if (string.IsNullOrEmpty(ModelName))
            {
                throw new ArgumentException("Model Name is required", nameof(ModelName));
            }

            if (ScanSchema == false && Examples.Any() == false)
            {
                throw new ArgumentException("Since scan schema is disabled please provide some example how Ai needs to select from which tables", nameof(Examples));

            }

        }

    }
}
