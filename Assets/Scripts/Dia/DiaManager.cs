using UnityEngine;
using UnityEngine.SceneManagement;

// Resumo de um dia de trabalho (mostrado na tela de fim do dia)
[System.Serializable]
public class ResumoDia
{
    public int dia;
    public int chamados;
    public int chamadosEquipe;
    public int erros;
    public float ganhos;
    public float salarios;
    public float compras;
    public float deltaReputacao;
    public float saldoFinal;
    public string horaSaida;
    public bool dormiuNoEscritorio;
}

// Relógio do jogo e ciclo do dia (no estilo dos "simuladores"):
// o dia começa às 8h, o expediente acaba às 18h, o jogador encerra o dia, vê o resumo, dorme e começa o próximo.
// Também move o sol, que vai se pondo, e escurece o céu à noite.
// É criado sozinho em qualquer cena que tenha um MissionManager.
public class DiaManager : MonoBehaviour
{
    public static DiaManager Instance;

    // Preenchido pelo SaveManager ao carregar (0 = jogo novo)
    public static int DiaSalvo;

    [Header("Relógio")]
    [Tooltip("Minutos do jogo que passam a cada segundo real (1 = o expediente de 10h dura 10 minutos)")]
    public float minutosPorSegundo = 1f;
    public int horaInicio = 8;
    public int horaFimExpediente = 18;
    [Tooltip("Se o jogador não for embora até essa hora, o dia acaba sozinho")]
    public int horaLimite = 23;

    [Header("Estado")]
    public int dia = 1;
    public float minutoAtual;
    public bool diaEncerrado;

    [Header("Custos")]
    [Tooltip("Dias úteis por mês: o salário diário é salário mensal / esse número")]
    public float diasUteisPorMes = 22f;

    // Estatísticas do dia
    ResumoDia hoje = new ResumoDia();
    bool avisouFim, avisouTarde;

    Light sol;
    float solYaw = 200f;

    EventBinding<ChamadoConcluidoEvent> bChamado;
    EventBinding<SaldoAlteradoEvent> bSaldo;
    EventBinding<ReputacaoAlteradaEvent> bRep;

    public float Hora => minutoAtual / 60f;
    public bool ForaDoExpediente => Hora >= horaFimExpediente;
    public bool EhNoite => Hora >= 18.5f || Hora < 6f;

    // ---------- Criação automática ----------

    static bool registrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!registrado)
        {
            SceneManager.sceneLoaded += (_, __) => CriarSePrecisar();
            registrado = true;
        }
        CriarSePrecisar();
    }

    static void CriarSePrecisar()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<MissionManager>() == null) return;
        new GameObject("DiaManager").AddComponent<DiaManager>();
    }

    // ---------- Ciclo de vida ----------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (DiaSalvo > 0) dia = DiaSalvo;
        minutoAtual = horaInicio * 60f;
        hoje.dia = dia;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        bChamado = new EventBinding<ChamadoConcluidoEvent>(e =>
        {
            if (e.porFuncionario) hoje.chamadosEquipe++; else hoje.chamados++;
            if (e.cometeuErro) hoje.erros++;
        });
        bSaldo = new EventBinding<SaldoAlteradoEvent>(e =>
        {
            // Ignora o saldo carregado do save no começo da cena
            if (Time.timeSinceLevelLoad > 1f && e.delta > 0f) hoje.ganhos += e.delta;
            if (Time.timeSinceLevelLoad > 1f && e.delta < 0f) hoje.compras += -e.delta;
        });
        bRep = new EventBinding<ReputacaoAlteradaEvent>(e =>
        {
            if (Time.timeSinceLevelLoad > 1f) hoje.deltaReputacao += e.delta;
        });
        EventBus<ChamadoConcluidoEvent>.Register(bChamado);
        EventBus<SaldoAlteradoEvent>.Register(bSaldo);
        EventBus<ReputacaoAlteradaEvent>.Register(bRep);
    }

    void OnDisable()
    {
        EventBus<ChamadoConcluidoEvent>.Deregister(bChamado);
        EventBus<SaldoAlteradoEvent>.Deregister(bSaldo);
        EventBus<ReputacaoAlteradaEvent>.Deregister(bRep);
    }

    void Start()
    {
        AcharSol();
        CriarSaidaDoEscritorio();
        AtualizarSol();
    }

    void Update()
    {
        if (diaEncerrado) return;

        // Time.deltaTime já para quando o jogo está pausado (timeScale 0)
        minutoAtual += Time.deltaTime * minutosPorSegundo;

        if (!avisouFim && Hora >= horaFimExpediente)
        {
            avisouFim = true;
            EventBus<ExpedienteEncerradoEvent>.Raise(new ExpedienteEncerradoEvent { dia = dia });
        }
        if (!avisouTarde && Hora >= horaLimite - 2)
        {
            avisouTarde = true;
            GameUI.Instance?.Toast("Já está tarde... vá para casa descansar!", UIFactory.Alerta, 4f);
        }
        if (Hora >= horaLimite)
        {
            // Ficou até tarde demais: se ainda estava no escritório, dormiu lá (pega mal com os clientes)
            hoje.dormiuNoEscritorio = JogadorNoEscritorio();
            if (hoje.dormiuNoEscritorio) ReputationManager.Instance?.AlterarReputacao(-2f);
            EncerrarDia(true);
        }

        AtualizarSol();
    }

    // ---------- Encerrar e começar o dia ----------

    // Devolve false (e explica) se ainda não dá para encerrar
    public bool EncerrarDia(bool forcado = false)
    {
        if (diaEncerrado) return false;

        var mm = MissionManager.Instance;
        if (mm != null && mm.chamadoAtivo != null && !forcado)
        {
            GameUI.Instance?.Toast("Termine ou desista do chamado em atendimento antes de ir embora.", UIFactory.Perigo, 3f);
            return false;
        }
        if (mm != null && mm.chamadoAtivo != null) mm.AbandonarChamado();

        diaEncerrado = true;

        // Salários agora saem na folha mensal (aba Empresa); sem EmpresaManager, desconta por dia como antes
        float salarios = 0f;
        if (EmpresaManager.Instance == null && FuncionarioManager.Instance != null)
        {
            foreach (var f in FuncionarioManager.Instance.equipe) salarios += f.salarioMensal / Mathf.Max(1f, diasUteisPorMes);
            salarios = Mathf.Round(salarios);
            if (salarios > 0f && GameEconomy.Instance != null) GameEconomy.Instance.Gastar(salarios, LivroCaixa.Folha);
        }

        hoje.dia = dia;
        hoje.salarios = salarios;
        hoje.compras = Mathf.Max(0f, hoje.compras - salarios);   // o desconto dos salários também passou pelo saldo
        hoje.saldoFinal = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        hoje.horaSaida = HoraTexto();

        Debug.Log($"[Dia] Dia {dia} encerrado às {hoje.horaSaida}: {hoje.chamados} chamado(s), +R${hoje.ganhos:F0}, salários R${salarios:F0}");
        EventBus<DiaEncerradoEvent>.Raise(new DiaEncerradoEvent { resumo = hoje });
        return true;
    }

    public void ComecarNovoDia()
    {
        dia++;
        minutoAtual = horaInicio * 60f;
        diaEncerrado = false;
        avisouFim = avisouTarde = false;
        hoje = new ResumoDia { dia = dia };

        // O jogador acorda e começa de novo no escritório
        var jogador = GameObject.FindWithTag("Player");
        var spawn = PontoDeInicio();
        if (jogador != null && spawn != null)
        {
            Teleporte.MoverJogador(jogador.transform, spawn);
            // Acorda olhando para a frente (não para o chão onde estava olhando ao deitar)
            jogador.GetComponent<PlayerController>()?.OlharPara(spawn.position + spawn.forward * 5f + Vector3.up * 0.6f);
        }

        MissionManager.Instance?.GerarChamados();
        AtualizarSol();

        EventBus<DiaIniciadoEvent>.Raise(new DiaIniciadoEvent { dia = dia });
        SaveManager.Instance?.Salvar();
        Debug.Log($"[Dia] Começou o dia {dia}");
    }

    // Onde o jogador começa o dia: a casa (quando existir) ou o escritório
    Transform PontoDeInicio()
    {
        var casa = GameObject.Find("SpawnCasa");
        if (casa != null) return casa.transform;
        var escritorio = GameObject.Find("SpawnEscritorio");
        return escritorio != null ? escritorio.transform : null;
    }

    // Cenário com rua e casa (menu "TI Service/4. Montar rua e casa"): o dia acaba na cama
    public bool TemCasa { get; private set; }

    // Se não há casa, o jogador sempre "está no escritório" (comportamento antigo)
    bool JogadorNoEscritorio()
    {
        var sala = GameObject.Find("Cenario/Escritorio");
        var jogador = GameObject.FindWithTag("Player");
        if (!TemCasa || sala == null || jogador == null) return true;
        Vector3 local = sala.transform.InverseTransformPoint(jogador.transform.position);
        return Mathf.Abs(local.x) < 3.2f && Mathf.Abs(local.z) < 3.2f;
    }

    // Porta do escritório que encerra o dia (criada aqui para não precisar remontar o cenário).
    // Com rua e casa, a porta só abre: o jogador vai a pé para casa e dorme na cama.
    void CriarSaidaDoEscritorio()
    {
        var sala = GameObject.Find("Cenario/Escritorio");
        TemCasa = FindAnyObjectByType<Cama>() != null;
        if (sala == null) return;

        // Bancada para fazer cabos, encostada na parede leste (embaixo da janela)
        if (FindAnyObjectByType<Bancada>() == null)
            Bancada.Criar(sala.transform, new Vector3(2.65f, 0f, -1.3f), -90f);

        if (TemCasa || FindAnyObjectByType<SaidaEscritorio>() != null) return;
        var porta = new GameObject("Porta_EncerrarDia");
        porta.transform.SetParent(sala.transform, false);
        porta.transform.localPosition = new Vector3(0f, 1f, -2.75f);
        var saida = porta.AddComponent<SaidaEscritorio>();
        saida.distanciaInteracao = 1.3f;
    }

    // ---------- Sol e céu ----------

    void AcharSol()
    {
        sol = RenderSettings.sun;
        if (sol == null)
            foreach (var l in FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null) solYaw = sol.transform.eulerAngles.y;
    }

    void AtualizarSol()
    {
        if (sol == null) return;
        float h = Hora;

        // Altura do sol: nasce às 6h, ponto mais alto ao meio-dia, se põe às 18h
        float altura = (h >= 6f && h <= 18f) ? 60f * Mathf.Sin(Mathf.PI * (h - 6f) / 12f) : -15f;
        float dia01 = Mathf.Clamp01(altura / 25f);
        float yaw = solYaw + (h - 12f) * 8f;

        // À noite o sol fica abaixo do horizonte (o céu escurece sozinho)
        sol.transform.rotation = Quaternion.Euler(altura, yaw, 0f);
        sol.intensity = 1.25f * dia01;
        sol.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.95f, 0.86f), Mathf.Clamp01(altura / 35f));
        sol.shadows = dia01 > 0.01f ? LightShadows.Soft : LightShadows.None;

        RenderSettings.ambientIntensity = Mathf.Lerp(0.18f, 0.65f, Mathf.Clamp01((altura + 10f) / 40f));

        // Luz ambiente em 3 cores (céu, horizonte, chão): o lado das paredes sem sol não fica preto.
        // (O ambiente vindo do skybox depende de iluminação pré-calculada, que o cenário montado por código não tem.)
        float claro = Mathf.Clamp01((altura + 10f) / 40f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.05f, 0.06f, 0.10f), new Color(0.60f, 0.68f, 0.82f), claro);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.04f, 0.04f, 0.06f), new Color(0.50f, 0.50f, 0.48f), claro);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.02f, 0.02f, 0.02f), new Color(0.30f, 0.28f, 0.24f), claro);
        RenderSettings.reflectionIntensity = Mathf.Lerp(0.3f, 0.8f, dia01);
    }

    // ---------- Calendário ----------
    // O dia 1 do jogo é uma segunda-feira, 2 de março de 2026. Impostos e boletos vencem em datas do mês.

    public static readonly System.DateTime Inicio = new System.DateTime(2026, 3, 2);
    public static System.DateTime DataDoDia(int d) => Inicio.AddDays(Mathf.Max(1, d) - 1);
    public System.DateTime Data => DataDoDia(dia);
    public bool FimDeSemana => Data.DayOfWeek == System.DayOfWeek.Saturday || Data.DayOfWeek == System.DayOfWeek.Sunday;

    // ---------- Textos ----------

    static readonly string[] diasSemana = { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };

    public string DiaSemana => diasSemana[(int)Data.DayOfWeek];
    public string DataTexto => Data.ToString("dd/MM/yyyy");

    public string HoraTexto()
    {
        int m = Mathf.FloorToInt(minutoAtual);
        return $"{(m / 60) % 24:00}:{m % 60:00}";
    }
}
