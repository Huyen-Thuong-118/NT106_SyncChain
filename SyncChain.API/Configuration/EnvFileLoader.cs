using Npgsql;

namespace SyncChain.API.Configuration;

public static class EnvFileLoader
{
    public static void Load()
    {
        var envPath = FindEnvFile();
        if (envPath == null)
            return;

        foreach (var rawLine in File.ReadLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = Unquote(line[(separatorIndex + 1)..].Trim());

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvFile()
    {
        var startDirectories = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var startDirectory in startDirectories.Distinct())
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, ".env");
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }
        }

        return null;
    }

    public static string ToPostgreSqlConnectionString(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgresql" && uri.Scheme != "postgres"))
        {
            throw new InvalidOperationException(
                "DATABASE_URL phai co dang postgresql://user:password@host:port/database");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.Trim('/');
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("DATABASE_URL thieu tai khoan hoac ten database");

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(database),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        }.ConnectionString;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
