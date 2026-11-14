namespace VehicleServiceStation.Infrastructure;

public static class EnvironmentFile
{
    public static void Load()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var root = FindRoot(start);
            if (root is null)
            {
                continue;
            }

            var file = Path.Combine(root, ".env");
            if (!File.Exists(file))
            {
                continue;
            }

            foreach (var line in File.ReadLines(file))
            {
                var entry = line.Trim();
                if (entry.Length == 0 || entry.StartsWith('#'))
                {
                    continue;
                }

                var separator = entry.IndexOf('=');
                if (separator < 1)
                {
                    continue;
                }

                var key = entry[..separator].Trim();
                var value = entry[(separator + 1)..].Trim();
                if (value.Length >= 2 && (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
                {
                    value = value[1..^1];
                }

                if (Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }
    }

    private static string? FindRoot(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env")) || Directory.Exists(Path.Combine(directory.FullName, "commands")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
