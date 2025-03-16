using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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

        private static Dictionary<int, string> _cache = new();

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
            if (_cache.ContainsKey(input.GetHashCode()))
            {
                return _cache[input.GetHashCode()];
            }

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

            var context = new PipelineContext
            {
                AutoFunction = false,
                EnableFunctions = false,

                RequestMessage = new RequestMessage(systemPromptTrasnformer, MessageRole.System, Guid.NewGuid())
                {
                    Temperature = 0.1,
                    History = new List<RequestMessage> {
                         new RequestMessage(systemPromptTrasnformer, MessageRole.System, Guid.NewGuid()),
                         new RequestMessage(input, MessageRole.User, Guid.NewGuid())

                    }.ToArray()


                }
            };

            await pipeline.ExecuteStepAsync(context, default);

            var result = context.Response.Content;




            // try to execute the query :  
            if (!string.IsNullOrEmpty(result))
            {

                IEnumerable<dynamic> objList = default;

                try
                {
                    objList = await _dbConnector.ExecuteQueryAsync(result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Tag error " + ex.Message);
                    return string.Empty;
                }

                var json = JsonSerializer.Serialize(objList);

                // try to make it more human  

                var response = await HumanizeAsync(json, input);


                _cache.Add(input.GetHashCode(), response);

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
";

            var context = new PipelineContext
            {
                AutoFunction = false,
                EnableFunctions = false,
                RequestMessage = new RequestMessage(prompt, MessageRole.System, Guid.NewGuid())
                {

                    History = new List<RequestMessage> {
                         new RequestMessage(prompt, MessageRole.System, Guid.NewGuid()),
                         new RequestMessage(@$"
                            ***json***
                            {json}
                            *** Input ***
                            {input}

                        ", MessageRole.User, Guid.NewGuid())

                    }.ToArray()


                }
            };

            await pipeline.ExecuteStepAsync(context, default);

            var result = context.Response.Content;


            return result;

        }


    }
}
