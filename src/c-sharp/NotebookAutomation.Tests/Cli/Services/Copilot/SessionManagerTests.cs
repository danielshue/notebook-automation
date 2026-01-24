// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using Moq;

using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for SessionManager class.
/// </summary>
/// <remarks>
/// Tests cover session save/load/list/delete/purge functionality
/// with file-based persistence in ~/.notebookautomation/sessions/.
/// </remarks>
[TestClass]
public class SessionManagerTests
{
    private Mock<ILogger<SessionManager>> mockLogger = null!;
    private SessionManager sessionManager = null!;
    private string testSessionsDir = null!;

    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<SessionManager>>();

        // Create a temporary sessions directory for testing
        testSessionsDir = Path.Combine(Path.GetTempPath(), "notebookautomation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testSessionsDir);

        // Create SessionManager with test directory using internal constructor
        sessionManager = new SessionManager(mockLogger.Object, testSessionsDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test directory
        if (Directory.Exists(testSessionsDir))
        {
            Directory.Delete(testSessionsDir, recursive: true);
        }
    }

    /// <summary>
    /// Tests that SaveSessionAsync creates a session file.
    /// </summary>
    [TestMethod]
    public async Task SaveSessionAsync_CreatesSessionFile()
    {
        // Arrange
        var session = CreateTestSession("test-session-1");

        // Act
        await sessionManager.SaveSessionAsync(session);

        // Assert
        var sessionFile = Path.Combine(testSessionsDir, $"{session.SessionId}.json");
        Assert.IsTrue(File.Exists(sessionFile), "Session file should be created");
    }

    /// <summary>
    /// Tests that LoadSessionAsync retrieves a saved session.
    /// </summary>
    [TestMethod]
    public async Task LoadSessionAsync_RetrievesSavedSession()
    {
        // Arrange
        var session = CreateTestSession("test-session-2");
        await sessionManager.SaveSessionAsync(session);

        // Act
        var loadedSession = await sessionManager.LoadSessionAsync(session.SessionId);

        // Assert
        Assert.IsNotNull(loadedSession, "Loaded session should not be null");
        Assert.AreEqual(session.SessionId, loadedSession.SessionId);
        Assert.AreEqual(session.Model, loadedSession.Model);
    }

    /// <summary>
    /// Tests that LoadSessionAsync returns null for non-existent session.
    /// </summary>
    [TestMethod]
    public async Task LoadSessionAsync_ReturnsNullForNonExistentSession()
    {
        // Act
        var loadedSession = await sessionManager.LoadSessionAsync("non-existent-session");

        // Assert
        Assert.IsNull(loadedSession, "Should return null for non-existent session");
    }

    /// <summary>
    /// Tests that ListSessionsAsync returns all saved sessions.
    /// </summary>
    [TestMethod]
    public async Task ListSessionsAsync_ReturnsAllSessions()
    {
        // Arrange
        var session1 = CreateTestSession("session-list-1");
        var session2 = CreateTestSession("session-list-2");
        var session3 = CreateTestSession("session-list-3");

        await sessionManager.SaveSessionAsync(session1);
        await sessionManager.SaveSessionAsync(session2);
        await sessionManager.SaveSessionAsync(session3);

        // Act
        var sessions = await sessionManager.ListSessionsAsync();

        // Assert
        Assert.AreEqual(3, sessions.Count, "Should return all 3 sessions");
    }

    /// <summary>
    /// Tests that ListSessionsAsync returns empty list when no sessions exist.
    /// </summary>
    [TestMethod]
    public async Task ListSessionsAsync_ReturnsEmptyListWhenNoSessions()
    {
        // Act
        var sessions = await sessionManager.ListSessionsAsync();

        // Assert
        Assert.AreEqual(0, sessions.Count, "Should return empty list");
    }

    /// <summary>
    /// Tests that DeleteSessionAsync removes a session.
    /// </summary>
    [TestMethod]
    public async Task DeleteSessionAsync_RemovesSession()
    {
        // Arrange
        var session = CreateTestSession("session-to-delete");
        await sessionManager.SaveSessionAsync(session);

        // Verify it exists
        var beforeDelete = await sessionManager.LoadSessionAsync(session.SessionId);
        Assert.IsNotNull(beforeDelete, "Session should exist before delete");

        // Act
        await sessionManager.DeleteSessionAsync(session.SessionId);

        // Assert
        var afterDelete = await sessionManager.LoadSessionAsync(session.SessionId);
        Assert.IsNull(afterDelete, "Session should be null after delete");
    }

    /// <summary>
    /// Tests that DeleteSessionAsync handles non-existent session gracefully.
    /// </summary>
    [TestMethod]
    public async Task DeleteSessionAsync_HandlesNonExistentSessionGracefully()
    {
        // Act & Assert - should not throw
        await sessionManager.DeleteSessionAsync("non-existent-session");
    }

    /// <summary>
    /// Tests that PurgeOldSessionsAsync removes sessions older than retention period.
    /// </summary>
    [TestMethod]
    public async Task PurgeOldSessionsAsync_RemovesOldSessions()
    {
        // Arrange
        var oldSession = CreateTestSession("old-session", DateTime.UtcNow.AddDays(-40));
        var recentSession = CreateTestSession("recent-session", DateTime.UtcNow.AddDays(-5));

        await sessionManager.SaveSessionAsync(oldSession);
        await sessionManager.SaveSessionAsync(recentSession);

        // Act
        var deletedCount = await sessionManager.PurgeOldSessionsAsync(30);

        // Assert
        Assert.AreEqual(1, deletedCount, "Should delete 1 old session");

        var sessions = await sessionManager.ListSessionsAsync();
        Assert.AreEqual(1, sessions.Count, "Should have 1 remaining session");
        Assert.AreEqual(recentSession.SessionId, sessions[0].SessionId, "Recent session should remain");
    }

    /// <summary>
    /// Tests that GetLastSessionAsync returns most recently accessed session.
    /// </summary>
    [TestMethod]
    public async Task GetLastSessionAsync_ReturnsMostRecentSession()
    {
        // Arrange
        var oldSession = CreateTestSession("oldest", DateTime.UtcNow.AddDays(-10));
        var middleSession = CreateTestSession("middle", DateTime.UtcNow.AddDays(-5));
        var newestSession = CreateTestSession("newest", DateTime.UtcNow.AddDays(-1));

        // Save in random order
        await sessionManager.SaveSessionAsync(middleSession);
        await sessionManager.SaveSessionAsync(oldSession);
        await sessionManager.SaveSessionAsync(newestSession);

        // Act
        var lastSession = await sessionManager.GetLastSessionAsync();

        // Assert
        Assert.IsNotNull(lastSession, "Should return a session");
        Assert.AreEqual(newestSession.SessionId, lastSession.SessionId, "Should return newest session");
    }

    /// <summary>
    /// Tests that GetLastSessionAsync returns null when no sessions exist.
    /// </summary>
    [TestMethod]
    public async Task GetLastSessionAsync_ReturnsNullWhenNoSessions()
    {
        // Act
        var lastSession = await sessionManager.GetLastSessionAsync();

        // Assert
        Assert.IsNull(lastSession, "Should return null when no sessions exist");
    }

    /// <summary>
    /// Tests that saving a session updates the index file.
    /// </summary>
    [TestMethod]
    public async Task SaveSessionAsync_UpdatesIndexFile()
    {
        // Arrange
        var session = CreateTestSession("indexed-session");

        // Act
        await sessionManager.SaveSessionAsync(session);

        // Assert
        var indexFile = Path.Combine(testSessionsDir, "index.json");
        Assert.IsTrue(File.Exists(indexFile), "Index file should be created");

        var indexContent = await File.ReadAllTextAsync(indexFile);
        Assert.IsTrue(indexContent.Contains(session.SessionId), "Index should contain session ID");
    }

    /// <summary>
    /// Tests that updating an existing session updates the index.
    /// </summary>
    [TestMethod]
    public async Task SaveSessionAsync_UpdatesExistingSessionInIndex()
    {
        // Arrange
        var session = CreateTestSession("update-session");
        await sessionManager.SaveSessionAsync(session);

        var updatedSession = session with { MessageCount = 10, LastAccessedAt = DateTime.UtcNow };

        // Act
        await sessionManager.SaveSessionAsync(updatedSession);

        // Assert
        var sessions = await sessionManager.ListSessionsAsync();
        Assert.AreEqual(1, sessions.Count, "Should still have only 1 session");
        Assert.AreEqual(10, sessions[0].MessageCount, "Message count should be updated");
    }

    /// <summary>
    /// Tests constructor with null logger throws.
    /// </summary>
    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new SessionManager(null!, testSessionsDir));
    }

    /// <summary>
    /// Helper method to create a test session metadata.
    /// </summary>
    private static CopilotSessionMetadata CreateTestSession(string sessionId, DateTime? createdAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        return new CopilotSessionMetadata(
            SessionId: sessionId,
            CreatedAt: created,
            LastAccessedAt: created,
            Model: "gpt-4",
            MessageCount: 5);
    }
}
