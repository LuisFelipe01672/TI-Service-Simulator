using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LancamentoMes
{
    public int mes;
    public string categoria;
    public float valor;   // positivo = entrou, negativo = saiu
}

public enum GrupoContabil { Receita, Impostos, Custos, Despesas, Investimentos, Financeiro, Devolucoes }

// Livro-caixa: soma cada entrada e saída do mês por categoria, para o relatório de resultado (DRE simplificado).
public static class LivroCaixa
{
    public const string Servicos = "Serviços prestados", Impostos = "Impostos",
        Suprimentos = "Tinta e toner", Licencas = "Licenças e fornecedores", Material = "Material de projetos",
        Ferramentas = "Ferramentas e peças", Consertos = "Consertos",
        Folha = "Folha de pagamento", Sede = "Sede (aluguel e contas)", Contador = "Contador", Marketing = "Marketing",
        Taxas = "Taxas e adesões", Multas = "Multas",
        Equipamentos = "Equipamentos", Moveis = "Móveis", Desenvolvimento = "Desenvolvimento de software", Garantias = "Caução e garantias",
        EmprestimoRecebido = "Empréstimo recebido", Parcelas = "Parcelas de empréstimo", Devolucao = "Devoluções (caução e garantia)",
        OutrasReceitas = "Outras receitas", OutrasDespesas = "Outras despesas";

    static readonly Dictionary<string, GrupoContabil> grupos = new Dictionary<string, GrupoContabil>
    {
        { Servicos, GrupoContabil.Receita }, { OutrasReceitas, GrupoContabil.Receita },
        { Impostos, GrupoContabil.Impostos },
        { Suprimentos, GrupoContabil.Custos }, { Licencas, GrupoContabil.Custos }, { Material, GrupoContabil.Custos },
        { Ferramentas, GrupoContabil.Custos }, { Consertos, GrupoContabil.Custos },
        { Folha, GrupoContabil.Despesas }, { Sede, GrupoContabil.Despesas }, { Contador, GrupoContabil.Despesas },
        { Marketing, GrupoContabil.Despesas }, { Taxas, GrupoContabil.Despesas }, { Multas, GrupoContabil.Despesas }, { OutrasDespesas, GrupoContabil.Despesas },
        { Equipamentos, GrupoContabil.Investimentos }, { Moveis, GrupoContabil.Investimentos },
        { Desenvolvimento, GrupoContabil.Investimentos }, { Garantias, GrupoContabil.Investimentos },
        { EmprestimoRecebido, GrupoContabil.Financeiro }, { Parcelas, GrupoContabil.Financeiro },
        { Devolucao, GrupoContabil.Devolucoes },
    };

    public static List<LancamentoMes> lancamentos = new List<LancamentoMes>();

    public static GrupoContabil Grupo(string categoria) => grupos.TryGetValue(categoria, out var g) ? g : GrupoContabil.Despesas;

    static int MesAtual => EmpresaManager.MesDoDia(DiaManager.Instance != null ? DiaManager.Instance.dia : 1);

    public static void Registrar(float valor, string categoria)
    {
        if (Mathf.Abs(valor) < 0.005f) return;
        if (lancamentos == null) lancamentos = new List<LancamentoMes>();
        if (string.IsNullOrEmpty(categoria)) categoria = valor > 0 ? OutrasReceitas : OutrasDespesas;
        int mes = MesAtual;
        var l = lancamentos.Find(x => x.mes == mes && x.categoria == categoria);
        if (l == null) { l = new LancamentoMes { mes = mes, categoria = categoria }; lancamentos.Add(l); }
        l.valor += valor;
        // Guarda só os últimos 24 meses
        lancamentos.RemoveAll(x => x.mes < mes - 24);
    }

    // Categoria de uma guia (imposto, folha, contas da sede...) pelo nome
    public static string CategoriaDaGuia(string nome)
    {
        if (nome.StartsWith("DAS") || nome.StartsWith("INSS autônomo") || nome.StartsWith("Carnê")) return Impostos;
        if (nome.StartsWith("Folha") || nome.StartsWith("FGTS") || nome.StartsWith("INSS patronal") || nome.StartsWith("13º")) return Folha;
        if (nome.StartsWith("Aluguel") || nome.StartsWith("Condomínio") || nome.StartsWith("Conta de luz") || nome.StartsWith("Internet")) return Sede;
        if (nome.StartsWith("Honorários")) return Contador;
        if (nome.StartsWith("Parcela do empréstimo")) return Parcelas;
        if (nome.StartsWith("Equipe de desenvolvimento")) return Desenvolvimento;
        if (nome.StartsWith("Manutenção e servidores")) return Licencas;
        if (nome.StartsWith("Multa")) return Multas;
        return OutrasDespesas;
    }

    public static float Total(int mes, GrupoContabil g)
    {
        float t = 0f;
        foreach (var l in lancamentos) if (l.mes == mes && Grupo(l.categoria) == g) t += l.valor;
        return t;
    }

    public static List<LancamentoMes> DoMes(int mes) => lancamentos.FindAll(l => l.mes == mes);

    // Resultado operacional: receitas - impostos - custos - despesas (sem investimentos e empréstimos)
    public static float Resultado(int mes) =>
        Total(mes, GrupoContabil.Receita) + Total(mes, GrupoContabil.Impostos) + Total(mes, GrupoContabil.Custos) + Total(mes, GrupoContabil.Despesas);

    [System.Serializable]
    public class Dados { public List<LancamentoMes> lancamentos = new List<LancamentoMes>(); }
    public static Dados Exportar() => new Dados { lancamentos = new List<LancamentoMes>(lancamentos ?? new List<LancamentoMes>()) };
    public static void Carregar(Dados d) => lancamentos = d?.lancamentos ?? new List<LancamentoMes>();
    public static void Iniciar() => lancamentos = new List<LancamentoMes>();
}
