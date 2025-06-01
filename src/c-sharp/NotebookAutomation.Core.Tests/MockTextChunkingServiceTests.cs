using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Tests for the MockTextChunkingService class to ensure it works correctly for testing purposes.
    /// </summary>
    [TestClass]
    public class MockTextChunkingServiceTests
    {
        private MockTextChunkingService _mockChunkingService;

        [TestInitialize]
        public void SetUp()
        {
            _mockChunkingService = new MockTextChunkingService();
        }

        /// <summary>
        /// Tests that SplitTextIntoChunks returns predefined chunks when specified.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_WithPredefinedChunks_ReturnsPredefinedChunks()
        {
            // Arrange
            var predefinedChunks = new List<string> { "Chunk 1", "Chunk 2", "Chunk 3" };
            _mockChunkingService.PredefinedChunks = predefinedChunks;

            string inputText = "This is some test text that should be ignored.";

            // Act
            var result = _mockChunkingService.SplitTextIntoChunks(inputText, 100, 10);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(predefinedChunks, result);
            Assert.IsTrue(_mockChunkingService.SplitTextWasCalled);
            Assert.AreEqual(inputText, _mockChunkingService.LastInputText);
            Assert.AreEqual(100, _mockChunkingService.LastChunkSize);
            Assert.AreEqual(10, _mockChunkingService.LastOverlap);
        }

        /// <summary>
        /// Tests that SplitTextIntoChunks performs real chunking when no predefined chunks are provided.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_WithoutPredefinedChunks_PerformsChunking()
        {
            // Arrange
            string inputText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int chunkSize = 10;
            int overlap = 3;

            // Act
            var result = _mockChunkingService.SplitTextIntoChunks(inputText, chunkSize, overlap);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(_mockChunkingService.SplitTextWasCalled);
            Assert.AreEqual(inputText, _mockChunkingService.LastInputText);

            // Check that the chunks cover the whole input text
            string reconstructed = result[0];
            for (int i = 1; i < result.Count; i++)
            {
                reconstructed += result[i].Substring(overlap);
            }

            // Reconstructed should be at least as long as the original
            Assert.IsTrue(reconstructed.Length >= inputText.Length);
            Assert.IsTrue(reconstructed.StartsWith(inputText));
        }

        /// <summary>
        /// Tests that EstimateTokenCount returns the configured token count.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_WithConfiguredTokenCount_ReturnsConfiguredValue()
        {
            // Arrange
            _mockChunkingService.TokenCountToReturn = 42;
            string text = "Some text for token estimation";

            // Act
            var result = _mockChunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(42, result);
            Assert.IsTrue(_mockChunkingService.EstimateTokenWasCalled);
        }

        /// <summary>
        /// Tests that EstimateTokenCount falls back to character-based estimation.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_WithoutConfiguredTokenCount_UsesCharacterRatio()
        {
            // Arrange
            string text = "ABCDEFGHIJKLMNOP"; // 16 characters = 4 tokens at 4:1 ratio

            // Act
            var result = _mockChunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(4, result);
            Assert.IsTrue(_mockChunkingService.EstimateTokenWasCalled);
        }

        /// <summary>
        /// Tests that null text in SplitTextIntoChunks throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SplitTextIntoChunks_NullText_ThrowsArgumentNullException()
        {
            // Act
            _mockChunkingService.SplitTextIntoChunks(null, 100, 10);
        }

        /// <summary>
        /// Tests that invalid chunk size throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SplitTextIntoChunks_ZeroChunkSize_ThrowsArgumentOutOfRangeException()
        {
            // Act
            _mockChunkingService.SplitTextIntoChunks("Test text", 0, 10);
        }

        /// <summary>
        /// Tests that negative overlap throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SplitTextIntoChunks_NegativeOverlap_ThrowsArgumentOutOfRangeException()
        {
            // Act
            _mockChunkingService.SplitTextIntoChunks("Test text", 100, -5);
        }

        /// <summary>
        /// Tests that overlap greater than chunk size throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SplitTextIntoChunks_OverlapGreaterThanChunkSize_ThrowsArgumentException()
        {
            // Act
            _mockChunkingService.SplitTextIntoChunks("Test text", 10, 20);
        }
    }
}
