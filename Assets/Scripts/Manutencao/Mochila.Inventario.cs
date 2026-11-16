using System.Collections.Generic;
using UnityEngine;

// Inventário com limite (peso): a mochila carrega até CapacidadeKg. O que não cabe fica no ESTOQUE do
// escritório (a estante de aço). Na estante dá para passar itens de um lado para o outro.
public partial class Mochila
{
    public const float CapacidadeKg = 10f;
    public const float KgPorMetroDeCabo = 0.045f;

    // Estoque do escritório (índice = (int)Item) e os cabos prontos guardados lá
    public List<int> estoque = new List<int>();
    public List<CaboPronto> cabosEstoque = new List<CaboPronto>();

    // Peso de UMA unidade, em kg (valores de mercado, arredondados)
    public static float Peso(Item item)
    {
        switch (item)
        {
            case Item.Notebook: return 2.0f;
            case Item.TestadorCabo: return 0.3f;
            case Item.AlicateCrimpagem: return 0.4f;
            case Item.ConectorRJ45: return 0.002f;
            case Item.Clipe: return 0.001f;
            case Item.Toner: return 0.9f;
            case Item.PendriveDrivers: return 0.02f;
            case Item.Cilindro: return 0.8f;
            case Item.TonerUsado: return 0.7f;
            case Item.Memoria: return 0.03f;
            case Item.Fonte: return 1.4f;
            case Item.ArComprimido: return 0.4f;
            case Item.PastaTermica: return 0.004f;
            case Item.SSD: return 0.06f;
            case Item.PlacaPOST: return 0.05f;
            case Item.TestadorFonte: return 0.25f;
            case Item.Multimetro: return 0.35f;
            case Item.PendriveDiagnostico: return 0.02f;
            case Item.BateriaCMOS: return 0.003f;
            case Item.PecaVelha: return 0.4f;
            case Item.BateriaNotebook: return 0.3f;
            case Item.CarregadorNotebook: return 0.35f;
            case Item.DobradicaNotebook: return 0.08f;
            default: return 0f;   // caixa de cabo (305 m) fica na bancada, não na mochila
        }
    }

    public static float PesoCabo(CaboPronto c) => 0.03f + c.metros * KgPorMetroDeCabo;

    public float PesoAtual
    {
        get
        {
            float kg = 0f;
            for (int i = 0; i < quantidades.Count; i++) kg += quantidades[i] * Peso((Item)i);
            foreach (var c in cabos) kg += PesoCabo(c);
            return kg;
        }
    }

    public float KgLivres => Mathf.Max(0f, CapacidadeKg - PesoAtual);
    public bool Cabe(float kg) => PesoAtual + kg <= CapacidadeKg + 0.0005f;
    public bool Cabe(Item item, int qtd) => Cabe(Peso(item) * qtd);

    public int NoEstoque(Item item) => (int)item < estoque.Count ? estoque[(int)item] : 0;

    void GarantirEstoque()
    {
        int n = System.Enum.GetValues(typeof(Item)).Length;
        while (estoque.Count < n) estoque.Add(0);
        while (quantidades.Count < n) quantidades.Add(0);
    }

    // Mochila → estante
    public bool Guardar(Item item, int qtd)
    {
        GarantirEstoque();
        if (item == Item.Notebook || item == Item.CaixaCabo) return false;
        qtd = Mathf.Min(qtd, Quantidade(item));
        if (qtd <= 0) return false;
        quantidades[(int)item] -= qtd;
        estoque[(int)item] += qtd;
        return true;
    }

    // Estante → mochila (só o que couber)
    public bool Pegar(Item item, int qtd)
    {
        GarantirEstoque();
        qtd = Mathf.Min(qtd, NoEstoque(item));
        if (Info(item).ferramenta && Quantidade(item) > 0) return false;   // já tem uma na mochila
        if (qtd <= 0 || !Cabe(item, qtd)) return false;
        estoque[(int)item] -= qtd;
        quantidades[(int)item] += qtd;
        return true;
    }

    public void GuardarCabo(CaboPronto c) { if (cabos.Remove(c)) cabosEstoque.Add(c); }

    public bool PegarCaboDoEstoque(CaboPronto c)
    {
        if (!Cabe(PesoCabo(c)) || !cabosEstoque.Remove(c)) return false;
        cabos.Add(c);
        return true;
    }

    // Cabo novo feito na bancada: vai para a mochila se couber; senão fica na estante. Devolve true se foi para a mochila.
    public bool ReceberCabo(CaboPronto c)
    {
        if (Cabe(PesoCabo(c))) { cabos.Add(c); return true; }
        cabosEstoque.Add(c);
        return false;
    }

    // Save antigo (de antes do limite) com mais peso do que cabe: o excesso vai para a estante do escritório,
    // começando pelos consumíveis mais pesados; ferramentas e notebook ficam na mochila
    public void PassarExcessoParaEstante()
    {
        GarantirEstoque();
        var ordem = new List<Item>((Item[])System.Enum.GetValues(typeof(Item)));
        ordem.Sort((a, b) => Peso(b).CompareTo(Peso(a)));
        foreach (var item in ordem)
        {
            if (Info(item).ferramenta || item == Item.Notebook || item == Item.CaixaCabo) continue;
            while (PesoAtual > CapacidadeKg && Quantidade(item) > 0) Guardar(item, 1);
        }
        while (PesoAtual > CapacidadeKg && cabos.Count > 0) GuardarCabo(cabos[cabos.Count - 1]);
    }

    // Tira do estoque (descarte, por exemplo)
    public int TirarDoEstoque(Item item)
    {
        GarantirEstoque();
        int n = estoque[(int)item];
        estoque[(int)item] = 0;
        return n;
    }
}
