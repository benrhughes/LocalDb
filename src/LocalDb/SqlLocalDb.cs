using System;
using System.Collections.Generic;
using System.Diagnostics;

static class SqlLocalDb
{
    public static void Start(string instance)
    {
        RunLocalDbCommand($"create \"{instance}\" -s");
    }

    public static IEnumerable<string> Instances()
    {
        return RunLocalDbCommand("i");
    }

    public static InstanceInfo Info(string instance)
    {
        var instanceInfo = new InstanceInfo();
        foreach (var line in RunLocalDbCommand($"i {instance}"))
        {
            if (line == "The automatic instance \"{instance}\" is not created.")
            {
                throw new Exception(line);
            }
            var colonIndex = line.IndexOf(":");
            var key = line.Substring(0, colonIndex);
            var value = line.Substring(colonIndex+1).Trim();
            if (key == "Name")
            {
                instanceInfo.Name = value;
                continue;
            }

            if (key == "Version")
            {
                instanceInfo.Version = Version.Parse(value);
                continue;
            }

            if (key == "Shared name")
            {
                instanceInfo.SharedName = value;
                continue;
            }

            if (key == "Owner")
            {
                instanceInfo.Owner = value;
                continue;
            }

            if (key == "Auto-create")
            {
                instanceInfo.AutoCreate = value != "No";
                continue;
            }

            if (key == "State")
            {
                instanceInfo.State = (State) Enum.Parse(typeof(State), value);
                continue;
            }

            if (key == "Last start time")
            {
                instanceInfo.LastStartTime = DateTime.Parse(value);
                continue;
            }

            if (key == "Instance pipe name")
            {
                instanceInfo.InstancePipeName = value;
                continue;
            }

            throw new Exception($"Unknown key: {key}");
        }

        return instanceInfo;
    }

    public static void DeleteInstance(string instance)
    {
        RunLocalDbCommand($"stop \"{instance}\"");
        RunLocalDbCommand($"delete \"{instance}\"");
    }

    static List<string> RunLocalDbCommand(string command)
    {
        var startInfo = new ProcessStartInfo("sqllocaldb", command)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        try
        {
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var readToEnd = process.StandardError.ReadToEnd();
                    throw new Exception($"ExitCode: {process.ExitCode}. Output: {readToEnd}");
                }

                string line;
                var list = new List<string>();
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    list.Add(line);
                }

                return list;
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(RunLocalDbCommand)}
{nameof(command)}: sqllocaldb {command}
");
        }
    }
}