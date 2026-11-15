using System.Collections.Generic;
using UnityEngine;

// Equipamentos usados (Fase 2): de tempos em tempos aparece um lote de empresa que fechou. Você compra no
// escuro, testa cada peça na bancada, recupera as que têm conserto, APAGA OS DADOS (LGPD) e revende; o que
// não tem conserto vira sucata para o ponto de coleta. Vender com dados do antigo dono pode dar multa.
public enum CondicaoUsado { Bom, DefeitoLeve, Sucata }

[System.Serializable]
public class EquipUsado
{
    public string nome;
    public CondicaoUsado condicao;
    public bool testado, recuperado, dadosApagados;
    public bool temDados;        // computador e impressora com memória guardam dados do antigo dono
    public float valorBom;       // quanto vale funcionando
    public float custoConserto;  // peças para recuperar
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
    }

    static Dados d = new Dados();
    public static List<EquipUsado> Estoque => d.estoque;

    public const float MinutosTeste = 20f, MinutosConserto = 45f, MinutosApagar = 15f;
    public const float MultaLgpd = 300f, ChanceMulta = 0.35f;

    public static void Iniciar() => d = new Dados();
    public static Dados Exportar() => d;
    public static void Carregar(Dados dados) => d = dados ?? new Dados();

    static readonly string[] Origens = { "uma contabilidade que fechou", "uma escola que trocou os computadores", "uma loja que encerrou as atividades", "um escritório de advocacia que se mudou" };
    static readonly (string nome, float valor, bool dados)[] Modelos =
    {
        ("Computador de mesa usado", 420f, true), ("Monitor 19\" usado", 160f, false), ("Impressora laser usada", 380f, true),
        ("Roteador usado", 90f, false), ("Switch 8 portas usado", 110f, false), ("Notebook usado", 650f, true),
    };

    static int Dia => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    // Um lote novo a cada 4 dias (fica disponível por 2). Determinístico pelo dia.
    public static bool LoteDisponivel(out int itens, out float preco, out string origem)
    {
        int ciclo = (Dia - 1) / 4;
        int inicio = ciclo * 4 + 2;
        bool aberto = Dia >= inicio && Dia <= inicio + 1;
        if (aberto && d.diaDoLote != inicio)
        {
            var rnd = new System.Random(inicio * 7919);
            d.diaDoLote = inicio;
            d.itensNoLote = rnd.Next(3, 6);
            d.origem = Origens[rnd.Next(Origens.Length)];
            d.precoDoLote = Mathf.Round(d.itensNoLote * (float)(70 + rnd.NextDouble() * 50));
            d.loteComprado = false;
        }
        itens = d.itensNoLote; preco = d.precoDoLote; origem = d.origem;
        return aberto && !d.loteComprado;
    }

    public static string ComprarLote()
    {
        if (!LoteDisponivel(out int itens, out float preco, out string origem)) return "Não há lote à venda hoje.";
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < preco) return $"Saldo insuficiente: o lote custa {UIFactory.Dinheiro(preco)}.";
        eco?.Gastar(preco, LivroCaixa.Equipamentos);
        d.loteComprado = true;
        for (int i = 0; i < itens; i++)
        {
            var m = Modelos[Random.Range(0, Modelos.Length)];
            float r = Random.value;
            d.estoque.Add(new EquipUsado
            {
                nome = m.nome, temDados = m.dados, valorBom = Mathf.Round(m.valor * Random.Range(0.85f, 1.15f)),
                condicao = r < 0.35f ? CondicaoUsado.Bom : r < 0.75f ? CondicaoUsado.DefeitoLeve : CondicaoUsado.Sucata,
                custoConserto = Mathf.Round(m.valor * Random.Range(0.15f, 0.3f)),
            });
        }
        return $"Lote comprado: {itens} equipamentos de {origem}. Estão no escritório — teste um por um.";
    }

    static void Gastar(float minutos) { if (DiaManager.Instance != null) DiaManager.Instance.minutoAtual += minutos; }

    public static string Testar(EquipUsado e)
    {
        Gastar(MinutosTeste);
        e.testado = true;
        return e.condicao == CondicaoUsado.Bom ? $"{e.nome}: ligou e funcionou. Está bom!"
             : e.condicao == CondicaoUsado.DefeitoLeve ? $"{e.nome}: defeito com conserto (peças: {UIFactory.Dinheiro(e.custoConserto)})."
             : $"{e.nome}: placa queimada, não compensa. É sucata.";
    }

    public static string Recuperar(EquipUsado e)
    {
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < e.custoConserto) return "Sem dinheiro para as peças.";
        eco?.Gastar(e.custoConserto, LivroCaixa.Consertos);
        Gastar(MinutosConserto);
        e.recuperado = true;
        e.condicao = CondicaoUsado.Bom;
        return $"{e.nome} recuperado: limpeza, peça trocada e teste. Funcionando.";
    }

    public static string ApagarDados(EquipUsado e)
    {
        Gastar(MinutosApagar);
        e.dadosApagados = true;
        return $"{e.nome}: dados do antigo dono apagados de verdade (formatação segura).";
    }

    public static float ValorVenda(EquipUsado e) => e.condicao == CondicaoUsado.Bom ? e.valorBom : 0f;

    public static string Vender(EquipUsado e)
    {
        if (e.condicao != CondicaoUsado.Bom) return "Só dá para vender o que está funcionando.";
        d.estoque.Remove(e);
        GameEconomy.Instance?.ReceberPagamento(e.valorBom, LivroCaixa.OutrasReceitas);
        string msg = $"{e.nome} vendido por {UIFactory.Dinheiro(e.valorBom)}.";
        if (e.temDados && !e.dadosApagados && Random.value < ChanceMulta)
        {
            GameEconomy.Instance?.Gastar(MultaLgpd, LivroCaixa.Multas);
            ReputationManager.Instance?.AlterarReputacao(-4f);
            msg += $" <color=#F2545B>O comprador achou arquivos do antigo dono: multa de {UIFactory.Dinheiro(MultaLgpd)} (LGPD) e -4 de reputação!</color>";
        }
        return msg;
    }

    public static string Descartar(EquipUsado e)
    {
        d.estoque.Remove(e);
        ReputationManager.Instance?.AlterarReputacao(0.25f);
        return $"{e.nome} levado ao ponto de coleta de lixo eletrônico (descarte correto).";
    }
}
