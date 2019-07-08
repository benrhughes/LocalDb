﻿using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

namespace TestBase
{
    #region EfTestBase

    public class TestBase
    {
        static SqlInstance<TheDbContext> instance;

        static TestBase()
        {
            instance = new SqlInstance<TheDbContext>(
                constructInstance: builder => new TheDbContext(builder.Options));
        }

        public Task<SqlDatabase<TheDbContext>> LocalDb(
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            return instance.Build(GetType().Name, databaseSuffix, memberName);
        }
    }

    public class Tests :
        TestBase
    {
        [Fact]
        public async Task Test()
        {
            using (var database = await LocalDb())
            {
                var entity = new TheEntity
                {
                    Property = "prop"
                };
                await database.AddData(entity);

                Assert.Single(database.Context.TestEntities);
            }
        }
    }

    #endregion
}