using Microsoft.Extensions.DependencyInjection;
using Serina.Semantic.Ai.Pipelines.Filters;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Utils;
 
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using Serina.TagMemory.Plugin;
using Serina.TagMemory.Services;

namespace Serina.TagMemory
{
    public class TagMemoryFactory(IServiceProvider serviceProvider) : ITagMemoryFactory
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private MemoryConfig _config; 

        public ITagMemoryFactory WithMemoryConfig(MemoryConfig memoryConfig)
        {
            _config = memoryConfig;

            memoryConfig.Validate();

            return this;
        }

        private IDbConnector _dbConnector;
        public async Task<ITagMemoryFactory> WithDbConnector(IDbConnector connector)
        {
            _dbConnector = connector;

            if (!await _dbConnector.TestConnectionAsync())
            {
                throw new InvalidOperationException("Db connector was unable to connect to database");
            }

            return this;
        }
        
        public SqlMemoryPlugin BuildPlugin()
        {
            BuildPipelines();

            var plugin = new SqlMemoryPlugin(
                 new AiService(_config, _serviceProvider.GetRequiredService<ISchemaScanner>(), 
                 _serviceProvider.GetRequiredService<IDbConnector>()));

            return plugin;
        }

        private void BuildPipelines()
        {
            if (_config == default)
            {
                throw new InvalidOperationException("config not provided");
            }

            const string SqlGeneratorAgentName = "SqlGenerator";
            const string SqlRefinerAgentName = "SqlRefiner";

            if (!PipelineRegistry.Exists(SqlGeneratorAgentName))
            {

                PipelineBuilder.New().New(new SimpleChatStep())
                                    .WithKernel(new SemanticKernelOptions
                                    {
                                        Models = new List<SemanticModelOption>
                                        {
                                         new SemanticModelOption
                                         {
                                               Endpoint = _config.Endpoint,
                                               Name = _config.ModelName,
                                               Key = _config.Key,
                                               EngineType = (int)_config.EngineType
                                         }
                                        }
                                    })
                                    .AddFilter(new ClearTextFilter())
                                    .AddFilter(new TextChunkerFilter())
                                   
                                    .AttachKernel()
                                    .WithName(SqlGeneratorAgentName)
                                    .Build();


                PipelineBuilder.New().New(new SimpleChatStep())
                                  .WithKernel(new SemanticKernelOptions
                                  {
                                      Models = new List<SemanticModelOption>
                                      {
                                         new SemanticModelOption
                                         {
                                               Endpoint = _config.Endpoint,
                                               Name = _config.ModelName,
                                               Key = _config.Key,
                                               EngineType = (int)_config.EngineType
                                         }
                                      }
                                  })
                                  .AttachKernel()
                                  .WithName(SqlRefinerAgentName)
                                  .Build();

            } 
        }
    }
}
