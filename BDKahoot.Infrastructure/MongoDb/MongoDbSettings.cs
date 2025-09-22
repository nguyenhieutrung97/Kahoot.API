namespace BDKahoot.Infrastructure.MongoDb
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
        public bool UseInMemory { get; set; } = false;
    }
}
