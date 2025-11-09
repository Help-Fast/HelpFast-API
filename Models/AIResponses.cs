using System;
using System.Collections.Generic;

namespace ApiHelpFast.Models;

public class ChatResponse
{
    public string? Resposta { get; set; }
    public bool EscalarParaHumano { get; set; }
    public string? Categoria { get; set; }
}

public class CategorizacaoResponse
{
    public string? Categoria { get; set; }
    public string? Subcategoria { get; set; }
    public decimal? Confianca { get; set; }
}

public class AtribuicaoResponse
{
    public int? TecnicoId { get; set; }
    public string? TecnicoNome { get; set; }
    public string? TecnicoEmail { get; set; }
    public decimal? Confianca { get; set; }
    public string? Justificativa { get; set; }
}

public class AnalisePadroesResponse
{
    public EstatisticasGerais Estatisticas { get; set; } = new();
    public List<TendenciaCategoria> TendenciaCategorias { get; set; } = new();
    public List<TendenciaTempo> TendenciaTempo { get; set; } = new();
    public List<ProblemaRecorrente> ProblemasRecorrentes { get; set; } = new();
}

public class EstatisticasGerais
{
    public int TotalChamados { get; set; }
    public double TempoMedioResolucao { get; set; }
    public double TaxaResolucao { get; set; }
    public string? CategoriaMaisComum { get; set; }
}

public class TendenciaCategoria
{
    public string? Categoria { get; set; }
    public int QuantidadeChamados { get; set; }
    public decimal? ConfiancaMedia { get; set; }
}

public class TendenciaTempo
{
    public DateTime Data { get; set; }
    public int QuantidadeChamados { get; set; }
    public double TempoMedioResolucao { get; set; }
}

public class ProblemaRecorrente
{
    public string? Descricao { get; set; }
    public int Frequencia { get; set; }
    public string? Categoria { get; set; }
}

