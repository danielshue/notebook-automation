namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// OpenAI API configuration section.
    /// </summary>
    public class OpenAiConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string? Model { get; internal set; }
    }
}
