// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Models;

/// <summary>
/// Unit tests for the QueueChangedEventArgs class.
/// </summary>
[TestClass]
public class QueueChangedEventArgsTests
{

    /// <summary>
    /// Tests constructor with valid queue and no changed item.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidQueue_SetsPropertiesCorrectly()
    {
        // Arrange
        var queueItems = new List<QueueItem>
        {
            new("file1.txt", "PDF"),
            new("file2.txt", "VIDEO")
        };

        // Act
        var eventArgs = new QueueChangedEventArgs(queueItems);

        // Assert
        Assert.AreEqual(2, eventArgs.Queue.Count);
        Assert.AreEqual("file1.txt", eventArgs.Queue[0].FilePath);
        Assert.AreEqual("file2.txt", eventArgs.Queue[1].FilePath);
        Assert.IsNull(eventArgs.ChangedItem);
    }

    /// <summary>
    /// Tests constructor with valid queue and changed item.
    /// </summary>
    [TestMethod]
    public void Constructor_WithQueueAndChangedItem_SetsPropertiesCorrectly()
    {
        // Arrange
        var queueItems = new List<QueueItem>
        {
            new("file1.txt", "PDF"),
            new("file2.txt", "VIDEO")
        };
        var changedItem = new QueueItem("file3.txt", "MARKDOWN");

        // Act
        var eventArgs = new QueueChangedEventArgs(queueItems, changedItem);

        // Assert
        Assert.AreEqual(2, eventArgs.Queue.Count);
        Assert.IsNotNull(eventArgs.ChangedItem);
        Assert.AreEqual("file3.txt", eventArgs.ChangedItem.FilePath);
        Assert.AreEqual(DocumentProcessingStatus.Waiting, eventArgs.ChangedItem.Status);
    }

    /// <summary>
    /// Tests constructor with empty queue.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyQueue_CreatesValidEventArgs()
    {
        // Arrange
        var queueItems = new List<QueueItem>();

        // Act
        var eventArgs = new QueueChangedEventArgs(queueItems);

        // Assert
        Assert.AreEqual(0, eventArgs.Queue.Count);
        Assert.IsNull(eventArgs.ChangedItem);
    }

    /// <summary>
    /// Tests that Queue property is read-only and immutable.
    /// </summary>
    [TestMethod]
    public void Queue_IsReadOnly()
    {
        // Arrange
        var queueItems = new List<QueueItem>
        {
            new("file1.txt", "processing")
        };
        var eventArgs = new QueueChangedEventArgs(queueItems);

        // Assert type
        Assert.IsInstanceOfType(eventArgs.Queue, typeof(IReadOnlyList<QueueItem>));

        // Assert immutability (modification should throw)
        Assert.ThrowsException<NotSupportedException>(() => ((IList<QueueItem>)eventArgs.Queue).Add(new QueueItem("file2.txt", "processing")));
    }

    /// <summary>
    /// Tests that modifying the original list doesn't affect the event args queue.
    /// </summary>
    [TestMethod]
    public void Queue_IsIndependentOfOriginalList()
    {
        // Arrange
        var queueItems = new List<QueueItem>
        {
            new("file1.txt", "PDF")
        };
        var eventArgs = new QueueChangedEventArgs(queueItems);

        // Act
        queueItems.Add(new QueueItem("file2.txt", "VIDEO"));

        // Assert
        Assert.AreEqual(1, eventArgs.Queue.Count);
        Assert.AreEqual(2, queueItems.Count);
    }

    /// <summary>
    /// Tests that the class properly inherits from EventArgs.
    /// </summary>
    [TestMethod]
    public void QueueChangedEventArgs_InheritsFromEventArgs()
    {
        // Arrange
        var queueItems = new List<QueueItem>();
        var eventArgs = new QueueChangedEventArgs(queueItems);

        // Act & Assert
        Assert.IsInstanceOfType<EventArgs>(eventArgs);
    }

    /// <summary>
    /// Tests constructor with null changed item explicitly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithExplicitNullChangedItem_SetsPropertiesCorrectly()
    {
        // Arrange
        var queueItems = new List<QueueItem>
        {
            new("file1.txt", "PDF")
        };

        // Act
        var eventArgs = new QueueChangedEventArgs(queueItems, null);

        // Assert
        Assert.AreEqual(1, eventArgs.Queue.Count);
        Assert.IsNull(eventArgs.ChangedItem);
    }
}