﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace EfLocalDb
{
    public class SqlDatabaseWithRollback<TDbContext> :
        ISqlDatabase<TDbContext>
        where TDbContext : DbContext
    {
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;
        IEnumerable<object>? data;
        DbContextPool<TDbContext>? contextPool;

        internal SqlDatabaseWithRollback(
            string connectionString,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            IEnumerable<object> data)
        {
            Name = "withRollback";
            this.constructInstance = constructInstance;
            this.data = data;
            ConnectionString = connectionString;
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Snapshot
            };
            Transaction = new CommittableTransaction(transactionOptions);
            Connection = new SqlConnection(ConnectionString);
        }

        public string Name { get; }
        public SqlConnection Connection { get; }
        public string ConnectionString { get; }

        public async Task<SqlConnection> OpenNewConnection()
        {
            var sqlConnection = new SqlConnection(ConnectionString);
            await sqlConnection.OpenAsync();
            Connection.EnlistTransaction(Transaction);
            return sqlConnection;
        }

        public static implicit operator TDbContext(SqlDatabaseWithRollback<TDbContext> instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return instance.Context;
        }

        public static implicit operator SqlConnection(SqlDatabaseWithRollback<TDbContext> instance)
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
            Context = NewDbContext();
            if (data != null)
            {
                await this.AddData(data);
            }
        }

        public Transaction Transaction { get; }

        public TDbContext Context { get; private set; } = null!;

        public TDbContext NewDbContext()
        {
            var dbContext = contextPool!.Rent();
            dbContext.Database.EnlistTransaction(Transaction);
            return dbContext;
        }

        public void Dispose()
        {
            Transaction.Rollback();
            Transaction.Dispose();

            Context?.Dispose();
            Connection.Dispose();
            contextPool?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            Transaction.Rollback();
            Transaction.Dispose();

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
    }
}