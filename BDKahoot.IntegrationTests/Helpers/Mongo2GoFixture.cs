using BDKahoot.Infrastructure.MongoDb;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;
using System;

public class MongoDbFixture : IDisposable
{
    private readonly MongoDbRunner _runner;
    public string ConnectionString => _runner.ConnectionString;
    public string DatabaseName { get; } = "BDKahoot_TestDb";
    public IMongoDatabase Database { get; }

    public MongoDbFixture()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        Database = client.GetDatabase(DatabaseName);
    }

    public MongoDbContext GetDbContext()
    {
        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName
        });
        return new MongoDbContext(settings);
    }

    public void Dispose()
    {
        _runner.Dispose();
    }
}
