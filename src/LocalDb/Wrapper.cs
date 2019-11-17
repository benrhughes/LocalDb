﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MethodTimer;
using Microsoft.Data.SqlClient;

class Wrapper
{
    public readonly string Directory;
    ushort size;
    public readonly string MasterConnectionString;
    public readonly string WithRollbackConnectionString;
    string instance;
    string TemplateDataFile;
    string TemplateLogFile;
    string TemplateConnectionString;
    public readonly string ServerName;
    Task startupTask = null!;
    SqlConnection masterConnection = null!;

    public Wrapper(string instance, string directory, ushort size = 3)
    {
        Guard.AgainstDatabaseSize(nameof(size), size);
        Guard.AgainstInvalidFileNameCharacters(nameof(instance), instance);

        LocalDbLogging.WrapperCreated = true;
        this.instance = instance;
        MasterConnectionString = $"Data Source=(LocalDb)\\{instance};Database=master;MultipleActiveResultSets=True";
        TemplateConnectionString = $"Data Source=(LocalDb)\\{instance};Database=template;Pooling=false";
        WithRollbackConnectionString = $"Data Source=(LocalDb)\\{instance};Database=withRollback;Pooling=false";
        Directory = directory;
        this.size = size;
        TemplateDataFile = Path.Combine(directory, "template.mdf");
        TemplateLogFile = Path.Combine(directory, "template_log.ldf");
        System.IO.Directory.CreateDirectory(directory);
        ServerName = $@"(LocalDb)\{instance}";
    }

    public async Task<string> CreateDatabaseFromTemplate(string name, bool withRollback = false)
    {
        //TODO: if dataFile doesnt exists do a drop and recreate
        var stopwatch = Stopwatch.StartNew();

        // Explicitly dont take offline here, since that is done at startup
        var dataFile = Path.Combine(Directory, $"{name}.mdf");
        var logFile = Path.Combine(Directory, $"{name}_log.ldf");
        var commandText = SqlBuilder.GetCreateOrMakeOnlineCommand(name, dataFile, logFile, withRollback);
        if (string.Equals(name, "template", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("The database name 'template' is reserved.");
        }

        await startupTask;
        File.Copy(TemplateDataFile, dataFile, true);
        File.Copy(TemplateLogFile, logFile, true);

        await ExecuteOnMasterAsync(commandText);
        Trace.WriteLine($"Create DB `{name}` {stopwatch.ElapsedMilliseconds}ms.", "LocalDb");
        return $"Data Source=(LocalDb)\\{instance};Database={name};MultipleActiveResultSets=True;Pooling=false";
    }

    public void Start(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
    {
#if RELEASE
        try
        {
#endif
        var stopwatch = Stopwatch.StartNew();
        InnerStart(timestamp, buildTemplate);
        var message = $"Start `{ServerName}` {stopwatch.ElapsedMilliseconds}ms.";

        Trace.WriteLine(message, "LocalDb");
#if RELEASE
        }
        catch (Exception exception)
        {
            throw ExceptionBuilder.WrapLocalDbFailure(instance, Directory, exception);
        }
#endif
    }

    public Task AwaitStart()
    {
        return startupTask;
    }

    void InnerStart(DateTime timestamp, Func<SqlConnection, Task> buildTemplate)
    {
        void CleanStart()
        {
            FileExtensions.FlushDirectory(Directory);
            LocalDbApi.CreateInstance(instance);
            LocalDbApi.StartInstance(instance);
            startupTask = CreateAndDetachTemplate(
                timestamp,
                buildTemplate,
                rebuild: true,
                optimize: true);
            InitRollbackTask();
        }

        var info = LocalDbApi.GetInstance(instance);

        if (!info.Exists)
        {
            CleanStart();
            return;
        }

        if (!info.IsRunning)
        {
            LocalDbApi.DeleteInstance(instance);
            CleanStart();
            return;
        }

        var templateLastMod = File.GetCreationTime(TemplateDataFile);
        if (timestamp == templateLastMod)
        {
            LocalDbLogging.Log("Not modified so skipping rebuild");
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, false, false);
        }
        else
        {
            startupTask = CreateAndDetachTemplate(timestamp, buildTemplate, true, false);
        }

        InitRollbackTask();
    }

    [Time]
    async Task CreateAndDetachTemplate(DateTime timestamp, Func<SqlConnection, Task> buildTemplate, bool rebuild, bool optimize)
    {
        masterConnection = new SqlConnection(MasterConnectionString);
        masterConnection.Open();
        var takeDbsOffline = ExecuteOnMasterAsync(SqlBuilder.TakeDbsOfflineCommand);
        if (LocalDbLogging.Enabled)
        {
            Trace.WriteLine($"SqlServerVersion: {masterConnection.ServerVersion}", "LocalDb");
        }

        if (optimize)
        {
            await ExecuteOnMasterAsync(SqlBuilder.GetOptimizationCommand(size));
        }

        if (!rebuild)
        {
            await takeDbsOffline;
            return;
        }

        DeleteTemplateFiles();
        await ExecuteOnMasterAsync(SqlBuilder.GetCreateTemplateCommand(TemplateDataFile, TemplateLogFile));

#if NETSTANDARD2_1
        await using (var templateConnection = new SqlConnection(TemplateConnectionString))
#else
        using (var templateConnection = new SqlConnection(TemplateConnectionString))
#endif
        {
            await templateConnection.OpenAsync();
            await buildTemplate(templateConnection);
        }

        await ExecuteOnMasterAsync(SqlBuilder.DetachTemplateCommand);

        File.SetCreationTime(TemplateDataFile, timestamp);
        await takeDbsOffline;
    }

    Task ExecuteOnMasterAsync(string command)
    {
        return masterConnection.ExecuteCommandAsync(command);
    }

    [Time]
    public void DeleteInstance()
    {
        LocalDbApi.StopAndDelete(instance);
        System.IO.Directory.Delete(Directory, true);
    }

    void DeleteTemplateFiles()
    {
        File.Delete(TemplateDataFile);
        File.Delete(TemplateLogFile);
    }

    [Time]
    public async Task DeleteDatabase(string dbName)
    {
        var commandText = SqlBuilder.BuildDeleteDbCommand(dbName);
        await ExecuteOnMasterAsync(commandText);
        var dataFile = Path.Combine(Directory, $"{dbName}.mdf");
        var logFile = Path.Combine(Directory, $"{dbName}_log.ldf");
        File.Delete(dataFile);
        File.Delete(logFile);
    }

    Lazy<Task> withRollbackTask = null!;

    void InitRollbackTask()
    {
        withRollbackTask = new Lazy<Task>(() => CreateDatabaseFromTemplate("withRollback", true));
    }

    public async Task CreateWithRollbackDatabase()
    {
        await startupTask;
        await withRollbackTask.Value;
    }
}