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
    CaixaCabo,
    Cilindro,
    TonerUsado,
    Memoria,
    Fonte,
    ArComprimido,
    PastaTermica,
    SSD,
    // Etapa 4: ferramentas de diagnóstico (no fim do enum: o save guarda as quantidades pelo índice)
    PlacaPOST,
    TestadorFonte,
    Multimetro,
    PendriveDiagnostico,
    BateriaCMOS,
    // Etapa 6: peça velha retirada num conserto (vai para o lixo eletrônico)
    PecaVelha,
    // Etapa 7: peças de notebook
    BateriaNotebook,
    CarregadorNotebook,
    DobradicaNotebook,
    // Etapa 8: rede física (organizar rack, projeto de cabeamento)
    Etiquetadora,
    PunchDown
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
    public bool naPorta;     // já chegou: está na porta de casa esperando o jogador abrir a caixa
    public int fornecedor = Mercado.FornecedorPadrao;
    public bool defeito, errado;   // sorteados na compra; o jogador descobre ao abrir a caixa
    public int diaPrevisto = -1;   // atrasou: o fornecedor avisa quando passar do dia previsto
    public bool reposicao;         // é a troca de um RMA (não sorteia defeito de novo)
}

// Mochila do técnico: começa só com o notebook. O resto se compra pelo tablet.
// Ferramentas ficam para sempre; consumíveis (conectores, cabos, toner) acabam.
public partial class Mochila : MonoBehaviour
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
        new InfoItem(Item.PunchDown, "Ferramenta de inserção (punch-down)", "Encaixa os 8 fios do cabo nos contatos da tomada e do patch panel e corta a sobra. Para cabeamento estruturado.", 75f, 1, true),
        new InfoItem(Item.Etiquetadora, "Etiquetadora eletrônica", "Imprime etiquetas para porta de patch panel, cordão e tomada (P-01, P-02...). Rede sem etiqueta é rede que ninguém entende.", 160f, 1, true),
        new InfoItem(Item.Toner, "Cartucho de toner", "Para impressora a laser. O cliente reembolsa a peça usada.", 180f, 1, false),
        new InfoItem(Item.PendriveDrivers, "Pendrive com drivers", "Instala drivers de rede e o sistema mesmo sem internet.", 40f, 1, true),
        // Ferramentas de diagnóstico (Etapa 4): cada uma revela uma parte da saúde das peças
        new InfoItem(Item.Multimetro, "Multímetro digital", "Mede tensão (tomada, fonte, bateria da placa-mãe) e continuidade (fusível, cabo).", 85f, 1, true),
        new InfoItem(Item.TestadorFonte, "Testador de fonte ATX", "Liga no conector de 24 pinos: LEDs e visor mostram cada tensão da fonte e o sinal Power Good.", 70f, 1, true),
        new InfoItem(Item.PlacaPOST, "Placa POST (diagnóstico de placa-mãe)", "Vai num slot da placa-mãe e mostra no visor o código do passo em que o computador parou.", 60f, 1, true),
        new InfoItem(Item.PendriveDiagnostico, "Pendrive de diagnóstico", "Dá boot fora do sistema: teste de memória, saúde do disco (S.M.A.R.T.) e sensores de temperatura.", 55f, 1, true),
        new InfoItem(Item.BateriaCMOS, "Bateria da placa-mãe CR2032 (5 un.)", "Bateria de 3 V que guarda a data e a configuração da placa-mãe.", 20f, 5, false),
        new InfoItem(Item.ArComprimido, "Pincel e ar comprimido", "Para tirar a poeira de dentro do gabinete e do cooler sem estragar nada.", 45f, 1, true),
        new InfoItem(Item.PastaTermica, "Pasta térmica (seringa, 5 aplicações)", "Vai entre o processador e o cooler. Pasta velha e seca = computador que esquenta mesmo limpo.", 25f, 5, false),
        new InfoItem(Item.SSD, "SSD 480 GB", "Para trocar HD com setores ruins. Faça o backup antes!", 220f, 1, false),
        new InfoItem(Item.Memoria, "Pente de memória 8 GB", "Para trocar memória com defeito (computador que liga, bipa e não dá vídeo).", 160f, 1, false),
        new InfoItem(Item.Fonte, "Fonte ATX 500 W", "Para trocar fonte queimada (computador que não dá nenhum sinal de vida).", 190f, 1, false),
        new InfoItem(Item.BateriaNotebook, "Bateria de notebook", "Bateria interna compatível. Bateria estufada sai na hora (e vai para o descarte, nunca para o lixo comum).", 210f, 1, false),
        new InfoItem(Item.CarregadorNotebook, "Carregador universal de notebook 90 W", "Com pontas trocáveis e saída de 19 V. Para carregador com cabo rompido ou fonte queimada.", 95f, 1, false),
        new InfoItem(Item.DobradicaNotebook, "Par de dobradiças de notebook", "Dobradiças da tela com os parafusos. A velha costuma quebrar a bucha da carcaça junto.", 65f, 1, false),
        new InfoItem(Item.Cilindro, "Cilindro (unidade de imagem)", "Peça da impressora a laser que dura milhares de páginas. Riscado = listra vertical na folha.", 150f, 1, false),
    };

    public static InfoItem Info(Item item)
    {
        foreach (var i in Catalogo) if (i.item == item) return i;
        if (item == Item.TonerUsado) return new InfoItem(Item.TonerUsado, "Toner usado (para descarte)", "Cartucho vazio retirado de um cliente: vai para a logística reversa.", 0f, 1, false);
        if (item == Item.PecaVelha) return new InfoItem(Item.PecaVelha, "Peça velha (para descarte)", "Peça retirada num conserto: memória, fonte, cilindro, HD, bateria. Vai para o lixo eletrônico do escritório.", 0f, 1, false);
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

    void Update()
    {
        VerificarEntregas();
        EncomendaFisica.Sincronizar(entregas);   // caixas na porta (inclusive as de um jogo carregado)
    }

    // ---------- Itens ----------

    public void Inicial()
    {
        quantidades.Clear();
        foreach (Item _ in System.Enum.GetValues(typeof(Item))) quantidades.Add(0);
        quantidades[(int)Item.Notebook] = 1;
        entregas.Clear();
        cabos.Clear();
        estoque.Clear();
        cabosEstoque.Clear();
        GarantirEstoque();
    }

    public int Quantidade(Item item) => (int)item < quantidades.Count ? quantidades[(int)item] : 0;
    public bool Tem(Item item) => Quantidade(item) > 0 && !EsquecidaLonge;

    // Mochila física (Fase 1A): fica em casa até o jogador pegar (MochilaFisica). Sem ela, no cliente,
    // as ferramentas e peças não estão com você. Cenas sem a mochila física: sempre com você.
    public bool comVoce = true;
    public bool EsquecidaLonge => !comVoce && NoCliente();

    static bool NoCliente()
    {
        var sala = GameObject.Find("Cenario/Cliente");
        var jogador = GameObject.FindWithTag("Player");
        if (sala == null || jogador == null) return false;
        Vector3 d = jogador.transform.position - sala.transform.position;
        return Mathf.Abs(d.x) < 7f && Mathf.Abs(d.z) < 6f;
    }

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
        if (EsquecidaLonge) return null;   // os cabos estão na mochila, que ficou em casa
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
    public string Comprar(Item item, bool expressa) => Comprar(item, expressa, Mercado.FornecedorPadrao);

    // Compra num fornecedor: preço do dia, prazo dele; atraso, defeito e pedido errado são sorteados agora
    // (o jogador só descobre quando a entrega atrasa ou quando abre a caixa)
    public string Comprar(Item item, bool expressa, int fornecedor)
    {
        var info = Info(item);
        var forn = Mercado.Fornecedores[Mathf.Clamp(fornecedor, 0, Mercado.Fornecedores.Length - 1)];
        if (info.ferramenta && (Tem(item) || ACaminho(item))) return $"Você já tem {info.nome}.";
        if (expressa && !forn.expressa) return $"{forn.nome} não faz entrega expressa.";

        float total = Mercado.Preco(info, fornecedor) + (expressa ? TaxaExpressa : 0f);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < total) return $"Saldo insuficiente: {info.nome} custa {UIFactory.Dinheiro(total)}.";
        eco?.Gastar(total, LivroCaixa.Ferramentas);

        var d = DiaManager.Instance;
        var entrega = new Entrega { item = item, quantidade = info.quantidadePorCompra, fornecedor = fornecedor };
        if (d == null)
        {
            Adicionar(item, info.quantidadePorCompra);
            return $"Compra feita: {info.nome}!";
        }
        entrega.defeito = Random.value < forn.chanceDefeito;
        entrega.errado = !entrega.defeito && Random.value < forn.chanceErrado;
        if (expressa)
        {
            entrega.dia = d.dia;
            entrega.minuto = d.minutoAtual + MinutosEntregaExpressa;
        }
        else
        {
            entrega.dia = d.dia + forn.prazoDias;
            entrega.minuto = d.horaInicio * 60f + Random.Range(0f, 240f);   // chega durante a manhã
            if (Random.value < forn.chanceAtraso) { entrega.diaPrevisto = entrega.dia; entrega.dia += 1; }
        }
        entregas.Add(entrega);
        string quando = expressa ? $"chega em {MinutosEntregaExpressa:F0} minutos (motoboy)"
                      : forn.prazoDias == 1 ? "chega amanhã de manhã" : $"chega em {forn.prazoDias} dias";
        return $"Pedido feito em {forn.nome}: {info.nome} ({UIFactory.Dinheiro(total)}), {quando}.";
    }

    // Ao abrir a caixa: pedido errado ou peça com defeito aciona a garantia (troca sem custo, mas demora)
    public string ProblemaNaEntrega(Entrega e)
    {
        if (e == null || (!e.defeito && !e.errado)) return null;
        var forn = Mercado.Fornecedores[Mathf.Clamp(e.fornecedor, 0, Mercado.Fornecedores.Length - 1)];
        var d = DiaManager.Instance;
        int dias = forn.prazoDias + 1;
        entregas.Add(new Entrega
        {
            item = e.item, quantidade = e.quantidade, fornecedor = e.fornecedor, reposicao = true,
            dia = (d != null ? d.dia : 0) + dias, minuto = (d != null ? d.horaInicio : 8) * 60f + Random.Range(0f, 240f),
        });
        string nome = Info(e.item).nome;
        return e.errado
            ? $"Veio o produto errado no lugar de <b>{nome}</b>! Troca aberta com {forn.nome}: chega em {dias} dias."
            : $"<b>{nome}</b> veio com defeito! Garantia (RMA) aberta com {forn.nome}: reposição em {dias} dias.";
    }

    void VerificarEntregas()
    {
        if (entregas.Count == 0) return;
        var d = DiaManager.Instance;
        for (int i = entregas.Count - 1; i >= 0; i--)
        {
            var e = entregas[i];
            if (e.naPorta) continue;   // já está na porta esperando
            // Atrasou: passou da hora prevista e o fornecedor avisa (uma vez)
            if (e.diaPrevisto >= 0 && d != null && (d.dia > e.diaPrevisto || (d.dia == e.diaPrevisto && d.minutoAtual >= e.minuto)))
            {
                e.diaPrevisto = -1;
                var fa = Mercado.Fornecedores[Mathf.Clamp(e.fornecedor, 0, Mercado.Fornecedores.Length - 1)];
                GameUI.Instance?.Toast($"{fa.nome}: a entrega de <b>{Info(e.item).nome}</b> atrasou e chega amanhã. Desculpe o transtorno.", UIFactory.Alerta, 5f);
            }
            bool chegou = d == null || d.dia > e.dia || (d.dia == e.dia && d.minutoAtual >= e.minuto);
            if (!chegou) continue;
            // Com casa: a encomenda fica na porta como caixa, até o jogador ir buscar (EncomendaFisica)
            if (EncomendaFisica.TemPontoDeEntrega)
            {
                e.naPorta = true;
                SomManager.Tocar("bip", 0.5f, 0.8f);
                GameUI.Instance?.Toast($"Interfone: entregaram <b>{Info(e.item).nome}</b> na porta de casa. Vá buscar.", UIFactory.Destaque, 4f);
                continue;
            }
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
        public List<int> estoque = new List<int>();
        public List<CaboPronto> cabosEstoque = new List<CaboPronto>();
    }

    // Preenchido pelo SaveManager se ele carregar antes da mochila existir
    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados
    {
        quantidades = new List<int>(quantidades),
        entregas = new List<Entrega>(entregas),
        cabos = new List<CaboPronto>(cabos),
        estoque = new List<int>(estoque),
        cabosEstoque = new List<CaboPronto>(cabosEstoque)
    };

    public void Carregar(Dados d)
    {
        Inicial();
        if (d == null) return;
        for (int i = 0; i < d.quantidades.Count && i < quantidades.Count; i++) quantidades[i] = d.quantidades[i];
        quantidades[(int)Item.Notebook] = 1;
        entregas = d.entregas ?? new List<Entrega>();
        cabos = d.cabos ?? new List<CaboPronto>();
        if (d.estoque != null) for (int i = 0; i < d.estoque.Count && i < estoque.Count; i++) estoque[i] = d.estoque[i];
        cabosEstoque = d.cabosEstoque ?? new List<CaboPronto>();
        PassarExcessoParaEstante();
    }
}
