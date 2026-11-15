using UnityEngine;

// Fornecedores (Fase 2) — nomes fictícios. Cada um tem preço, prazo e confiabilidade diferentes.
public class Fornecedor
{
    public string nome, perfil;
    public float multPreco;       // sobre o preço de mercado do dia
    public int prazoDias;         // entrega normal
    public bool expressa;         // tem motoboy no mesmo dia
    public float chanceAtraso, chanceDefeito, chanceErrado;

    public Fornecedor(string nome, string perfil, float multPreco, int prazoDias, bool expressa,
                      float atraso, float defeito, float errado)
    {
        this.nome = nome; this.perfil = perfil; this.multPreco = multPreco; this.prazoDias = prazoDias;
        this.expressa = expressa; chanceAtraso = atraso; chanceDefeito = defeito; chanceErrado = errado;
    }
}

public static class Mercado
{
    public static readonly Fornecedor[] Fornecedores =
    {
        new Fornecedor("Distribuidora Centro TI", "Atacado: preço bom, entrega em 2 dias, às vezes atrasa.", 0.88f, 2, false, 0.15f, 0.02f, 0.03f),
        new Fornecedor("Loja do Bairro Informática", "Mais cara, mas entrega amanhã e tem motoboy no mesmo dia.", 1.08f, 1, true, 0.05f, 0.02f, 0.01f),
        new Fornecedor("MegaShop Online", "Marketplace: o mais barato, 3 dias, atrasa muito e às vezes erra o pedido.", 0.76f, 3, false, 0.30f, 0.07f, 0.08f),
    };

    public const int FornecedorPadrao = 1;   // Loja do Bairro (o comportamento antigo: amanhã ou expressa)

    // Índice de preço do dia para um item (0,85 a 1,25): oscila de forma suave e determinística pelo dia,
    // então não precisa salvar. Simula dólar, falta de componente e promoções.
    public static float IndiceDoDia(Item item, int dia)
    {
        float n = Mathf.PerlinNoise(dia * 0.31f + 3.7f, (int)item * 7.13f + 0.5f);
        return Mathf.Lerp(0.85f, 1.25f, n);
    }

    public static int DiaAtual => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;

    public static float Preco(InfoItem info, int fornecedor)
    {
        var f = Fornecedores[Mathf.Clamp(fornecedor, 0, Fornecedores.Length - 1)];
        return Mathf.Round(info.preco * IndiceDoDia(info.item, DiaAtual) * f.multPreco);
    }

    // Texto da variação do dia em relação ao preço de tabela (ex.: "+8% hoje")
    public static string VariacaoTexto(Item item)
    {
        int p = Mathf.RoundToInt((IndiceDoDia(item, DiaAtual) - 1f) * 100f);
        if (p == 0) return "<color=#9AA6B8>preço normal hoje</color>";
        return p > 0 ? $"<color=#F2545B>+{p}% hoje</color>" : $"<color=#3CCB7F>{p}% hoje</color>";
    }
}
