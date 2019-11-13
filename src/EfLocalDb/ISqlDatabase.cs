using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace EfLocalDb
{
    public interface ISqlDatabase<TDbContext>:
        IAsyncDisposable,
        IDisposable
        where TDbContext : DbContext
    {
        string Name { get; }
        SqlConnection Connection { get; }
        string ConnectionString { get; }
        TDbContext Context { get; }
        Task<SqlConnection> OpenNewConnection();
        Task Start();
        TDbContext NewDbContext();
        DbContextPool<TDbContext> ContextPool { get; }
    }
}