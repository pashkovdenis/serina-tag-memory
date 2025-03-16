using Serina.TagMemory;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Models;
using Serina.TagMemory.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagMemorySample
{
    internal class TestService
    {
        private readonly ITagMemoryFactory _factory;
        private readonly IDbConnector _connector;
        private readonly MemoryConfig _config;

        public TestService(
            ITagMemoryFactory factory, 
            IDbConnector connector, 
            MemoryConfig config)
        {
            _factory = factory;
            _connector = connector;
            _config = config;
        }

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
                Console.WriteLine(" ");

            }

        }

    }
}
