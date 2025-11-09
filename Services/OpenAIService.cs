using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ApiHelpFast.Models;

namespace ApiHelpFast.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        _httpClient = httpClient;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Configure OpenAI:ApiKey no appsettings ou nas variáveis de ambiente.");
        }

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<string> EnviarPerguntaAsync(string perguntaUsuario, string systemPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(perguntaUsuario))
        {
            throw new ArgumentException("A pergunta do usuário é obrigatória.", nameof(perguntaUsuario));
        }

        var payload = new
        {
            model = _options.ChatModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? string.Empty },
                new { role = "user", content = perguntaUsuario }
            },
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = JsonSerializer.Serialize(payload, _serializerOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenAI retornou status {(int)response.StatusCode}: {responseContent}", null, response.StatusCode);
        }

        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;
            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("OpenAI não retornou nenhuma escolha.");
            }

            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? string.Empty;
            }

            if (firstChoice.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }

            return responseContent;
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException("Formato inesperado na resposta da OpenAI.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Falha ao interpretar a resposta da OpenAI.", ex);
        }
    }
}

