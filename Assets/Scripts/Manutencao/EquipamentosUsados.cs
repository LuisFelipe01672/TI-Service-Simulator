using System.Collections.Generic;
using UnityEngine;

// Equipamentos usados (Fase 2 + Etapa 6): de tempos em tempos aparece um lote de empresa que fechou. Você compra no
// escuro e o lote chega em caixas no escritório. Cada equipamento é levado à BANCADA, onde o técnico testa (liga e vê
// a pista: tela, folha, luzes), diagnostica (placa POST, testador de fonte), troca a peça ruim, limpa, APAGA OS DADOS
// (LGPD), cola a etiqueta de patrimônio e decide o destino: anunciar à venda, colocar na frota de locação (impressora)
// ou levar a sucata ao ponto de descarte. Vender com dados do antigo dono pode dar multa.
// Aqui fica só a regra (testável no EditMode); os objetos 3D estão em UsadosFisicos.cs e as ações em AcoesBancada.cs.
public enum CondicaoUsado { Bom, DefeitoLeve, Sucata }
public enum TipoUsado { Computador, Monitor, Impressora, Roteador, Switch, Notebook }
public enum DefeitoUsado { Nenhum, Memoria, Fonte, Cilindro, Capacitor, FonteExterna, Bateria, Placa }
public enum LocalUsado { Caixa, Chao, Bancada, Mao }

[System.Serializable]
public class EquipUsado
{
    public string nome;
    public CondicaoUsado condicao;
    public bool testado, recuperado, dadosApagados;
    public bool temDados;        // computador, notebook e impressora com memória guardam dados do antigo dono
    public float valorBom;       // quanto vale funcionando
    public float custoConserto;  // peças para recuperar (quando a peça não vem da mochila)

    // Etapa 6: o equipamento é um objeto no escritório
    public int id;
    public TipoUsado tipo;
    public bool tipoDefinido;    // save antigo: o tipo é deduzido do nome ao carregar
    public DefeitoUsado defeito;
    public bool diagnosticado;   // já se sabe qual peça está ruim
    public bool limpo;           // limpeza feita (vale um pouco mais)
    public bool anunciado;       // à venda: o comprador retira no dia seguinte
    public bool pecasAproveitadas;
    public LocalUsado local;
    public int patrimonio;       // 0 = sem etiqueta
    public int vaga = -1;        // lugar no chão do escritório

    public bool PrecisaApagar => temDados && !dadosApagados;
    public string Etiqueta => patrimonio > 0 ? EquipamentosUsados.CodigoPatrimonio(patrimonio) : "";
}

public static class EquipamentosUsados
{
    [System.Serializable]
    public class Dados
    {
        public List<EquipUsado> estoque = new List<EquipUsado>();
        public int diaDoLote = -1, itensNoLote;
        public float precoDoLote;
        public string origem = "";
        public bool loteComprado;
        public int proximoId = 1, proximoPatrimonio = 1;
    }

    static Dados d = new Dados();
    public static List<EquipUsado> Estoque => d.estoque;

    public const float MinutosTeste = 20f, MinutosConserto = 45f, MinutosApagar = 15f, MinutosLimpar = 10f, MinutosDiagnosticoNoChute = 25f;
    public const float MultaLgpd = 300f, ChanceMulta = 0.35f, BonusLimpeza = 1.1f;

    // Avisa o visual (UsadosFisicos) que algo mudou num equipamento: "testar", "trocar", "limpar", "apagar"...
    public static System.Action<EquipUsado, string> AoMudar;
    static void Avisar(EquipUsado e, string o) => AoMudar?.Invoke(e, o);

    public static void Iniciar() => d = new Dados();
    public static Dados Exportar() => d;
    public static void Carregar(Dados dados)
    {
        d = dados ?? new Dados();
        if (d.proximoId < 1) d.proximoId = 1;
        if (d.proximoPatrimonio < 1) d.proximoPatrimonio = 1;
        foreach (var e in d.estoque) Completar(e);
    }

    // Save de antes da Etapa 6: deduz tipo e defeito, dá um id e põe o equipamento no chão (sem caixa)
    static void Completar(EquipUsado e)
    {
        if (e.id <= 0) e.id = d.proximoId++;
        if (!e.tipoDefinido)
        {
            e.tipo = TipoPeloNome(e.nome);
            e.tipoDefinido = true;
            e.local = LocalUsado.Chao;
            if (e.condicao == CondicaoUsado.DefeitoLeve && e.defeito == DefeitoUsado.Nenhum) e.defeito = DefeitoPadrao(e.tipo, 0);
            if (e.condicao == CondicaoUsado.Sucata) e.defeito = DefeitoUsado.Placa;
            e.diagnosticado = e.testado;
        }
        if (e.local == LocalUsado.Mao) e.local = LocalUsado.Chao;   // ninguém carrega nada ao carregar o jogo
    }

    public static TipoUsado TipoPeloNome(string nome)
    {
        string n = (nome ?? "").ToLowerInvariant();
        if (n.Contains("notebook")) return TipoUsado.Notebook;
        if (n.Contains("monitor")) return TipoUsado.Monitor;
        if (n.Contains("impressora")) return TipoUsado.Impressora;
        if (n.Contains("roteador")) return TipoUsado.Roteador;
        if (n.Contains("switch")) return TipoUsado.Switch;
        return TipoUsado.Computador;
    }

    static readonly string[] Origens = { "uma contabilidade que fechou", "uma escola que trocou os computadores", "uma loja que encerrou as atividades", "um escritório de advocacia que se mudou" };
    static readonly (TipoUsado tipo, string nome, float valor, bool dados)[] Modelos =
    {
        (TipoUsado.Computador, "Computador de mesa usado", 420f, true), (TipoUsado.Monitor, "Monitor 19\" usado", 160f, false),
        (TipoUsado.Impressora, "Impressora laser usada", 380f, true), (TipoUsado.Roteador, "Roteador usado", 90f, false),
        (TipoUsado.Switch, "Switch 8 portas usado", 110f, false), (TipoUsado.Notebook, "Notebook usado", 650f, true),
    };

    static int Dia => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    // Um lote novo a cada 4 dias (fica disponível por 2). Determinístico pelo dia.
    public static bool LoteDisponivel(out int itens, out float preco, out string origem)
    {
        int ciclo = (Dia - 1) / 4;
        int inicio = ciclo * 4 + 2;
        bool aberto = Dia >= inicio && Dia <= inicio + 1;
        if (aberto && d.diaDoLote != inicio) SortearLote(inicio);
        itens = d.itensNoLote; preco = d.precoDoLote; origem = d.origem;
        return aberto && !d.loteComprado;
    }

    static void SortearLote(int inicio)
    {
        var rnd = new System.Random(inicio * 7919);
        d.diaDoLote = inicio;
        d.itensNoLote = rnd.Next(3, 6);
        d.origem = Origens[rnd.Next(Origens.Length)];
        d.precoDoLote = Mathf.Round(d.itensNoLote * (float)(70 + rnd.NextDouble() * 50));
        d.loteComprado = false;
    }

    // Atalho de teste: põe um lote à venda hoje, fora do ciclo
    public static void ForcarLoteHoje()
    {
        SortearLote(Dia);
        d.diaDoLote = -2;   // não sorteia de novo pelo ciclo
    }

    public static string ComprarLote()
    {
        bool forcado = d.diaDoLote == -2 && !d.loteComprado;
        if (!forcado && !LoteDisponivel(out _, out _, out _)) return "Não há lote à venda hoje.";
        int itens = d.itensNoLote; float preco = d.precoDoLote; string origem = d.origem;
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < preco) return $"Saldo insuficiente: o lote custa {UIFactory.Dinheiro(preco)}.";
        eco?.Gastar(preco, LivroCaixa.Equipamentos);
        d.loteComprado = true;
        for (int i = 0; i < itens; i++)
        {
            var m = Modelos[Random.Range(0, Modelos.Length)];
            float r = Random.value;
            var cond = r < 0.35f ? CondicaoUsado.Bom : r < 0.75f ? CondicaoUsado.DefeitoLeve : CondicaoUsado.Sucata;
            d.estoque.Add(Novo(m.tipo, m.nome, m.valor, m.dados, cond, Random.value));
        }
        Avisar(null, "lote");
        return $"Lote comprado: {itens} equipamentos de {origem}. As caixas foram deixadas no escritório: leve um por vez à bancada.";
    }

    // Cria um usado (também usado pelos testes)
    public static EquipUsado Novo(TipoUsado tipo, string nome, float valor, bool dados, CondicaoUsado cond, float sorteio = 0f)
    {
        var e = new EquipUsado
        {
            id = d.proximoId++, tipo = tipo, tipoDefinido = true, nome = nome, temDados = dados, condicao = cond,
            valorBom = Mathf.Round(valor * Random.Range(0.85f, 1.15f)), custoConserto = Mathf.Round(valor * Random.Range(0.15f, 0.3f)),
            local = LocalUsado.Caixa,
            defeito = cond == CondicaoUsado.Sucata ? DefeitoUsado.Placa : cond == CondicaoUsado.DefeitoLeve ? DefeitoPadrao(tipo, sorteio) : DefeitoUsado.Nenhum,
        };
        return e;
    }

    public static EquipUsado Adicionar(EquipUsado e) { d.estoque.Add(e); Avisar(e, "lote"); return e; }

    static DefeitoUsado DefeitoPadrao(TipoUsado t, float sorteio)
    {
        switch (t)
        {
            case TipoUsado.Computador: return sorteio < 0.5f ? DefeitoUsado.Memoria : DefeitoUsado.Fonte;
            case TipoUsado.Notebook: return DefeitoUsado.Bateria;
            case TipoUsado.Impressora: return DefeitoUsado.Cilindro;
            case TipoUsado.Monitor: return DefeitoUsado.Capacitor;
            default: return DefeitoUsado.FonteExterna;
        }
    }

    // Peça da mochila que conserta o defeito (null = a peça é comprada no balcão de componentes, em dinheiro)
    public static Item? PecaDaMochila(DefeitoUsado df)
    {
        switch (df)
        {
            case DefeitoUsado.Memoria: return Item.Memoria;
            case DefeitoUsado.Fonte: return Item.Fonte;
            case DefeitoUsado.Cilindro: return Item.Cilindro;
            default: return null;
        }
    }

    public static string NomeDefeito(DefeitoUsado df)
    {
        switch (df)
        {
            case DefeitoUsado.Memoria: return "pente de memória com defeito";
            case DefeitoUsado.Fonte: return "fonte queimada";
            case DefeitoUsado.Cilindro: return "cilindro (unidade de imagem) riscado";
            case DefeitoUsado.Capacitor: return "capacitores estufados na placa da fonte";
            case DefeitoUsado.FonteExterna: return "fonte externa (12 V) queimada";
            case DefeitoUsado.Bateria: return "bateria estufada";
            case DefeitoUsado.Placa: return "placa principal em curto";
            default: return "nenhum";
        }
    }

    public static float CustoPeca(EquipUsado e)
    {
        var item = PecaDaMochila(e.defeito);
        return item != null ? Mochila.Info(item.Value).preco : e.custoConserto;
    }

    public static string CodigoPatrimonio(int n) => $"TIS-{n:0000}";

    static void Gastar(float minutos) { if (DiaManager.Instance != null) DiaManager.Instance.minutoAtual += minutos; }

    // ---------- Na bancada ----------

    public static string Testar(EquipUsado e)
    {
        Gastar(MinutosTeste);
        e.testado = true;
        // A pista já diz qual é a peça, menos no computador de mesa (sem vídeo pode ser muita coisa)
        e.diagnosticado = !(e.tipo == TipoUsado.Computador && e.condicao == CondicaoUsado.DefeitoLeve);
        Avisar(e, "testar");
        return PistaDoTeste(e);
    }

    public static string PistaDoTeste(EquipUsado e)
    {
        bool bom = e.condicao == CondicaoUsado.Bom, sucata = e.condicao == CondicaoUsado.Sucata;
        switch (e.tipo)
        {
            case TipoUsado.Computador:
                if (bom) return "Ligou e entrou no sistema antigo: na área de trabalho estão as planilhas e fotos do antigo dono.";
                if (sucata) return "Estalo, cheiro de queimado e fumaça saindo da traseira: placa-mãe em curto. É sucata.";
                return e.defeito == DefeitoUsado.Memoria
                    ? "Ventoinhas giram e o LED acende, mas o monitor fica sem sinal e o gabinete apita. Descubra a peça."
                    : "Nada: nem LED, nem ventoinha, nem apito. Descubra a peça.";
            case TipoUsado.Notebook:
                if (bom) return "Ligou e entrou no sistema: a pasta Documentos está cheia de arquivos do antigo dono.";
                if (sucata) return "Não liga nem na tomada e a placa está oxidada (pegou água). É sucata.";
                return "Liga só na tomada, e a carcaça está estufada embaixo do touchpad: bateria estufada. Precisa trocar.";
            case TipoUsado.Impressora:
                if (bom) return "Página de teste perfeita. Na memória ainda tem a agenda de digitalização do antigo dono.";
                if (sucata) return "Barulho de engrenagem quebrando e o visor mostra erro de motor. É sucata.";
                return "A página de teste saiu com uma listra preta vertical de cima a baixo: cilindro riscado.";
            case TipoUsado.Monitor:
                if (bom) return "Imagem limpa, sem mancha nem pixel morto.";
                if (sucata) return "Tela trincada por dentro: o painel não tem conserto. É sucata.";
                return "A imagem aparece e apaga em seguida, piscando. Pela grade dá para ver capacitores estufados na fonte.";
            default:
                if (bom) return "Todas as luzes acenderam e as portas funcionaram com o cabo de teste.";
                if (sucata) return "As luzes piscam todas juntas sem parar: placa danificada. É sucata.";
                return "Nenhuma luz acende. Com o multímetro, a fonte externa dá 0 V: fonte queimada.";
        }
    }

    // Placa POST: código 55 = memória; visor apagado = sem energia (fonte)
    public static string DiagnosticarPost(EquipUsado e)
    {
        Gastar(5f);
        e.diagnosticado = true;
        Avisar(e, "post");
        return e.defeito == DefeitoUsado.Memoria ? "A placa POST parou no código 55: falha de memória. O pente está ruim."
             : "O visor da placa POST nem acende: não chega energia na placa. A fonte está queimada.";
    }

    // Testador de fonte: só confirma (ou descarta) a fonte
    public static string DiagnosticarFonte(EquipUsado e)
    {
        Gastar(5f);
        Avisar(e, "fonte");
        if (e.defeito == DefeitoUsado.Fonte) { e.diagnosticado = true; return "Testador de fonte: nenhum LED acende e o Power Good não vem. Fonte queimada."; }
        return "Testador de fonte: todas as tensões dentro da norma e Power Good ok. A fonte está boa — o problema é outro.";
    }

    // Sem ferramenta: troca peça de bancada por peça de bancada até achar (demora)
    public static string DiagnosticarNoChute(EquipUsado e)
    {
        Gastar(MinutosDiagnosticoNoChute);
        e.diagnosticado = true;
        Avisar(e, "chute");
        return $"Sem placa POST, você foi trocando peças da bancada uma a uma até ligar: {NomeDefeito(e.defeito)}. (Com a ferramenta seria bem mais rápido.)";
    }

    public static string TrocarPeca(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.DefeitoLeve) return "Não há peça para trocar.";
        if (!e.diagnosticado) return "Primeiro descubra qual peça está ruim.";
        var item = PecaDaMochila(e.defeito);
        var m = Mochila.Instance;
        string velha = "";
        if (item != null)
        {
            if (m != null && !m.Usar(item.Value)) return $"Você não tem {Mochila.Info(item.Value).nome} na mochila.";
        }
        else
        {
            var eco = GameEconomy.Instance;
            if (eco != null && eco.saldo < e.custoConserto) return "Sem dinheiro para a peça.";
            eco?.Gastar(e.custoConserto, LivroCaixa.Consertos);
        }
        if (m != null && m.Cabe(Item.PecaVelha, 1)) { m.Adicionar(Item.PecaVelha, 1); velha = " A peça velha foi para a mochila (descarte no lixo eletrônico)."; }
        string feito = TextoTroca(e.defeito, item != null);
        Gastar(MinutosConserto);
        e.recuperado = true;
        e.condicao = CondicaoUsado.Bom;
        var antes = e.defeito;
        e.defeito = DefeitoUsado.Nenhum;
        Avisar(e, "trocar:" + antes);
        return feito + velha;
    }

    static string TextoTroca(DefeitoUsado df, bool daMochila)
    {
        switch (df)
        {
            case DefeitoUsado.Memoria: return "Travas abertas, pente ruim fora e o novo encaixado até o clique. Ligou e deu vídeo.";
            case DefeitoUsado.Fonte: return "Quatro parafusos, conectores da placa e dos discos: fonte nova no lugar. Ligou.";
            case DefeitoUsado.Cilindro: return "Cilindro riscado fora, unidade de imagem nova encaixada. Página de teste limpa.";
            case DefeitoUsado.Capacitor: return "Com o ferro de solda você trocou os capacitores estufados (comprados no balcão de componentes). Imagem firme.";
            case DefeitoUsado.FonteExterna: return "Fonte externa 12 V nova (comprada no balcão de componentes). Luzes acesas.";
            case DefeitoUsado.Bateria: return "Bateria estufada fora (direto para o descarte) e bateria nova comprada no balcão. Carcaça fechou reta.";
            default: return "Peça trocada.";
        }
    }

    // Atalho antigo: diagnostica (se precisar) e troca
    public static string Recuperar(EquipUsado e)
    {
        if (!e.diagnosticado) e.diagnosticado = true;
        return TrocarPeca(e);
    }

    public static string Limpar(EquipUsado e)
    {
        Gastar(MinutosLimpar);
        e.limpo = true;
        Avisar(e, "limpar");
        return "Pincel, ar comprimido nas grades e pano com álcool isopropílico na carcaça. Parece outro equipamento.";
    }

    public static string ApagarDados(EquipUsado e)
    {
        if (!e.temDados) return "Este equipamento não guarda dados.";
        if (e.condicao != CondicaoUsado.Bom) return "Ele precisa ligar para apagar os dados: conserte primeiro.";
        Gastar(MinutosApagar);
        e.dadosApagados = true;
        Avisar(e, "apagar");
        return e.tipo == TipoUsado.Impressora
            ? "Agenda, digitalizações e fila de impressão apagadas; padrão de fábrica restaurado."
            : "Apagamento seguro com 3 passagens no disco inteiro e sistema reinstalado limpo. Nada do antigo dono sobrou.";
    }

    public static string Etiquetar(EquipUsado e)
    {
        if (e.patrimonio > 0) return $"Já tem a etiqueta {e.Etiqueta}.";
        e.patrimonio = d.proximoPatrimonio++;
        Avisar(e, "etiqueta");
        return $"Etiqueta de patrimônio {e.Etiqueta} colada no equipamento.";
    }

    public static float ValorVenda(EquipUsado e) => e.condicao == CondicaoUsado.Bom ? Mathf.Round(e.valorBom * (e.limpo ? BonusLimpeza : 1f)) : 0f;

    public static string Anunciar(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.Bom) return "Só dá para anunciar o que está funcionando.";
        e.anunciado = true;
        Avisar(e, "anunciar");
        return $"Fotos tiradas e anúncio publicado por {UIFactory.Dinheiro(ValorVenda(e))}. O comprador vem buscar amanhã cedo." +
               (e.PrecisaApagar ? " <color=#F5B83F>Os dados do antigo dono ainda estão nele!</color>" : "");
    }

    public static string Vender(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.Bom) return "Só dá para vender o que está funcionando.";
        float valor = ValorVenda(e);
        d.estoque.Remove(e);
        GameEconomy.Instance?.ReceberPagamento(valor, LivroCaixa.OutrasReceitas);
        string msg = $"{e.nome} vendido por {UIFactory.Dinheiro(valor)}.";
        if (e.PrecisaApagar && Random.value < ChanceMulta)
        {
            GameEconomy.Instance?.Gastar(MultaLgpd, LivroCaixa.Multas);
            ReputationManager.Instance?.AlterarReputacao(-4f);
            msg += $" <color=#F2545B>O comprador achou arquivos do antigo dono: multa de {UIFactory.Dinheiro(MultaLgpd)} (LGPD) e -4 de reputação!</color>";
        }
        Avisar(e, "saiu");
        return msg;
    }

    // Novo dia: os compradores dos anúncios passam e retiram
    public static List<string> VenderAnunciados()
    {
        var msgs = new List<string>();
        foreach (var e in new List<EquipUsado>(d.estoque))
            if (e.anunciado && e.local != LocalUsado.Mao) msgs.Add(Vender(e));
        return msgs;
    }

    public static bool PodeIrParaFrota(EquipUsado e, out string motivo)
    {
        motivo = null;
        if (e.tipo != TipoUsado.Impressora) motivo = "Só impressora entra na frota de locação.";
        else if (e.condicao != CondicaoUsado.Bom) motivo = "Conserte antes.";
        else if (e.PrecisaApagar) motivo = "Apague os dados do antigo dono antes.";
        else if (e.patrimonio <= 0) motivo = "Cole a etiqueta de patrimônio antes.";
        else if (e.anunciado) motivo = "Ela está anunciada à venda.";
        return motivo == null;
    }

    public static string ColocarNaFrota(EquipUsado e)
    {
        if (!PodeIrParaFrota(e, out string motivo)) return motivo;
        var cm = ContratosManager.Instance;
        if (cm != null)
        {
            string erro = cm.ReceberImpressoraUsada(e.Etiqueta, e.limpo);
            if (erro != null) return erro;
        }
        d.estoque.Remove(e);
        Avisar(e, "frota");
        return $"Impressora {e.Etiqueta} entrou na frota de locação (recondicionada, na estante do estoque). Já pode ir para um contrato.";
    }

    public static string Descartar(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.Sucata) return "Isto ainda tem valor: conserte e venda em vez de jogar fora.";
        d.estoque.Remove(e);
        ReputationManager.Instance?.AlterarReputacao(0.25f);
        Avisar(e, "saiu");
        return $"{e.nome} no lixo eletrônico (vai para a recicladora certificada). Descarte correto.";
    }

    // Sucata de computador: o pente de memória ainda presta
    public static string AproveitarPecas(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.Sucata || e.tipo != TipoUsado.Computador || e.pecasAproveitadas) return "Não há nada para aproveitar.";
        var m = Mochila.Instance;
        if (m != null && !m.Cabe(Item.Memoria, 1)) return "A mochila está cheia.";
        e.pecasAproveitadas = true;
        m?.Adicionar(Item.Memoria, 1);
        Gastar(10f);
        Avisar(e, "aproveitar");
        return "Você tirou o pente de memória da placa queimada e testou no computador de bancada: está bom. Foi para a mochila.";
    }
}
