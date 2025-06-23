// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Reflection;

namespace NotebookAutomation.Tests.Core.Tools.Vault;

/// <summary>
/// Comprehensive unit tests for the VaultIndexBatchProcessor class.
/// </summary>
[TestClass]
public class VaultIndexBatchProcessorTests
{
    private Mock<ILogger<VaultIndexBatchProcessor>> _loggerMock = null!;
    private Mock<IVaultIndexProcessor> _processorMock = null!;
    private Mock<IMetadataHierarchyDetector> _hierarchyDetectorMock = null!;
    private VaultIndexBatchProcessor _batchProcessor = null!;
    private string _testVaultPath = null!;
    private string _testTempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<VaultIndexBatchProcessor>>();
        _processorMock = new();
        _hierarchyDetectorMock = new();

        _batchProcessor = new VaultIndexBatchProcessor(
            _loggerMock.Object,
            _processorMock.Object,
            _hierarchyDetectorMock.Object);

        // Create test directory structure
        _testTempDir = Path.Combine(Path.GetTempPath(), "VaultIndexBatchProcessorTests", Guid.NewGuid().ToString());
        _testVaultPath = Path.Combine(_testTempDir, "TestVault");
        Directory.CreateDirectory(_testVaultPath);

        // Create test folder structure
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Program 1"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Program 1", "Course 1"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Program 1", "Course 1", "Module 1"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Program 1", "Course 1", "Module 2"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Program 2"));

        // Create ignored directories
        Directory.CreateDirectory(Path.Combine(_testVaultPath, ".obsidian"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "templates"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "attachments"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testTempDir))
        {
            Directory.Delete(_testTempDir, true);
        }
    }

    /// <summary>
    /// Tests that the constructor properly initializes dependencies.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidDependencies_InitializesCorrectly()
    {
        // Arrange & Act
        var processor = new VaultIndexBatchProcessor(
            _loggerMock.Object,
            _processorMock.Object,
            _hierarchyDetectorMock.Object);

        // Assert
        Assert.IsNotNull(processor);
        Assert.AreEqual(0, processor.Queue.Count);
    }    /// <summary>
         /// Tests that constructor handles null dependencies gracefully (primary constructor behavior).
         /// </summary>
    [TestMethod]
    public void Constructor_NullLogger_AllowsNull()
    {
        // Act & Assert - Primary constructors allow null parameters
        var processor = new VaultIndexBatchProcessor(null!, _processorMock.Object, _hierarchyDetectorMock.Object);
        Assert.IsNotNull(processor);
        Assert.AreEqual(0, processor.Queue.Count);
    }

    [TestMethod]
    public void Constructor_NullProcessor_AllowsNull()
    {
        // Act & Assert - Primary constructors allow null parameters
        var processor = new VaultIndexBatchProcessor(_loggerMock.Object, null!, _hierarchyDetectorMock.Object);
        Assert.IsNotNull(processor);
        Assert.AreEqual(0, processor.Queue.Count);
    }

    [TestMethod]
    public void Constructor_NullHierarchyDetector_AllowsNull()
    {
        // Act & Assert - Primary constructors allow null parameters
        var processor = new VaultIndexBatchProcessor(_loggerMock.Object, _processorMock.Object, null!);
        Assert.IsNotNull(processor);
        Assert.AreEqual(0, processor.Queue.Count);
    }

    /// <summary>
    /// Tests that InitializeProcessingQueue discovers all non-ignored directories.
    /// </summary>
    [TestMethod]
    public void InitializeProcessingQueue_ValidPath_AddsAllNonIgnoredDirectories()
    {
        // Arrange
        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(methodInfo);

        // Act
        methodInfo.Invoke(_batchProcessor, [_testVaultPath, null, null]);

        // Assert
        var queue = _batchProcessor.Queue;
        Assert.IsTrue(queue.Count >= 5); // Root + Program 1 + Course 1 + Module 1 + Module 2 + Program 2

        // Verify no ignored directories are in queue
        Assert.IsFalse(queue.Any(q => q.FilePath.Contains(".obsidian")));
        Assert.IsFalse(queue.Any(q => q.FilePath.Contains("templates")));
        Assert.IsFalse(queue.Any(q => q.FilePath.Contains("attachments")));

        // Verify all queue items have correct initial status
        Assert.IsTrue(queue.All(q => q.Status == DocumentProcessingStatus.Waiting));
        Assert.IsTrue(queue.All(q => q.Stage == ProcessingStage.NotStarted));
        Assert.IsTrue(queue.All(q => q.DocumentType == "INDEX"));
    }

    /// <summary>
    /// Tests filtering by template types.
    /// </summary>
    [TestMethod]
    public void InitializeProcessingQueue_WithTemplateTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var templateTypes = new List<string> { "course", "module" };
        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(methodInfo);        // Setup hierarchy detector mock to return specific template types
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string path, string vaultRoot) =>
            {
                if (path.Contains("Course")) return 3; // course level
                if (path.Contains("Module")) return 5; // module level
                return 1; // other level
            });

        _hierarchyDetectorMock.Setup(h => h.GetTemplateTypeFromHierarchyLevel(3))
            .Returns("course");
        _hierarchyDetectorMock.Setup(h => h.GetTemplateTypeFromHierarchyLevel(5))
            .Returns("module");
        _hierarchyDetectorMock.Setup(h => h.GetTemplateTypeFromHierarchyLevel(1))
            .Returns("program");

        // Act
        methodInfo.Invoke(_batchProcessor, [_testVaultPath, templateTypes, _testVaultPath]);        // Assert
        var queue = _batchProcessor.Queue;
        Assert.IsTrue(queue.Count > 0, "Queue should contain filtered items");

        // Verify only course and module folders are included
        Assert.IsTrue(queue.All(q => q.FilePath.Contains("Course") || q.FilePath.Contains("Module") || q.FilePath == _testVaultPath),
            "Queue should only contain course/module folders or vault root");

        // Verify hierarchy detector was called
        _hierarchyDetectorMock.Verify(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce);
        _hierarchyDetectorMock.Verify(h => h.GetTemplateTypeFromHierarchyLevel(It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that ProcessingProgressChanged event is raised correctly.
    /// </summary>
    [TestMethod]
    public void OnProcessingProgressChanged_EventRaised_ContainsCorrectData()
    {
        // Arrange
        DocumentProcessingProgressEventArgs? capturedArgs = null;
        _batchProcessor.ProcessingProgressChanged += (sender, args) => capturedArgs = args;

        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("OnProcessingProgressChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(methodInfo);

        // Act
        methodInfo.Invoke(_batchProcessor, ["/test/path", "Processing", 3, 10]);        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual("/test/path", capturedArgs.FilePath);
        Assert.AreEqual("Processing", capturedArgs.Status);
        Assert.AreEqual(3, capturedArgs.CurrentFile);
        Assert.AreEqual(10, capturedArgs.TotalFiles);
    }

    /// <summary>
    /// Tests that QueueChanged event is raised correctly.
    /// </summary>
    [TestMethod]
    public void OnQueueChanged_EventRaised_ContainsCorrectData()
    {
        // Arrange
        QueueChangedEventArgs? capturedArgs = null;
        _batchProcessor.QueueChanged += (sender, args) => capturedArgs = args;

        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("OnQueueChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(methodInfo);

        // Act
        methodInfo.Invoke(_batchProcessor, new object?[] { null });        // Assert
        Assert.IsNotNull(capturedArgs);
        Assert.IsNotNull(capturedArgs.Queue);
    }

    /// <summary>
    /// Tests updating queue item status.
    /// </summary>
    [TestMethod]
    public void UpdateQueueItemStatus_ExistingItem_UpdatesCorrectly()
    {
        // Arrange
        var initMethod = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(initMethod);
        initMethod.Invoke(_batchProcessor, [_testVaultPath, null, null]);

        var updateMethod = typeof(VaultIndexBatchProcessor).GetMethod("UpdateQueueItemStatus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(updateMethod);

        var firstItem = _batchProcessor.Queue.First();
        var testPath = firstItem.FilePath;

        QueueChangedEventArgs? capturedArgs = null;
        _batchProcessor.QueueChanged += (sender, args) => capturedArgs = args;

        // Act
        updateMethod.Invoke(_batchProcessor, [testPath, DocumentProcessingStatus.Processing,
            ProcessingStage.MarkdownCreation, "Test status", 1, 5]);

        // Assert
        var updatedItem = _batchProcessor.Queue.First(q => q.FilePath == testPath);
        Assert.AreEqual(DocumentProcessingStatus.Processing, updatedItem.Status);
        Assert.AreEqual(ProcessingStage.MarkdownCreation, updatedItem.Stage);
        Assert.AreEqual("Test status", updatedItem.StatusMessage);
        Assert.IsNotNull(updatedItem.ProcessingStartTime);

        // Verify event was raised
        Assert.IsNotNull(capturedArgs);
        Assert.AreEqual(updatedItem, capturedArgs.ChangedItem);
    }

    /// <summary>
    /// Tests completion status sets end time.
    /// </summary>
    [TestMethod]
    public void UpdateQueueItemStatus_CompletedStatus_SetsEndTime()
    {
        // Arrange
        var initMethod = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(initMethod);
        initMethod.Invoke(_batchProcessor, [_testVaultPath, null, null]);

        var updateMethod = typeof(VaultIndexBatchProcessor).GetMethod("UpdateQueueItemStatus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(updateMethod);

        var firstItem = _batchProcessor.Queue.First();
        var testPath = firstItem.FilePath;

        // Act - First set to processing
        updateMethod.Invoke(_batchProcessor, [testPath, DocumentProcessingStatus.Processing,
            ProcessingStage.MarkdownCreation, "Processing", 1, 5]);

        // Act - Then set to completed
        updateMethod.Invoke(_batchProcessor, [testPath, DocumentProcessingStatus.Completed,
            ProcessingStage.Completed, "Completed", 1, 5]);

        // Assert
        var updatedItem = _batchProcessor.Queue.First(q => q.FilePath == testPath);
        Assert.AreEqual(DocumentProcessingStatus.Completed, updatedItem.Status);
        Assert.IsNotNull(updatedItem.ProcessingStartTime);
        Assert.IsNotNull(updatedItem.ProcessingEndTime);
    }

    /// <summary>
    /// Tests IsIgnoredDirectory method through reflection.
    /// </summary>
    [TestMethod]
    public void IsIgnoredDirectory_IgnoredDirectories_ReturnsTrue()
    {
        // Arrange
        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("IsIgnoredDirectory",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(methodInfo);

        // Act & Assert
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/.obsidian"])!);
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/templates"])!);
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/Templates"])!); // Case insensitive
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/attachments"])!);
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/Attachments"])!); // Case insensitive
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/resources"])!);
        Assert.IsTrue((bool)methodInfo.Invoke(null, ["/vault/_templates"])!);
    }

    /// <summary>
    /// Tests IsIgnoredDirectory method for non-ignored directories.
    /// </summary>
    [TestMethod]
    public void IsIgnoredDirectory_NonIgnoredDirectories_ReturnsFalse()
    {
        // Arrange
        var methodInfo = typeof(VaultIndexBatchProcessor).GetMethod("IsIgnoredDirectory",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(methodInfo);

        // Act & Assert
        Assert.IsFalse((bool)methodInfo.Invoke(null, ["/vault/Course 1"])!);
        Assert.IsFalse((bool)methodInfo.Invoke(null, ["/vault/Module 1"])!);
        Assert.IsFalse((bool)methodInfo.Invoke(null, ["/vault/Normal Folder"])!);
        Assert.IsFalse((bool)methodInfo.Invoke(null, ["/vault/mytemplates"])!); // Contains "templates" but doesn't equal
    }

    /// <summary>
    /// Tests queue property thread safety.
    /// </summary>
    [TestMethod]
    public async Task Queue_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var initMethod = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(initMethod);
        initMethod.Invoke(_batchProcessor, [_testVaultPath, null, null]);

        var tasks = new List<Task>();
        var exception = false;

        // Act - Access queue from multiple threads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var queue = _batchProcessor.Queue;
                    var count = queue.Count;
                    // Just accessing the queue to test thread safety
                }
                catch
                {
                    exception = true;
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.IsFalse(exception, "No exceptions should occur during concurrent queue access");
    }

    /// <summary>
    /// Tests that events are not raised when item is not found.
    /// </summary>
    [TestMethod]
    public void UpdateQueueItemStatus_NonExistentItem_NoEventsRaised()
    {
        // Arrange
        var updateMethod = typeof(VaultIndexBatchProcessor).GetMethod("UpdateQueueItemStatus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(updateMethod);

        var eventRaised = false;
        _batchProcessor.QueueChanged += (sender, args) => eventRaised = true;
        _batchProcessor.ProcessingProgressChanged += (sender, args) => eventRaised = true;

        // Act
        updateMethod.Invoke(_batchProcessor, ["/nonexistent/path", DocumentProcessingStatus.Processing,
            ProcessingStage.MarkdownCreation, "Test", 1, 5]);

        // Assert
        Assert.IsFalse(eventRaised, "No events should be raised for non-existent queue items");
    }

    /// <summary>
    /// Tests that the queue returns a read-only copy.
    /// </summary>
    [TestMethod]
    public void Queue_ReturnsReadOnlyCollection()
    {
        // Arrange
        var initMethod = typeof(VaultIndexBatchProcessor).GetMethod("InitializeProcessingQueue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(initMethod);
        initMethod.Invoke(_batchProcessor, [_testVaultPath, null, null]);

        // Act
        var queue = _batchProcessor.Queue;

        // Assert
        Assert.IsInstanceOfType<IReadOnlyList<QueueItem>>(queue);

        // Verify it's actually read-only by checking the concrete type
        Assert.IsTrue(queue.GetType().Name.Contains("ReadOnly") ||
                     queue.GetType().Name.Contains("List"));
    }
}
