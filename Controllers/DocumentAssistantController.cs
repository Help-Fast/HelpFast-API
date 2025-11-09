using System.Threading;
using System.Threading.Tasks;
using ApiHelpFast.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentAssistantController : ControllerBase
{
    private readonly IAIService _aiService;

    public DocumentAssistantController(IAIService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("perguntar")]
    public async Task<IActionResult> PerguntarDocumento([FromBody] DocumentQuestionRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "O corpo da requisição é obrigatório." });
        }

        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            return BadRequest(new { error = "Informe a pergunta que deseja realizar." });
        }

        var resposta = await _aiService.PerguntarDocumentoAsync(request.Pergunta, request.UsuarioId, cancellationToken);

        return Ok(new
        {
            resposta = resposta.Resposta,
            escalarParaHumano = resposta.EscalarParaHumano
        });
    }

    public class DocumentQuestionRequest
    {
        public string Pergunta { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
    }
}

