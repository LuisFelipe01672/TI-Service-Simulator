using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;

// Salva e carrega o progresso do jogador em JSON (Application.persistentDataPath/save.json).
// Salva sozinho quando: um chamado é concluído, alguém é contratado, uma fase é desbloqueada e ao sair do jogo.
[Serializable]
public class SaveData
{
    public int versao = 2;
    public int dia = 1;
    public Mochila.Dados mochila;
    public EmpresaManager.Dados empresa;
    public ContratosManager.Dados contratos;
    public ServicosManager.Dados servicos;
    public SedeManager.Dados sede;
    public Concorrencia.Dados concorrencia;
    public ProjetosManager.Dados projetos;
    public BancoManager.Dados banco;
    public Parcerias.Dados parcerias;
    public Conquistas.Dados conquistas;
    public FocoEmpresa.Dados foco;
    public ProdutosProprios.Dados produtos;
    public Licitacoes.Dados licitacoes;
    public LivroCaixa.Dados caixa;
    public EventosAleatorios.Dados eventos;
    public float saldo;
    public float reputacao;
    public int fase;
    public int chamadosConcluidos;
    public List<Funcionario> equipe = new List<Funcionario>();
    public string salvoEm;
}

[DefaultExecutionOrder(-100)] // carrega antes do MissionManager gerar os chamados
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    // O menu principal define isso: true = "Continuar", false = "Novo jogo"
    public static bool CarregarAoIniciar = true;

    public static string CaminhoSave => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool ExisteSave => File.Exists(CaminhoSave);

    [ShowNonSerializedField] private int chamadosConcluidos;

    EventBinding<ChamadoConcluidoEvent> bindChamado;
    EventBinding<FuncionarioContratadoEvent> bindContratado;
    EventBinding<FaseDesbloqueadaEvent> bindFase;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnEnable()
    {
        bindChamado = new EventBinding<ChamadoConcluidoEvent>(_ => { chamadosConcluidos++; Salvar(); });
        bindContratado = new EventBinding<FuncionarioContratadoEvent>(Salvar);
        bindFase = new EventBinding<FaseDesbloqueadaEvent>(Salvar);
        EventBus<ChamadoConcluidoEvent>.Register(bindChamado);
        EventBus<FuncionarioContratadoEvent>.Register(bindContratado);
        EventBus<FaseDesbloqueadaEvent>.Register(bindFase);
    }

    void OnDisable()
    {
        EventBus<ChamadoConcluidoEvent>.Deregister(bindChamado);
        EventBus<FuncionarioContratadoEvent>.Deregister(bindContratado);
        EventBus<FaseDesbloqueadaEvent>.Deregister(bindFase);
    }

    void Start()
    {
        if (!CarregarAoIniciar || !Carregar()) NovoJogo();
    }

    void OnApplicationQuit() => Salvar();

    [Button("Salvar agora")]
    public void Salvar()
    {
        if (BancoManager.Instance != null && BancoManager.Instance.faliu) return;   // empresa falida não salva
        var data = new SaveData
        {
            saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f,
            reputacao = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 50f,
            fase = ReputationManager.Instance != null ? ReputationManager.Instance.FaseAtual : 1,
            chamadosConcluidos = chamadosConcluidos,
            mochila = Mochila.Instance != null ? Mochila.Instance.Exportar() : null,
            empresa = EmpresaManager.Instance != null ? EmpresaManager.Instance.Exportar() : null,
            contratos = ContratosManager.Instance != null ? ContratosManager.Instance.Exportar() : null,
            servicos = ServicosManager.Instance != null ? ServicosManager.Instance.Exportar() : null,
            sede = SedeManager.Instance != null ? SedeManager.Instance.Exportar() : null,
            concorrencia = Concorrencia.Exportar(),
            projetos = ProjetosManager.Instance != null ? ProjetosManager.Instance.Exportar() : null,
            banco = BancoManager.Instance != null ? BancoManager.Instance.Exportar() : null,
            parcerias = Parcerias.Exportar(),
            conquistas = Conquistas.Exportar(),
            foco = FocoEmpresa.Exportar(),
            produtos = ProdutosProprios.Exportar(),
            licitacoes = Licitacoes.Exportar(),
            caixa = LivroCaixa.Exportar(),
            eventos = EventosAleatorios.Exportar(),
            dia = DiaManager.Instance != null ? DiaManager.Instance.dia : Mathf.Max(1, DiaManager.DiaSalvo),
            equipe = FuncionarioManager.Instance != null
                     ? new List<Funcionario>(FuncionarioManager.Instance.equipe)
                     : new List<Funcionario>(),
            salvoEm = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        try
        {
            File.WriteAllText(CaminhoSave, JsonUtility.ToJson(data, true));
            Debug.Log($"[Save] Jogo salvo em {CaminhoSave}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Falha ao salvar: {e.Message}");
        }
    }

    [Tooltip("Dinheiro com que um jogo novo começa (para comprar as primeiras ferramentas)")]
    public float saldoInicial = 300f;

    // Jogo novo: dia 1, mochila só com o notebook e um dinheirinho para as primeiras ferramentas
    void NovoJogo()
    {
        DiaManager.DiaSalvo = 0;
        Mochila.dadosSalvos = null;
        if (Mochila.Instance != null) Mochila.Instance.Inicial();
        EmpresaManager.dadosSalvos = null;
        EmpresaManager.Instance?.NovoJogo();
        ContratosManager.dadosSalvos = null;
        ContratosManager.Instance?.NovoJogo();
        ServicosManager.dadosSalvos = null;
        ServicosManager.Instance?.NovoJogo();
        SedeManager.dadosSalvos = null;
        SedeManager.Instance?.NovoJogo();
        Concorrencia.Iniciar();
        ProjetosManager.dadosSalvos = null;
        ProjetosManager.Instance?.NovoJogo();
        BancoManager.dadosSalvos = null;
        BancoManager.Instance?.NovoJogo();
        Parcerias.Iniciar();
        Conquistas.Iniciar();
        FocoEmpresa.Iniciar();
        ProdutosProprios.Iniciar();
        Objetivos.Iniciar();
        Licitacoes.Iniciar();
        LivroCaixa.Iniciar();
        EventosAleatorios.Iniciar();
        GameEconomy.Instance?.DefinirSaldo(saldoInicial);
        Debug.Log($"[Save] Jogo novo: R${saldoInicial:F0} para começar.");
    }

    [Button("Carregar")]
    void BotaoCarregar() => Carregar();

    public bool Carregar()
    {
        if (!ExisteSave) return false;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(CaminhoSave));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Save corrompido, começando do zero: {e.Message}");
            return false;
        }
        if (data == null) return false;

        if (GameEconomy.Instance != null) GameEconomy.Instance.DefinirSaldo(data.saldo);
        if (ReputationManager.Instance != null) ReputationManager.Instance.DefinirEstado(data.reputacao, data.fase);
        if (FuncionarioManager.Instance != null)
        {
            FuncionarioManager.Instance.equipe = data.equipe ?? new List<Funcionario>();
            foreach (var f in FuncionarioManager.Instance.equipe) f.ocupado = false;
        }
        chamadosConcluidos = data.chamadosConcluidos;
        DiaManager.DiaSalvo = Mathf.Max(1, data.dia);
        if (Mochila.Instance != null) Mochila.Instance.Carregar(data.mochila);
        else Mochila.dadosSalvos = data.mochila;
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.Carregar(data.empresa);
        else EmpresaManager.dadosSalvos = data.empresa;
        if (ContratosManager.Instance != null) ContratosManager.Instance.Carregar(data.contratos);
        else ContratosManager.dadosSalvos = data.contratos;
        if (ServicosManager.Instance != null) ServicosManager.Instance.Carregar(data.servicos);
        else ServicosManager.dadosSalvos = data.servicos;
        if (SedeManager.Instance != null) SedeManager.Instance.Carregar(data.sede);
        else SedeManager.dadosSalvos = data.sede;
        Concorrencia.Carregar(data.concorrencia);
        if (ProjetosManager.Instance != null) ProjetosManager.Instance.Carregar(data.projetos);
        else ProjetosManager.dadosSalvos = data.projetos;
        if (BancoManager.Instance != null) BancoManager.Instance.Carregar(data.banco);
        else BancoManager.dadosSalvos = data.banco;
        Parcerias.Carregar(data.parcerias);
        Conquistas.Carregar(data.conquistas);
        FocoEmpresa.Carregar(data.foco);
        ProdutosProprios.Carregar(data.produtos);
        Licitacoes.Carregar(data.licitacoes);
        LivroCaixa.Carregar(data.caixa);
        EventosAleatorios.Carregar(data.eventos);
        if (data.parcerias == null || data.parcerias.estados == null || data.parcerias.estados.Count == 0) MigrarParcerias(data);
        if (DiaManager.Instance != null) DiaManager.Instance.dia = DiaManager.DiaSalvo;

        Debug.Log($"[Save] Jogo carregado (salvo em {data.salvoEm}): R${data.saldo:F2}, reputação {data.reputacao:F0}, fase {data.fase}, {data.equipe?.Count ?? 0} funcionário(s)");
        return true;
    }

    // Saves antigos (antes das parcerias): quem já vendia um serviço vira revenda dele
    static void MigrarParcerias(SaveData data)
    {
        if (data.servicos != null)
            foreach (var c in data.servicos.contratos)
            {
                var p = Parcerias.DoServico(c.tipo);
                if (p != null) Parcerias.Estado(p.id).status = 2;
            }
        if (data.contratos != null && data.contratos.patrimonio != null && data.contratos.patrimonio.Count > 0)
            Parcerias.Estado(Parcerias.Impressoras).status = 2;
    }

    [Button("Apagar save")]
    void BotaoApagar() => ApagarSave();

    public static void ApagarSave()
    {
        if (ExisteSave) File.Delete(CaminhoSave);
        Debug.Log("[Save] Save apagado.");
    }
}
