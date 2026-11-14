using System.Diagnostics;

namespace VehicleServiceStation.Infrastructure;

public sealed class CommandFileRunner(ILogger<CommandFileRunner> logger)
{
    public async Task RunAsync(string commandName, CancellationToken cancellationToken)
    {
        var commandDirectory = FindCommandDirectory();
        var scriptPath = OperatingSystem.IsWindows()
            ? Path.Combine(commandDirectory, "windows", $"{commandName}.ps1")
            : Path.Combine(commandDirectory, "linux", $"{commandName}.sh");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Database command file was not found: {scriptPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "bash",
            WorkingDirectory = commandDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);
        }
        else
        {
            startInfo.ArgumentList.Add(scriptPath);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the database command process.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));

        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new TimeoutException($"Database command '{commandName}' exceeded its two minute limit.");
        }

        var output = await outputTask;
        var error = await errorTask;
        if (!string.IsNullOrWhiteSpace(output))
        {
            logger.LogInformation("Database command {CommandName}: {CommandOutput}", commandName, output.Trim());
        }

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException($"Database command '{commandName}' failed: {details.Trim()}");
        }
    }

    public async Task RunSqlFileAsync(string fileName, DatabaseSettings settings, CancellationToken cancellationToken)
    {
        var commandDirectory = FindCommandDirectory();
        var sqlPath = Path.Combine(commandDirectory, "sql", fileName);
        if (!File.Exists(sqlPath))
        {
            throw new FileNotFoundException($"Database SQL file was not found: {sqlPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "psql",
            WorkingDirectory = commandDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["PGPASSWORD"] = settings.AppPassword;
        foreach (var argument in new[] { "-X", "-w", "--set", "ON_ERROR_STOP=1", "--host", settings.Host, "--port", settings.Port.ToString(), "--username", settings.AppUser, "--dbname", settings.Database, "--file", sqlPath })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start psql.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new TimeoutException($"SQL command '{fileName}' exceeded its two minute limit.");
        }

        var output = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException($"SQL command '{fileName}' failed: {details.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            logger.LogInformation("SQL command {SqlFile}: {CommandOutput}", fileName, output.Trim());
        }
    }

    private static string FindCommandDirectory()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var sourceCommands = Path.Combine(directory.FullName, "commands");
                if (File.Exists(Path.Combine(sourceCommands, "windows", "create-database.ps1")) || File.Exists(Path.Combine(sourceCommands, "linux", "create-database.sh")))
                {
                    return sourceCommands;
                }

                if (File.Exists(Path.Combine(directory.FullName, "commands", "sql", "schema.sql")))
                {
                    return Path.Combine(directory.FullName, "commands");
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("The commands folder was not found beside the application or in its parent directories.");
    }
}
