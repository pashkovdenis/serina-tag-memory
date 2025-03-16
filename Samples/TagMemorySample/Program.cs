using Microsoft.Extensions.DependencyInjection;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.TagMemory.Connectors;
using Serina.TagMemory.Extensions;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using Serina.TagMemory.Plugin;

namespace TagMemorySample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing TAG Plugin using local ollama model");

            // Configure Dependency Injection
            var serviceProvider = new ServiceCollection()

                // Register configurations

                .AddSingleton(new MemoryConfig
                {
                    ModelName = "mistral",
                    Endpoint = "http://127.0.0.1:11434",
                    EngineType = EngineType.Ollama, 
                    EngineTypeDescription = "This is SQLServer use T-SQL syntax",
                     ScanSchema = true,
                    Examples = new()
                    {
                        

                        @"SELECT TOP (1000) [UserId]
      ,[Username]
      ,[Email]
      ,[PasswordHash]
      ,[CreatedAt]
        FROM [shared].[dbo].[Users]
        
        database type is SQLServer

"

                    }
                })

                .AddSingleton(new DbConnectionConfig
                {
                    ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass",
                    DbType = "PostgreSQL",
                    EnableScaffolding = true
                })
                .AddScoped<SqlMemoryPlugin>()
                // Register services
                .AddTagMemory()
                .AddTransient<IDbConnector>(x => new SqlServerConnector("Server=192.168.88.230;Database=shared;User Id=sa;Password=Rommie055alpha;Encrypt=False;TrustServerCertificate=True;"))
                .AddSingleton<TestService>()
                .BuildServiceProvider();

            // Resolve and run the test
            var testService = serviceProvider.GetRequiredService<TestService>();
            await testService.RunTestAsync();
        }
    }
}
