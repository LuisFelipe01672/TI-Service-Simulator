using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Chamados")]
    public List<Chamado> chamadosDisponiveis = new List<Chamado>();
    public Chamado chamadoAtivo;
    public int maxChamadosSimultaneos = 3;

    [Header("Locais")]
    public Transform[] locaisClientes;

    private static readonly string[] problemasImpressoraFacil = {
        "Não consigo imprimir.",
        "Impressora offline no sistema.",
        "Papel preso na impressora.",
    };
    private static readonly string[] problemasRedeMedio = {
        "Computador sem acesso à rede.",
        "Wi-Fi caindo toda hora.",
        "VPN não conecta.",
    };
    private static readonly string[] problemasHardwareDificil = {
        "Roteador desconfigurado, ninguém acessa a internet.",
        "Servidor travando, sistema fora do ar.",
        "HD corrompido, dados inacessíveis.",
    };

    private static readonly string[] clientesFase1 = {
        "Contabilidade Alfa", "Escritório Beta", "Clínica Gama"
    };
    private static readonly string[] clientesFase2 = {
        "Indústria Delta", "Construtora Épsilon", "Farmácia Zeta"
    };
    private static readonly string[] clientesFase3 = {
        "Banco Eta", "Hospital Theta", "Multinacional Iota"
    };

    private Transform jogador;
    private float tempoInicioAtendimento;

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

        int fase = ReputationManager.Instance != null
                   ? ReputationManager.Instance.FaseAtual : 1;

        int quantidade = Mathf.Min(fase == 1 ? 3 : fase == 2 ? 4 : 5,
                                   maxChamadosSimultaneos);

        for (int i = 0; i < quantidade; i++)
            chamadosDisponiveis.Add(CriarChamadoPorFase(fase));

        Debug.Log($"[MissionManager] {chamadosDisponiveis.Count} chamados gerados para fase {fase}.");
    }

    Chamado CriarChamadoPorFase(int fase)
    {
        DificuldadeChamado dif;
        if      (fase == 1) dif = DificuldadeChamado.Facil;
        else if (fase == 2) dif = Random.value < 0.5f
                                  ? DificuldadeChamado.Facil
                                  : DificuldadeChamado.Medio;
        else                dif = Random.value < 0.4f
                                  ? DificuldadeChamado.Medio
                                  : DificuldadeChamado.Dificil;

        string[] problemas = dif == DificuldadeChamado.Facil  ? problemasImpressoraFacil :
                             dif == DificuldadeChamado.Medio  ? problemasRedeMedio :
                                                                problemasHardwareDificil;

        string[] clientes  = fase == 1 ? clientesFase1 :
                             fase == 2 ? clientesFase2 : clientesFase3;

        TipoChamado tipo   = dif == DificuldadeChamado.Facil  ? TipoChamado.Impressora :
                             dif == DificuldadeChamado.Medio  ? TipoChamado.Rede :
                                                                TipoChamado.Notebook;

        float tempoLimite  = dif == DificuldadeChamado.Facil  ? 300f :
                             dif == DificuldadeChamado.Medio  ? 200f : 120f;

        float recompensa   = dif == DificuldadeChamado.Facil  ? 150f :
                             dif == DificuldadeChamado.Medio  ? 300f : 500f;

        return new Chamado(
            clientes[Random.Range(0, clientes.Length)],
            problemas[Random.Range(0, problemas.Length)],
            tipo, dif, recompensa, tempoLimite
        );
    }

    public void AceitarChamado(Chamado chamado)
    {
        chamadoAtivo = chamado;
        chamadosDisponiveis.Remove(chamado);
        tempoInicioAtendimento = Time.time;
        Debug.Log($"[MissionManager] Chamado aceito: {chamado.nomeCliente} — {chamado.descricaoProblema}");
        TeleportarParaCliente();
    }

    public void ConcluirChamado(bool cometeuErro = false)
    {
        if (chamadoAtivo == null) return;

        float tempoGasto = Time.time - tempoInicioAtendimento;

        if (ReputationManager.Instance != null)
            ReputationManager.Instance.AvaliarChamado(tempoGasto, chamadoAtivo.tempoLimite, cometeuErro);

        if (GameEconomy.Instance != null)
            GameEconomy.Instance.ReceberPagamento(
                cometeuErro ? chamadoAtivo.recompensa * 0.3f : chamadoAtivo.recompensa
            );

        Debug.Log($"[MissionManager] Concluído em {tempoGasto:F1}s (limite: {chamadoAtivo.tempoLimite}s) | Erro: {cometeuErro}");

        chamadoAtivo.concluido = true;
        chamadoAtivo = null;

        if (chamadosDisponiveis.Count < 2)
            GerarChamados();
    }

    void TeleportarParaCliente()
    {
        if (jogador == null)
            jogador = GameObject.FindWithTag("Player").transform;

        if (locaisClientes != null && locaisClientes.Length > 0)
        {
            CharacterController cc = jogador.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            jogador.position = locaisClientes[0].position;
            if (cc != null) cc.enabled = true;
            Debug.Log($"[MissionManager] Teleportado para: {locaisClientes[0].position}");
        }
    }
}