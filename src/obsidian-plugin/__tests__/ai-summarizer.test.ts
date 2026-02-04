// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { AISummarizer } from '../services/AISummarizer';
import { TextChunkingService } from '../utils/TextChunking';

// Mock fetch globally
global.fetch = jest.fn();

describe('AISummarizer', () => {
  let summarizer: AISummarizer;
  let mockFetch: jest.Mock;
  const testApiKey = 'test-api-key';

  beforeEach(() => {
    mockFetch = global.fetch as jest.Mock;
    mockFetch.mockClear();
    summarizer = new AISummarizer(testApiKey);
  });

  describe('summarizeWithVariables', () => {
    it('should return empty string for null or empty input', async () => {
      const result = await summarizer.summarizeWithVariables('');
      expect(result).toBe('');
      
      const result2 = await summarizer.summarizeWithVariables('   ');
      expect(result2).toBe('');
    });

    it('should call OpenAI API for short text', async () => {
      const shortText = 'This is a short text to summarize.';
      
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          choices: [
            {
              message: {
                content: 'This is a summary.'
              }
            }
          ]
        })
      });

      const result = await summarizer.summarizeWithVariables(shortText);

      expect(mockFetch).toHaveBeenCalledTimes(1);
      expect(mockFetch).toHaveBeenCalledWith(
        'https://api.openai.com/v1/chat/completions',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Authorization': `Bearer ${testApiKey}`
          })
        })
      );
      expect(result).toBe('This is a summary.');
    });

    it('should use chunking strategy for large text', async () => {
      const largeText = 'A'.repeat(10000); // Exceeds 8000 character limit
      
      // Mock responses for chunk summaries and final aggregation
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            choices: [{ message: { content: 'Summary of chunk 1' } }]
          })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            choices: [{ message: { content: 'Summary of chunk 2' } }]
          })
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            choices: [{ message: { content: 'Final aggregated summary' } }]
          })
        });

      const result = await summarizer.summarizeWithVariables(largeText);

      // Should have called OpenAI multiple times (for chunks + aggregation)
      expect(mockFetch.mock.calls.length).toBeGreaterThan(1);
      expect(result).toBe('Final aggregated summary');
    });

    it('should include variables in system prompt', async () => {
      const text = 'Test text';
      const variables = {
        course: 'MBA Strategy',
        type: 'video_transcript'
      };
      
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          choices: [{ message: { content: 'Summary with variables' } }]
        })
      });

      await summarizer.summarizeWithVariables(text, variables);

      const callArgs = mockFetch.mock.calls[0][1];
      const body = JSON.parse(callArgs.body);
      
      expect(body.messages[0].content).toContain('MBA Strategy');
      expect(body.messages[0].content).toContain('video_transcript');
    });

    it('should retry on retriable errors', async () => {
      const text = 'Test text';
      
      // First call fails with timeout, second succeeds
      mockFetch
        .mockRejectedValueOnce(new Error('timeout'))
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            choices: [{ message: { content: 'Success after retry' } }]
          })
        });

      const result = await summarizer.summarizeWithVariables(text);

      expect(mockFetch).toHaveBeenCalledTimes(2);
      expect(result).toBe('Success after retry');
    });

    it('should return empty string on non-retriable errors after max retries', async () => {
      const text = 'Test text';
      
      // All calls fail
      mockFetch.mockRejectedValue(new Error('timeout'));

      const result = await summarizer.summarizeWithVariables(text);

      // Should have retried max times (default is 4 total attempts: 1 initial + 3 retries)
      expect(mockFetch.mock.calls.length).toBeGreaterThan(1);
      expect(result).toBe('');
    }, 30000); // 30 second timeout for retries with delays

    it('should handle API errors gracefully', async () => {
      const text = 'Test text';
      
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        text: async () => 'Server error details'
      });

      const result = await summarizer.summarizeWithVariables(text);

      expect(result).toBe('');
    });
  });

  describe('chunking integration', () => {
    it('should use TextChunkingService correctly', async () => {
      const chunkingService = new TextChunkingService();
      const summarizer = new AISummarizer(testApiKey, chunkingService);
      
      const largeText = 'B'.repeat(20000);
      
      // Mock all OpenAI calls
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          choices: [{ message: { content: 'Mock summary' } }]
        })
      });

      await summarizer.summarizeWithVariables(largeText);

      // Verify that chunking was used (multiple API calls)
      expect(mockFetch.mock.calls.length).toBeGreaterThan(1);
    });
  });
});
