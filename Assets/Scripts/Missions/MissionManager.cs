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
    [Tooltip("Reputação perdida se o técnico deixar sujeira no chão do cliente")]
    public float penalidadeSujeira = 2f;

    [Header("Investigação")]
    [Tooltip("Segundos do prazo gastos em cada pergunta ao cliente")]
    public float custoPergunta = 5f;
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
        // Fim de semana: sábado tem pouco movimento, domingo quase nada
        var d = DiaManager.Instance;
        if (d != null && d.Data.DayOfWeek == System.DayOfWeek.Saturday) quantidade = Mathf.Min(quantidade, 2);
        if (d != null && d.Data.DayOfWeek == System.DayOfWeek.Sunday) quantidade = 1;

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
        // Quanto maior o cliente, mais exige nota fiscal
        chamado.exigeNota = Random.value < (fase == 1 ? 0.25f : fase == 2 ? 0.6f : 0.9f);
        return chamado;
    }

    public void AceitarChamado(Chamado chamado)
    {
        var empresa = EmpresaManager.Instance;
        if (empresa != null && !empresa.PodeAtender(chamado, out string motivo))
        {
            GameUI.Instance?.Toast(motivo, UIFactory.Perigo, 4f);
            return;
        }
        chamadoAtivo = chamado;
        chamadosDisponiveis.Remove(chamado);
        chamado.estado = new EstadoEquipamentos();
        if (chamado.problema != null) CatalogoProblemas.Quebrar(chamado.problema, chamado.estado);
        Sujeira.LimparTudo();
        tempoInicioAtendimento = Time.time;
        EventBus<ChamadoAceitoEvent>.Raise(new ChamadoAceitoEvent { chamado = chamado });
        Debug.Log($"[MissionManager] Chamado aceito: {chamado.nomeCliente} — {chamado.descricaoProblema}");
        TeleportarParaCliente();
    }

    // ---------- Manutenção ----------

    public EstadoEquipamentos Estado => chamadoAtivo?.estado;

    // Onde fica o equipamento do chamado (para espalhar sujeira, por exemplo)
    public Vector3 PosicaoEquipamentoAtual
    {
        get
        {
            if (chamadoAtivo != null)
                foreach (var eq in FindObjectsByType<Equipamento>(FindObjectsSortMode.None))
                    if (eq.tipoEquipamento == chamadoAtivo.equipamento) return eq.transform.position;
            return jogador != null ? jogador.position : Vector3.zero;
        }
    }

    // O jogador conferiu algo (imprimiu, pingou, reiniciou...). Se o problema sumiu, o chamado acaba.
    public bool Verificar()
    {
        var ch = chamadoAtivo;
        if (ch == null || ch.problema == null || ch.estado == null) return false;
        if (!CatalogoProblemas.Resolvido(ch.problema, ch.estado)) return false;
        GameUI.Instance?.Toast($"<b>{ch.contato}</b> conferiu: \"Funcionou! Muito obrigado!\"", UIFactory.Sucesso, 3f);
        ConcluirChamado(false);
        return true;
    }

    // Uma ação que causou estrago (resetar sem precisar, desligar sem salvar...)
    public void RegistrarErro(string motivo, float reputacao)
    {
        if (chamadoAtivo == null) return;
        float penalidade = reputacao * chamadoAtivo.MultErro;
        chamadoAtivo.tentativasErradas++;
        ReputationManager.Instance?.AlterarReputacao(-penalidade);
        EventBus<AcaoErradaEvent>.Raise(new AcaoErradaEvent { acao = motivo, penalidade = penalidade });
    }

    // Peças usadas no cliente (toner, cabo) entram na conta dele
    public void CobrarPeca(float valor)
    {
        if (chamadoAtivo != null) chamadoAtivo.pecasCobradas += valor;
    }

    public void RegistrarAcao()
    {
        if (chamadoAtivo != null) chamadoAtivo.acoesFeitas++;
    }

    // ---------- Conversa com o cliente ----------

    // Gasta parte do prazo (perguntar leva tempo)
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

    public void ConcluirChamado(bool cometeuErro = false)
    {
        if (chamadoAtivo == null) return;

        float tempoGasto = TempoDecorrido;
        var ch = chamadoAtivo;

        ReputationManager.Instance?.AvaliarChamado(tempoGasto, ch.tempoLimite, cometeuErro, ch.MultReputacao);
        float valor = (cometeuErro ? ch.recompensa * 0.3f : ch.recompensa) + ch.pecasCobradas;
        if (EmpresaManager.Instance != null)
            GameUI.Instance?.Toast(EmpresaManager.Instance.RegistrarServico(ch, valor, false), UIFactory.Destaque, 4f);
        else
            GameEconomy.Instance?.ReceberPagamento(valor);

        // Sujeira esquecida no chão: o cliente reclama
        if (Sujeira.Todas.Count > 0)
        {
            ReputationManager.Instance?.AlterarReputacao(-penalidadeSujeira);
            GameUI.Instance?.Toast($"{ch.contato} reclamou da sujeira que você deixou no chão (-{penalidadeSujeira:F0} reputação).", UIFactory.Perigo, 4f);
            Sujeira.LimparTudo();
        }

        // Bônus: investigou (conversou) e resolveu sem fazer estrago, dentro do prazo
        bool certeiro = !cometeuErro && ch.tentativasErradas == 0 && tempoGasto <= ch.tempoLimite && ch.perguntasFeitas.Count > 0;
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
