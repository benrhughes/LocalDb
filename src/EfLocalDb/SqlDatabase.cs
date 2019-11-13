﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace EfLocalDb
{
    public class SqlDatabase<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;
        Func<Task> delete;
        IEnumerable<object>? data;
        DbContextPool<TDbContext>? contextPool;

        internal SqlDatabase(
            string connectionString,
            string name,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Func<Task> delete,
            IEnumerable<object>? data)
        {
            Name = name;
            this.constructInstance = constructInstance;
            this.delete = delete;
            this.data = data;
            ConnectionString = connectionString;
            Connection = new SqlConnection(connectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
            return sqlConnection;
        }

        public static implicit operator TDbContext(SqlDatabase<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Context;
        }

        public static implicit operator SqlConnection(SqlDatabase<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Connection;
        }

        public async Task Start()
        {
            await Connection.OpenAsync();
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(Connection);
            contextPool = new DbContextPool<TDbContext>(builder.Options);
            Context = contextPool.Rent();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public TDbContext Context { get; private set; } = null!;

        public TDbContext NewDbContext()
        {
            return contextPool!.Rent();
        }

        public void Dispose()
        {
            Context?.Dispose();
            Connection.Dispose();
            contextPool?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Context != null)
            {
                await Context.DisposeAsync();
            }
            await Connection.DisposeAsync();
            if (contextPool != null)
            {
                await contextPool.DisposeAsync();
            }
        }

        public async Task Delete()
        {
            await DisposeAsync();
            await delete();
        }
    }
}