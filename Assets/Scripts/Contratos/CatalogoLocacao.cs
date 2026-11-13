using UnityEngine;

// Equipamentos que a empresa compra para alugar, com custo e custo por página.
// Nomes fictícios; custos de compra e por página vêm de preços de mercado de 2026 (aproximados);
// estrutura dos planos (franquia + excedente, desconto por prazo) segue tabelas reais de outsourcing de impressão.
public class TipoEquipamentoLocacao
{
    public string id, nome;
    public float custoCompra;
    public float custoPaginaPB, custoPaginaCor;   // suprimento original
    public bool colorida;
    public int paginasRecomendadasMes;            // acima disso desgasta mais rápido
    public int rendimentoRefil;                   // páginas por reposição de tinta/toner
    public PlanoLocacao[] planos;

    public TipoEquipamentoLocacao(string id, string nome, float custo, float cppPB, float cppCor, bool colorida, int recomendadas, int rendimento, params PlanoLocacao[] planos)
    {
        this.id = id; this.nome = nome; custoCompra = custo; custoPaginaPB = cppPB; custoPaginaCor = cppCor;
        this.colorida = colorida; paginasRecomendadasMes = recomendadas; rendimentoRefil = rendimento; this.planos = planos;
    }
}

public class PlanoLocacao
{
    public int franquia;          // páginas incluídas por mês (0 = sem franquia)
    public float mensal;          // valor fixo por mês (contrato de 12 meses, sem desconto)
    public float excedentePB, excedenteCor;

    public PlanoLocacao(int franquia, float mensal, float excPB, float excCor)
    {
        this.franquia = franquia; this.mensal = mensal; excedentePB = excPB; excedenteCor = excCor;
    }

    public string Nome => franquia == 0 ? "Sem franquia" : $"{franquia:N0} páginas".Replace(",", ".");
}

// Fornecedor de suprimentos (tinta/toner): mais barato = mais defeito
public class FornecedorSuprimento
{
    public string nome;
    public float multCusto;      // sobre o custo por página original
    public float multDefeito;    // chance de defeito no equipamento
    public string descricao;
    public FornecedorSuprimento(string nome, float custo, float defeito, string descricao)
    { this.nome = nome; multCusto = custo; multDefeito = defeito; this.descricao = descricao; }
}

// Estado de compra do equipamento
public class CondicaoEquipamento
{
    public string nome;
    public float multCusto, multDefeito;
    public string descricao;
    public CondicaoEquipamento(string nome, float custo, float defeito, string descricao)
    { this.nome = nome; multCusto = custo; multDefeito = defeito; this.descricao = descricao; }
}

public static class CatalogoLocacao
{
    public const float Implantacao = 150f;
    public const float Frete = 70f;
    public const float MarkupPecas = 1.7f;    // peças vendidas ao cliente: custo × 1,7

    // Desconto do mensal pelo prazo do contrato
    public static readonly int[] Prazos = { 12, 24, 36 };
    public static float DescontoPrazo(int meses) => meses >= 36 ? 0.125f : meses >= 24 ? 0.08f : meses >= 12 ? 0.04f : 0f;

    public static readonly TipoEquipamentoLocacao[] Equipamentos =
    {
        new TipoEquipamentoLocacao("tanque-compacta", "Multifuncional tanque de tinta compacta", 1100f, 0.02f, 0.05f, true, 800, 4500,
            new PlanoLocacao(500, 180f, 0.10f, 0.25f)),
        new TipoEquipamentoLocacao("tanque-media", "Multifuncional tanque de tinta média", 2500f, 0.02f, 0.05f, true, 1500, 6000,
            new PlanoLocacao(2000, 290f, 0.10f, 0.25f)),
        new TipoEquipamentoLocacao("jato-empresarial", "Multifuncional jato de tinta empresarial", 4200f, 0.04f, 0.30f, true, 5000, 10000,
            new PlanoLocacao(5000, 380f, 0.15f, 0.30f)),
        new TipoEquipamentoLocacao("laser-cor", "Multifuncional laser colorida", 3300f, 0.09f, 0.54f, true, 2500, 7500,
            new PlanoLocacao(1000, 500f, 0.15f, 0.60f)),
        new TipoEquipamentoLocacao("laser-pb", "Multifuncional laser preto e branco", 2000f, 0.03f, 0f, false, 4000, 10000,
            new PlanoLocacao(0, 130f, 0.10f, 0f),
            new PlanoLocacao(1000, 180f, 0.09f, 0f),
            new PlanoLocacao(2000, 220f, 0.08f, 0f),
            new PlanoLocacao(4000, 300f, 0.06f, 0f),
            new PlanoLocacao(10000, 480f, 0.05f, 0f)),
    };

    public static readonly FornecedorSuprimento[] Fornecedores =
    {
        new FornecedorSuprimento("Original", 1.0f, 1.0f, "Preço cheio, sem dor de cabeça."),
        new FornecedorSuprimento("Paralelo", 0.8f, 1.6f, "20% mais barato; às vezes dá problema."),
        new FornecedorSuprimento("Compatível", 0.6f, 3.0f, "40% mais barato; mancha, entope e o cliente reclama."),
    };

    public static readonly CondicaoEquipamento[] Condicoes =
    {
        new CondicaoEquipamento("Novo", 1.0f, 1.0f, "Garantia de 1 ano."),
        new CondicaoEquipamento("Recondicionado", 0.65f, 2.0f, "35% mais barato, sem garantia."),
        new CondicaoEquipamento("Leilão", 0.45f, 3.5f, "Bem barato, mas é uma loteria."),
    };

    public static TipoEquipamentoLocacao Tipo(string id)
    {
        foreach (var t in Equipamentos) if (t.id == id) return t;
        return Equipamentos[0];
    }

    public static FornecedorSuprimento Fornecedor(int i) => Fornecedores[Mathf.Clamp(i, 0, Fornecedores.Length - 1)];
    public static CondicaoEquipamento Condicao(int i) => Condicoes[Mathf.Clamp(i, 0, Condicoes.Length - 1)];

    // Mensal com o desconto do prazo
    public static float MensalComDesconto(PlanoLocacao p, int meses) => Mathf.Round(p.mensal * (1f - DescontoPrazo(meses)) * 100f) / 100f;

    // Quanto um cliente com esse perfil pagaria por mês nesse plano (fixo + excedente esperado)
    public static float ContaEsperada(PlanoLocacao p, int meses, int paginasMes, float fracCor)
    {
        int excedente = Mathf.Max(0, paginasMes - p.franquia);
        float exc = excedente * ((1f - fracCor) * p.excedentePB + fracCor * p.excedenteCor);
        return MensalComDesconto(p, meses) + exc;
    }

    // Custo de suprimento por página para um perfil de impressão
    public static float CustoPorPagina(TipoEquipamentoLocacao t, float fracCor, FornecedorSuprimento f)
    {
        float cor = t.colorida ? fracCor : 0f;
        return ((1f - cor) * t.custoPaginaPB + cor * t.custoPaginaCor) * f.multCusto;
    }

    static readonly string[] nomesEmpresas =
    {
        "Imobiliária Horizonte", "Escola Pequeno Saber", "Clínica Vida Plena", "Advocacia Moraes", "Construtora Pilar",
        "Contabilidade Exata", "Ótica Visão Clara", "Pet Shop Amigo Fiel", "Autopeças Rota 10", "Laboratório Bioanálise",
        "Agência Pixel", "Farmácia Bem Estar", "Despachante Rápido", "Loja de Móveis Lar", "Gráfica Expressa"
    };

    public static string SortearEmpresa() => nomesEmpresas[Random.Range(0, nomesEmpresas.Length)];

}
