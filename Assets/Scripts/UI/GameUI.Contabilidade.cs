using TMPro;
using UnityEngine;

// Parte da GameUI (aba Empresa): contabilidade — resultado do mês (DRE simplificado) e para onde foi o dinheiro.
public partial class GameUI
{
    int mesContabilVisto = -1;

    void MontarContabilidade()
    {
        int hoje = DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
        int mesAtual = EmpresaManager.MesDoDia(hoje);
        if (mesContabilVisto < 0 || mesContabilVisto > mesAtual) mesContabilVisto = mesAtual;
        int mes = mesContabilVisto;

        Secao("CONTABILIDADE");
        var c = Cartao(UIFactory.CartaoClaro);
        var nav = LinhaBotoes(c);
        UIFactory.Botao(nav, "<", () => { mesContabilVisto = Mathf.Max(0, mesContabilVisto - 1); AtualizarTablet(); }, UIFactory.CartaoClaro, 56, 40, 20);
        UIFactory.Texto(nav, $"<b>{EmpresaManager.PrimeiroDiaDoMes(mes):MM/yyyy}</b>{(mes == mesAtual ? "  <size=16><color=#9AA6B8>(mês em andamento)</color></size>" : "")}", 20, UIFactory.CorTexto);
        if (mes < mesAtual) UIFactory.Botao(nav, ">", () => { mesContabilVisto++; AtualizarTablet(); }, UIFactory.CartaoClaro, 56, 40, 20);

        float receita = LivroCaixa.Total(mes, GrupoContabil.Receita);
        float impostos = LivroCaixa.Total(mes, GrupoContabil.Impostos);
        float custos = LivroCaixa.Total(mes, GrupoContabil.Custos);
        float despesas = LivroCaixa.Total(mes, GrupoContabil.Despesas);
        float resultado = LivroCaixa.Resultado(mes);
        float invest = LivroCaixa.Total(mes, GrupoContabil.Investimentos);
        float finan = LivroCaixa.Total(mes, GrupoContabil.Financeiro) + LivroCaixa.Total(mes, GrupoContabil.Devolucoes);

        LinhaDre(c, "Receita recebida", receita, true);
        LinhaDre(c, "(-) Impostos", impostos, false);
        LinhaDre(c, "(-) Custos dos serviços (tinta, licenças, material, peças)", custos, false);
        LinhaDre(c, "(-) Despesas (folha, sede, contador, taxas)", despesas, false);
        string margem = receita > 0.01f ? $"  <size=16><color=#9AA6B8>margem {resultado / receita * 100f:0}%</color></size>" : "";
        UIFactory.Texto(c, $"<b>= Resultado operacional: <color={(resultado >= 0 ? "#3CCB7F" : "#F2545B")}>{UIFactory.Dinheiro(resultado)}</color></b>{margem}", 21, UIFactory.CorTexto);
        if (Mathf.Abs(invest) > 0.01f) LinhaDre(c, "Investimentos (equipamentos, móveis, software, cauções)", invest, false);
        if (Mathf.Abs(finan) > 0.01f) LinhaDre(c, "Empréstimos e devoluções (líquido)", finan, true);

        // Detalhe por categoria
        var lista = LivroCaixa.DoMes(mes);
        lista.Sort((a, b) => Mathf.Abs(b.valor).CompareTo(Mathf.Abs(a.valor)));
        if (lista.Count > 0)
        {
            var det = new System.Text.StringBuilder("<size=16><color=#9AA6B8>");
            foreach (var l in lista) det.Append($"{l.categoria}: {(l.valor >= 0 ? "+" : "")}{UIFactory.Dinheiro(l.valor)}   ");
            det.Append("</color></size>");
            UIFactory.Texto(c, det.ToString(), 16, UIFactory.CorSuave);
        }
        else UIFactory.Texto(c, "Nenhum movimento neste mês.", 17, UIFactory.CorSuave);
    }

    void LinhaDre(RectTransform pai, string rotulo, float valor, bool entrada)
    {
        string cor = Mathf.Abs(valor) < 0.01f ? "#9AA6B8" : valor >= 0 ? "#3CCB7F" : "#F2545B";
        UIFactory.Texto(pai, $"{rotulo}: <color={cor}>{UIFactory.Dinheiro(valor)}</color>", 18, UIFactory.CorTexto);
    }
}
