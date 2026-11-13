using System.Collections.Generic;
using UnityEngine;

public enum ObjetoLicitacao { Impressao, Suporte, Cabeamento }

[System.Serializable]
public class Licitacao
{
    public int id;
    public string orgao;
    public ObjetoLicitacao objeto;
    public int quantidade;
    public string tipoImpressora;    // locação: modelo exigido
    public int paginasPorUnidade;
    public int meses = 12;
    public float valorEstimado;       // mensal (locação/suporte) ou total (cabeamento)
    public int atestadoMinimo;        // contratos/projetos parecidos já feitos (capacidade técnica)
    public int diaSessao;

    public int status;                // 0 aberto, 1 proposta enviada, 2 ganhou, 3 perdeu, 4 inabilitado, 5 concluído, 6 rescindido
    public float desconto;            // desconto oferecido sobre o estimado
    public float valorContratado;
    public float garantia;
    public int prazoEntrega;          // locação: até quando entregar as impressoras
    public int diaFim;                // quando devolve a garantia
    public int projetoId = -1;        // cabeamento: o projeto criado (a garantia volta na entrega)
    public string resultado;

    public bool Recorrente => objeto != ObjetoLicitacao.Cabeamento;
    public float ValorTotal(float valor) => Recorrente ? valor * meses : valor;
}

// Licitações públicas (pregão eletrônico, menor preço), com regras simplificadas da Lei 14.133/2021 e da LC 123/2006:
// habilitação (CNPJ, certidões em dia = sem impostos atrasados, atestado de capacidade técnica),
// empate ficto de ME/EPP (até 5% acima no pregão, pode cobrir o lance), garantia contratual de 5%,
// prazo de entrega e sanções (multa e impedimento de licitar) para quem não cumpre.
public static class Licitacoes
{
    public const float Garantia = 0.05f;
    public const float EmpateFicto = 0.05f;
    public const float MultaRescisao = 0.10f;
    public const int DiasImpedimento = 180;
    public const int PrazoEntregaDias = 15;
    public static readonly float[] Descontos = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f };

    public static List<Licitacao> lista = new List<Licitacao>();
    public static int proximoId = 1;
    public static int impedidoAteDia = -1;
    public static int ganhas;

    static readonly string[] orgaos =
    {
        "Prefeitura de Vale do Ipê", "Câmara Municipal de Morro Alto", "Secretaria de Educação de Santa Aurora",
        "Hospital Municipal Bom Jesus do Campo", "Fórum da Comarca de Serra Clara", "Universidade Estadual do Vale",
        "Secretaria de Saúde de Lagoa Serena", "Instituto de Previdência de Vale do Ipê"
    };

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
    static void Avisar(string t, Color c, float d = 5f) => GameUI.Instance?.Toast(t, c, d);

    public static bool Impedido => Hoje < impedidoAteDia;

    public static void Iniciar() { lista = new List<Licitacao>(); proximoId = 1; impedidoAteDia = -1; ganhas = 0; }

    // ---------- Edital ----------

    static void NovoEdital()
    {
        var obj = (ObjetoLicitacao)Random.Range(0, 3);
        var l = new Licitacao { id = proximoId++, orgao = orgaos[Random.Range(0, orgaos.Length)], objeto = obj, diaSessao = Hoje + Random.Range(6, 11) };
        switch (obj)
        {
            case ObjetoLicitacao.Impressao:
                l.quantidade = Random.Range(3, 13);
                l.tipoImpressora = "laser-pb";
                l.paginasPorUnidade = new[] { 1000, 2000, 4000 }[Random.Range(0, 3)];
                var t = CatalogoLocacao.Tipo(l.tipoImpressora);
                PlanoLocacao plano = t.planos[0];
                foreach (var p in t.planos) if (p.franquia <= l.paginasPorUnidade) plano = p;
                l.valorEstimado = Mathf.Round(CatalogoLocacao.MensalComDesconto(plano, 12) * l.quantidade * Random.Range(1.0f, 1.15f));
                l.atestadoMinimo = Random.Range(1, 4);
                break;
            case ObjetoLicitacao.Suporte:
                l.quantidade = Random.Range(20, 81);
                l.valorEstimado = Mathf.Round(CatalogoServicos.Mensal(TipoServico.Suporte, l.quantidade, 12) * Random.Range(1.0f, 1.15f));
                l.atestadoMinimo = Random.Range(1, 4);
                break;
            default:
                l.quantidade = Random.Range(20, 61);
                l.valorEstimado = Mathf.Round(ProjetosManager.PrecoJusto(TipoProjeto.Cabeamento, l.quantidade) * Random.Range(1.0f, 1.2f));
                l.atestadoMinimo = Random.Range(1, 3);
                break;
        }
        lista.Add(l);
        Avisar($"<b>Edital publicado:</b> {l.orgao} — {Descrever(l)}. Sessão em {DiaManager.DataDoDia(l.diaSessao):dd/MM} (aba Negócios).", UIFactory.Destaque);
    }

    public static string Descrever(Licitacao l)
    {
        switch (l.objeto)
        {
            case ObjetoLicitacao.Impressao: return $"locação de {l.quantidade} impressoras laser P&B (~{l.paginasPorUnidade} pág./mês cada) por {l.meses} meses";
            case ObjetoLicitacao.Suporte: return $"suporte e monitoramento de {l.quantidade} computadores por {l.meses} meses";
            default: return $"cabeamento estruturado de {l.quantidade} pontos de rede";
        }
    }

    // Atestado de capacidade técnica: quantos serviços parecidos você já prestou
    public static int Atestados(ObjetoLicitacao o)
    {
        switch (o)
        {
            case ObjetoLicitacao.Impressao: return ContratosManager.Instance != null ? ContratosManager.Instance.contratos.FindAll(c => c.Instalado).Count : 0;
            case ObjetoLicitacao.Suporte: return ServicosManager.Instance != null ? ServicosManager.Instance.contratos.FindAll(c => c.tipo == TipoServico.Suporte && c.Implantado).Count : 0;
            default: return ProjetosManager.Instance != null ? ProjetosManager.Instance.concluidos : 0;
        }
    }

    public static bool Habilitado(Licitacao l, out string motivo)
    {
        motivo = null;
        var emp = EmpresaManager.Instance;
        if (emp != null && !emp.TemCnpj) { motivo = "Precisa de CNPJ."; return false; }
        if (Impedido) { motivo = $"Você está impedido de licitar até {DiaManager.DataDoDia(impedidoAteDia):dd/MM/yyyy}."; return false; }
        if (emp != null && emp.Irregular) { motivo = "Certidões negativas recusadas: há impostos atrasados há mais de 60 dias."; return false; }
        if (l.objeto == ObjetoLicitacao.Suporte && !Parcerias.Ativa(Parcerias.Antivirus)) { motivo = "Precisa da parceria de antivírus/monitoramento."; return false; }
        int at = Atestados(l.objeto);
        if (at < l.atestadoMinimo) { motivo = $"Atestado de capacidade técnica: o edital pede {l.atestadoMinimo} serviço(s) parecido(s), você tem {at}."; return false; }
        return true;
    }

    // ---------- Proposta e sessão ----------

    public static string EnviarProposta(Licitacao l, float desconto)
    {
        if (l.status != 0) return "Proposta já enviada.";
        if (Hoje >= l.diaSessao) return "A sessão já começou.";
        l.desconto = Mathf.Clamp(desconto, 0f, 0.5f);
        l.status = 1;
        return $"Proposta cadastrada no portal de compras: {UIFactory.Dinheiro(l.valorEstimado * (1f - l.desconto))}" +
               $"{(l.Recorrente ? "/mês" : "")} ({l.desconto * 100:0}% abaixo do estimado). Sessão em {DiaManager.DataDoDia(l.diaSessao):dd/MM}.";
    }

    static void Sessao(Licitacao l)
    {
        if (l.status == 0) { l.status = 3; l.resultado = "Você não participou."; return; }
        if (!Habilitado(l, out string motivo))
        {
            l.status = 4; l.resultado = "Inabilitado: " + motivo;
            Avisar($"<b>Licitação {l.orgao}:</b> {l.resultado}", UIFactory.Perigo, 6f);
            return;
        }

        // Melhor lance dos concorrentes (pregão é agressivo); às vezes o vencedor também é ME/EPP
        float melhor = l.valorEstimado * (1f - Random.Range(0.06f, 0.26f));
        bool rivalPequeno = Random.value < 0.4f;
        float meu = l.valorEstimado * (1f - l.desconto);
        var rival = Concorrencia.Sortear();
        bool souPequeno = EmpresaManager.Instance == null || EmpresaManager.Instance.regime != Regime.Autonomo;   // MEI e ME são ME/EPP

        if (meu > melhor)
        {
            if (souPequeno && !rivalPequeno && meu <= melhor * (1f + EmpateFicto))
            {
                meu = Mathf.Round(melhor * 0.995f);   // empate ficto: a ME cobre o lance
                l.resultado = $"Empate ficto (ME/EPP): você cobriu o lance da {rival.nome}.";
            }
            else
            {
                l.status = 3;
                l.resultado = $"A {rival.nome} venceu com {UIFactory.Dinheiro(melhor)}{(l.Recorrente ? "/mês" : "")}.";
                Concorrencia.Registrar(rival, false);
                Avisar($"<b>Licitação {l.orgao}:</b> {l.resultado}", UIFactory.Alerta, 5f);
                return;
            }
        }
        Concorrencia.Registrar(rival, true);
        Vencer(l, meu);
    }

    static void Vencer(Licitacao l, float valor)
    {
        l.status = 2;
        ganhas++;
        l.valorContratado = Mathf.Round(valor * 100f) / 100f;
        l.garantia = Mathf.Round(l.ValorTotal(l.valorContratado) * Garantia);
        GameEconomy.Instance?.Gastar(l.garantia, LivroCaixa.Garantias);
        l.diaFim = l.Recorrente ? Hoje + l.meses * 30 : int.MaxValue;
        string extra;

        switch (l.objeto)
        {
            case ObjetoLicitacao.Suporte:
                ServicosManager.Instance?.CriarContrato(TipoServico.Suporte, l.orgao, l.quantidade, l.meses, l.valorContratado, 0f);
                extra = "Faça a implantação na aba Contratos.";
                break;
            case ObjetoLicitacao.Cabeamento:
                var proj = ProjetosManager.Instance?.CriarProjetoContratado(TipoProjeto.Cabeamento, l.orgao, l.quantidade, l.valorContratado);
                if (proj != null) l.projetoId = proj.id;
                extra = "O projeto já está aprovado na aba Negócios (órgão público paga tudo na entrega).";
                break;
            default:
                l.prazoEntrega = Hoje + PrazoEntregaDias;
                extra = $"Entregue as {l.quantidade} impressoras até {DiaManager.DataDoDia(l.prazoEntrega):dd/MM} (aba Negócios), ou leva multa e impedimento.";
                break;
        }
        l.resultado = (l.resultado != null ? l.resultado + " " : "") + $"Contrato assinado: {UIFactory.Dinheiro(l.valorContratado)}{(l.Recorrente ? "/mês" : "")}.";
        Conquistas.Desbloquear("licitacao");
        Avisar($"<b>Você venceu a licitação da {l.orgao}!</b> {l.resultado} Garantia de 5% depositada ({UIFactory.Dinheiro(l.garantia)}). {extra}", UIFactory.Sucesso, 8f);
    }

    // Locação: entrega todas as impressoras de uma vez (precisa ter em estoque)
    public static string EntregarImpressoras(Licitacao l)
    {
        if (l.objeto != ObjetoLicitacao.Impressao || l.status != 2 || l.prazoEntrega <= 0) return "Nada para entregar.";
        var cm = ContratosManager.Instance;
        if (cm == null) return "Sem gerenciador de contratos.";
        int estoque = cm.EmEstoque(l.tipoImpressora).Count;
        if (estoque < l.quantidade) return $"Você tem {estoque} de {l.quantidade} impressoras laser P&B em estoque. Compre o resto na aba Contratos.";
        var t = CatalogoLocacao.Tipo(l.tipoImpressora);
        int plano = 0;
        for (int i = 0; i < t.planos.Length; i++) if (t.planos[i].franquia <= l.paginasPorUnidade) plano = i;
        float porUnidade = Mathf.Round(l.valorContratado / l.quantidade * 100f) / 100f;
        for (int i = 0; i < l.quantidade; i++)
            cm.CriarContratoDireto(l.orgao, l.tipoImpressora, plano, l.meses, porUnidade, l.paginasPorUnidade, 0f);
        l.prazoEntrega = -1;
        return $"{l.quantidade} impressoras separadas para a {l.orgao}. Instale cada uma na aba Contratos.";
    }

    // ---------- Dia a dia ----------

    public static void NovoDia(int dia)
    {
        var emp = EmpresaManager.Instance;
        int abertos = lista.FindAll(x => x.status <= 1).Count;
        if ((emp == null || emp.TemCnpj) && abertos < 3 && Random.value < 0.08f) NovoEdital();

        foreach (var l in lista)
        {
            if (l.status <= 1 && dia >= l.diaSessao) Sessao(l);

            // Não entregou as impressoras no prazo: rescisão, multa, perde a garantia e fica impedido
            if (l.status == 2 && l.objeto == ObjetoLicitacao.Impressao && l.prazoEntrega > 0 && dia > l.prazoEntrega)
            {
                l.status = 6;
                float multa = Mathf.Round(l.ValorTotal(l.valorContratado) * MultaRescisao);
                emp?.AdicionarGuia($"Multa contratual: {l.orgao}", multa, dia + 15, EmpresaManager.MesDoDia(dia));
                impedidoAteDia = dia + DiasImpedimento;
                l.resultado = $"Rescindido por não entregar: multa de {UIFactory.Dinheiro(multa)}, garantia perdida e impedimento de {DiasImpedimento} dias.";
                ReputationManager.Instance?.AlterarReputacao(-5f);
                Avisar($"<b>{l.orgao} rescindiu o contrato!</b> {l.resultado}", UIFactory.Perigo, 8f);
            }

            // Cabeamento: terminou quando o projeto foi entregue
            if (l.status == 2 && !l.Recorrente && ProjetosManager.Instance != null)
            {
                var p = ProjetosManager.Instance.projetos.Find(x => x.id == l.projetoId);
                if (p == null || p.etapa == 5) l.diaFim = dia;
            }

            // Fim do contrato: a garantia volta
            if (l.status == 2 && dia >= l.diaFim)
            {
                l.status = 5;
                GameEconomy.Instance?.ReceberPagamento(l.garantia, LivroCaixa.Devolucao);
                Avisar($"Contrato com a {l.orgao} encerrado. Garantia de {UIFactory.Dinheiro(l.garantia)} devolvida.", UIFactory.Sucesso);
            }
        }
        lista.RemoveAll(x => (x.status == 3 || x.status == 4) && dia - x.diaSessao > 10);
        lista.RemoveAll(x => (x.status == 5 || x.status == 6) && dia - x.diaFim > 30);
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados { public List<Licitacao> lista = new List<Licitacao>(); public int proximoId = 1, impedidoAteDia = -1, ganhas; }

    public static Dados Exportar() => new Dados { lista = new List<Licitacao>(lista), proximoId = proximoId, impedidoAteDia = impedidoAteDia, ganhas = ganhas };
    public static void Carregar(Dados d)
    {
        if (d == null) { Iniciar(); return; }
        lista = d.lista ?? new List<Licitacao>(); proximoId = Mathf.Max(1, d.proximoId); impedidoAteDia = d.impedidoAteDia; ganhas = d.ganhas;
    }
}
