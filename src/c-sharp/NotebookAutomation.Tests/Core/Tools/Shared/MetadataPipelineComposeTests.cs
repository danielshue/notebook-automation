// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Tools.Shared;

/// <summary>
/// Tests for <see cref="IMetadataPipeline"/> composition behavior, including
/// precedence rules, cleanup of transient fields, date field normalization,
/// and template/resolver integration across different note types.
/// </summary>
/// <remarks>
/// These tests verify that the pipeline:
/// - Strips yaml frontmatter from the input body while merging metadata.
/// - Applies precedence: overrides &gt; frontmatter &gt; resolvers/defaults.
/// - Removes processing-only fields (e.g., <c>_internal_path</c>, <c>yaml-frontmatter</c>).
/// - Removes date-related fields (<c>date-*</c> and <c>*-date</c>).
/// - Adds template markers and maps OneDrive share links to <c>onedrive-shared-link</c>.
/// </remarks>
[TestClass]
public class MetadataPipelineComposeTests
{
    /// <summary>
    /// Builds a service provider with Notebook Automation services and overrides
    /// <see cref="IOneDriveService"/> to return a deterministic share link for testing.
    /// </summary>
    /// <param name="fakeLink">Outputs the predictable OneDrive share link used by the fake service.</param>
    /// <returns>An initialized <see cref="ServiceProvider"/> for resolving test dependencies.</returns>
    private static ServiceProvider BuildProviderWithFakeOneDrive(out string fakeLink)
    {
        var services = new ServiceCollection();
        var cfgDict = new Dictionary<string, string?>
        {
            ["paths:logging_dir"] = "logs",
            ["paths:onedrive_fullpath_root"] = "C:/OneDrive",
            ["paths:onedrive_resources_basepath"] = "Resources",
            ["paths:notebook_vault_fullpath_root"] = "C:/Vault"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(cfgDict!).Build();

        // Register all services
        ServiceRegistration.AddNotebookAutomationServices(services, configuration, debug: false);

        // Override OneDrive with a fake that returns a deterministic link
        fakeLink = "https://example.local/share";
        var capturedLink = fakeLink; // avoid capturing out var in lambda
        services.AddScoped<IOneDriveService>(_ => new FakeOneDriveService(capturedLink));

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Resolves the <see cref="IMetadataPipeline"/> and <see cref="IYamlHelper"/> services from the given provider.
    /// </summary>
    /// <param name="provider">The configured <see cref="ServiceProvider"/> used to resolve services.</param>
    /// <returns>A tuple containing the resolved pipeline and YAML helper instances.</returns>
    private static (IMetadataPipeline pipeline, IYamlHelper yaml) ResolvePipeline(ServiceProvider provider)
    {
        var pipeline = provider.GetRequiredService<IMetadataPipeline>();
        var yaml = provider.GetRequiredService<IYamlHelper>();
        return (pipeline, yaml);
    }

    /// <summary>
    /// Verifies composition behavior for a PDF note when frontmatter is present and overrides are supplied,
    /// ensuring precedence and cleanup rules are correctly applied.
    /// </summary>
    /// <remarks>
    /// Asserts the following:
    /// - Body frontmatter is removed.
    /// - Overrides take precedence over frontmatter values.
    /// - Date fields and transient/internal fields are removed.
    /// - OneDrive share link is resolved and mapped to <c>onedrive-shared-link</c>.
    /// - Template markers are present and consistent with the note type.
    /// </remarks>
    [TestMethod]
    public void Compose_PdfNote_PrecedenceAndCleanup()
    {
        var provider = BuildProviderWithFakeOneDrive(out var expectedLink);
        var (pipeline, yaml) = ResolvePipeline(provider);

        var body = """
---
 title: From Frontmatter
 status: custom-status
 date-created: 2001-01-01
 completion-date: 2001-02-03
 yaml-frontmatter: shouldremove
---
# Content Title

Body text.
""";

        var overrides = new Dictionary<string, object>
        {
            ["status"] = "override-status",
            ["custom-field"] = "override-value",
            ["_internal_path"] = "C:/OneDrive/Resources/some.pdf"
        };

        var context = new Dictionary<string, object>
        {
            ["filePath"] = "C:/OneDrive/Resources/some.pdf"
        };

        var (clean, meta) = pipeline.Compose(body, overrides, "PDF Note", context);

        // Body should have frontmatter removed
        Assert.IsFalse(clean.TrimStart().StartsWith("---"), "Frontmatter should be stripped from body");

        // Precedence: overrides > frontmatter > resolvers/defaults
        Assert.AreEqual("override-status", meta["status"].ToString());

        // Title from frontmatter should remain since not overridden
        Assert.AreEqual("From Frontmatter", meta["title"].ToString());

        // Cleanup: date-* and *-date removed
        Assert.IsFalse(meta.ContainsKey("date-created"));
        Assert.IsFalse(meta.ContainsKey("completion-date"));

        // Cleanup: internal/transient fields removed
        Assert.IsFalse(meta.ContainsKey("_internal_path"));
        Assert.IsFalse(meta.ContainsKey("yaml-frontmatter"));
        Assert.IsFalse(meta.ContainsKey("yamlfrontmatter"));
        Assert.IsFalse(meta.ContainsKey("aliases"));
        Assert.IsFalse(meta.ContainsKey("permalink"));

        // Share link resolved and mapped
        Assert.IsTrue(meta.ContainsKey("onedrive-shared-link"));
        Assert.AreEqual(expectedLink, meta["onedrive-shared-link"]?.ToString());

        // Template markers present
        Assert.AreEqual("pdf-reference", meta["template-type"].ToString());
        Assert.AreEqual("pdf-reference", meta["type"].ToString());
    }

    /// <summary>
    /// Verifies composition behavior for a Video note when no frontmatter is present in the body.
    /// </summary>
    /// <remarks>
    /// Ensures the body remains unchanged (no frontmatter to strip), default status is applied,
    /// template markers are set appropriately, and no date fields remain.
    /// </remarks>
    [TestMethod]
    public void Compose_VideoNote_DefaultsApplied_NoFrontmatter()
    {
        var provider = BuildProviderWithFakeOneDrive(out _);
        var (pipeline, _) = ResolvePipeline(provider);

        var body = "# Video Content\n\nSome text.";
        var (clean, meta) = pipeline.Compose(body, inputMetadata: null, noteType: "Video Note", context: null);

        // Body unchanged (no frontmatter)
        Assert.IsFalse(clean.TrimStart().StartsWith("---"));

        // Status should default to unwatched (from template logic)
        Assert.AreEqual("unwatched", meta["status"].ToString());

        // Should include template markers
        Assert.AreEqual("video-reference", meta["template-type"].ToString());
        Assert.AreEqual("video-reference", meta["type"].ToString());

        // No date-* retained
        foreach (var key in meta.Keys)
        {
            Assert.IsFalse(key.StartsWith("date-") || key.EndsWith("-date"), $"Unexpected date field: {key}");
        }
    }

    /// <summary>
    /// Minimal fake implementation of <see cref="IOneDriveService"/> for unit testing that returns
    /// a deterministic share link and no-ops for all other operations.
    /// </summary>
    private sealed class FakeOneDriveService : IOneDriveService
    {
        private readonly string _link;
        public FakeOneDriveService(string link) => _link = link;
        public Task AuthenticateAsync() => Task.CompletedTask;
        public void SetForceRefresh(bool forceRefresh) { }
        public Task RefreshAuthenticationAsync() => Task.CompletedTask;
        public Task DownloadFileAsync(string oneDrivePath, string localPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<string>> ListFilesAsync(string oneDriveFolder, CancellationToken cancellationToken = default) => Task.FromResult(new List<string>());
        public Task UploadFileAsync(string localPath, string oneDrivePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string?> CreateShareLinkAsync(string filePath, string linkType = "view", string scope = "anonymous", CancellationToken cancellationToken = default) => Task.FromResult<string?>(_link);
        public Task<List<Dictionary<string, object>>> SearchFilesAsync(string query, CancellationToken cancellationToken = default) => Task.FromResult(new List<Dictionary<string, object>>());
        public void ConfigureVaultRoots(string localVaultRoot, string oneDriveVaultRoot) { }
        public string MapLocalToOneDrivePath(string localPath) => localPath;
        public string MapOneDriveToLocalPath(string oneDrivePath) => oneDrivePath;
        public Task<string> GetShareLinkAsync(string filePath, bool forceRefresh = false, CancellationToken cancellationToken = default) => Task.FromResult(_link);
        public Task<bool> IsTokenValidAsync() => Task.FromResult(true);
    }
}
