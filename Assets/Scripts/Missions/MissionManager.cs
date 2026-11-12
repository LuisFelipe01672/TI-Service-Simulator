using UnityEngine;
using System.Collections.Generic;

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

    [Header("Locais")]
    public Transform[] locaisClientes;

    // Catálogo de problemas: cada um diz qual equipamento olhar e qual ação resolve.
    // As ações precisam bater com as opções de Equipamento.GetOpcoes().
    struct Problema
    {
        public string descricao;
        public TipoEquipamento equipamento;
        public string solucao;
        public Problema(string d, TipoEquipamento e, string s) { descricao = d; equipamento = e; solucao = s; }
    }

    static readonly Problema[] problemasFaceis = {
        new Problema("Não consigo imprimir nada.",                      TipoEquipamento.Impressora, "Reiniciar spooler"),
        new Problema("Impressora aparece offline no sistema.",          TipoEquipamento.Impressora, "Verificar cabo"),
        new Problema("Um documento travou e nada mais imprime.",        TipoEquipamento.Impressora, "Verificar fila de impressão"),
        new Problema("A impressão está saindo clara e falhada.",        TipoEquipamento.Impressora, "Trocar toner"),
    };
    static readonly Problema[] problemasMedios = {
        new Problema("Computador sem acesso à rede.",                   TipoEquipamento.Computador, "Verificar cabo de rede"),
        new Problema("Aparece \"conflito de IP\" e a rede não conecta.", TipoEquipamento.Computador, "Verificar IP"),
        new Problema("A placa de rede sumiu depois de uma atualização.", TipoEquipamento.Computador, "Reinstalar driver"),
        new Problema("O computador está travando e lento.",             TipoEquipamento.Computador, "Reiniciar equipamento"),
    };
    static readonly Problema[] problemasDificeis = {
        new Problema("Roteador desconfigurado, ninguém acessa a internet.", TipoEquipamento.Roteador, "Resetar para padrão"),
        new Problema("A internet cai para todo mundo a cada 10 minutos.",   TipoEquipamento.Roteador, "Reiniciar roteador"),
        new Problema("A rede Wi-Fi sumiu depois de mexerem no roteador.",   TipoEquipamento.Roteador, "Verificar configurações"),
        new Problema("Os computadores pegam IP errado e não navegam.",      TipoEquipamento.Roteador, "Verificar IP"),
    };

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

        Problema[] pool = dif == DificuldadeChamado.Facil ? problemasFaceis :
                          dif == DificuldadeChamado.Medio ? problemasMedios : problemasDificeis;
        Problema p = pool[Random.Range(0, pool.Length)];

        string[] clientes = fase == 1 ? clientesFase1 : fase == 2 ? clientesFase2 : clientesFase3;

        TipoChamado tipo = p.equipamento == TipoEquipamento.Impressora ? TipoChamado.Impressora :
                           p.equipamento == TipoEquipamento.Roteador   ? TipoChamado.Rede : TipoChamado.Notebook;

        float tempoLimite = dif == DificuldadeChamado.Facil ? 300f : dif == DificuldadeChamado.Medio ? 200f : 120f;
        float recompensa  = dif == DificuldadeChamado.Facil ? 150f : dif == DificuldadeChamado.Medio ? 300f : 500f;

        var chamado = new Chamado(clientes[Random.Range(0, clientes.Length)], p.descricao, tipo, dif, recompensa, tempoLimite);
        chamado.equipamento = p.equipamento;
        chamado.solucao = p.solucao;
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
            chamadoAtivo.tentativasErradas++;
            ReputationManager.Instance?.AlterarReputacao(-penalidadeAcaoErrada);
            EventBus<AcaoErradaEvent>.Raise(new AcaoErradaEvent { acao = acao, penalidade = penalidadeAcaoErrada });
        }
        return acertou;
    }

    public void ConcluirChamado(bool cometeuErro = false)
    {
        if (chamadoAtivo == null) return;

        float tempoGasto = TempoDecorrido;

        ReputationManager.Instance?.AvaliarChamado(tempoGasto, chamadoAtivo.tempoLimite, cometeuErro);
        GameEconomy.Instance?.ReceberPagamento(cometeuErro ? chamadoAtivo.recompensa * 0.3f : chamadoAtivo.recompensa);

        Debug.Log($"[MissionManager] Concluído em {tempoGasto:F1}s (limite: {chamadoAtivo.tempoLimite}s) | Erro: {cometeuErro}");

        chamadoAtivo.concluido = true;
        EventBus<ChamadoConcluidoEvent>.Raise(new ChamadoConcluidoEvent { chamado = chamadoAtivo, tempoGasto = tempoGasto, cometeuErro = cometeuErro, porFuncionario = false });
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
