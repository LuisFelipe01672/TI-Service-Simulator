using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Parte da GameUI: aba "Contratos" do tablet (locação de impressoras):
// contatos comerciais com montagem de proposta, contratos ativos (instalar, visitar) e compra de equipamentos.
public partial class GameUI
{
    // Escolhas da proposta de cada contato: [equipamento, plano, prazo, fornecedor]
    readonly Dictionary<int, int[]> propostas = new Dictionary<int, int[]>();

    void MontarAbaContratos()
    {
        var cm = ContratosManager.Instance;
        if (cm == null) return;

        int ativos = 0, instalados = 0, estoque = 0;
        foreach (var c in cm.contratos) if (c.ativo) { ativos++; if (c.Instalado) instalados++; }
        foreach (var e in cm.patrimonio) if (e.contratoId < 0) estoque++;

        Secao("LOCAÇÃO DE IMPRESSORAS");
        var resumo = Cartao(UIFactory.CartaoClaro);
        UIFactory.Texto(resumo, $"Receita recorrente: <b>{UIFactory.Dinheiro(cm.Mrr)}/mês</b>   ·   Contratos: {ativos} ({instalados} instalados)   ·   No estoque: {estoque}", 21, UIFactory.CorTexto);
        UIFactory.Texto(resumo, "Cada contrato cobra um fixo por mês + páginas excedentes. O excedente só é cobrado se você ler o contador (visita) no mês.", 18, UIFactory.CorSuave);
        if (!cm.Liberado)
        {
            UIFactory.Texto(tabletConteudo, "<color=#F5B83F>Locação exige CNPJ.</color> Abra o MEI na aba Empresa para receber contatos de empresas.", 21, UIFactory.CorTexto);
            MontarCompras(cm);
            return;
        }

        // ---------- Contatos comerciais ----------
        Secao($"CONTATOS COMERCIAIS ({cm.leads.Count})");
        if (cm.leads.Count == 0) UIFactory.Texto(tabletConteudo, "Nenhum contato agora. Novos aparecem ao longo dos dias.", 20, UIFactory.CorSuave);
        foreach (var l in new List<Lead>(cm.leads)) MontarLead(cm, l);

        // ---------- Contratos ----------
        Secao("CONTRATOS");
        if (ativos == 0) UIFactory.Texto(tabletConteudo, "Nenhum contrato ainda.", 20, UIFactory.CorSuave);
        foreach (var c in cm.contratos)
        {
            if (!c.ativo) continue;
            var contrato = c;
            var e = cm.Equip(c.equipamentoId);
            var card = Cartao();
            string sat = c.satisfacao >= 60 ? "#3CCB7F" : c.satisfacao >= 35 ? "#F5B83F" : "#F2545B";
            UIFactory.Texto(card, $"<b>{c.empresa}</b>   {UIFactory.Dinheiro(c.mensal)}/mês   <size=18><color={sat}>satisfação {c.satisfacao:F0}</color></size>", 22, UIFactory.CorTexto);
            int diasRestantes = c.diaFim - (DiaManager.Instance != null ? DiaManager.Instance.dia : 1);
            UIFactory.Texto(card, $"{(e != null ? e.Tipo.nome : "?")}  ·  franquia {c.franquia} pág.  ·  {c.meses} meses (faltam {Mathf.Max(0, diasRestantes / 30)})", 18, UIFactory.CorSuave);
            if (!c.Instalado)
            {
                UIFactory.Texto(card, "<color=#F5B83F>Aguardando instalação.</color> O contrato só começa a render depois de instalado.", 18, UIFactory.CorTexto);
                UIFactory.Botao(LinhaBotoes(card), "Instalar (2 h)", () => { Toast(cm.Instalar(contrato), UIFactory.Destaque, 4f); AtualizarTablet(); }, UIFactory.Destaque, 220, 46, 20);
                continue;
            }
            int mes = EmpresaManager.MesDoDia(DiaManager.Instance != null ? DiaManager.Instance.dia : 1);
            string sup = c.suprimento <= 0f ? "<color=#F2545B>SEM TINTA</color>" : c.suprimento < 0.15f ? $"<color=#F5B83F>{c.suprimento * 100:F0}%</color>" : $"{c.suprimento * 100:F0}%";
            UIFactory.Texto(card, $"Tinta/toner: {sup}  ·  Páginas este mês: {c.paginasNoMes}  ·  Contador lido este mês: {(c.mesLeitura == mes ? "sim" : "<color=#F5B83F>não</color>")}" +
                (c.chamadoAberto ? "  ·  <color=#F2545B>chamado aberto</color>" : ""), 18, UIFactory.CorTexto);
            UIFactory.Botao(LinhaBotoes(card), "Visitar: ler contador e repor tinta (1 h 30)", () => { Toast(cm.Visitar(contrato), UIFactory.Destaque, 4f); AtualizarTablet(); }, UIFactory.CartaoClaro, 480, 46, 19);
        }

        MontarCompras(cm);
        MontarServicosNaAbaContratos();
    }

    void MontarLead(ContratosManager cm, Lead l)
    {
        if (!propostas.TryGetValue(l.id, out var esc))
        {
            esc = new[] { l.fracCor > 0.02f ? 1 : 4, 0, 2, 0 };
            propostas[l.id] = esc;
        }
        var tipos = CatalogoLocacao.Equipamentos;
        esc[0] = Mathf.Clamp(esc[0], 0, tipos.Length - 1);
        var t = tipos[esc[0]];
        esc[1] = Mathf.Clamp(esc[1], 0, t.planos.Length - 1);
        int meses = CatalogoLocacao.Prazos[Mathf.Clamp(esc[2], 0, 2)];
        var forn = CatalogoLocacao.Fornecedor(esc[3]);

        var card = Cartao(l.grafica ? UIFactory.Hex("#3A2F1F") : (Color?)null);
        UIFactory.Texto(card, $"<b>{l.empresa}</b>   <size=18><color=#9AA6B8>~{l.paginasMes} páginas/mês · {(l.fracCor > 0.02f ? $"{l.fracCor * 100:F0}% coloridas" : "só preto e branco")} · responde até {DiaManager.DataDoDia(l.expiraNoDia):dd/MM}</color></size>", 22, UIFactory.CorTexto);
        if (l.grafica) UIFactory.Texto(card, "<color=#F5B83F>Gráfica: volume altíssimo. Paga bem, mas acaba com qualquer equipamento.</color>", 18, UIFactory.CorTexto);

        var linha1 = LinhaBotoes(card);
        UIFactory.Botao(linha1, t.nome.Replace("Multifuncional ", ""), () => { esc[0] = (esc[0] + 1) % tipos.Length; esc[1] = 0; AtualizarTablet(); }, UIFactory.CartaoClaro, 400, 44, 17);
        UIFactory.Botao(linha1, $"Plano: {t.planos[esc[1]].Nome}", () => { esc[1] = (esc[1] + 1) % t.planos.Length; AtualizarTablet(); }, UIFactory.CartaoClaro, 250, 44, 17);
        var linha2 = LinhaBotoes(card);
        UIFactory.Botao(linha2, $"Prazo: {meses} meses (-{CatalogoLocacao.DescontoPrazo(meses) * 100:0.#}%)", () => { esc[2] = (esc[2] + 1) % 3; AtualizarTablet(); }, UIFactory.CartaoClaro, 300, 44, 17);
        UIFactory.Botao(linha2, $"Tinta: {forn.nome}", () => { esc[3] = (esc[3] + 1) % CatalogoLocacao.Fornecedores.Length; AtualizarTablet(); }, UIFactory.CartaoClaro, 220, 44, 17);

        string aviso = cm.AvaliarProposta(l, t.id, esc[1], meses, esc[3], out float conta, out float custo);
        int emEstoque = cm.EmEstoque(t.id).Count;
        float margem = conta - custo;
        UIFactory.Texto(card, $"Conta do cliente: <b>{UIFactory.Dinheiro(conta)}/mês</b>   ·   Seu custo de tinta: ~{UIFactory.Dinheiro(custo)}/mês   ·   " +
            $"Sobra: <color={(margem >= 0 ? "#3CCB7F" : "#F2545B")}>{UIFactory.Dinheiro(margem)}/mês</color>   ·   Em estoque: {emEstoque}", 18, UIFactory.CorTexto);
        UIFactory.Texto(card, $"Equipamento custa {UIFactory.Dinheiro(t.custoCompra)} novo; volta em ~{(margem > 0 ? Mathf.CeilToInt(t.custoCompra / margem) : 999)} meses.", 17, UIFactory.CorSuave);
        if (aviso != null) UIFactory.Texto(card, $"<color=#F5B83F>{aviso}</color>", 18, UIFactory.CorTexto);

        var linha3 = LinhaBotoes(card);
        var enviar = UIFactory.Botao(linha3, "Enviar proposta", () =>
        {
            string r = cm.EnviarProposta(l, t.id, esc[1], meses, esc[3]);
            propostas.Remove(l.id);
            Toast(r, r.StartsWith("Contrato fechado") ? UIFactory.Sucesso : UIFactory.Alerta, 5f);
            AtualizarTablet();
        }, UIFactory.Destaque, 240, 46, 20);
        enviar.interactable = emEstoque > 0;
        if (emEstoque == 0) UIFactory.Texto(linha3, "<color=#F5B83F>Compre esse equipamento antes (abaixo).</color>", 18, UIFactory.CorTexto);
    }

    void MontarCompras(ContratosManager cm)
    {
        Secao("COMPRAR EQUIPAMENTOS PARA LOCAÇÃO (chegam no dia seguinte)");
        foreach (var t in CatalogoLocacao.Equipamentos)
        {
            var tipo = t;
            var card = Cartao();
            int estoque = cm.EmEstoque(t.id).Count;
            int caminho = cm.patrimonio.FindAll(e => e.tipoId == t.id && e.contratoId < 0 && e.diaChegada > (DiaManager.Instance != null ? DiaManager.Instance.dia : 1)).Count;
            UIFactory.Texto(card, $"<b>{t.nome}</b>   <size=18><color=#9AA6B8>estoque: {estoque}{(caminho > 0 ? $" · a caminho: {caminho}" : "")}</color></size>", 21, UIFactory.CorTexto);
            UIFactory.Texto(card, $"Tinta por página: R$ {t.custoPaginaPB:0.00} P&B" + (t.colorida ? $" / R$ {t.custoPaginaCor:0.00} cor" : "") +
                $"  ·  aguenta ~{t.paginasRecomendadasMes} pág./mês", 17, UIFactory.CorSuave);
            var linha = LinhaBotoes(card);
            for (int i = 0; i < CatalogoLocacao.Condicoes.Length; i++)
            {
                int cond = i;
                var c = CatalogoLocacao.Condicoes[i];
                UIFactory.Botao(linha, $"{c.nome} {UIFactory.Dinheiro(ContratosManager.PrecoCompra(t.id, cond))}", () =>
                {
                    Toast(cm.ComprarEquipamento(tipo.id, cond), UIFactory.Destaque, 4f);
                    AtualizarTablet();
                }, cond == 0 ? UIFactory.Destaque : UIFactory.CartaoClaro, 270, 44, 17);
            }
        }
        UIFactory.Texto(tabletConteudo, "Recondicionado e leilão saem mais barato, mas quebram mais (e defeito vira chamado incluso no contrato).", 17, UIFactory.CorSuave);
    }
}
