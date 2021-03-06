<!--
GENERATED FILE - DO NOT EDIT
This file was generated by [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets).
Source File: /pages/mdsource/ef-usage.source.md
To change this file edit the source file and then run MarkdownSnippets.
-->

# EntityFramework Usage

Interactions with SqlLocalDB via [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).


## EfLocalDb package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.svg)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/


## Schema and data

The snippets use a DbContext of the following form:

<!-- snippet: TheDbContext.cs -->
<a id='snippet-TheDbContext.cs'/></a>
```cs
using Microsoft.EntityFrameworkCore;

public class TheDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public TheDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/TheDbContext.cs#L1-L17' title='File snippet `TheDbContext.cs` was extracted from'>snippet source</a> | <a href='#snippet-TheDbContext.cs' title='Navigate to start of snippet `TheDbContext.cs`'>anchor</a></sup>
<!-- endsnippet -->

<!-- snippet: TheEntity.cs -->
<a id='snippet-TheEntity.cs'/></a>
```cs
public class TheEntity
{
    public int Id { get; set; }
    public string? Property { get; set; }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/TheEntity.cs#L1-L5' title='File snippet `TheEntity.cs` was extracted from'>snippet source</a> | <a href='#snippet-TheEntity.cs' title='Navigate to start of snippet `TheEntity.cs`'>anchor</a></sup>
<!-- endsnippet -->


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

<!-- snippet: EfStaticConstructor -->
<a id='snippet-efstaticconstructor'/></a>
```cs
public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests()
    {
        sqlInstance = new SqlInstance<TheDbContext>(
            builder => new TheDbContext(builder.Options));
    }

    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        var data = new List<object> {entity};
        await using var database = await sqlInstance.Build(data);
        Assert.Single(database.Context.TestEntities);
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/StaticConstructor.cs#L8-L32' title='File snippet `efstaticconstructor` was extracted from'>snippet source</a> | <a href='#snippet-efstaticconstructor' title='Navigate to start of snippet `efstaticconstructor`'>anchor</a></sup>
<!-- endsnippet -->


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

<!-- snippet: EfTestBase -->
<a id='snippet-eftestbase'/></a>
```cs
public class TestBase
{
    static SqlInstance<TheDbContext> sqlInstance;

    static TestBase()
    {
        sqlInstance = new SqlInstance<TheDbContext>(
            constructInstance: builder => new TheDbContext(builder.Options));
    }

    public Task<SqlDatabase<TheDbContext>> LocalDb(
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        return sqlInstance.Build(GetType().Name, databaseSuffix, memberName);
    }
}

public class Tests :
    TestBase
{
    [Fact]
    public async Task Test()
    {
        await using var database = await LocalDb();
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/TestBaseUsage.cs#L8-L45' title='File snippet `eftestbase` was extracted from'>snippet source</a> | <a href='#snippet-eftestbase' title='Navigate to start of snippet `eftestbase`'>anchor</a></sup>
<!-- endsnippet -->


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

<!-- snippet: EfBuildDatabase -->
<a id='snippet-efbuilddatabase'/></a>
```cs
await using var database = await sqlInstance.Build();
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L19-L23' title='File snippet `efbuilddatabase` was extracted from'>snippet source</a> | <a href='#snippet-efbuilddatabase' title='Navigate to start of snippet `efbuilddatabase`'>anchor</a></sup>
<!-- endsnippet -->

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using DbContexts

<!-- snippet: EfBuildContext -->
<a id='snippet-efbuildcontext'/></a>
```cs
await using (var dbContext = database.NewDbContext())
{
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L25-L30' title='File snippet `efbuildcontext` was extracted from'>snippet source</a> | <a href='#snippet-efbuildcontext' title='Navigate to start of snippet `efbuildcontext`'>anchor</a></sup>
<!-- endsnippet -->


#### Full Test

The above are combined in a full test:

<!-- snippet: EfSnippetTests.cs -->
<a id='snippet-EfSnippetTests.cs'/></a>
```cs
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;
    static EfSnippetTests()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            connection => new MyDbContext(connection));
    }

    [Fact]
    public async Task TheTest()
    {
        using var database = await sqlInstance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }

    [Fact]
    public async Task TheTestWithDbName()
    {
        using var database = await sqlInstance.Build("TheTestWithDbName");
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
```
<sup><a href='/src/EfClassicLocalDb.Tests/Snippets/EfSnippetTests.cs#L1-L46' title='File snippet `EfSnippetTests.cs` was extracted from'>snippet source</a> | <a href='#snippet-EfSnippetTests.cs' title='Navigate to start of snippet `EfSnippetTests.cs`'>anchor</a></sup>
<a id='snippet-EfSnippetTests.cs-1'/></a>
```cs
using System.Threading.Tasks;
using EfLocalDb;
using Xunit;

public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;
    static EfSnippetTests()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            builder => new MyDbContext(builder.Options));
    }


    [Fact]
    public async Task TheTest()
    {

        await using var database = await sqlInstance.Build();



        await using (var dbContext = database.NewDbContext())
        {


            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }

    }


    [Fact]
    public async Task TheTestWithDbName()
    {
        await using var database = await sqlInstance.Build("TheTestWithDbName");
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L1-L55' title='File snippet `EfSnippetTests.cs` was extracted from'>snippet source</a> | <a href='#snippet-EfSnippetTests.cs-1' title='Navigate to start of snippet `EfSnippetTests.cs`'>anchor</a></sup>
<!-- endsnippet -->


### EntityFramework DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

<!-- snippet: DefaultOptionsBuilder.cs -->
<a id='snippet-DefaultOptionsBuilder.cs'/></a>
```cs
using Microsoft.EntityFrameworkCore;

static class DefaultOptionsBuilder
{
    static LogCommandInterceptor interceptor = new LogCommandInterceptor();

    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        if (LocalDbLogging.SqlLoggingEnabled)
        {
            builder.AddInterceptors(interceptor);
        }
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        return builder;
    }
}
```
<sup><a href='/src/EfLocalDb/DefaultOptionsBuilder.cs#L1-L19' title='File snippet `DefaultOptionsBuilder.cs` was extracted from'>snippet source</a> | <a href='#snippet-DefaultOptionsBuilder.cs' title='Navigate to start of snippet `DefaultOptionsBuilder.cs`'>anchor</a></sup>
<!-- endsnippet -->
