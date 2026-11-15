using UnityEngine;
using UnityEngine.SceneManagement;

// Energia do técnico (0–100). Cai com as horas acordado e com o trabalho; dormir recupera conforme a hora
// em que você foi deitar; café dá um empurrão. Cansado, você anda mais devagar e as ações demoram mais.
public class EnergiaTecnico : MonoBehaviour
{
    public static EnergiaTecnico Instance;
    public static float energiaSalva = -1f;   // o SaveManager preenche ao carregar

    public float energia = 100f;
    [Tooltip("Quanto cai por hora do jogo acordado")]
    public float gastoPorHora = 5.5f;
    [Tooltip("Quanto cada chamado concluído cansa")]
    public float gastoPorChamado = 4f;

    public const float Cansado = 30f, Exausto = 12f;

    // Multiplicador de velocidade das ações de "segurar E" e do andar
    public static float Ritmo => Instance == null ? 1f : Instance.energia < Exausto ? 0.6f : Instance.energia < Cansado ? 0.8f : 1f;
    public static float RitmoAndar => Instance == null ? 1f : Instance.energia < Exausto ? 0.75f : Instance.energia < Cansado ? 0.9f : 1f;

    float horaDeitou = -1f;
    bool dormiuNoEscritorio;
    bool avisouCansado, avisouExausto;
    static bool registrado;

    EventBinding<ChamadoConcluidoEvent> bChamado;
    EventBinding<DiaEncerradoEvent> bEncerrado;
    EventBinding<DiaIniciadoEvent> bIniciado;

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
        new GameObject("EnergiaTecnico").AddComponent<EnergiaTecnico>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (energiaSalva >= 0f) { energia = energiaSalva; energiaSalva = -1f; }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bChamado = new EventBinding<ChamadoConcluidoEvent>(_ => Gastar(gastoPorChamado));
        bEncerrado = new EventBinding<DiaEncerradoEvent>(e =>
        {
            horaDeitou = DiaManager.Instance != null ? DiaManager.Instance.Hora : 22f;
            dormiuNoEscritorio = e.resumo != null && e.resumo.dormiuNoEscritorio;
        });
        bIniciado = new EventBinding<DiaIniciadoEvent>(_ => Acordar());
        EventBus<ChamadoConcluidoEvent>.Register(bChamado);
        EventBus<DiaEncerradoEvent>.Register(bEncerrado);
        EventBus<DiaIniciadoEvent>.Register(bIniciado);
    }

    void OnDisable()
    {
        EventBus<ChamadoConcluidoEvent>.Deregister(bChamado);
        EventBus<DiaEncerradoEvent>.Deregister(bEncerrado);
        EventBus<DiaIniciadoEvent>.Deregister(bIniciado);
    }

    void Update()
    {
        var d = DiaManager.Instance;
        if (d == null) return;
        // horas do jogo que passaram neste quadro (o relógio corre 'minutosPorSegundo' minutos por segundo)
        float horas = Time.deltaTime * d.minutosPorSegundo / 60f;
        Gastar(horas * gastoPorHora);
    }

    public void Gastar(float quanto)
    {
        energia = Mathf.Clamp(energia - quanto, 0f, 100f);
        if (!avisouCansado && energia < Cansado)
        {
            avisouCansado = true;
            GameUI.Instance?.Toast("Você está cansado: anda e trabalha mais devagar. Um café ajuda; dormir resolve.", UIFactory.Alerta, 4f);
        }
        if (!avisouExausto && energia < Exausto)
        {
            avisouExausto = true;
            GameUI.Instance?.Toast("Exausto! Vá para casa dormir.", UIFactory.Perigo, 4f);
        }
    }

    public void Recuperar(float quanto)
    {
        energia = Mathf.Clamp(energia + quanto, 0f, 100f);
        if (energia >= Cansado) avisouCansado = false;
        if (energia >= Exausto) avisouExausto = false;
    }

    // Deitar até 21h30 recupera tudo; até 22h30, 90%; ficar acordado até o limite (23h), 75%;
    // dormir no escritório, 55%
    void Acordar()
    {
        float recuperada;
        if (dormiuNoEscritorio) recuperada = 55f;
        else if (horaDeitou < 0f || horaDeitou <= 21.5f) recuperada = 100f;
        else if (horaDeitou <= 22.5f) recuperada = 90f;
        else recuperada = 75f;
        energia = recuperada;
        avisouCansado = avisouExausto = false;
        horaDeitou = -1f;
        dormiuNoEscritorio = false;
        if (recuperada < 100f)
            GameUI.Instance?.Toast($"Dormiu pouco: acordou com {recuperada:0}% de energia.", UIFactory.Alerta, 4f);
    }
}
