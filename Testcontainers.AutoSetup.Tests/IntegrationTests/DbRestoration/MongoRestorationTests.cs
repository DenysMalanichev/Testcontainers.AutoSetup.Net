using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.AutoSetup.Core.Attributes;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Tests.IntegrationTests.TestCollections;
using Xunit.Abstractions;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.DbRestoration;

[DbReset]
[Trait("Category", "Integration")]
[Collection(nameof(ParallelIntegrationTestsCollection))]
public class MongoRestorationTests : IntegrationTestsBase
{
    private readonly ITestOutputHelper _output;

    public MongoRestorationTests(ContainersFixture fixture, ITestOutputHelper outputHelper)
         : base(fixture)
    {
        _output = outputHelper;
    }

    [Fact]
    public async Task MongoDbRestorer_WithMongoContainerBuilder_MigratesDatabase()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        var client = new MongoClient(Setup.MongoContainerFromSpecificBuilder.GetConnectionString()); 
        var dbSetup = (RawMongoDbSetup)Setup.MongoContainer_FromSpecificBuilder_RawMongoDbSetup!;
        var collection = client.GetDatabase(dbSetup.DbName).GetCollection<BsonDocument>(dbSetup.MongoFiles.Keys.First());
        var filter = Builders<BsonDocument>.Filter.Empty;

        // Act 
        var count = await collection.CountDocumentsAsync(filter);

        // Assert
        Assert.NotNull(Setup.MongoContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MongoContainerFromSpecificBuilder.State);

        Assert.True(count > 0, "No migrations were applied, collection is empty.");
    }

    [Fact]
    public async Task MongoDbRestorer_WithMongoContainerBuilder_ReseedsDb()
    {
        // Containers setup and seeding are done within the GlobalTestSetup
        // Arrange
        var client = new MongoClient(Setup.MongoContainerFromSpecificBuilder.GetConnectionString()); 
        var dbSetup = (RawMongoDbSetup)Setup.MongoContainer_FromSpecificBuilder_RawMongoDbSetup!;
        var collection = client.GetDatabase(dbSetup.DbName).GetCollection<BsonDocument>(dbSetup.MongoFiles.Keys.First());

        var filter = Builders<BsonDocument>.Filter.Eq("name", "test");

        // Insert
        await collection.InsertOneAsync(new BsonDocument { { "name", "test" } });

        // Act
        await Setup.ResetEnvironmentAsync(this.GetType());
        var testDocumentsCount = await collection.CountDocumentsAsync(filter);
        var totalCount = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        // Assert
        Assert.NotNull(Setup.MongoContainerFromSpecificBuilder);
        Assert.Equal(TestcontainersStates.Running, Setup.MongoContainerFromSpecificBuilder.State);

        Assert.Equal(0, testDocumentsCount);        
        Assert.True(totalCount > 0, "Collection's documents must be restored.");        
    }
}
