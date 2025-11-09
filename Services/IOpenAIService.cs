using System.Threading;
using System.Threading.Tasks;

namespace ApiHelpFast.Services;

public interface IOpenAIService
{
    Task<string> EnviarPerguntaAsync(string perguntaUsuario, string systemPrompt, CancellationToken cancellationToken = default);
}

