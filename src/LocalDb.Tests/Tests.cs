﻿using System;
using System.IO;
using System.Threading.Tasks;
using ApprovalTests;
using LocalDb;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    XunitLoggingBase
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance(
            name: "Name",
            buildTemplate: TestDbBuilder.CreateTable);

        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
            var settings = DbPropertyReader.Read(connection, "Tests_Simple");
            ObjectApprover.VerifyWithJson(settings, s => s.Replace(Path.GetTempPath(), ""));
        }
    }

    [Fact]
    public async Task NoFileAndNoInstance()
    {
        SqlLocalDb.DeleteInstance("NoFileAndNoInstance");
        var directory = DirectoryFinder.Find("NoFileAndNoInstance");

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
        var instance = new SqlInstance(
            name: "NoFileAndNoInstance",
            buildTemplate: TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task WithFileAndNoInstance()
    {
        new SqlInstance(
            name: "WithFileAndNoInstance",
            buildTemplate: TestDbBuilder.CreateTable);
        SqlLocalDb.DeleteInstance("WithFileAndNoInstance");
        var instance = new SqlInstance(
            name: "WithFileAndNoInstance",
            buildTemplate: TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public async Task NoFileAndWithInstance()
    {
        ManagedLocalDbApi.CreateInstance("NoFileAndWithInstance");
        var directory = DirectoryFinder.Find("NoFileAndWithInstance");

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
        var instance = new SqlInstance(
            name: "NoFileAndWithInstance",
            buildTemplate: TestDbBuilder.CreateTable);

        await AddAndVerifyData(instance);
    }

    [Fact]
    public void Duplicate()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        SqlInstanceService.Register("LocalDbDuplicate", TestDbBuilder.CreateTable);
    }

    [Fact]
    public async Task WithRebuild()
    {
        var instance1 = new SqlInstance(
            name: "rebuild",
            buildTemplate: TestDbBuilder.CreateTable,
            requiresRebuild: dbContext => true);
        using (var database1 = await instance1.Build())
        {
            await TestDbBuilder.AddData(database1.Connection);
        }

        var instance2 = new SqlInstance(
            name: "rebuild",
            buildTemplate: (string connection) => throw new Exception(),
            requiresRebuild: dbContext => false);
        await AddAndVerifyData(instance2);
    }

    static async Task AddAndVerifyData(SqlInstance instance)
    {
        using (var database = await instance.Build())
        {
            var connection = database.Connection;
            var data = await TestDbBuilder.AddData(connection);
            Assert.Contains(data, await TestDbBuilder.GetData(connection));
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}