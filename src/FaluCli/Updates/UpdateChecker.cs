using Falu.Config;
using System.Net.Http.Json;

namespace Falu.Updates;

internal class UpdateChecker : BackgroundService
{
    private static readonly SemaphoreSlim locker = new(1);
    private static readonly SemanticVersioning.Version? currentVersion = SemanticVersioning.Version.Parse(VersioningHelper.ProductVersion);
    private static SemanticVersioning.Version? latestVersion;
    private static string? latestVersionHtmlUrl;
    private static string? latestVersionBody;

    private readonly IHostEnvironment environment;
    private readonly IConfigValuesProvider configValuesProvider;
    private readonly InvocationContext invocationContext;
    private readonly HttpClient httpClient;

    public UpdateChecker(IHostEnvironment environment, IConfigValuesProvider configValuesProvider, InvocationContext invocationContext, HttpClient httpClient)
    {
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        this.configValuesProvider = configValuesProvider ?? throw new ArgumentNullException(nameof(configValuesProvider));
        this.invocationContext = invocationContext ?? throw new ArgumentNullException(nameof(invocationContext));
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (environment.IsDevelopment() || latestVersion is not null) return;

            // skip update checks if the configuration values say so or the command has overriden the value.
            var skipUpdateChecks = (await configValuesProvider.GetConfigValuesAsync(stoppingToken)).SkipUpdateChecks;
            var skipUpdateChecksOption = invocationContext.ParseResult.ValueForOption<bool?>("--skip-update-checks");
            if (skipUpdateChecksOption is not null) skipUpdateChecks = skipUpdateChecksOption.Value;
            if (skipUpdateChecks) return;

            try
            {
                await locker.WaitAsync(stoppingToken);
                var release = await httpClient.GetFromJsonAsync(
                    requestUri: $"https://api.github.com/repos/{Constants.RepositoryOwner}/{Constants.RepositoryName}/releases/latest",
                    jsonTypeInfo: FaluCliJsonSerializerContext.Default.GitHubLatestRelease,
                    cancellationToken: stoppingToken);
                Interlocked.Exchange(ref latestVersion, SemanticVersioning.Version.Parse(release!.TagName));
                Interlocked.Exchange(ref latestVersionHtmlUrl, release.HtmlUrl);
                Interlocked.Exchange(ref latestVersionBody, release.Body);
            }
            finally
            {
                locker.Release();
            }
        }
        catch { } // nothing to do here
    }

    public static SemanticVersioning.Version? LatestVersion => latestVersion;
    public static SemanticVersioning.Version? CurrentVersion => currentVersion;
    public static string? LatestVersionHtmlUrl => latestVersionHtmlUrl;
    public static string? LatestVersionBody => latestVersionBody;
}
