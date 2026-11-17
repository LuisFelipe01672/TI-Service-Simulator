using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;

// Salva e carrega o progresso do jogador em JSON (Application.persistentDataPath/save.json).
// Salva sozinho quando: um chamado é concluído, alguém é contratado, uma fase é desbloqueada e ao sair do jogo.
// À prova de queda: grava num .tmp e só então troca pelo save (o anterior vira .bak). Se o save não abrir, ele é
// guardado à parte (.corrompido) e o jogo tenta o .bak — nunca sobrescreve um save que falhou ao carregar.
// Versão: todo campo novo precisa de valor inicial declarado; mudança de formato ganha um passo em Migrar().
[Serializable]
public class SaveData
{
    public int versao = SaveManager.VersaoAtual;
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
    public EquipamentosUsados.Dados usados;
    public MissionManager.Dados atendimento;   // OS fechadas, estrelas e retornos em garantia (versão 4)
    public CarroTecnico.Dados carro;           // Etapa 9C: onde o carro ficou, combustível, hodômetro e porta-malas (null = save antigo)
    public float saldo;
    public float energia = -1f;   // energia do técnico (-1 = save antigo, começa cheia)
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

    // O editor usa outro arquivo: testar na Unity não mexe no save de quem joga pelo executável
    public static string CaminhoSave => Path.Combine(Application.persistentDataPath, Application.isEditor ? "save_editor.json" : "save.json");
    static string CaminhoTmp => CaminhoSave + ".tmp";
    static string CaminhoBak => CaminhoSave + ".bak";
    public static bool ExisteSave => File.Exists(CaminhoSave) || File.Exists(CaminhoBak);

    // 3 = save à prova de queda + migração em cadeia (formato igual ao 2)
    // 4 = atendimento: ordens de serviço, estrelas dos clientes e retornos em garantia
    public const int VersaoAtual = 4;

    // Save de uma versão mais nova do jogo: não carrega e não deixa sobrescrever
    static bool bloqueadoPorVersao;
    static string avisoPendente;

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
        if (bloqueadoPorVersao) { Debug.LogWarning("[Save] O save é de uma versão mais nova do jogo: não vou sobrescrever."); return; }
        var data = new SaveData
        {
            saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f,
            energia = EnergiaTecnico.Instance != null ? EnergiaTecnico.Instance.energia : -1f,
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
            usados = EquipamentosUsados.Exportar(),
            carro = CarroTecnico.Exportar(),
            atendimento = MissionManager.Instance != null ? MissionManager.Instance.Exportar() : null,
            dia = DiaManager.Instance != null ? DiaManager.Instance.dia : Mathf.Max(1, DiaManager.DiaSalvo),
            equipe = FuncionarioManager.Instance != null
                     ? new List<Funcionario>(FuncionarioManager.Instance.equipe)
                     : new List<Funcionario>(),
            salvoEm = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        try
        {
            GravarSeguro(JsonUtility.ToJson(data, true));
            Debug.Log($"[Save] Jogo salvo em {CaminhoSave}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Falha ao salvar: {e.Message}");
        }
    }

    // Grava no .tmp (com flush no disco) e troca de uma vez: queda no meio deixa o save anterior inteiro
    static void GravarSeguro(string json)
    {
        using (var fs = new FileStream(CaminhoTmp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var w = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
        {
            w.Write(json);
            w.Flush();
            fs.Flush(true);
        }
        if (File.Exists(CaminhoSave)) File.Replace(CaminhoTmp, CaminhoSave, CaminhoBak, true);
        else File.Move(CaminhoTmp, CaminhoSave);
    }

    // Lê e valida um arquivo de save; null se não abriu
    static SaveData Ler(string caminho, out string erro)
    {
        erro = null;
        try
        {
            if (!File.Exists(caminho)) { erro = "arquivo não existe"; return null; }
            string json = File.ReadAllText(caminho);
            if (string.IsNullOrWhiteSpace(json)) { erro = "arquivo vazio"; return null; }
            var d = JsonUtility.FromJson<SaveData>(json);
            if (d == null) erro = "conteúdo inválido";
            return d;
        }
        catch (Exception e) { erro = e.Message; return null; }
    }

    // Passos de migração, um por versão (2→3, 3→4...). Saves antigos sem o campo vêm como versão 0.
    static void Migrar(SaveData data)
    {
        if (data.versao < 3)
        {
            // 2→3: só o jeito de gravar mudou. Saves de antes das parcerias viram revenda do que já vendiam.
            if (data.parcerias == null || data.parcerias.estados == null || data.parcerias.estados.Count == 0) MigrarParcerias(data);
        }
        if (data.versao < 4)
        {
            // 3→4: o atendimento (OS, estrelas, retornos) começa vazio
            if (data.atendimento == null || data.atendimento.proximaOS <= 0) data.atendimento = new MissionManager.Dados();
            data.atendimento.tutorialFeito = true;   // quem já jogava não precisa do chamado guiado
        }
        data.versao = VersaoAtual;
    }

    // O aviso ao jogador espera a interface existir
    System.Collections.IEnumerator MostrarAviso()
    {
        for (int i = 0; i < 300 && GameUI.Instance == null; i++) yield return null;
        yield return new WaitForSeconds(1.5f);
        if (avisoPendente != null && GameUI.Instance != null) GameUI.Instance.Toast(avisoPendente, UIFactory.Alerta, 7f);
        avisoPendente = null;
    }

    [Tooltip("Dinheiro com que um jogo novo começa (para comprar as primeiras ferramentas)")]
    public float saldoInicial = 300f;

    // Jogo novo: dia 1, mochila só com o notebook e um dinheirinho para as primeiras ferramentas
    void NovoJogo()
    {
        DiaManager.DiaSalvo = 0;
        EnergiaTecnico.energiaSalva = -1f;
        if (EnergiaTecnico.Instance != null) EnergiaTecnico.Instance.energia = 100f;
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
        EquipamentosUsados.Iniciar();
        CarroTecnico.NovoJogo();
        MissionManager.dadosSalvos = null;
        MissionManager.Instance?.NovoJogo();
        GameEconomy.Instance?.DefinirSaldo(saldoInicial);
        Debug.Log($"[Save] Jogo novo: R${saldoInicial:F0} para começar.");
    }

    [Button("Carregar")]
    void BotaoCarregar() => Carregar();

    public bool Carregar()
    {
        if (!ExisteSave) return false;
        bloqueadoPorVersao = false;

        SaveData data = Ler(CaminhoSave, out string erro);
        if (data == null)
        {
            // Guarda o arquivo estragado à parte (nunca é sobrescrito) e tenta a cópia anterior
            if (File.Exists(CaminhoSave))
            {
                string guardado = CaminhoSave + ".corrompido-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                try { File.Move(CaminhoSave, guardado); } catch (Exception e) { Debug.LogError("[Save] Não consegui guardar o save estragado: " + e.Message); }
                Debug.LogError($"[Save] Save não abriu ({erro}). Guardado em {guardado}.");
            }
            data = Ler(CaminhoBak, out string erroBak);
            if (data == null)
            {
                Debug.LogError($"[Save] A cópia anterior também não abriu ({erroBak}). Começando do zero.");
                avisoPendente = "Não consegui abrir o seu jogo salvo. O arquivo foi guardado à parte e um jogo novo começou.";
                StartCoroutine(MostrarAviso());
                return false;
            }
            avisoPendente = "O último save estava estragado: o jogo voltou para a cópia anterior.";
            StartCoroutine(MostrarAviso());
        }

        if (data.versao > VersaoAtual)
        {
            bloqueadoPorVersao = true;
            Debug.LogError($"[Save] Save da versão {data.versao}, este jogo vai até a {VersaoAtual}. Não carrego nem sobrescrevo.");
            avisoPendente = "Este save é de uma versão mais nova do jogo. Atualize o jogo para continuar.";
            StartCoroutine(MostrarAviso());
            return false;
        }
        Migrar(data);

        if (GameEconomy.Instance != null) GameEconomy.Instance.DefinirSaldo(data.saldo);
        if (data.energia >= 0f) { if (EnergiaTecnico.Instance != null) EnergiaTecnico.Instance.energia = data.energia; else EnergiaTecnico.energiaSalva = data.energia; }
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
        EquipamentosUsados.Carregar(data.usados);
        CarroTecnico.CarregarSalvo(data.carro);
        if (MissionManager.Instance != null) MissionManager.Instance.Carregar(data.atendimento);
        else MissionManager.dadosSalvos = data.atendimento;
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

    // "Novo jogo" ou falência: o jogo anterior não some de vez, fica guardado como _anterior (o último só)
    public static void ApagarSave()
    {
        try
        {
            if (File.Exists(CaminhoSave))
            {
                string anterior = CaminhoSave.Replace(".json", "_anterior.json");
                if (File.Exists(anterior)) File.Delete(anterior);
                File.Move(CaminhoSave, anterior);
            }
            if (File.Exists(CaminhoBak)) File.Delete(CaminhoBak);
            if (File.Exists(CaminhoTmp)) File.Delete(CaminhoTmp);
        }
        catch (Exception e) { Debug.LogError("[Save] Falha ao apagar: " + e.Message); }
        bloqueadoPorVersao = false;
        Debug.Log("[Save] Save apagado (o anterior fica guardado como _anterior).");
    }
}
