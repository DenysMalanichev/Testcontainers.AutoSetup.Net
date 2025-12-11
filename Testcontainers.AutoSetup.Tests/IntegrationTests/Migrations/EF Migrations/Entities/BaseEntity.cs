namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Entities;

// This can easily be modified to be BaseEntity<T> and public T Id to support different key types.
// Using non-generic integer types for simplicity and to ease caching logic
public abstract class BaseEntity
{
    public virtual int Id { get; init; }
}
