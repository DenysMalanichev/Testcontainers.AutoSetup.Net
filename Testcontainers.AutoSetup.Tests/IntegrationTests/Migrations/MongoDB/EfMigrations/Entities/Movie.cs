using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MongoDB.EfMigrations.Entities;

public class Movie
{
    [BsonId]
    public ObjectId _id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("rated")]
    public string Rated { get; set; } = null!;

    [BsonElement("plot")]
    public string Plot { get; set; } = null!;
}