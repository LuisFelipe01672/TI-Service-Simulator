using TMPro;
using UnityEngine;

// Parte da GameUI: aba "Empresa" do tablet (regime, faturamento, notas/recibos, impostos e abertura de CNPJ).
public partial class GameUI
{
    void MontarAbaEmpresa()
    {
        var emp = EmpresaManager.Instance;
        if (emp == null) return;
        int hoje = DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
        int mes = EmpresaManager.MesDoDia(hoje);

        // ---------- Situação ----------
        Secao("SUA SITUAÇÃO");
        var c = Cartao(UIFactory.CartaoClaro);
        UIFactory.Texto(c, $"<b>{emp.NomeRegime}</b>" + (emp.Irregular ? "   <color=#F2545B>IRREGULAR (impostos atrasados)</color>" : ""), 24, UIFactory.CorTexto);
        UIFactory.Texto(c, $"Faturamento este mês: {UIFactory.Dinheiro(emp.FaturamentoNoMes(mes))}   ·   No ano: {UIFactory.Dinheiro(emp.FaturamentoAno)}" +
            (emp.regime == Regime.MEI ? $"  <color=#9AA6B8>(limite MEI {UIFactory.Dinheiro(EmpresaManager.LimiteMei)})</color>" : ""), 19, UIFactory.CorSuave);
        UIFactory.Texto(c, $"A receber: {UIFactory.Dinheiro(emp.AReceber)}   ·   Notas emitidas: {emp.notasEmitidas}   ·   Funcionários permitidos: " +
            (emp.LimiteFuncionarios == 0 ? "nenhum (precisa de CNPJ)" : emp.LimiteFuncionarios == 1 ? "1" : "sem limite"), 19, UIFactory.CorSuave);
        UIFactory.Texto(c, emp.regime == Regime.Autonomo
            ? "Como autônomo você só pega clientes que aceitam recibo. Paga INSS (R$ 178,31) e carnê-leão (isento até R$ 5 mil/mês)."
            : emp.regime == Regime.MEI
                ? "MEI: DAS fixo de R$ 86,05 todo dia 20, limite de R$ 81 mil por ano, 1 funcionário e nota fiscal para empresas."
                : "Microempresa no Simples Nacional (Anexo III): imposto a partir de 6% do faturamento, contador e funcionários à vontade.",
            18, UIFactory.CorSuave);

        // ---------- Abrir CNPJ ----------
        if (emp.regime != Regime.ME)
        {
            Secao("ABRIR CNPJ");
            if (emp.regime == Regime.Autonomo)
            {
                var mei = Cartao();
                UIFactory.Texto(mei, "<b>MEI</b>   grátis, na hora (Portal do Empreendedor)", 22, UIFactory.CorTexto);
                UIFactory.Texto(mei, "Pega clientes que exigem nota fiscal, contrata 1 funcionário. DAS fixo R$ 86,05/mês. Limite R$ 81 mil/ano.", 18, UIFactory.CorSuave);
                UIFactory.Botao(LinhaBotoes(mei), "Abrir MEI", () => { Toast(emp.AbrirMei(), UIFactory.Sucesso, 5f); AtualizarTablet(); }, UIFactory.Sucesso, 220, 46, 20);
            }
            var me = Cartao();
            UIFactory.Texto(me, $"<b>Microempresa (ME)</b>   {UIFactory.Dinheiro(EmpresaManager.CustoAberturaMe)}, fica pronta em {EmpresaManager.DiasAberturaMe} dias", 22, UIFactory.CorTexto);
            UIFactory.Texto(me, $"Sem limite de funcionários, até R$ 4,8 milhões/ano. Imposto de 6% no começo + contador ({UIFactory.Dinheiro(EmpresaManager.HonorariosContador)}/mês).", 18, UIFactory.CorSuave);
            if (emp.meProntaNoDia > 0)
                UIFactory.Texto(me, $"<color=#F5B83F>Abertura em andamento: pronta em {DiaManager.DataDoDia(emp.meProntaNoDia):dd/MM}.</color>", 19, UIFactory.CorTexto);
            else
                UIFactory.Botao(LinhaBotoes(me), "Abrir Microempresa", () => { Toast(emp.AbrirMe(), UIFactory.Destaque, 5f); AtualizarTablet(); }, UIFactory.Destaque, 280, 46, 20);
        }

        // ---------- Serviços sem documento ----------
        Secao(emp.TemCnpj ? "NOTAS FISCAIS A EMITIR" : "RECIBOS");
        int pendentes = 0;
        foreach (var r in emp.recebiveis)
        {
            if (r.documentoEmitido) continue;
            pendentes++;
            var rec = r;
            var card = Cartao(r.exigeNota ? UIFactory.Hex("#3A2F1F") : (Color?)null);
            UIFactory.Texto(card, $"<b>{r.cliente}</b>   {UIFactory.Dinheiro(r.valor)}   <size=18><color=#9AA6B8>{DiaManager.DataDoDia(r.diaServico):dd/MM} · {r.forma}" +
                $"{(r.pago ? " · pago" : "")}</color></size>", 22, UIFactory.CorTexto);
            UIFactory.Texto(card, r.exigeNota ? "<color=#F5B83F>Empresa: só paga depois da nota fiscal.</color>" : "Cliente aceita recibo (opcional, mas passa confiança).", 18, UIFactory.CorSuave);
            UIFactory.Botao(LinhaBotoes(card), emp.TemCnpj ? "Emitir NFS-e" : "Emitir recibo", () =>
            {
                Toast(emp.EmitirDocumento(rec), UIFactory.Sucesso, 3f);
                AtualizarTablet();
            }, UIFactory.Destaque, 240, 46, 20);
        }
        if (pendentes == 0) UIFactory.Texto(tabletConteudo, "Nada pendente.", 20, UIFactory.CorSuave);

        // ---------- Impostos ----------
        Secao("IMPOSTOS E CONTAS");
        int abertas = 0;
        foreach (var g in emp.guias)
        {
            if (g.paga) continue;
            abertas++;
            var guia = g;
            float valor = emp.ValorAtual(g);
            bool atrasada = hoje > g.vencimento;
            var card = Cartao(atrasada ? UIFactory.Hex("#3A1F22") : (Color?)null);
            UIFactory.Texto(card, $"<b>{g.nome}</b>  <size=18><color=#9AA6B8>competência {g.competencia}</color></size>   {UIFactory.Dinheiro(valor)}", 22, UIFactory.CorTexto);
            UIFactory.Texto(card, atrasada
                ? $"<color=#F2545B>Venceu em {DiaManager.DataDoDia(g.vencimento):dd/MM}: multa de 0,33% ao dia (até 20%) + juros.</color>"
                : $"Vence em {DiaManager.DataDoDia(g.vencimento):dd/MM}.", 18, UIFactory.CorSuave);
            UIFactory.Botao(LinhaBotoes(card), "Pagar", () => { Toast(emp.PagarGuia(guia), UIFactory.Sucesso, 3f); AtualizarTablet(); }, UIFactory.Sucesso, 160, 46, 20);
        }
        if (abertas == 0) UIFactory.Texto(tabletConteudo, "Nenhuma guia em aberto. As guias chegam quando o mês vira.", 20, UIFactory.CorSuave);

        MontarSede();
        MontarConcorrencia();
    }
}
