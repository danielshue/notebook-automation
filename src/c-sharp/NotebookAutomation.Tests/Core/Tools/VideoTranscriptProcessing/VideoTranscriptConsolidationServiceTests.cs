// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Tools.Resolvers;
using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

namespace NotebookAutomation.Tests.Core.Tools.VideoTranscriptProcessing;

[TestClass]
public class VideoTranscriptConsolidationServiceTests
{
    private string _baseDirectory = null!;
    private string _oneDriveRoot = null!;
    private string _vaultRoot = null!;
    private AppConfig _appConfig = null!;
    private YamlHelper _yamlHelper = null!;
    private VideoTranscriptConsolidationService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _baseDirectory = Path.Combine(Path.GetTempPath(), "VideoTranscriptTests", Guid.NewGuid().ToString());
        _oneDriveRoot = Path.Combine(_baseDirectory, "OneDrive");
        _vaultRoot = Path.Combine(_baseDirectory, "Vault");
        Directory.CreateDirectory(_oneDriveRoot);
        Directory.CreateDirectory(_vaultRoot);

        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                OnedriveFullpathRoot = _oneDriveRoot,
                NotebookVaultFullpathRoot = _vaultRoot,
                MetadataSchemaFile = Path.Combine(AppContext.BaseDirectory, "config", "metadata-schema.yml")
            },
            PreferredTranscriptLanguages = ["en"]
        };

        _yamlHelper = new YamlHelper(NullLogger.Instance);
        var markdownBuilder = new MarkdownNoteBuilder(_yamlHelper, _appConfig);
        var markdownParser = new MarkdownParser(NullLogger.Instance);
        var pipeline = new PassthroughMetadataPipeline();
        var videoNoteProcessor = CreateVideoNoteProcessor(_appConfig);

        _service = new VideoTranscriptConsolidationService(
            NullLogger<VideoTranscriptConsolidationService>.Instance,
            _appConfig,
            markdownBuilder,
            markdownParser,
            _yamlHelper,
            pipeline,
            videoNoteProcessor);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_baseDirectory))
        {
            try
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
            catch
            {
                // ignore clean-up failures in tests
            }
        }
    }

    [TestMethod]
    public async Task ConsolidateAsync_ShouldAggregateTranscriptsAndCreateMarkdown()
    {
        var classFolder = Path.Combine(_oneDriveRoot, "Program", "Course", "ModuleA");
        Directory.CreateDirectory(classFolder);

        // Video with language-specific transcript
        File.WriteAllText(Path.Combine(classFolder, "module-intro.mp4"), string.Empty);
        File.WriteAllText(Path.Combine(classFolder, "module-intro.en.txt"), "Module introduction transcript");

        // Video in subfolder with generic transcript
        var subFolder = Path.Combine(classFolder, "ModuleB");
        Directory.CreateDirectory(subFolder);
        File.WriteAllText(Path.Combine(subFolder, "case-study.mp4"), string.Empty);
        File.WriteAllText(Path.Combine(subFolder, "case-study.txt"), "Case study transcript");

        // Video without transcript should be skipped
        File.WriteAllText(Path.Combine(subFolder, "orphan.mp4"), string.Empty);

        var request = new VideoTranscriptConsolidationRequest(classFolder, true, true, false);
        var result = await _service.ConsolidateAsync(request).ConfigureAwait(false);

        Assert.AreEqual(2, result.AggregatedCount);
        Assert.AreEqual(1, result.SkippedCount);
        Assert.IsTrue(result.WasWritten);
        Assert.IsTrue(File.Exists(result.OutputPath));

        var markdown = File.ReadAllText(result.OutputPath);
        var frontmatterText = _yamlHelper.ExtractFrontmatter(markdown);
        Assert.IsNotNull(frontmatterText);
        var frontmatter = _yamlHelper.ParseYamlToDictionary(frontmatterText!);

        Assert.AreEqual("video_transcript_consolidation", frontmatter["template-type"]);
        Assert.IsTrue(frontmatter.ContainsKey("video-onedrive-relative-path"));

        var videoSources = (frontmatter["video-onedrive-relative-path"] as IEnumerable<object>)?.Select(o => o?.ToString()).ToList();
        Assert.IsNotNull(videoSources);
        Assert.AreEqual(2, videoSources!.Count);
        Assert.AreEqual("Program/Course/ModuleA/module-intro.mp4", videoSources[0]);
        Assert.AreEqual("Program/Course/ModuleA/ModuleB/case-study.mp4", videoSources[1]);

        // Markdown body checks
        Assert.IsTrue(markdown.Contains("## Table of Contents"));
        Assert.IsTrue(markdown.Contains($"- [[#{result.Sources[0].FriendlyTitle}|{result.Sources[0].FriendlyTitle}]]"));
        Assert.IsTrue(markdown.Contains($"## {result.Sources[0].FriendlyTitle}"));
        Assert.IsTrue(markdown.Contains($"## {result.Sources[1].FriendlyTitle}"));
        Assert.IsTrue(markdown.Contains("Module introduction transcript"));
        Assert.IsTrue(markdown.Contains("Case study transcript"));
    }

    [TestMethod]
    public async Task ConsolidateAsync_ShouldSkipRewriteWhenSourcesUnchanged()
    {
        var folder = Path.Combine(_oneDriveRoot, "Program", "Course", "Lesson");
        Directory.CreateDirectory(folder);

        File.WriteAllText(Path.Combine(folder, "lesson-01.mp4"), string.Empty);
        File.WriteAllText(Path.Combine(folder, "lesson-01.txt"), "Transcript content");

        var firstRequest = new VideoTranscriptConsolidationRequest(folder, false, true, false);
        var firstResult = await _service.ConsolidateAsync(firstRequest).ConfigureAwait(false);
        Assert.IsTrue(firstResult.WasWritten);

        var writeTime = File.GetLastWriteTimeUtc(firstResult.OutputPath);

        var secondRequest = new VideoTranscriptConsolidationRequest(folder, false, false, false);
        var secondResult = await _service.ConsolidateAsync(secondRequest).ConfigureAwait(false);

        Assert.AreEqual(1, secondResult.AggregatedCount);
        Assert.IsFalse(secondResult.WasWritten);
        Assert.AreEqual(writeTime, File.GetLastWriteTimeUtc(firstResult.OutputPath));
    }

    private sealed class PassthroughMetadataPipeline : IMetadataPipeline
    {
        public (string CleanBody, Dictionary<string, object> Metadata) Compose(
            string bodyText,
            Dictionary<string, object>? metadata,
            string noteType,
            Dictionary<string, object>? context = null)
        {
            var resolvedMetadata = metadata != null
                ? new Dictionary<string, object>(metadata)
                : new Dictionary<string, object>();

            if (context != null && context.TryGetValue(VideoTranscriptConstants.SourcesContextKey, out var value))
            {
                var resolver = new VideoSourcesResolver(NullLogger<VideoSourcesResolver>.Instance);
                var resolved = resolver.Resolve("video_sources", context) as IEnumerable<string>;
                resolvedMetadata["video-onedrive-relative-path"] = resolved?.ToList() ?? new List<string>();
            }

            return (bodyText, resolvedMetadata);
        }
    }

    private static VideoNoteProcessor CreateVideoNoteProcessor(AppConfig config)
    {
        var logger = NullLogger<VideoNoteProcessor>.Instance;
        var yamlHelper = new YamlHelper(NullLogger.Instance);
        var markdownBuilder = new MarkdownNoteBuilder(new YamlHelper(NullLogger.Instance), config);

        return new VideoNoteProcessor(
            logger,
            Mock.Of<IAISummarizer>(),
            yamlHelper,
            Mock.Of<IMetadataHierarchyDetector>(),
            Mock.Of<IMetadataTemplateManager>(),
            Mock.Of<ICourseStructureExtractor>(),
            markdownBuilder,
            oneDriveService: null,
            appConfig: config);
    }
}
