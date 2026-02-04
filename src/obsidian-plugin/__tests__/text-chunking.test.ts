// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { TextChunkingService } from '../utils/TextChunking';

describe('TextChunkingService', () => {
  let service: TextChunkingService;

  beforeEach(() => {
    service = new TextChunkingService();
  });

  describe('splitTextIntoChunks', () => {
    it('should return empty array for null or empty text', () => {
      expect(service.splitTextIntoChunks('', 100, 10)).toEqual([]);
      expect(service.splitTextIntoChunks(null as any, 100, 10)).toEqual([]);
    });

    it('should throw error for invalid chunk size', () => {
      expect(() => service.splitTextIntoChunks('test', 0, 10)).toThrow('Chunk size must be positive');
      expect(() => service.splitTextIntoChunks('test', -1, 10)).toThrow('Chunk size must be positive');
    });

    it('should throw error for negative overlap', () => {
      expect(() => service.splitTextIntoChunks('test', 100, -1)).toThrow('Overlap cannot be negative');
    });

    it('should throw error when overlap >= chunk size', () => {
      expect(() => service.splitTextIntoChunks('test', 100, 100)).toThrow('Overlap must be less than chunk size');
      expect(() => service.splitTextIntoChunks('test', 100, 101)).toThrow('Overlap must be less than chunk size');
    });

    it('should split text into single chunk when text is smaller than chunk size', () => {
      const text = 'This is a short text';
      const chunks = service.splitTextIntoChunks(text, 100, 10);
      
      expect(chunks).toHaveLength(1);
      expect(chunks[0]).toBe(text);
    });

    it('should split text into multiple chunks with overlap', () => {
      const text = 'A'.repeat(250); // 250 characters
      const chunks = service.splitTextIntoChunks(text, 100, 20);
      
      expect(chunks.length).toBeGreaterThan(1);
      
      // Check that chunks have the right size
      expect(chunks[0].length).toBe(100);
      expect(chunks[1].length).toBe(100);
      
      // Check overlap - last 20 chars of chunk 0 should match first 20 chars of chunk 1
      expect(chunks[0].substring(80)).toBe(chunks[1].substring(0, 20));
    });

    it('should handle text that divides evenly into chunks', () => {
      const text = 'A'.repeat(300);
      const chunks = service.splitTextIntoChunks(text, 100, 0);
      
      expect(chunks).toHaveLength(3);
      expect(chunks[0].length).toBe(100);
      expect(chunks[1].length).toBe(100);
      expect(chunks[2].length).toBe(100);
    });

    it('should match C# implementation behavior for 8000 char chunks with 500 char overlap', () => {
      // Test the actual values used in the AISummarizer
      const text = 'B'.repeat(20000); // 20,000 characters
      const chunks = service.splitTextIntoChunks(text, 8000, 500);
      
      expect(chunks.length).toBeGreaterThan(1);
      expect(chunks[0].length).toBe(8000);
      
      // Verify overlap
      expect(chunks[0].substring(7500)).toBe(chunks[1].substring(0, 500));
    });
  });

  describe('estimateTokenCount', () => {
    it('should return 0 for null or empty text', () => {
      expect(service.estimateTokenCount('')).toBe(0);
      expect(service.estimateTokenCount('   ')).toBe(0);
      expect(service.estimateTokenCount(null as any)).toBe(0);
    });

    it('should estimate tokens using 4:1 character-to-token ratio', () => {
      // 100 characters = 25 tokens (ceiling)
      expect(service.estimateTokenCount('A'.repeat(100))).toBe(25);
      
      // 99 characters = 25 tokens (ceiling)
      expect(service.estimateTokenCount('A'.repeat(99))).toBe(25);
      
      // 101 characters = 26 tokens (ceiling)
      expect(service.estimateTokenCount('A'.repeat(101))).toBe(26);
    });

    it('should use ceiling for fractional token counts', () => {
      // 10 characters / 4 = 2.5 -> 3 tokens
      expect(service.estimateTokenCount('A'.repeat(10))).toBe(3);
      
      // 5 characters / 4 = 1.25 -> 2 tokens
      expect(service.estimateTokenCount('A'.repeat(5))).toBe(2);
      
      // 4 characters / 4 = 1 token
      expect(service.estimateTokenCount('A'.repeat(4))).toBe(1);
    });

    it('should handle realistic text samples', () => {
      const sampleText = 'This is a typical MBA course transcript with various technical terms and frameworks.';
      const tokens = service.estimateTokenCount(sampleText);
      
      // Should be roughly length / 4 with ceiling
      expect(tokens).toBeGreaterThan(0);
      expect(tokens).toBe(Math.ceil(sampleText.length / 4.0));
    });
  });
});
