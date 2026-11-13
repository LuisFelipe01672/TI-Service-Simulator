using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Item
{
    Notebook,
    TestadorCabo,
    AlicateCrimpagem,
    ConectorRJ45,
    CaboRede,
    Clipe,
    Toner,
    PendriveDrivers,
    CaixaCabo
}

// Um cabo de rede já feito, guardado na mochila
[System.Serializable]
public class CaboPronto
{
    public float metros;
    public bool pontaABoa = true, pontaBBoa = true;
    public bool Bom => pontaABoa && pontaBBoa;
    public CaboPronto() {}
    public CaboPronto(float metros, bool a = true, bool b = true) { this.metros = metros; pontaABoa = a; pontaBBoa = b; }
}

// Um item à venda na loja do tablet
public class InfoItem
{
    public Item item;
    public string nome;
    public string descricao;
    public float preco;
    public int quantidadePorCompra;
    public bool ferramenta;   // ferramenta: compra uma vez e fica. Consumível: gasta ao usar.

    public InfoItem(Item item, string nome, string descricao, float preco, int quantidade, bool ferramenta)
    {
        this.item = item; this.nome = nome; this.descricao = descricao;
        this.preco = preco; quantidadePorCompra = quantidade; this.ferramenta = ferramenta;
    }
}

// Uma compra a caminho
[System.Serializable]
public class Entrega
{
    public Item item;
    public int quantidade;
    public int dia;          // dia em que chega
    public float minuto;     // minuto do dia em que chega
}

// Mochila do técnico: começa só com o notebook. O resto se compra pelo tablet.
// Ferramentas ficam para sempre; consumíveis (conectores, cabos, toner) acabam.
public class Mochila : MonoBehaviour
{
    public static Mochila Instance;

    public const float TaxaExpressa = 40f;
    public const float MinutosEntregaExpressa = 30f;

    public static readonly InfoItem[] Catalogo =
    {
        new InfoItem(Item.TestadorCabo, "Testador de cabo de rede", "Mostra quais dos 8 fios passam sinal. Descobre cabo rompido ou mal crimpado.", 120f, 1, true),
        new InfoItem(Item.AlicateCrimpagem, "Alicate de crimpagem", "Corta, descasca e prende o conector RJ45 no cabo.", 90f, 1, true),
        new InfoItem(Item.ConectorRJ45, "Conectores RJ45 (10 un.)", "Para refazer a ponta de um cabo. Errou a ordem dos fios? Gasta outro.", 20f, 10, false),
        new InfoItem(Item.CaboRede, "Cabo de rede pronto 3 m", "Já vem crimpado. Prático, mas só tem 3 m.", 25f, 1, false),
        new InfoItem(Item.CaixaCabo, "Caixa de cabo de rede Cat6 (305 m)", "Fica na sede. Na bancada você corta do tamanho certo e crimpa as pontas.", 450f, 305, false),
        new InfoItem(Item.Clipe, "Ferramenta de reset (clipe)", "Para apertar o botão de reset escondido do roteador.", 5f, 1, true),
        new InfoItem(Item.Toner, "Cartucho de toner", "Para impressora a laser. O cliente reembolsa a peça usada.", 180f, 1, false),
        new InfoItem(Item.PendriveDrivers, "Pendrive com drivers", "Instala drivers de rede mesmo sem internet.", 40f, 1, true),
    };

    public static InfoItem Info(Item item)
    {
        foreach (var i in Catalogo) if (i.item == item) return i;
        return new InfoItem(Item.Notebook, "Notebook", "Seu notebook com o sistema de chamados.", 0f, 1, true);
    }

    // Quantidades (índice = (int)Item). Salvo pelo SaveManager.
    public List<int> quantidades = new List<int>();
    public List<Entrega> entregas = new List<Entrega>();
    public List<CaboPronto> cabos = new List<CaboPronto>();   // cabos prontos na mochila

    public int MetrosNaCaixa => Quantidade(Item.CaixaCabo);

    EventBinding<DiaIniciadoEvent> bDia;

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
        new GameObject("Mochila").AddComponent<Mochila>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
        else Inicial();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(_ => VerificarEntregas());
        EventBus<DiaIniciadoEvent>.Register(bDia);
    }

    void OnDisable() => EventBus<DiaIniciadoEvent>.Deregister(bDia);

    void Update() => VerificarEntregas();

    // ---------- Itens ----------

    public void Inicial()
    {
        quantidades.Clear();
        foreach (Item _ in System.Enum.GetValues(typeof(Item))) quantidades.Add(0);
        quantidades[(int)Item.Notebook] = 1;
        entregas.Clear();
        cabos.Clear();
    }

    public int Quantidade(Item item) => (int)item < quantidades.Count ? quantidades[(int)item] : 0;
    public bool Tem(Item item) => Quantidade(item) > 0;

    public void Adicionar(Item item, int qtd)
    {
        // Cabo pronto vai para a lista de cabos (cada um tem seu tamanho)
        if (item == Item.CaboRede) { for (int i = 0; i < qtd; i++) cabos.Add(new CaboPronto(3f)); return; }
        while (quantidades.Count <= (int)item) quantidades.Add(0);
        quantidades[(int)item] += qtd;
        if (Info(item).ferramenta) quantidades[(int)item] = Mathf.Min(quantidades[(int)item], 1);
    }

    // Gasta um consumível. Ferramentas não se gastam.
    public bool Usar(Item item)
    {
        if (!Tem(item)) return false;
        if (!Info(item).ferramenta) quantidades[(int)item]--;
        return true;
    }

    public bool ACaminho(Item item) => entregas.Exists(e => e.item == item);

    // Escolhe o menor cabo que alcança a distância (e tira da mochila). Null se nenhum alcança.
    public CaboPronto PegarCabo(float metrosNecessarios)
    {
        CaboPronto melhor = null;
        foreach (var c in cabos)
            if (c.metros >= metrosNecessarios && (melhor == null || c.metros < melhor.metros)) melhor = c;
        if (melhor != null) cabos.Remove(melhor);
        return melhor;
    }

    public string ListaCabos()
    {
        if (cabos.Count == 0) return "nenhum";
        var partes = new List<string>();
        foreach (var c in cabos) partes.Add($"{c.metros:0.#} m");
        return string.Join(", ", partes);
    }

    // Puxa metros da caixa da sede (para a bancada)
    public bool TirarDaCaixa(float metros)
    {
        int m = Mathf.CeilToInt(metros);
        if (MetrosNaCaixa < m) return false;
        quantidades[(int)Item.CaixaCabo] -= m;
        return true;
    }

    // ---------- Loja ----------

    // Devolve uma mensagem explicando o que aconteceu
    public string Comprar(Item item, bool expressa)
    {
        var info = Info(item);
        if (info.ferramenta && (Tem(item) || ACaminho(item))) return $"Você já tem {info.nome}.";

        float total = info.preco + (expressa ? TaxaExpressa : 0f);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < total) return $"Saldo insuficiente: {info.nome} custa {UIFactory.Dinheiro(total)}.";
        eco?.Gastar(total, LivroCaixa.Ferramentas);

        var d = DiaManager.Instance;
        var entrega = new Entrega { item = item, quantidade = info.quantidadePorCompra };
        if (d == null)
        {
            Adicionar(item, info.quantidadePorCompra);
            return $"Compra feita: {info.nome}!";
        }
        if (expressa)
        {
            entrega.dia = d.dia;
            entrega.minuto = d.minutoAtual + MinutosEntregaExpressa;
        }
        else
        {
            entrega.dia = d.dia + 1;
            entrega.minuto = d.horaInicio * 60f;
        }
        entregas.Add(entrega);
        return expressa
            ? $"{info.nome} a caminho por motoboy: chega em {MinutosEntregaExpressa:F0} minutos."
            : $"Compra feita: {info.nome}. Chega amanhã às {d.horaInicio}h.";
    }

    void VerificarEntregas()
    {
        if (entregas.Count == 0) return;
        var d = DiaManager.Instance;
        for (int i = entregas.Count - 1; i >= 0; i--)
        {
            var e = entregas[i];
            bool chegou = d == null || d.dia > e.dia || (d.dia == e.dia && d.minutoAtual >= e.minuto);
            if (!chegou) continue;
            entregas.RemoveAt(i);
            Adicionar(e.item, e.quantidade);
            GameUI.Instance?.Toast($"Entrega chegou: <b>{Info(e.item).nome}</b> já está na sua mochila.", UIFactory.Sucesso, 3f);
        }
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public List<int> quantidades = new List<int>();
        public List<Entrega> entregas = new List<Entrega>();
        public List<CaboPronto> cabos = new List<CaboPronto>();
    }

    // Preenchido pelo SaveManager se ele carregar antes da mochila existir
    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados
    {
        quantidades = new List<int>(quantidades),
        entregas = new List<Entrega>(entregas),
        cabos = new List<CaboPronto>(cabos)
    };

    public void Carregar(Dados d)
    {
        Inicial();
        if (d == null) return;
        for (int i = 0; i < d.quantidades.Count && i < quantidades.Count; i++) quantidades[i] = d.quantidades[i];
        quantidades[(int)Item.Notebook] = 1;
        entregas = d.entregas ?? new List<Entrega>();
        cabos = d.cabos ?? new List<CaboPronto>();
    }
}
