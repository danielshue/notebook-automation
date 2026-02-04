// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { TextChunkingService } from '../utils/TextChunking';
import { IPromptService } from './PromptService';
import { ICacheService } from './CacheService';

/**
 * Configuration for timeout and retry behavior.
 */
export interface TimeoutConfig {
  /** Maximum number of retry attempts for failed API calls. */
  maxRetryAttempts: number;
  /** Base delay in seconds for exponential backoff. */
  baseRetryDelaySeconds: number;
  /** Maximum delay in seconds for exponential backoff. */
  maxRetryDelaySeconds: number;
  /** Maximum number of parallel chunk processing operations. */
  maxChunkParallelism: number;
  /** Rate limit delay in milliseconds between chunk processing starts. */
  chunkRateLimitMs: number;
}

/**
 * Default timeout configuration.
 */
const DEFAULT_TIMEOUT_CONFIG: TimeoutConfig = {
  maxRetryAttempts: 3,
  baseRetryDelaySeconds: 2,
  maxRetryDelaySeconds: 30,
  maxChunkParallelism: 3,
  chunkRateLimitMs: 500
};

/**
 * Interface for AI summarization service.
 */
export interface IAISummarizer {
  /**
   * Generates an AI-powered summary for the given text.
   * Automatically selects between direct summarization and chunked processing based on text length.
   * 
   * @param inputText - The text content to summarize.
   * @param variables - Optional dictionary of variables for prompt template substitution.
   * @param promptFileName - Optional prompt template filename (without extension).
   * @returns The generated summary text, or null if no AI service is available.
   */
  summarizeWithVariables(
    inputText: string,
    variables?: Record<string, string>,
    promptFileName?: string
  ): Promise<string | null>;
}

/**
 * Provides AI-powered text summarization using OpenAI API.
 * Implements intelligent chunking strategies for large text processing.
 */
export class AISummarizer implements IAISummarizer {
  private readonly chunkingService: TextChunkingService;
  private readonly timeoutConfig: TimeoutConfig;
  // NOTE: Despite the naming, these are character counts, not token counts
  // This maintains compatibility with the C# implementation
  private readonly maxChunkTokens = 8000; // Maximum characters per chunk
  private readonly overlapTokens = 500; // Character overlap between chunks

  /**
   * Creates a new AISummarizer instance.
   * 
   * @param apiKey - OpenAI API key for authentication.
   * @param chunkingService - Optional text chunking service. If not provided, creates a default instance.
   * @param timeoutConfig - Optional timeout configuration. If not provided, uses defaults.
   * @param promptService - Optional prompt service for template loading.
   * @param cacheService - Optional cache service for caching summaries.
   * @throws {Error} If API key is invalid or empty.
   */
  constructor(
    private readonly apiKey: string,
    chunkingService?: TextChunkingService,
    timeoutConfig?: TimeoutConfig,
    private readonly promptService?: IPromptService,
    private readonly cacheService?: ICacheService
  ) {
    // Validate API key
    if (!apiKey || apiKey.trim().length === 0) {
      throw new Error('OpenAI API key is required');
    }
    if (!apiKey.startsWith('sk-')) {
      console.warn('[AISummarizer] API key does not start with "sk-" - this may be invalid');
    }
    
    this.chunkingService = chunkingService || new TextChunkingService();
    this.timeoutConfig = timeoutConfig || DEFAULT_TIMEOUT_CONFIG;
  }

  /**
   * Generates an AI-powered summary for the given text using OpenAI.
   * Automatically selects between direct summarization and chunked processing based on text length.
   * Checks cache before making API calls if cache service is available.
   * 
   * @param inputText - The text content to summarize.
   * @param variables - Optional dictionary of variables for prompt template substitution.
   * @param promptFileName - Optional prompt template filename (defaults to "final_summary_prompt").
   * @returns The generated summary text, or null if the operation fails.
   */
  async summarizeWithVariables(
    inputText: string,
    variables?: Record<string, string>,
    promptFileName?: string
  ): Promise<string | null> {
    if (!inputText || inputText.trim().length === 0) {
      console.warn('[AISummarizer] Input text is null or empty');
      return '';
    }

    const effectivePromptFileName = promptFileName || 'final_summary_prompt';

    // Check cache first if available
    if (this.cacheService) {
      const cacheKey = this.cacheService.generateKey(
        inputText + JSON.stringify(variables || {}) + effectivePromptFileName,
        'summary'
      );
      const cachedSummary = this.cacheService.get<string>(cacheKey);
      
      if (cachedSummary) {
        console.log('[AISummarizer] Using cached summary');
        return cachedSummary;
      }
    }

    // Load prompt template if service is available
    let systemPrompt = this.buildSystemPrompt(variables);
    if (this.promptService) {
      const template = await this.promptService.loadTemplate(effectivePromptFileName);
      if (template) {
        systemPrompt = variables 
          ? this.promptService.substituteVariables(template, variables)
          : template;
        console.log('[AISummarizer] Using custom prompt template');
      }
    }

    console.log(`[AISummarizer] Using prompt: ${effectivePromptFileName}`);

    // Check if input likely exceeds character limits and needs chunking
    let summary: string | null = null;
    if (inputText.length > this.maxChunkTokens) {
      console.log(`[AISummarizer] Input text is large (${inputText.length} characters). Using chunking strategy.`);
      summary = await this.summarizeWithChunking(inputText, variables, systemPrompt);
    } else {
      // For smaller texts, use direct approach
      summary = await this.summarizeDirect(inputText, variables, systemPrompt);
    }

    // Cache the result if cache service is available
    if (summary && this.cacheService) {
      const cacheKey = this.cacheService.generateKey(
        inputText + JSON.stringify(variables || {}) + effectivePromptFileName,
        'summary'
      );
      this.cacheService.set(cacheKey, summary, 3600); // Cache for 1 hour
      console.log('[AISummarizer] Cached summary');
    }

    return summary;
  }

  /**
   * Summarizes text directly using a single OpenAI API call.
   * 
   * @param inputText - The text to summarize.
   * @param variables - Optional variables for prompt enhancement.
   * @param systemPrompt - Optional custom system prompt (uses default if not provided).
   * @returns The summary text.
   */
  private async summarizeDirect(
    inputText: string,
    variables?: Record<string, string>,
    systemPrompt?: string
  ): Promise<string | null> {
    try {
      const prompt = systemPrompt || this.buildSystemPrompt(variables);
      const result = await this.callOpenAI(prompt, inputText);
      return result;
    } catch (error) {
      console.error('[AISummarizer] Failed to generate summary:', error);
      return '';
    }
  }

  /**
   * Summarizes text using chunking to handle large inputs.
   * Implements a two-stage process: individual chunk summarization followed by aggregation.
   * 
   * @param inputText - The text content to summarize.
   * @param variables - Optional variables for prompt enhancement.
   * @param systemPrompt - Optional custom system prompt (uses default if not provided).
   * @returns The consolidated summary combining all chunk summaries.
   */
  private async summarizeWithChunking(
    inputText: string,
    variables?: Record<string, string>,
    systemPrompt?: string
  ): Promise<string | null> {
    try {
      console.log('[AISummarizer] Starting chunked summarization process');

      // Split text into character-based chunks
      const chunks = this.chunkingService.splitTextIntoChunks(
        inputText,
        this.maxChunkTokens,
        this.overlapTokens
      );

      console.log(`[AISummarizer] Split into ${chunks.length} chunks`);

      if (chunks.length === 0) {
        console.warn('[AISummarizer] No valid chunks were generated');
        return '';
      }

      // Process chunks sequentially to respect rate limits
      const chunkSummaries = await this.processChunksSequentially(chunks, variables);

      console.log(`[AISummarizer] Completed processing ${chunkSummaries.length}/${chunks.length} chunks`);

      if (chunkSummaries.length === 0) {
        console.warn('[AISummarizer] No chunk summaries generated');
        return '';
      }

      // Aggregate chunk summaries into final summary
      const aggregatedText = chunkSummaries.join('\n\n');
      const finalSummary = await this.aggregateChunkSummaries(aggregatedText, variables);

      return finalSummary;
    } catch (error) {
      console.error('[AISummarizer] Failed to generate chunked summary:', error);
      return '';
    }
  }

  /**
   * Processes chunks sequentially with rate limiting.
   * 
   * @param chunks - The text chunks to process.
   * @param variables - Variables for prompt enhancement.
   * @returns Array of chunk summaries.
   */
  private async processChunksSequentially(
    chunks: string[],
    variables?: Record<string, string>
  ): Promise<string[]> {
    console.log(`[AISummarizer] Processing ${chunks.length} chunks sequentially`);

    const chunkSummaries: string[] = [];
    const systemPrompt = this.buildChunkSystemPrompt(variables);

    for (let i = 0; i < chunks.length; i++) {
      // Skip processing if chunk is only whitespace
      if (!chunks[i] || chunks[i].trim().length === 0) {
        console.warn(`[AISummarizer] Skipping chunk ${i + 1} as it contains only whitespace`);
        continue;
      }

      console.log(`[AISummarizer] Processing chunk ${i + 1}/${chunks.length}`);

      try {
        const summary = await this.callOpenAI(systemPrompt, chunks[i]);
        if (summary && summary.trim().length > 0) {
          chunkSummaries.push(summary.trim());
          console.log(`[AISummarizer] Chunk ${i + 1} summary completed`);
        } else {
          console.warn(`[AISummarizer] Chunk ${i + 1} returned empty summary`);
        }
      } catch (error) {
        console.error(`[AISummarizer] Failed to process chunk ${i + 1}:`, error);
      }

      // Rate limiting between chunks
      if (i < chunks.length - 1 && this.timeoutConfig.chunkRateLimitMs > 0) {
        await this.delay(this.timeoutConfig.chunkRateLimitMs);
      }
    }

    return chunkSummaries;
  }

  /**
   * Aggregates multiple chunk summaries into a final consolidated summary.
   * 
   * @param aggregatedText - The combined chunk summaries.
   * @param variables - Variables for prompt enhancement.
   * @returns The final aggregated summary.
   */
  private async aggregateChunkSummaries(
    aggregatedText: string,
    variables?: Record<string, string>
  ): Promise<string | null> {
    console.log('[AISummarizer] Aggregating chunk summaries into final summary');

    const systemPrompt = this.buildFinalSummaryPrompt(variables);
    const finalSummary = await this.callOpenAI(systemPrompt, aggregatedText);

    if (!finalSummary || finalSummary.trim().length === 0) {
      console.warn('[AISummarizer] Final aggregation returned empty. Using combined chunks.');
      return aggregatedText;
    }

    return finalSummary;
  }

  /**
   * Builds the system prompt for direct summarization.
   * 
   * @param variables - Optional variables for prompt enhancement.
   * @returns The system prompt.
   */
  private buildSystemPrompt(variables?: Record<string, string>): string {
    const course = variables?.course || '';
    const type = variables?.type || '';
    
    let prompt = 'You are an expert MBA instructor. Summarize the following content, highlighting key concepts, frameworks, and real-world applications relevant to MBA studies.';
    
    if (course) {
      prompt += ` This content is from the course: ${course}.`;
    }
    
    if (type) {
      prompt += ` Content type: ${type}.`;
    }
    
    return prompt;
  }

  /**
   * Builds the system prompt for chunk summarization.
   * 
   * @param variables - Optional variables for prompt enhancement.
   * @returns The chunk system prompt.
   */
  private buildChunkSystemPrompt(variables?: Record<string, string>): string {
    const course = variables?.course || '';
    
    let prompt = 'You are an expert MBA instructor. Summarize the following content from video transcripts and course PDFs, highlighting key concepts, frameworks, and real-world applications relevant to MBA studies.';
    
    if (course) {
      prompt += ` This content is from the course: ${course}.`;
    }
    
    return prompt;
  }

  /**
   * Builds the system prompt for final summary aggregation.
   * 
   * @param variables - Optional variables for prompt enhancement.
   * @returns The final summary prompt.
   */
  private buildFinalSummaryPrompt(variables?: Record<string, string>): string {
    const course = variables?.course || '';
    
    let prompt = 'You are an expert MBA instructor. Below are summaries of different sections of content. Synthesize these summaries into a cohesive, comprehensive summary that captures all key concepts, frameworks, and applications.';
    
    if (course) {
      prompt += ` This content is from the course: ${course}.`;
    }
    
    return prompt;
  }

  /**
   * Calls the OpenAI API with retry logic.
   * 
   * @param systemPrompt - The system prompt to guide the AI.
   * @param userContent - The user content to process.
   * @returns The AI response text.
   */
  private async callOpenAI(systemPrompt: string, userContent: string): Promise<string | null> {
    const maxAttempts = this.timeoutConfig.maxRetryAttempts + 1;
    const baseDelay = this.timeoutConfig.baseRetryDelaySeconds * 1000; // Convert to ms
    const maxDelay = this.timeoutConfig.maxRetryDelaySeconds * 1000;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        console.log(`[AISummarizer] Calling OpenAI API (attempt ${attempt}/${maxAttempts})`);

        const response = await fetch('https://api.openai.com/v1/chat/completions', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${this.apiKey}`
          },
          body: JSON.stringify({
            model: 'gpt-4o-mini',
            messages: [
              { role: 'system', content: systemPrompt },
              { role: 'user', content: userContent }
            ],
            temperature: 0.7,
            max_tokens: 2000
          })
        });

        if (!response.ok) {
          const errorText = await response.text();
          throw new Error(`OpenAI API error: ${response.status} ${response.statusText} - ${errorText}`);
        }

        const data = await response.json();
        const result = data.choices?.[0]?.message?.content?.trim();

        if (result) {
          console.log('[AISummarizer] Successfully received OpenAI response');
          return result;
        } else {
          console.warn('[AISummarizer] OpenAI returned empty response');
          return null;
        }
      } catch (error) {
        const isRetriable = this.isRetriableError(error);
        const isLastAttempt = attempt === maxAttempts;

        if (isRetriable && !isLastAttempt) {
          const delay = Math.min(baseDelay * Math.pow(2, attempt - 1), maxDelay);
          console.warn(`[AISummarizer] Attempt ${attempt}/${maxAttempts} failed. Retrying in ${delay}ms. Error:`, error);
          await this.delay(delay);
        } else {
          console.error(`[AISummarizer] Failed to call OpenAI after ${attempt} attempts:`, error);
          throw error;
        }
      }
    }

    return null;
  }

  /**
   * Determines if an error is retriable.
   * 
   * @param error - The error to check.
   * @returns True if the error indicates a transient failure.
   */
  private isRetriableError(error: any): boolean {
    if (!error) return false;

    const message = error.message?.toLowerCase() || '';
    
    return (
      message.includes('timeout') ||
      message.includes('network') ||
      message.includes('connection') ||
      message.includes('temporarily unavailable') ||
      message.includes('service unavailable') ||
      message.includes('rate limit') ||
      message.includes('429')
    );
  }

  /**
   * Delays execution for the specified number of milliseconds.
   * 
   * @param ms - Milliseconds to delay.
   */
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
