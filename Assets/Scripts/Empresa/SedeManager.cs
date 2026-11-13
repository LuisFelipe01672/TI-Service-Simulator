using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ImovelInfo
{
    public string nome;
    public float aluguel;
    public int maxEstacoes;     // quantas estações de trabalho cabem
    public string descricao;
    public ImovelInfo(string nome, float aluguel, int maxEstacoes, string descricao)
    { this.nome = nome; this.aluguel = aluguel; this.maxEstacoes = maxEstacoes; this.descricao = descricao; }
}

public enum Movel { Mesa, Cadeira, PcEquipe, BancadaTecnica, Estante, ArCondicionado, Recepcao, Fachada }

public class MovelInfo
{
    public Movel tipo;
    public string nome, efeito;
    public float preco;
    public MovelInfo(Movel tipo, string nome, float preco, string efeito) { this.tipo = tipo; this.nome = nome; this.preco = preco; this.efeito = efeito; }
}

// A sede da empresa: começa num quarto em casa; com CNPJ dá para alugar uma sala, que chega vazia.
// Móveis definem quantos funcionários cabem (estação = mesa + cadeira + PC), quanto estoque cabe e a imagem para clientes.
// Valores aproximados para balanceamento.
public class SedeManager : MonoBehaviour
{
    public static SedeManager Instance;

    public static readonly ImovelInfo[] Imoveis =
    {
        new ImovelInfo("Quarto em casa", 0f, 0, "Sem aluguel. Só você trabalha aqui."),
        new ImovelInfo("Sala comercial 20 m²", 1200f, 3, "Cabem 3 estações de trabalho."),
        new ImovelInfo("Sala comercial 45 m²", 2400f, 6, "Cabem 6 estações e uma recepção."),
        new ImovelInfo("Loja/galpão 120 m²", 5000f, 15, "Equipe grande, estoque e garagem."),
    };

    public static readonly MovelInfo[] Moveis =
    {
        new MovelInfo(Movel.Mesa, "Mesa de escritório", 450f, "Parte de uma estação de trabalho."),
        new MovelInfo(Movel.Cadeira, "Cadeira", 350f, "Parte de uma estação de trabalho."),
        new MovelInfo(Movel.PcEquipe, "Computador da equipe", 3000f, "Parte de uma estação de trabalho."),
        new MovelInfo(Movel.BancadaTecnica, "Bancada técnica", 900f, "Mais espaço para preparar equipamentos."),
        new MovelInfo(Movel.Estante, "Estante de estoque", 400f, "Guarda mais 8 equipamentos de locação."),
        new MovelInfo(Movel.ArCondicionado, "Ar-condicionado", 2500f, "Equipe mais produtiva (aumenta a conta de luz)."),
        new MovelInfo(Movel.Recepcao, "Recepção", 1200f, "Clientes confiam mais nas propostas."),
        new MovelInfo(Movel.Fachada, "Placa da fachada", 800f, "Clientes confiam mais nas propostas."),
    };

    public const int CaucaoMeses = 3;
    public const float Internet = 150f;
    public const float LuzBase = 150f, LuzPorEstacao = 60f, LuzArCondicionado = 250f;
    public const int EstoqueCasa = 3, EstoquePorEstante = 8;

    public int imovel;                         // índice em Imoveis
    public float caucaoPaga;
    public List<int> moveis = new List<int>(); // quantidade por Movel
    public int ultimoMesCobrado = -1;

    EventBinding<DiaIniciadoEvent> bDia;

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
        new GameObject("SedeManager").AddComponent<SedeManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; } else NovoJogo();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(e => NovoDia(e.dia));
        EventBus<DiaIniciadoEvent>.Register(bDia);
    }

    void OnDisable() => EventBus<DiaIniciadoEvent>.Deregister(bDia);

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    public ImovelInfo Imovel => Imoveis[Mathf.Clamp(imovel, 0, Imoveis.Length - 1)];
    public int Qtd(Movel m) => (int)m < moveis.Count ? moveis[(int)m] : 0;

    // Estação de trabalho = mesa + cadeira + computador, limitada pelo tamanho do imóvel
    public int Estacoes => Mathf.Min(Imovel.maxEstacoes, Mathf.Min(Qtd(Movel.Mesa), Mathf.Min(Qtd(Movel.Cadeira), Qtd(Movel.PcEquipe))));

    public int CapacidadeEstoque => EstoqueCasa + EstoquePorEstante * Qtd(Movel.Estante);

    // Bônus nas propostas comerciais (fachada e recepção passam confiança)
    public float BonusImagem => (Qtd(Movel.Fachada) > 0 ? 0.04f : 0f) + (Qtd(Movel.Recepcao) > 0 ? 0.04f : 0f);

    public float ContaLuz => imovel == 0 ? 0f : LuzBase + LuzPorEstacao * Estacoes + (Qtd(Movel.ArCondicionado) > 0 ? LuzArCondicionado : 0f);

    public float CustoFixoMensal => Imovel.aluguel + (imovel == 0 ? 0f : Internet + ContaLuz + Imovel.aluguel * 0.15f);

    // ---------- Ações ----------

    public string Alugar(int indice)
    {
        if (indice == imovel) return "Você já está nesse imóvel.";
        var emp = EmpresaManager.Instance;
        if (indice > 0 && emp != null && !emp.TemCnpj) return "Para alugar uma sala comercial você precisa de CNPJ (abra o MEI).";
        var novo = Imoveis[Mathf.Clamp(indice, 0, Imoveis.Length - 1)];
        if (indice > 0 && novo.maxEstacoes < Estacoes) return "Seus móveis não cabem nesse imóvel.";

        float caucaoNova = novo.aluguel * CaucaoMeses;
        float mudanca = imovel == 0 && indice > 0 ? 0f : 600f;   // caminhão de mudança
        float devolvida = caucaoPaga;
        var eco = GameEconomy.Instance;
        float precisa = caucaoNova + mudanca - devolvida;
        if (eco != null && precisa > 0 && eco.saldo < precisa) return $"Precisa de {UIFactory.Dinheiro(precisa)} (caução de {CaucaoMeses} aluguéis + mudança).";
        if (precisa > 0) eco?.Gastar(precisa); else if (precisa < 0) eco?.ReceberPagamento(-precisa);
        caucaoPaga = caucaoNova;
        imovel = indice;
        return indice == 0
            ? "Você voltou a trabalhar de casa. A caução foi devolvida."
            : $"Contrato assinado: {novo.nome} por {UIFactory.Dinheiro(novo.aluguel)}/mês. Ela chegou vazia: compre os móveis!";
    }

    public string ComprarMovel(Movel m)
    {
        if (imovel == 0) return "Na sua casa não cabe mobília de escritório. Alugue uma sala primeiro.";
        var info = Moveis[(int)m];
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < info.preco) return $"Saldo insuficiente: {UIFactory.Dinheiro(info.preco)}.";
        eco?.Gastar(info.preco);
        while (moveis.Count <= (int)m) moveis.Add(0);
        moveis[(int)m]++;
        return $"{info.nome} comprado ({UIFactory.Dinheiro(info.preco)}). Chega em caixas na porta da sede.";
    }

    // ---------- Contas do mês ----------

    void NovoDia(int dia)
    {
        int mesAtual = EmpresaManager.MesDoDia(dia);
        while (ultimoMesCobrado < mesAtual - 1)
        {
            ultimoMesCobrado++;
            if (imovel == 0 || EmpresaManager.Instance == null) continue;
            var proximo = EmpresaManager.PrimeiroDiaDoMes(ultimoMesCobrado + 1);
            var emp = EmpresaManager.Instance;
            emp.AdicionarGuia("Aluguel da sede", Imovel.aluguel, EmpresaManager.DiaDaData(proximo.AddDays(4)), ultimoMesCobrado);
            emp.AdicionarGuia("Condomínio e IPTU", Imovel.aluguel * 0.15f, EmpresaManager.DiaDaData(proximo.AddDays(9)), ultimoMesCobrado);
            emp.AdicionarGuia("Conta de luz", ContaLuz, EmpresaManager.DiaDaData(proximo.AddDays(9)), ultimoMesCobrado);
            emp.AdicionarGuia("Internet da sede", Internet, EmpresaManager.DiaDaData(proximo.AddDays(9)), ultimoMesCobrado);
        }

        // Aluguel atrasado há mais de 60 dias: despejo
        var e2 = EmpresaManager.Instance;
        if (imovel > 0 && e2 != null && e2.guias.Exists(g => !g.paga && g.nome == "Aluguel da sede" && dia - g.vencimento > 60))
        {
            imovel = 0;
            caucaoPaga = 0f;   // a caução cobre a dívida
            e2.guias.RemoveAll(g => !g.paga && g.nome == "Aluguel da sede");
            GameUI.Instance?.Toast("<b>Despejo!</b> O aluguel atrasou mais de 2 meses. Você perdeu a sala e a caução e voltou a trabalhar de casa.", UIFactory.Perigo, 7f);
        }
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public int imovel;
        public float caucaoPaga;
        public List<int> moveis = new List<int>();
        public int ultimoMesCobrado = -1;
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados { imovel = imovel, caucaoPaga = caucaoPaga, moveis = new List<int>(moveis), ultimoMesCobrado = ultimoMesCobrado };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        imovel = d.imovel; caucaoPaga = d.caucaoPaga; moveis = d.moveis ?? new List<int>(); ultimoMesCobrado = d.ultimoMesCobrado;
    }

    public void NovoJogo()
    {
        imovel = 0; caucaoPaga = 0f; ultimoMesCobrado = -1;
        moveis = new List<int>();
        foreach (Movel _ in System.Enum.GetValues(typeof(Movel))) moveis.Add(0);
    }
}
