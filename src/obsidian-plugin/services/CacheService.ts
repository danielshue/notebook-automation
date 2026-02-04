// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/**
 * Cache entry with value and expiration time.
 */
interface CacheEntry<T> {
  value: T;
  expiresAt: number;
}

/**
 * Statistics about cache performance.
 */
export interface CacheStats {
  hits: number;
  misses: number;
  size: number;
  hitRate: number;
}

/**
 * Service interface for caching AI summaries and other data.
 */
export interface ICacheService {
  /**
   * Gets a value from the cache.
   * 
   * @param key - The cache key.
   * @returns The cached value, or null if not found or expired.
   */
  get<T>(key: string): T | null;

  /**
   * Sets a value in the cache with optional TTL.
   * 
   * @param key - The cache key.
   * @param value - The value to cache.
   * @param ttlSeconds - Time to live in seconds (default: 3600 = 1 hour).
   */
  set<T>(key: string, value: T, ttlSeconds?: number): void;

  /**
   * Checks if a key exists and is not expired.
   * 
   * @param key - The cache key.
   * @returns True if the key exists and is valid.
   */
  has(key: string): boolean;

  /**
   * Removes a key from the cache.
   * 
   * @param key - The cache key.
   */
  delete(key: string): void;

  /**
   * Clears all entries from the cache.
   */
  clear(): void;

  /**
   * Gets cache statistics.
   * 
   * @returns Statistics about cache performance.
   */
  getStats(): CacheStats;

  /**
   * Generates a cache key from content using a simple hash.
   * 
   * @param content - The content to hash.
   * @param prefix - Optional prefix for the key.
   * @returns A cache key string.
   */
  generateKey(content: string, prefix?: string): string;
}

/**
 * Implementation of in-memory cache service with TTL support.
 */
export class CacheService implements ICacheService {
  private cache: Map<string, CacheEntry<any>> = new Map();
  private hits = 0;
  private misses = 0;
  private defaultTTL = 3600; // 1 hour in seconds
  private cleanupInterval: NodeJS.Timeout | null = null;

  constructor(
    defaultTTLSeconds?: number,
    enableAutoCleanup = true
  ) {
    if (defaultTTLSeconds) {
      this.defaultTTL = defaultTTLSeconds;
    }

    // Start automatic cleanup of expired entries
    if (enableAutoCleanup) {
      this.startAutoCleanup();
    }
  }

  get<T>(key: string): T | null {
    const entry = this.cache.get(key);

    if (!entry) {
      this.misses++;
      return null;
    }

    // Check if expired
    if (entry.expiresAt < Date.now()) {
      this.cache.delete(key);
      this.misses++;
      return null;
    }

    this.hits++;
    return entry.value as T;
  }

  set<T>(key: string, value: T, ttlSeconds?: number): void {
    const ttl = ttlSeconds ?? this.defaultTTL;
    const expiresAt = Date.now() + (ttl * 1000);

    this.cache.set(key, {
      value,
      expiresAt
    });
  }

  has(key: string): boolean {
    const entry = this.cache.get(key);

    if (!entry) {
      return false;
    }

    // Check if expired
    if (entry.expiresAt < Date.now()) {
      this.cache.delete(key);
      return false;
    }

    return true;
  }

  delete(key: string): void {
    this.cache.delete(key);
  }

  clear(): void {
    this.cache.clear();
    this.hits = 0;
    this.misses = 0;
    console.log('[CacheService] Cache cleared');
  }

  getStats(): CacheStats {
    const totalRequests = this.hits + this.misses;
    const hitRate = totalRequests > 0 ? this.hits / totalRequests : 0;

    return {
      hits: this.hits,
      misses: this.misses,
      size: this.cache.size,
      hitRate
    };
  }

  generateKey(content: string, prefix?: string): string {
    // Simple hash function (djb2)
    let hash = 5381;
    for (let i = 0; i < content.length; i++) {
      hash = ((hash << 5) + hash) + content.charCodeAt(i);
    }

    const hashStr = Math.abs(hash).toString(36);
    return prefix ? `${prefix}:${hashStr}` : hashStr;
  }

  /**
   * Starts automatic cleanup of expired entries every 5 minutes.
   */
  private startAutoCleanup(): void {
    if (this.cleanupInterval) {
      return;
    }

    this.cleanupInterval = setInterval(() => {
      this.cleanupExpired();
    }, 5 * 60 * 1000); // Every 5 minutes

    console.log('[CacheService] Auto-cleanup started');
  }

  /**
   * Stops the automatic cleanup interval.
   */
  stopAutoCleanup(): void {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
      console.log('[CacheService] Auto-cleanup stopped');
    }
  }

  /**
   * Removes all expired entries from the cache.
   */
  private cleanupExpired(): void {
    const now = Date.now();
    let removed = 0;

    for (const [key, entry] of this.cache.entries()) {
      if (entry.expiresAt < now) {
        this.cache.delete(key);
        removed++;
      }
    }

    if (removed > 0) {
      console.log(`[CacheService] Cleaned up ${removed} expired entries`);
    }
  }

  /**
   * Destroys the cache service, stopping cleanup and clearing all entries.
   */
  destroy(): void {
    this.stopAutoCleanup();
    this.clear();
  }
}
