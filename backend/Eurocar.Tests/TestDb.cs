using Eurocar.Api.Infrastructure;
using Eurocar.Api.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Tests;

/// <summary>In-memory SQLite database that lives as long as this object (real constraints, no files).</summary>
public sealed class TestDb : IDisposable
{
    readonly SqliteConnection _connection;
    public AppDb Db { get; }

    TestDb(SqliteConnection connection, AppDb db) { _connection = connection; Db = db; }

    public static TestDb Create(bool seedDemo = false)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        if (seedDemo) DemoSeeder.Seed(db, DateTime.UtcNow);
        return new TestDb(connection, db);
    }

    /// <summary>A second context on the same database, for asserting what was actually saved.</summary>
    public AppDb NewContext() => new(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);

    public void Dispose() { Db.Dispose(); _connection.Dispose(); }
}
