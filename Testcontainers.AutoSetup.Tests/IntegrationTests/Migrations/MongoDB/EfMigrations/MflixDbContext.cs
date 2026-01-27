using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MongoDB.EfMigrations.Entities;

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.MongoDB.EfMigrations;

public class MflixDbContext : DbContext
{
    public DbSet<Movie> Movies { get; init; }
    public static MflixDbContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<MflixDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);
    public MflixDbContext(DbContextOptions options)
        : base(options)
    {
        // Dissable Transaction for simplicity of the tests.
        // Must use replica sets in real scenarious
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Movie>().ToCollection("movies");

        // Seed data
        modelBuilder.Entity<Movie>().HasData(
            new Movie { _id = ObjectId.GenerateNewId(), Title = "Movie1", Rated = "4.6", Plot = "Some movie description..." },
            new Movie { _id = ObjectId.GenerateNewId(), Title = "Movie2", Rated = "3.9", Plot = "Some movie description..." }
        );
    }
}