// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.Resolvers;
using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

[TestClass]
public class VideoSourcesResolverTests
{
    private readonly Mock<ILogger<VideoSourcesResolver>> _loggerMock = new();

    [TestMethod]
    public void Resolve_ShouldReturnEmpty_WhenContextMissing()
    {
        var resolver = new VideoSourcesResolver(_loggerMock.Object);

        var result = resolver.Resolve("video-onedrive-relative-path", null);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string[]));
        Assert.AreEqual(0, ((string[])result!).Length);
    }

    [TestMethod]
    public void Resolve_ShouldReturnCollection_WhenEntriesPresent()
    {
        var resolver = new VideoSourcesResolver(_loggerMock.Object);
        var entries = new List<VideoTranscriptSourceEntry>
        {
            new(
                FriendlyTitle: "Module One Overview",
                Anchor: "module-one-overview",
                RelativeVideoPath: "Program/Course/Module/video.mp4",
                RelativeTranscriptPath: "Program/Course/Module/video.en.txt",
                NoteLink: "[[Program/Course/Module/video-video|Video Video]]",
                Language: "en",
                TranscriptContent: "Transcript body"),
            new(
                FriendlyTitle: "Module Two Deep Dive",
                Anchor: "module-two-deep-dive",
                RelativeVideoPath: "Program/Course/Module2/video.mp4",
                RelativeTranscriptPath: null,
                NoteLink: null,
                Language: null,
                TranscriptContent: "Another transcript")
        };

        var context = new Dictionary<string, object>
        {
            [VideoTranscriptConstants.SourcesContextKey] = entries
        };

        var result = resolver.Resolve("video-onedrive-relative-path", context);

        Assert.IsNotNull(result);
        var array = result as string[];
        Assert.IsNotNull(array);

        Assert.AreEqual(2, array!.Length);
        Assert.AreEqual("Program/Course/Module/video.mp4", array[0]);
        Assert.AreEqual("Program/Course/Module2/video.mp4", array[1]);
    }
}
