// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Tools.Shared;

[TestClass]
public class MetadataPipelineComposeTests
{
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

    private static (IMetadataPipeline pipeline, IYamlHelper yaml) ResolvePipeline(ServiceProvider provider)
    {
        var pipeline = provider.GetRequiredService<IMetadataPipeline>();
        var yaml = provider.GetRequiredService<IYamlHelper>();
        return (pipeline, yaml);
    }

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
