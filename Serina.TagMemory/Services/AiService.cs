using Polly;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Serina.TagMemory.Services
{
    public class AiService : IAiService
    {
        private readonly MemoryConfig _config;
        private readonly ISchemaScanner _schemaScanner;
        private readonly IDbConnector _dbConnector;

        // Agent const agent names. 
        private const string SqlGeneratorAgentName = "SqlGenerator";
        private const string SqlRefinerAgentName = "SqlRefiner";

        public int RetryFixCount { get; set; } = 10;  


        private readonly List<RequestMessage> _generatorHistory = new(); 
        private readonly List<RequestMessage> _refinerHistory = new();


        public AiService(MemoryConfig config,
            ISchemaScanner schemaScanner,
            IDbConnector dbConnector)
        {
            _config = config;
            _schemaScanner = schemaScanner;
            _dbConnector = dbConnector;
        }

        public async Task<string> TransformInputToSqlAsync(string input)
        {
            
            var schema_information = new StringBuilder();

            if (_config.Examples.Any())
            {
                schema_information.AppendLine("Here are some samples of the queries and tables: ");

                foreach (var example in _config.Examples)
                {
                    schema_information.AppendLine(example);
                }
            }

            if (_config.ScanSchema)
            {
                var tables = await _schemaScanner.GetTablesAsync();

                foreach (var table in tables)
                {
                    schema_information.AppendLine("Table: " + table.table_name);
                    schema_information.AppendLine("Columns: ");

                    var columns = await _schemaScanner.GetColumnsAsync((string)table.table_name);

                    foreach (var column in columns)
                    {
                        schema_information.AppendLine(column.column_name);
                    }
                }
            }

            var systemPromptTrasnformer = $@"
You are an AI SQL Assistant responsible for generating valid and optimized SQL queries from user input. 

Your outputs must follow this structured format:

<thinking>
- Analyze the user query and determine the required database operation (SELECT, INSERT, UPDATE, DELETE).
- Identify the relevant table(s) and column(s) based on the provided schema details.
- Construct an efficient SQL query following best practices.
- Ensure SQL syntax correctness and prevent SQL injection vulnerabilities.
</thinking>

<reflection>
- Verify if the query is syntactically correct.
- Ensure all referenced tables and columns exist in the provided schema.
- Optimize the query if possible (e.g., adding WHERE conditions to limit data, indexing recommendations).
- Confirm that the query achieves the intended user request without unnecessary complexity.
</reflection>

<output>
Provide only the final SQL query in a clean and executable format.
</output>

### **Additional Guidelines**
- **DO NOT** include any explanations, comments, or extra text in the final output.
- **DO NOT** generate queries for tables or columns that do not exist in the provided schema.
- **DO NOT** execute the query—only return the SQL statement.
- ** Return only SQL without any quotes etc
- If the user request is ambiguous, assume reasonable defaults and structure the query accordingly.

{_config.EngineTypeDescription}

### **Database Schema Details**
{schema_information}  
 ";

            var pipeline = PipelineRegistry.Get(SqlGeneratorAgentName);


            if (!_generatorHistory.Any())
            {
                _generatorHistory.Add(new RequestMessage(systemPromptTrasnformer, MessageRole.System, Guid.NewGuid())); 

            }

            _generatorHistory.Add(new RequestMessage(input, MessageRole.User, Guid.NewGuid())); 


            var context = new PipelineContext
            {
                AutoFunction = false,
                EnableFunctions = false,

                RequestMessage = new RequestMessage(systemPromptTrasnformer, MessageRole.System, Guid.NewGuid())
                {
                    Temperature = 0.1,
                    History = _generatorHistory.ToArray() 
                }
            };

            await pipeline.ExecuteStepAsync(context, default);

            var result = context.Response.Content;

            if (result.Contains("<output>"))
            {
                var mt = Regex.Match(result, @"<output>(.*?)</output>", RegexOptions.Singleline).Groups[1].Value;

                result = mt;
            }

            if (result.Contains("```sql"))
            {
                result = result.Split("```sql").Last().Split("```").First().Trim();
            }

            _generatorHistory.Add(new RequestMessage(result, MessageRole.Bot, Guid.NewGuid()));

            // try to execute the query :  
            if (!string.IsNullOrEmpty(result))
            {
                _refinerHistory.Clear();
                IEnumerable<dynamic> objList = default;

                // Define Polly Retry Policy (up to 5 retries)
                var retryPolicy = Policy
                    .Handle<Exception>() // Catch any SQL-related exceptions
                    .WaitAndRetryAsync(RetryFixCount , attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), async (exception, timeSpan, attempt, context) =>
                    {
                        Console.WriteLine($"Retrying due to SQL Error: {exception.Message} (Attempt {attempt})");

                        // Ask AI to refine query based on error message
                        result = await RefineSqlQueryAsync(input, result, exception.Message);


                    });
  
                try
                {
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        objList = await _dbConnector.ExecuteQueryAsync(result);
                    });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Tag error " + ex.Message);
                    Console.WriteLine("error " + ex.Message);
                    return string.Empty;
                }


                var json = JsonSerializer.Serialize(objList);

                var response = await HumanizeAsync(json, input);
 
                return response;
            }

            return string.Empty;
        }


        /// <summary>
        /// Take json  and return humanized text 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private async ValueTask<string> HumanizeAsync(string json, string input)
        {

            var pipeline = PipelineRegistry.Get(SqlGeneratorAgentName);

            var prompt = @$"
You will receive a JSON data structure and a user question.  
Your task is to analyze the JSON and generate a clear, human-friendly response based on the user's query.  
If the question requests multiple results (e.g., 'count:10'), ensure you provide the correct number of entries. 
Do not return a single result unless explicitly asked.
Provide only short answer max 10 words.
";

            if (!_refinerHistory.Any())
            {
                _refinerHistory.Add(new RequestMessage(prompt, MessageRole.System, Guid.NewGuid()));
            }

            _refinerHistory.Add(new RequestMessage(@$"
                            ***json***
                            {json}
                            *** Input ***
                            {input}

                        ", MessageRole.User, Guid.NewGuid()));

            var context = new PipelineContext
            {
                AutoFunction = false,
                EnableFunctions = false,
                RequestMessage = new RequestMessage(prompt, MessageRole.System, Guid.NewGuid())
                {

                    History = _refinerHistory.ToArray() 

                }
            };

            await pipeline.ExecuteStepAsync(context, default);

            var result = context.Response.Content;
            _refinerHistory.Add(new RequestMessage(result, MessageRole.Bot, Guid.NewGuid()));


            return result;

        }








        // 🔹 **Function to Refine Query Based on Error**
        private async Task<string> RefineSqlQueryAsync(string input, string previousQuery, string errorMessage)
        {
            var pipeline = PipelineRegistry.Get(SqlRefinerAgentName);
            
            var prompt = @$"
You generated an SQL query, but it caused an error. Your task is to analyze the error and fix the SQL query.
Think step by step but only keep a minimum draft for each thinking step, with 5 words at most. 
Pay attention on errors if id then add id or use max id + 1.
Make the necessary corrections and return only the fixed SQL query. Return only the answer at the end of the response after a separator ```sql  {{response}} ```
 
";

            if (!_refinerHistory.Any())
            {
                _refinerHistory.Add(new RequestMessage(prompt, MessageRole.System, Guid.NewGuid()));
            }


            var request = $@"
            Fix this generated sql: 
            {previousQuery}
            according to error: {errorMessage} 
            user request was : {input}

            ";

            _refinerHistory.Add(new RequestMessage(request, MessageRole.User, Guid.NewGuid()));

            var context = new PipelineContext
            {
                AutoFunction = false,
                EnableFunctions = false,
                
                RequestMessage = new RequestMessage("", MessageRole.System, Guid.NewGuid())
                {
                    History = _refinerHistory.ToArray(),
                    Temperature = 0.5
                }
            };

            await pipeline.ExecuteStepAsync(context, default);
            var result = context.Response.Content;

            if (result.Contains("<output>"))
            {
                result = Regex.Match(result, @"<output>(.*?)</output>", RegexOptions.Singleline).Groups[1].Value;
            }

            if (result.Contains("```sql"))
            {
                result = result.Split("```sql").Last().Split("```").First().Trim();
            }

            _refinerHistory.Add(new RequestMessage(result, MessageRole.Bot, Guid.NewGuid()));

            return result;
        }













    }






}
