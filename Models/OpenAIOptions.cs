namespace ApiHelpFast.Models;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.2;
}

