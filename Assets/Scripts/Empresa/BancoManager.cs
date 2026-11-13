using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Emprestimo
{
    public float valor;
    public float parcela;
    public int totalParcelas;
    public int parcelasGeradas;
    public int diaContratado;
    public float jurosMes;
    public int Restantes => totalParcelas - parcelasGeradas;
}

// Banco (empréstimo de capital de giro) e falência.
// Saldo negativo por 30 dias seguidos = a empresa quebra (game over). Avisos com 10 e 20 dias.
// Também escuta os chamados e o virar do dia para as conquistas.
public class BancoManager : MonoBehaviour
{
    public static BancoManager Instance;

    public const float JurosPessoaFisica = 0.055f;   // crédito pessoal (aprox.)
    public const float JurosEmpresa = 0.032f;        // capital de giro para pequena empresa (aprox.)
    public const int DiasParaFalencia = 30;
    public static readonly int[] OpcoesParcelas = { 6, 12 };

    public List<Emprestimo> emprestimos = new List<Emprestimo>();
    public int diasNoVermelho;
    public bool faliu;
    public int ultimoMesCobrado = -1;

    EventBinding<DiaIniciadoEvent> bDia;
    EventBinding<ChamadoConcluidoEvent> bChamado;

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
        new GameObject("BancoManager").AddComponent<BancoManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(e => NovoDia(e.dia));
        bChamado = new EventBinding<ChamadoConcluidoEvent>(e => { if (!e.porFuncionario) Conquistas.RegistrarChamado(e.diagnosticoCerteiro); });
        EventBus<DiaIniciadoEvent>.Register(bDia);
        EventBus<ChamadoConcluidoEvent>.Register(bChamado);
    }

    void OnDisable()
    {
        EventBus<DiaIniciadoEvent>.Deregister(bDia);
        EventBus<ChamadoConcluidoEvent>.Deregister(bChamado);
    }

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    // ---------- Crédito ----------

    public float JurosMes => EmpresaManager.Instance != null && EmpresaManager.Instance.TemCnpj ? JurosEmpresa : JurosPessoaFisica;

    public float SaldoDevedor
    {
        get { float t = 0f; foreach (var e in emprestimos) t += e.parcela * e.Restantes; return t; }
    }

    // Limite: média do faturamento dos últimos 3 meses × 2 (mínimo R$ 1.500 PF / R$ 3.000 CNPJ), menos o que já deve
    public float Limite
    {
        get
        {
            var emp = EmpresaManager.Instance;
            float media = 0f;
            if (emp != null)
            {
                int mes = EmpresaManager.MesDoDia(Hoje);
                for (int m = mes - 3; m < mes; m++) media += emp.FaturamentoNoMes(m);
                media /= 3f;
            }
            float minimo = emp != null && emp.TemCnpj ? 3000f : 1500f;
            return Mathf.Max(0f, Mathf.Round(Mathf.Max(minimo, media * 2f) - SaldoDevedor));
        }
    }

    // Tabela Price
    public static float Parcela(float valor, float juros, int n) =>
        Mathf.Round(valor * juros / (1f - Mathf.Pow(1f + juros, -n)) * 100f) / 100f;

    public string Tomar(float valor, int parcelas)
    {
        var emp = EmpresaManager.Instance;
        if (faliu) return "A empresa faliu.";
        if (emp != null && emp.Irregular) return "O banco negou: impostos atrasados (sem certidão negativa de débitos).";
        if (valor <= 0f) return "Valor inválido.";
        if (valor > Limite) return $"O banco aprova no máximo {UIFactory.Dinheiro(Limite)} agora.";
        float juros = JurosMes;
        if (ultimoMesCobrado < 0) ultimoMesCobrado = EmpresaManager.MesDoDia(Hoje);
        var e = new Emprestimo { valor = valor, parcela = Parcela(valor, juros, parcelas), totalParcelas = parcelas, diaContratado = Hoje, jurosMes = juros };
        emprestimos.Add(e);
        GameEconomy.Instance?.ReceberPagamento(valor);
        return $"Empréstimo aprovado: {UIFactory.Dinheiro(valor)} na conta. {parcelas}x de {UIFactory.Dinheiro(e.parcela)} " +
               $"({juros * 100:0.#}% ao mês; total {UIFactory.Dinheiro(e.parcela * parcelas)}). As parcelas chegam como guias todo mês.";
    }

    // ---------- Dia a dia ----------

    void NovoDia(int dia)
    {
        // Parcelas: uma por mês, vencendo no dia 10
        int mesAtual = EmpresaManager.MesDoDia(dia);
        if (ultimoMesCobrado < 0) ultimoMesCobrado = mesAtual;   // começa a contar do mês em que o jogo começou
        while (ultimoMesCobrado < mesAtual)
        {
            ultimoMesCobrado++;
            var inicio = EmpresaManager.PrimeiroDiaDoMes(ultimoMesCobrado);
            foreach (var e in emprestimos)
            {
                if (e.Restantes <= 0 || e.diaContratado >= EmpresaManager.DiaDaData(inicio)) continue;
                e.parcelasGeradas++;
                string nome = $"Parcela do empréstimo ({e.parcelasGeradas}/{e.totalParcelas})";
                if (EmpresaManager.Instance != null)
                    EmpresaManager.Instance.AdicionarGuia(nome, e.parcela, EmpresaManager.DiaDaData(inicio.AddDays(9)), ultimoMesCobrado - 1);
                else GameEconomy.Instance?.Gastar(e.parcela);
            }
        }
        // Empréstimo com todas as parcelas pagas
        foreach (var e in new List<Emprestimo>(emprestimos))
        {
            if (e.Restantes > 0) continue;
            bool pendente = EmpresaManager.Instance != null &&
                            EmpresaManager.Instance.guias.Exists(g => !g.paga && g.nome.StartsWith("Parcela do empréstimo"));
            if (pendente) continue;
            emprestimos.Remove(e);
            Conquistas.Desbloquear("emprestimo-quitado");
        }

        Conquistas.Verificar();
        VerificarFalencia(dia);
    }

    void VerificarFalencia(int dia)
    {
        if (faliu) return;
        float saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        if (saldo >= 0f) { diasNoVermelho = 0; return; }
        diasNoVermelho++;
        if (diasNoVermelho == 10 || diasNoVermelho == 20)
            GameUI.Instance?.Toast($"<b>Conta no vermelho há {diasNoVermelho} dias!</b> Com {DiasParaFalencia} dias a empresa quebra. " +
                                   "Cobre clientes, corte custos ou pegue um empréstimo (aba Empresa).", UIFactory.Perigo, 7f);
        if (diasNoVermelho >= DiasParaFalencia) Falir(dia);
    }

    void Falir(int dia)
    {
        faliu = true;
        EventBus<FalenciaEvent>.Raise(new FalenciaEvent
        {
            dia = dia,
            saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f,
            motivo = $"A conta ficou {DiasParaFalencia} dias seguidos no vermelho."
        });
        Debug.Log("[Banco] Falência!");
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public List<Emprestimo> emprestimos = new List<Emprestimo>();
        public int diasNoVermelho, ultimoMesCobrado = -1;
        public bool faliu;
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados { emprestimos = new List<Emprestimo>(emprestimos), diasNoVermelho = diasNoVermelho, ultimoMesCobrado = ultimoMesCobrado, faliu = faliu };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        emprestimos = d.emprestimos ?? new List<Emprestimo>();
        diasNoVermelho = d.diasNoVermelho; ultimoMesCobrado = d.ultimoMesCobrado; faliu = d.faliu;
    }

    public void NovoJogo() { emprestimos.Clear(); diasNoVermelho = 0; faliu = false; ultimoMesCobrado = -1; }
}
