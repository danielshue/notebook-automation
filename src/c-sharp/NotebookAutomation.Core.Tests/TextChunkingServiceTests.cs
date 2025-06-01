using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Test suite for the TextChunkingService class, verifying text splitting and token estimation functionality.
    /// </summary>
    [TestClass]
    public class TextChunkingServiceTests
    {
        private ITextChunkingService _chunkingService;

        [TestInitialize]
        public void SetUp()
        {
            _chunkingService = new TextChunkingService();
        }

        #region SplitTextIntoChunks Tests

        /// <summary>
        /// Tests that text shorter than chunk size returns a single chunk.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_ShortText_ReturnsSingleChunk()
        {
            // Arrange
            var text = "This is a short text.";
            var chunkSize = 100;
            var overlap = 10;

            // Act
            var result = _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(text, result[0]);
        }

        /// <summary>
        /// Tests that text longer than chunk size is split into multiple chunks.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_LongText_ReturnsMultipleChunks()
        {
            // Arrange
            var text = new string('A', 250); // 250 characters
            var chunkSize = 100;
            var overlap = 20;

            // Act
            var result = _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 1);

            // First chunk should be 100 characters
            Assert.AreEqual(100, result[0].Length);

            // Second chunk should start at position 80 (100 - 20 overlap)
            Assert.AreEqual(100, result[1].Length);

            // Verify overlap exists between chunks
            var firstChunkEnd = result[0].Substring(80);
            var secondChunkStart = result[1].Substring(0, 20);
            Assert.AreEqual(firstChunkEnd, secondChunkStart);
        }

        /// <summary>
        /// Tests that empty text returns an empty list.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_EmptyText_ReturnsEmptyList()
        {
            // Arrange
            var text = string.Empty;
            var chunkSize = 100;
            var overlap = 10;

            // Act
            var result = _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Tests that null text throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SplitTextIntoChunks_NullText_ThrowsArgumentNullException()
        {
            // Arrange
            string text = null;
            var chunkSize = 100;
            var overlap = 10;

            // Act
            _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);
        }

        /// <summary>
        /// Tests that invalid chunk size throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SplitTextIntoChunks_InvalidChunkSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var text = "Test text";
            var chunkSize = 0;
            var overlap = 10;

            // Act
            _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);
        }

        /// <summary>
        /// Tests that invalid overlap throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SplitTextIntoChunks_NegativeOverlap_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var text = "Test text";
            var chunkSize = 100;
            var overlap = -5;

            // Act
            _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);
        }        /// <summary>
        /// Tests that overlap larger than chunk size throws ArgumentException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SplitTextIntoChunks_OverlapLargerThanChunkSize_ThrowsArgumentException()
        {
            // Arrange
            var text = "Test text";
            var chunkSize = 50;
            var overlap = 100;

            // Act
            _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);
        }        /// <summary>
        /// Tests chunking with zero overlap works correctly.
        /// </summary>
        [TestMethod]
        public void SplitTextIntoChunks_ZeroOverlap_WorksCorrectly()
        {
            // Arrange
            var text = new string('A', 200); // 200 characters
            var chunkSize = 100;
            var overlap = 0;

            // Act
            var result = _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(100, result[0].Length);
            Assert.AreEqual(100, result[1].Length);

            // With zero overlap, chunks should be completely different
            Assert.AreEqual(new string('A', 100), result[0]);
            Assert.AreEqual(new string('A', 100), result[1]);
        }

        #endregion

        #region EstimateTokenCount Tests        /// <summary>
        /// Tests token estimation for normal text.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_NormalText_ReturnsExpectedCount()
        {
            // Arrange
            var text = "This is a test with exactly forty-four chars!"; // 45 characters
            var expectedTokens = (int)Math.Ceiling(45 / 4.0); // 12 tokens

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(expectedTokens, result);
        }

        /// <summary>
        /// Tests token estimation for empty text.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_EmptyText_ReturnsZero()
        {
            // Arrange
            var text = string.Empty;

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests token estimation for null text.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_NullText_ReturnsZero()
        {
            // Arrange
            string text = null;

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests token estimation for whitespace text.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_WhitespaceText_ReturnsZero()
        {
            // Arrange
            var text = "   \t\n  ";

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(0, result);
        }

        /// <summary>
        /// Tests token estimation for single character.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_SingleCharacter_ReturnsOne()
        {
            // Arrange
            var text = "A";

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests token estimation for text with exactly 4 characters per token.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_ExactlyFourCharacters_ReturnsOneToken()
        {
            // Arrange
            var text = "ABCD"; // Exactly 4 characters

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(1, result);
        }

        /// <summary>
        /// Tests token estimation for large text.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_LargeText_ReturnsCorrectEstimate()
        {
            // Arrange
            var text = new string('A', 10000); // 10,000 characters
            var expectedTokens = (int)Math.Ceiling(10000 / 4.0); // 2,500 tokens

            // Act
            var result = _chunkingService.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(expectedTokens, result);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests the integration of chunking and token estimation.
        /// </summary>
        [TestMethod]
        public void SplitAndEstimate_Integration_WorksCorrectly()
        {
            // Arrange
            var text = new string('A', 1000); // 1000 characters
            var chunkSize = 400;
            var overlap = 50;

            // Act
            var chunks = _chunkingService.SplitTextIntoChunks(text, chunkSize, overlap);
            var totalEstimatedTokens = 0;
            foreach (var chunk in chunks)
            {
                totalEstimatedTokens += _chunkingService.EstimateTokenCount(chunk);
            }

            // Assert
            Assert.IsTrue(chunks.Count > 1);
            Assert.IsTrue(totalEstimatedTokens > 0);

            // Due to overlap, total estimated tokens should be more than original text tokens
            var originalTokens = _chunkingService.EstimateTokenCount(text);
            Assert.IsTrue(totalEstimatedTokens >= originalTokens);
        }

        #endregion
    }
}
