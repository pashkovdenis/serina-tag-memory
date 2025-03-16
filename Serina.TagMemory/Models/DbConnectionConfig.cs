namespace Serina.TagMemory.Models
{
    public  class DbConnectionConfig
    {
        public string ConnectionString { get; set; } = "";
        public string DbType { get; set; } = ""; // e.g., "PostgreSQL", "SQLServer"
        public bool EnableScaffolding { get; set; } = false;
    }
}
