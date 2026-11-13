using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Chamados")]
    public List<Chamado> chamadosDisponiveis = new List<Chamado>();
    // NonSerialized: sem isso o Unity cria um chamado "vazio" pelo Inspector e o jogo acha que tem chamado ativo
    [System.NonSerialized] public Chamado chamadoAtivo;
    public int maxChamadosSimultaneos = 3;

    [Header("Penalidades")]
    [Tooltip("Reputação perdida a cada ação de diagnóstico errada")]
    public float penalidadeAcaoErrada = 3f;

    [Header("Investigação")]
    [Tooltip("Segundos do prazo gastos em cada pergunta ao cliente")]
    public float custoPergunta = 5f;
    [Tooltip("Segundos do prazo gastos em cada teste no equipamento")]
    public float custoTeste = 10f;
    [Tooltip("Perguntas que um cliente apressado aguenta antes de se irritar")]
    public int pacienciaApressado = 2;

    [Header("Locais")]
    public Transform[] locaisClientes;

    static readonly string[] clientesFase1 = { "Contabilidade Alfa", "Escritório Beta", "Clínica Gama" };
    static readonly string[] clientesFase2 = { "Indústria Delta", "Construtora Épsilon", "Farmácia Zeta" };
    static readonly string[] clientesFase3 = { "Banco Eta", "Hospital Theta", "Multinacional Iota" };

    private Transform jogador;
    private float tempoInicioAtendimento;

    public float TempoDecorrido => chamadoAtivo != null ? Time.time - tempoInicioAtendimento : 0f;
    public float TempoRestante => chamadoAtivo != null ? chamadoAtivo.tempoLimite - TempoDecorrido : 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        jogador = GameObject.FindWithTag("Player").transform;
        GerarChamados();
    }

    public void GerarChamados()
    {
        chamadosDisponiveis.Clear();

        int fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1;
        int quantidade = Mathf.Min(fase == 1 ? 3 : fase == 2 ? 4 : 5, maxChamadosSimultaneos);

        for (int i = 0; i < quantidade; i++)
            chamadosDisponiveis.Add(CriarChamadoPorFase(fase));

        Debug.Log($"[MissionManager] {chamadosDisponiveis.Count} chamados gerados para fase {fase}.");
    }

    Chamado CriarChamadoPorFase(int fase)
    {
        DificuldadeChamado dif;
        if      (fase == 1) dif = DificuldadeChamado.Facil;
        else if (fase == 2) dif = Random.value < 0.5f ? DificuldadeChamado.Facil : DificuldadeChamado.Medio;
        else                dif = Random.value < 0.4f ? DificuldadeChamado.Medio : DificuldadeChamado.Dificil;

        Problema p = CatalogoProblemas.Sortear(dif);
        Personalidade jeito = CatalogoProblemas.SortearPersonalidade(fase);

        string[] clientes = fase == 1 ? clientesFase1 : fase == 2 ? clientesFase2 : clientesFase3;

        TipoChamado tipo = p.equipamento == TipoEquipamento.Impressora ? TipoChamado.Impressora :
                           p.equipamento == TipoEquipamento.Roteador   ? TipoChamado.Rede : TipoChamado.Notebook;

        float tempoLimite = dif == DificuldadeChamado.Facil ? 300f : dif == DificuldadeChamado.Medio ? 200f : 120f;
        float recompensa  = dif == DificuldadeChamado.Facil ? 150f : dif == DificuldadeChamado.Medio ? 300f : 500f;
        tempoLimite = Mathf.Round(tempoLimite * Chamado.MultPrazo(jeito) / 5f) * 5f;
        recompensa  = Mathf.Round(recompensa * Chamado.MultPagamento(jeito) / 5f) * 5f;

        var chamado = new Chamado(clientes[Random.Range(0, clientes.Length)], p.descricao, tipo, dif, recompensa, tempoLimite);
        chamado.equipamento = p.equipamento;
        chamado.solucao = p.solucao;
        chamado.problema = p;
        chamado.personalidade = jeito;
        chamado.contato = CatalogoProblemas.SortearContato();
        return chamado;
    }

    public void AceitarChamado(Chamado chamado)
    {
        chamadoAtivo = chamado;
        chamadosDisponiveis.Remove(chamado);
        tempoInicioAtendimento = Time.time;
        EventBus<ChamadoAceitoEvent>.Raise(new ChamadoAceitoEvent { chamado = chamado });
        Debug.Log($"[MissionManager] Chamado aceito: {chamado.nomeCliente} — {chamado.descricaoProblema}");
        TeleportarParaCliente();
    }

    // Chamado pelo Equipamento quando o jogador escolhe uma ação de diagnóstico.
    // Devolve true se a ação resolveu o chamado ativo.
    public bool TentarSolucao(TipoEquipamento equipamento, string acao)
    {
        if (chamadoAtivo == null) return false;

        bool acertou = chamadoAtivo.equipamento == equipamento && chamadoAtivo.solucao == acao;
        if (acertou)
        {
            ConcluirChamado(false);
        }
        else
        {
            float penalidade = penalidadeAcaoErrada * chamadoAtivo.MultErro;
            chamadoAtivo.tentativasErradas++;
            chamadoAtivo.acoesErradas.Add(equipamento + ":" + acao);
            ReputationManager.Instance?.AlterarReputacao(-penalidade);
            EventBus<AcaoErradaEvent>.Raise(new AcaoErradaEvent { acao = acao, penalidade = penalidade });
        }
        return acertou;
    }

    // ---------- Investigação: conversa com o cliente e testes ----------

    // Gasta parte do prazo (perguntar e testar levam tempo)
    public void ConsumirTempo(float segundos)
    {
        if (chamadoAtivo != null) tempoInicioAtendimento -= segundos;
    }

    // Faz uma pergunta ao cliente do chamado ativo e devolve a fala dele
    public string Perguntar(Pergunta pergunta)
    {
        var ch = chamadoAtivo;
        if (ch == null || ch.problema == null) return "...";
        if (ch.perguntasFeitas.Contains(pergunta)) return ch.problema.Resposta(pergunta, ch.Mente);

        ConsumirTempo(custoPergunta);
        ch.conversou = true;
        ch.perguntasFeitas.Add(pergunta);

        string resposta = ch.problema.Resposta(pergunta, ch.Mente);
        ch.anotacoes.Add($"<b>{ch.contato}</b> ({CatalogoProblemas.RotuloPergunta(pergunta)}): {resposta}");

        if (ch.personalidade == Personalidade.Apressado && ch.perguntasFeitas.Count > pacienciaApressado)
        {
            ReputationManager.Instance?.AlterarReputacao(-1f);
            resposta += $"\n<color=#F5B83F>{CatalogoProblemas.Impaciencia()}</color>  <size=80%>(-1 reputação)</size>";
        }
        return resposta;
    }

    public bool JaTestou(TipoEquipamento equipamento, int indice, out string resultado)
    {
        resultado = null;
        if (chamadoAtivo == null) return false;
        int i = chamadoAtivo.testesFeitos.IndexOf(equipamento + ":" + indice);
        if (i < 0) return false;
        resultado = chamadoAtivo.resultadosTestes[i];
        return true;
    }

    public bool JaErrou(TipoEquipamento equipamento, string acao) =>
        chamadoAtivo != null && chamadoAtivo.acoesErradas.Contains(equipamento + ":" + acao);

    // Roda um teste no equipamento e devolve o que o técnico vê
    public string ExecutarTeste(TipoEquipamento equipamento, int indice)
    {
        var ch = chamadoAtivo;
        if (ch == null) return "Nenhum chamado em atendimento.";
        if (JaTestou(equipamento, indice, out string visto)) return visto;

        ConsumirTempo(custoTeste);
        bool aqui = ch.problema != null && ch.equipamento == equipamento;
        string resultado = aqui ? ch.problema.testes[indice] : CatalogoProblemas.ResultadoNormal(equipamento, indice);

        ch.testesFeitos.Add(equipamento + ":" + indice);
        ch.resultadosTestes.Add(resultado);
        string nomeTeste = CatalogoProblemas.Testes(equipamento)[indice];
        ch.anotacoes.Add($"<b>{equipamento}</b> ({nomeTeste}): {resultado}");
        return resultado;
    }

    public void ConcluirChamado(bool cometeuErro = false)
    {
        if (chamadoAtivo == null) return;

        float tempoGasto = TempoDecorrido;
        var ch = chamadoAtivo;

        ReputationManager.Instance?.AvaliarChamado(tempoGasto, ch.tempoLimite, cometeuErro, ch.MultReputacao);
        GameEconomy.Instance?.ReceberPagamento(cometeuErro ? ch.recompensa * 0.3f : ch.recompensa);

        // Bônus: investigou (conversou ou testou) e acertou de primeira, dentro do prazo
        bool certeiro = !cometeuErro && ch.tentativasErradas == 0 && tempoGasto <= ch.tempoLimite &&
                        (ch.perguntasFeitas.Count > 0 || ch.testesFeitos.Count > 0);
        if (certeiro) ReputationManager.Instance?.AlterarReputacao(1f);

        Debug.Log($"[MissionManager] Concluído em {tempoGasto:F1}s (limite: {chamadoAtivo.tempoLimite}s) | Erro: {cometeuErro}");

        chamadoAtivo.concluido = true;
        EventBus<ChamadoConcluidoEvent>.Raise(new ChamadoConcluidoEvent { chamado = chamadoAtivo, tempoGasto = tempoGasto, cometeuErro = cometeuErro, porFuncionario = false, diagnosticoCerteiro = certeiro });
        chamadoAtivo = null;

        if (chamadosDisponiveis.Count < 2)
            GerarChamados();
    }

    // Desistir do chamado: o cliente fica insatisfeito
    public void AbandonarChamado()
    {
        if (chamadoAtivo == null) return;
        Debug.Log($"[MissionManager] Chamado abandonado: {chamadoAtivo.nomeCliente}");
        chamadoAtivo.falhou = true;
        ReputationManager.Instance?.AlterarReputacao(-ReputationManager.Instance.punicaoErro);
        chamadoAtivo = null;
        if (chamadosDisponiveis.Count < 2)
            GerarChamados();
    }

    void TeleportarParaCliente()
    {
        if (jogador == null)
            jogador = GameObject.FindWithTag("Player").transform;

        if (locaisClientes != null && locaisClientes.Length > 0 && locaisClientes[0] != null)
        {
            Teleporte.MoverJogador(jogador, locaisClientes[0]); // posição e direção do olhar
            Debug.Log($"[MissionManager] Teleportado para: {locaisClientes[0].position}");
        }
    }
}
