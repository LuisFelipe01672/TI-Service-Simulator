using System;
using UnityEngine;

public class Objetivo
{
    public string titulo, dica;
    public Func<bool> feito;
    public Objetivo(string titulo, string dica, Func<bool> feito) { this.titulo = titulo; this.dica = dica; this.feito = feito; }
}

// Primeiros passos: uma trilha de objetivos que ensina o jogo aos poucos (do primeiro chamado à empresa com contratos).
// Cada objetivo é conferido pelo estado do jogo, então um save carregado já começa no ponto certo.
public static class Objetivos
{
    static EmpresaManager Emp => EmpresaManager.Instance;

    public static readonly Objetivo[] Lista =
    {
        new Objetivo("Resolva seu primeiro chamado", "Aperte Tab, aceite um chamado, vá até o cliente, converse e mexa no equipamento com E.",
            () => Conquistas.chamados >= 1),
        new Objetivo("Compre uma ferramenta", "Na aba Loja do tablet: o testador de cabo resolve muitos problemas de rede.",
            () => Mochila.Instance == null || Mochila.Instance.Quantidade(Item.TestadorCabo) > 0 || Mochila.Instance.Quantidade(Item.AlicateCrimpagem) > 0),
        new Objetivo("Emita um recibo", "Aba Empresa: recibo passa confiança ao cliente (e organiza seu caixa).",
            () => Emp == null || Emp.TemCnpj || Emp.recebiveis.Exists(r => r.documentoEmitido)),
        new Objetivo("Resolva 5 chamados", "Converse antes de mexer: perguntas certas dão bônus de diagnóstico certeiro.",
            () => Conquistas.chamados >= 5),
        new Objetivo("Abra seu MEI", "Aba Empresa. Com CNPJ você atende empresas que exigem nota e cobra mais pela hora.",
            () => Emp == null || Emp.TemCnpj),
        new Objetivo("Feche uma parceria com fornecedor", "Aba Negócios: para vender suporte, e-mail ou impressora você precisa ser revenda.",
            () => Parcerias.estados != null && Parcerias.estados.Exists(e => e.status > 0)),
        new Objetivo("Feche o primeiro contrato mensal", "Aba Contratos: contratos pagam todo mês. É isso que faz a empresa crescer.",
            () => Conquistas.Mrr() > 0f),
        new Objetivo("Pague uma guia", "Aba Empresa: impostos e contas chegam na virada do mês. Atrasar gera multa e juros.",
            () => Emp == null || Emp.guias.Exists(g => g.paga)),
        new Objetivo("Escolha o foco da empresa", "Aba Negócios: impressão, gerenciados, infraestrutura ou software.",
            () => FocoEmpresa.atual != AreaNegocio.Nenhuma),
        new Objetivo("Alugue uma sala comercial", "Aba Empresa. A sala chega vazia: compre mesa, cadeira e computador.",
            () => SedeManager.Instance == null || SedeManager.Instance.imovel > 0),
        new Objetivo("Contrate o primeiro funcionário", "Aba Equipe (fase 2). Cada funcionário precisa de uma estação de trabalho.",
            () => FuncionarioManager.Instance == null || FuncionarioManager.Instance.equipe.Count > 0),
        new Objetivo("Entregue um projeto de infraestrutura", "Aba Negócios: faça o levantamento antes de orçar para não tomar prejuízo.",
            () => ProjetosManager.Instance == null || ProjetosManager.Instance.concluidos > 0),
        new Objetivo("Vire Microempresa", "Aba Empresa: libera backup, bot, firewall e funcionários à vontade.",
            () => Emp == null || Emp.regime == Regime.ME),
        new Objetivo("Chegue a R$ 5 mil por mês em contratos", "Some locação, serviços e firewall. Cliente satisfeito renova.",
            () => Conquistas.Mrr() >= 5000f),
    };

    public static int atual = -1;   // índice do objetivo em andamento (-1 = ainda não calculado)

    public static bool Terminou => atual >= Lista.Length;
    public static Objetivo Atual => atual >= 0 && atual < Lista.Length ? Lista[atual] : null;

    // Avança pelos objetivos cumpridos. Devolve o título do que acabou de ser concluído (para o aviso), se houver.
    public static string Atualizar()
    {
        bool primeiraVez = atual < 0;
        if (primeiraVez) atual = 0;
        string concluido = null;
        while (atual < Lista.Length)
        {
            bool ok;
            try { ok = Lista[atual].feito(); } catch { ok = false; }
            if (!ok) break;
            concluido = Lista[atual].titulo;
            atual++;
        }
        return primeiraVez ? null : concluido;   // ao carregar um save não avisa tudo de uma vez
    }

    public static void Iniciar() => atual = -1;
}
