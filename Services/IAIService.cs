using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiHelpFast.Models;

namespace ApiHelpFast.Services;

public interface IAIService
{
    Task<ChatResponse?> ProcessarMensagemChatAsync(int usuarioId, string mensagem);
    Task<CategorizacaoResponse?> CategorizarChamadoAsync(Chamado chamado);
    Task<bool> ValidarCategorizacaoAsync(int chamadoId, string categoriaOriginal, string categoriaCorrigida);
    Task<AtribuicaoResponse?> AtribuirChamadoAsync(int chamadoId, List<Usuario> tecnicos);
    Task<AnalisePadroesResponse?> AnalisarPadroesAsync(DateTime dataInicio, DateTime dataFim, string? categoria = null);
    Task<List<string>> SugerirFAQAsync(string descricao, string? categoria = null);
    Task<bool> IsCategorizacaoAtivaAsync();
    Task<bool> IsAtribuicaoAtivaAsync();
    Task<ChatResponse> PerguntarDocumentoAsync(string pergunta, int? usuarioId = null, CancellationToken cancellationToken = default);
}

