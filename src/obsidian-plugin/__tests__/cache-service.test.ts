// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { CacheService } from '../services/CacheService';

describe('CacheService', () => {
  let cacheService: CacheService;

  beforeEach(() => {
    cacheService = new CacheService(60, false); // 60 second TTL, no auto-cleanup for tests
  });

  afterEach(() => {
    cacheService.destroy();
  });

  describe('set and get', () => {
    it('should store and retrieve values', () => {
      cacheService.set('key1', 'value1');
      const result = cacheService.get<string>('key1');
      
      expect(result).toBe('value1');
    });

    it('should return null for non-existent keys', () => {
      const result = cacheService.get<string>('nonexistent');
      
      expect(result).toBeNull();
    });

    it('should support different data types', () => {
      cacheService.set('string', 'test');
      cacheService.set('number', 42);
      cacheService.set('object', { foo: 'bar' });
      cacheService.set('array', [1, 2, 3]);
      
      expect(cacheService.get<string>('string')).toBe('test');
      expect(cacheService.get<number>('number')).toBe(42);
      expect(cacheService.get<any>('object')).toEqual({ foo: 'bar' });
      expect(cacheService.get<number[]>('array')).toEqual([1, 2, 3]);
    });
  });

  describe('TTL expiration', () => {
    it('should expire entries after TTL', async () => {
      cacheService.set('key1', 'value1', 0.1); // 0.1 second TTL
      
      expect(cacheService.get<string>('key1')).toBe('value1');
      
      // Wait for expiration
      await new Promise(resolve => setTimeout(resolve, 150));
      
      expect(cacheService.get<string>('key1')).toBeNull();
    });

    it('should use default TTL when not specified', () => {
      cacheService.set('key1', 'value1');
      
      expect(cacheService.has('key1')).toBe(true);
    });

    it('should support custom TTL per entry', () => {
      cacheService.set('short', 'value1', 1);
      cacheService.set('long', 'value2', 3600);
      
      expect(cacheService.has('short')).toBe(true);
      expect(cacheService.has('long')).toBe(true);
    });
  });

  describe('has', () => {
    it('should return true for existing non-expired keys', () => {
      cacheService.set('key1', 'value1');
      
      expect(cacheService.has('key1')).toBe(true);
    });

    it('should return false for non-existent keys', () => {
      expect(cacheService.has('nonexistent')).toBe(false);
    });

    it('should return false for expired keys', async () => {
      cacheService.set('key1', 'value1', 0.1);
      
      await new Promise(resolve => setTimeout(resolve, 150));
      
      expect(cacheService.has('key1')).toBe(false);
    });
  });

  describe('delete', () => {
    it('should remove entries from cache', () => {
      cacheService.set('key1', 'value1');
      
      expect(cacheService.has('key1')).toBe(true);
      
      cacheService.delete('key1');
      
      expect(cacheService.has('key1')).toBe(false);
    });

    it('should not error when deleting non-existent keys', () => {
      expect(() => cacheService.delete('nonexistent')).not.toThrow();
    });
  });

  describe('clear', () => {
    it('should remove all entries', () => {
      cacheService.set('key1', 'value1');
      cacheService.set('key2', 'value2');
      cacheService.set('key3', 'value3');
      
      expect(cacheService.getStats().size).toBe(3);
      
      cacheService.clear();
      
      expect(cacheService.getStats().size).toBe(0);
    });

    it('should reset statistics', () => {
      cacheService.set('key1', 'value1');
      cacheService.get('key1'); // hit
      cacheService.get('key2'); // miss
      
      cacheService.clear();
      
      const stats = cacheService.getStats();
      expect(stats.hits).toBe(0);
      expect(stats.misses).toBe(0);
    });
  });

  describe('getStats', () => {
    it('should track hits and misses', () => {
      cacheService.set('key1', 'value1');
      
      cacheService.get('key1'); // hit
      cacheService.get('key1'); // hit
      cacheService.get('key2'); // miss
      cacheService.get('key3'); // miss
      cacheService.get('key3'); // miss
      
      const stats = cacheService.getStats();
      
      expect(stats.hits).toBe(2);
      expect(stats.misses).toBe(3);
      expect(stats.hitRate).toBeCloseTo(0.4);
    });

    it('should return 0 hit rate when no requests', () => {
      const stats = cacheService.getStats();
      
      expect(stats.hitRate).toBe(0);
    });

    it('should reflect cache size', () => {
      cacheService.set('key1', 'value1');
      cacheService.set('key2', 'value2');
      
      const stats = cacheService.getStats();
      
      expect(stats.size).toBe(2);
    });
  });

  describe('generateKey', () => {
    it('should generate consistent keys for same content', () => {
      const content = 'test content for caching';
      
      const key1 = cacheService.generateKey(content);
      const key2 = cacheService.generateKey(content);
      
      expect(key1).toBe(key2);
    });

    it('should generate different keys for different content', () => {
      const key1 = cacheService.generateKey('content 1');
      const key2 = cacheService.generateKey('content 2');
      
      expect(key1).not.toBe(key2);
    });

    it('should support prefix', () => {
      const key = cacheService.generateKey('content', 'summary');
      
      expect(key).toContain('summary:');
    });

    it('should generate same key for same content and prefix', () => {
      const key1 = cacheService.generateKey('test', 'prefix');
      const key2 = cacheService.generateKey('test', 'prefix');
      
      expect(key1).toBe(key2);
    });
  });

  describe('integration with AI summarization', () => {
    it('should cache and retrieve summaries', () => {
      const content = 'This is a long document that needs to be summarized...';
      const summary = 'This is the summary of the document.';
      
      const cacheKey = cacheService.generateKey(content, 'summary');
      
      // First call - cache miss
      let cachedSummary = cacheService.get<string>(cacheKey);
      expect(cachedSummary).toBeNull();
      
      // Store summary
      cacheService.set(cacheKey, summary);
      
      // Second call - cache hit
      cachedSummary = cacheService.get<string>(cacheKey);
      expect(cachedSummary).toBe(summary);
    });

    it('should handle cache expiration for summaries', async () => {
      const content = 'Document content';
      const summary = 'Summary';
      const cacheKey = cacheService.generateKey(content, 'summary');
      
      cacheService.set(cacheKey, summary, 0.1); // Short TTL
      
      expect(cacheService.get<string>(cacheKey)).toBe(summary);
      
      await new Promise(resolve => setTimeout(resolve, 150));
      
      expect(cacheService.get<string>(cacheKey)).toBeNull();
    });
  });
});
