using System.Diagnostics;

namespace Platform.E2ETests;

// Brings the whole platform up with the root docker-compose.yml (its own compose project, so it never
// touches a development stack) and waits until every service answers through the gateway.
//
// E2E_USE_RUNNING_PLATFORM=1  reuse a platform that is already running instead of starting/stopping one
// E2E_LOG_FILE=<path>         write the compose logs there before tearing down (CI uploads it)
public sealed class PlatformFixture : IAsyncLifetime
{
    private const string ProjectName = "commerce-e2e";

    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromMinutes(5);

    private readonly bool _useRunningPlatform = Environment.GetEnvironmentVariable("E2E_USE_RUNNING_PLATFORM") == "1";

    public async Task InitializeAsync()
    {
        if (!_useRunningPlatform)
        {
            await Compose("up", "-d", "--build");
        }

        await WaitUntilReadyAsync();
    }

    public async Task DisposeAsync()
    {
        if (_useRunningPlatform)
        {
            return;
        }

        var logFile = Environment.GetEnvironmentVariable("E2E_LOG_FILE");
        if (!string.IsNullOrEmpty(logFile))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logFile))!);
            await File.WriteAllTextAsync(logFile, await Compose("logs", "--no-color"));
        }

        await Compose("down", "-v", "--remove-orphans");
    }

    private static async Task WaitUntilReadyAsync()
    {
        var deadline = DateTime.UtcNow + ReadinessTimeout;
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var admin = await ApiUser.SignInAsync("admin", "admin");
                foreach (var path in new[] { "/inventory/api/v1/products?pageSize=1", "/orders/api/v1/orders?pageSize=1", "/notifications/api/v1/notifications?pageSize=1" })
                {
                    using var response = await admin.SendAsync(HttpMethod.Get, path);
                    response.EnsureSuccessStatusCode();
                }

                using var mailpit = await new HttpClient().GetAsync(new Uri(Endpoints.Mailpit, "/livez"));
                mailpit.EnsureSuccessStatusCode();
                return;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                last = exception;
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        throw new TimeoutException($"The platform was not ready after {ReadinessTimeout.TotalMinutes} minutes. Last error: {last?.Message}");
    }

    private static async Task<string> Compose(params string[] arguments)
    {
        var root = FindRepositoryRoot();
        var startInfo = new ProcessStartInfo("docker")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[] { "compose", "-p", ProjectName, "-f", "docker-compose.yml" }.Concat(arguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start docker.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'docker compose {string.Join(' ', arguments)}' failed with exit code {process.ExitCode}. " +
                $"If a development platform is running, stop it first: the compose files publish the same ports.{Environment.NewLine}{await error}");
        }

        return await output + await error;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("docker-compose.yml was not found in any parent directory.");
    }
}

[CollectionDefinition(Name)]
public class PlatformCollection : ICollectionFixture<PlatformFixture>
{
    public const string Name = "Platform";
}
