# Semantic Kernel Database Query Plugin

## Overview
This plugin enables natural language interaction with databases using **Large Language Models (LLMs)** via **Semantic Kernel**. It translates user queries into **SQL** and executes them against a configured database, supporting **PostgreSQL**, **SQL Server**, and more.

## Features
- **Natural Language to SQL**: Converts user descriptions into executable SQL queries.
- **LLM Model Integration**: Works with models like **Ollama**.
- **Schema Scanning**: Can introspect database structure for improved query generation.
- **Configurable Pipelines**: Leverages **Semantic Kernel** with customizable AI pipelines.
- **Multiple Database Support**: Works with PostgreSQL, SQL Server, etc.
- **Dependency Injection**: Seamlessly integrates with .NET applications.

---

## Installation

1. Install the required NuGet packages:
   ```sh
   dotnet add package Serina.TagMemory.Plugin
   dotnet add package Serina.Semantic.Ai.Pipelines.SemanticKernel
   ```
2. Ensure you have a compatible LLM model (e.g., **Ollama** running at a local or remote endpoint).

---

## Configuration
To use this plugin, configure dependency injection in your **.NET 8** application:

```csharp
var serviceProvider = new ServiceCollection()
    .AddSingleton(new MemoryConfig
    {
        ModelName = "dolphin-llama3:8b",
        Endpoint = "http://192.168.88.104:11434",
        EngineType = EngineType.Ollama,
        EngineTypeDescription = "This is SQLServer use T-SQL syntax",
        ScanSchema = true,
        Examples = new()
        {
            @"SELECT TOP (1000) [UserId], [Username], [Email] FROM [shared].[dbo].[Users]"
        }
    })
    .AddSingleton(new DbConnectionConfig
    {
        ConnectionString = "Host=localhost;Database=mydb;Username=user;Password=pass",
        DbType = "PostgreSQL",
        EnableScaffolding = true
    })
    .AddScoped<SqlMemoryPlugin>()
    .AddTagMemory()
    .AddTransient<IDbConnector>(x => new SqlServerConnector("Server=192.168.88.230;Database=shared;User Id=sa;Password=yourpassword;Encrypt=False;TrustServerCertificate=True;"))
    .AddSingleton<TestService>()
    .BuildServiceProvider();
```

---

## Usage
### Running a Query from Natural Language Input
```csharp
var testService = serviceProvider.GetRequiredService<TestService>();
await testService.RunTestAsync();
```
Inside `TestService`, queries are processed as follows:

```csharp
public async Task RunTestAsync()
{
    var plugin = _factory.WithMemoryConfig(_config)
        .WithDbConnector(_connector).Result
        .BuildPlugin();

    while (true)
    {
        Console.WriteLine("Enter a query description:");
        string userInput = Console.ReadLine() ?? "Show me all shipped orders";
        string jsonResult = await plugin.ProcessQueryAsync(userInput);
        Console.WriteLine("\nGenerated JSON Result:");
        Console.WriteLine(jsonResult);
    }
}
```

---

## How It Works
1. **Receives a Natural Language Query** from the user.
2. **Uses Semantic Kernel Pipelines** to generate an SQL query.
3. **Executes the SQL Query** against the configured database.
4. **Returns Results** as a JSON object.

---

## Building AI Pipelines
The plugin builds **Semantic Kernel pipelines** for query generation and refinement:

```csharp
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
    .AddReducer(new PairedSlidingWindowReducer())
    .WithName("SqlGenerator")
    .Build();
```

---

## Supported Databases
- PostgreSQL
- SQL Server
- (More coming soon...)

---

## Contributing
1. Fork the repository.
2. Create a new branch (`feature-xyz`).
3. Submit a pull request.

---

## License
MIT License - Free for personal and commercial use.

---

## Contact & Support
For questions or issues, open an issue on GitHub or reach out via [your contact details].

