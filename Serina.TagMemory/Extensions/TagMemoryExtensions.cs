using Microsoft.Extensions.DependencyInjection;
using Serina.TagMemory.Interfaces;
using Serina.TagMemory.Services;
using Serina.TagMemory.SqlBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serina.TagMemory.Extensions
{
    public static class TagMemoryExtensions
    {
         
        /// <summary>
        /// Register tag memory extensions
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTagMemory(this IServiceCollection services)
        {

            services.AddTransient<ITagMemoryFactory, TagMemoryFactory>();
     
            services.AddTransient<IAiService, AiService>(); 

            services.AddTransient<ISchemaScanner, SchemaScanner>();
             
            return services;
        }
         
    }
}
