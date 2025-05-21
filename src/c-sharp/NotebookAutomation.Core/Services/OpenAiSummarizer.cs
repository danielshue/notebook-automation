using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Provides AI-powered summarization using the OpenAI API.
    /// </summary>
    public class OpenAiSummarizer
    {
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly string OpenAiEndpoint = "https://api.openai.com/v1/chat/completions";

        public OpenAiSummarizer(ILogger logger, string apiKey, string model = "gpt-3.5-turbo")
        {
            _logger = logger;
            _apiKey = apiKey;
            _model = model;
        }

        /// <summary>
        /// Loads a prompt template from the Prompts folder above src.
        /// </summary>
        /// <param name="promptFileName">The prompt file name (e.g., chunk_summary_prompt.md).</param>
        /// <returns>The prompt string, or null if not found.</returns>
        public static string? LoadPromptTemplate(string promptFileName)
        {
            try
            {
                // Assume workspace root is two levels above this file
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "../../../../.."));
                var promptPath = Path.Combine(projectRoot, "Prompts", promptFileName);
                if (File.Exists(promptPath))
                {
                    return File.ReadAllText(promptPath);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Generates a summary for the given text using OpenAI, with optional prompt file.
        /// </summary>
        /// <param name="inputText">The text to summarize.</param>
        /// <param name="prompt">Optional prompt to guide the summary.</param>
        /// <param name="promptFileName">Optional prompt file name to load from Prompts/.</param>
        /// <returns>The summary text, or null if failed.</returns>
        public async Task<string?> SummarizeAsync(string inputText, string? prompt = null, string? promptFileName = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("OpenAI API key is missing.");
                return null;
            }
            string? promptText = prompt;
            if (!string.IsNullOrWhiteSpace(promptFileName))
            {
                promptText = LoadPromptTemplate(promptFileName) ?? prompt;
            }
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                var messages = new[]
                {
                    new { role = "system", content = promptText ?? "Summarize the following text for a study note." },
                    new { role = "user", content = inputText }
                };
                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = 256,
                    temperature = 0.5
                };
                var json = JsonSerializer.Serialize(requestBody);
                var response = await client.PostAsync(OpenAiEndpoint, new StringContent(json, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var summary = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI summarization failed.");
                return null;
            }
        }
    }
}
