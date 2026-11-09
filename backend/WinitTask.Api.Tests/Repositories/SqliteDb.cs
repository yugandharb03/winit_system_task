using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Data;

namespace WinitTask.Api.Tests.Repositories;

internal sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public AppDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
