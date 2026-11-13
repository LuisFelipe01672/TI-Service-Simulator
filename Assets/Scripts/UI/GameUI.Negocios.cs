using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Parte da GameUI: aba "Negócios" do tablet (projetos de infraestrutura e parcerias com fornecedores).
public partial class GameUI
{
    readonly Dictionary<int, int> margemProjeto = new Dictionary<int, int>();

    void MontarAbaNegocios()
    {
        MontarProjetos();
        MontarParcerias();
    }

    void MontarProjetos()
    {
        var pm = ProjetosManager.Instance;
        if (pm == null) return;
        Secao($"PROJETOS DE INFRAESTRUTURA · {pm.concluidos} entregue(s)");
        UIFactory.Texto(tabletConteudo, $"Orçamento = material × margem + horas × {UIFactory.Dinheiro(ProjetosManager.ValorHoraProjeto)}. " +
            "Cliente paga 50% na aprovação e 50% na entrega. Sem levantamento, você orça no chute do cliente.", 17, UIFactory.CorSuave);

        if (pm.projetos.Count == 0)
            UIFactory.Texto(tabletConteudo, ProjetosManager.Liberado(TipoProjeto.Cabeamento, out string m) ? "Nenhum pedido de orçamento agora." : m, 19, UIFactory.CorSuave);

        foreach (var p in new List<Projeto>(pm.projetos))
        {
            var proj = p;
            var info = p.Info;
            var card = Cartao();
            string tam = p.levantado || p.etapa >= 3 ? ProjetosManager.Descrever(p.tipo, p.tamanhoReal) : $"uns {ProjetosManager.Descrever(p.tipo, p.tamanhoEstimado)} (palavra do cliente)";
            UIFactory.Texto(card, $"<b>{p.empresa}</b>   {info.nome}   <size=18><color=#9AA6B8>{tam}</color></size>", 21, UIFactory.CorTexto);

            switch (p.etapa)
            {
                case 0:
                {
                    if (!margemProjeto.TryGetValue(p.id, out int mg)) { mg = 1; margemProjeto[p.id] = mg; }
                    int t = pm.TamanhoParaOrcar(p);
                    ProjetosManager.Orcamento(p.tipo, t, mg, out float mat, out float mao, out float total);
                    float custo = ProjetosManager.CustoMaterialReal(p.tipo, t);
                    UIFactory.Texto(card, info.descricao, 17, UIFactory.CorSuave);
                    UIFactory.Texto(card, (info.comodato ? $"Equipamento em comodato (custa {UIFactory.Dinheiro(custo)} para você) + {UIFactory.Dinheiro(CatalogoServicos.MensalFirewall * t)}/mês por {ProjetosManager.MesesFirewall} meses.\n" :
                        $"Material: {UIFactory.Dinheiro(mat)} (custa {UIFactory.Dinheiro(custo)} para você{(Parcerias.Ativa(Parcerias.Infra) ? "" : ", preço de varejo")})   ·   ") +
                        $"Mão de obra: {UIFactory.Dinheiro(mao)} ({ProjetosManager.Horas(p.tipo, t):0.#} h)   ·   <b>Total {UIFactory.Dinheiro(total)}</b>   ·   responde até {DiaManager.DataDoDia(p.expiraNoDia):dd/MM}", 18, UIFactory.CorTexto);
                    var linha = LinhaBotoes(card);
                    if (!p.levantado)
                        UIFactory.Botao(linha, "Levantamento no local (1 h)", () => { Toast(pm.Levantar(proj), UIFactory.Destaque, 4f); AtualizarTablet(); }, UIFactory.CartaoClaro, 320, 44, 17);
                    if (!info.comodato)
                        UIFactory.Botao(linha, $"Margem material: ×{ProjetosManager.Margens[mg]:0.0}", () => { margemProjeto[proj.id] = (mg + 1) % ProjetosManager.Margens.Length; AtualizarTablet(); }, UIFactory.CartaoClaro, 270, 44, 17);
                    UIFactory.Botao(linha, "Enviar orçamento", () =>
                    {
                        string r = pm.EnviarOrcamento(proj, mg);
                        margemProjeto.Remove(proj.id);
                        Toast(r, r.StartsWith("Aprovado") ? UIFactory.Sucesso : UIFactory.Alerta, 6f);
                        AtualizarTablet();
                    }, UIFactory.Destaque, 230, 44, 18);
                    break;
                }
                case 2:
                    UIFactory.Texto(card, $"Aprovado por {UIFactory.Dinheiro(p.valorTotal)}. Material: {UIFactory.Dinheiro(ProjetosManager.CustoMaterialReal(p.tipo, p.tamanhoReal))}" +
                        $"   ·   prazo {DiaManager.DataDoDia(p.prazoDia):dd/MM}", 18, UIFactory.CorTexto);
                    UIFactory.Botao(LinhaBotoes(card), "Comprar material", () => { Toast(pm.ComprarMaterial(proj), UIFactory.Destaque, 6f); AtualizarTablet(); }, UIFactory.Destaque, 260, 44, 18);
                    break;
                case 3:
                    UIFactory.Texto(card, $"Material a caminho: chega {DiaManager.DataDoDia(p.diaMaterial):dd/MM}   ·   prazo {DiaManager.DataDoDia(p.prazoDia):dd/MM}", 18, UIFactory.CorSuave);
                    break;
                case 4:
                {
                    bool atrasado = DiaManager.Instance != null && DiaManager.Instance.dia > p.prazoDia;
                    UIFactory.Texto(card, $"Em execução: {ProjetosManager.Progresso(p) * 100:0}% ({p.horasFeitas:0.#}/{p.horasTotais:0.#} h)   ·   " +
                        (atrasado ? "<color=#F2545B>ATRASADO</color>" : $"prazo {DiaManager.DataDoDia(p.prazoDia):dd/MM}") +
                        "   ·   a equipe ajuda nos dias úteis", 18, UIFactory.CorTexto);
                    UIFactory.Botao(LinhaBotoes(card), "Trabalhar no projeto (2 h)", () =>
                    {
                        string r = pm.Trabalhar(proj);
                        Toast(r, r.Contains("entregue") ? UIFactory.Sucesso : UIFactory.Destaque, r.Contains("entregue") ? 7f : 3f);
                        AtualizarTablet();
                    }, UIFactory.Destaque, 300, 44, 18);
                    break;
                }
                case 5:
                    UIFactory.Texto(card, $"<color=#3CCB7F>Entregue.</color> Recebido {UIFactory.Dinheiro(p.valorTotal)}, material {UIFactory.Dinheiro(p.custoMaterial)}.", 18, UIFactory.CorSuave);
                    break;
            }
        }
    }

    void MontarParcerias()
    {
        Secao("PARCERIAS COM FORNECEDORES");
        UIFactory.Texto(tabletConteudo, "Para revender um produto você precisa ser parceiro do fornecedor. Quanto mais vende, maior o nível: " +
            "Prata (4+ clientes) dá 8% e Ouro (10+) dá 15% de desconto no custo.", 17, UIFactory.CorSuave);
        foreach (var p in Parcerias.Lista)
        {
            var info = p;
            var e = Parcerias.Estado(p.id);
            var card = Cartao(e.status == 2 ? UIFactory.Hex("#1F3A2C") : (Color?)null);
            string status = e.status == 2 ? $"<color=#3CCB7F>Parceiro {Parcerias.NomeNivel(p.id)}</color> · {Parcerias.Volume(p.id)} cliente(s)/itens"
                          : e.status == 1 ? $"<color=#F5B83F>Em análise até {DiaManager.DataDoDia(e.ativaNoDia):dd/MM}</color>" : "Sem parceria";
            UIFactory.Texto(card, $"<b>{p.nome}</b>   <size=18><color=#9AA6B8>{p.area}</color></size>   {status}", 20, UIFactory.CorTexto);
            if (e.status == 2) continue;

            string req = (p.regimeMinimo == Regime.ME ? "Microempresa" : "CNPJ") + (p.reputacaoMinima > 0 ? $", reputação {p.reputacaoMinima:0}" : "");
            float adesao = Parcerias.Adesao(p);
            UIFactory.Texto(card, $"{p.descricao} <color=#9AA6B8>Sem ela: {p.semParceria}</color>", 17, UIFactory.CorSuave);
            UIFactory.Texto(card, $"Requisitos: {req}   ·   Adesão: {(adesao > 0 ? UIFactory.Dinheiro(adesao) : "grátis")}   ·   Análise: {p.diasAnalise} dia(s)", 18, UIFactory.CorTexto);
            if (e.status == 1) continue;

            var linha = LinhaBotoes(card);
            bool ok = Parcerias.AtendeRequisitos(p, out string motivo);
            if (!ok) { UIFactory.Texto(linha, $"<color=#F2545B>{motivo}</color>", 18, UIFactory.CorTexto); continue; }
            if (p.adesao > 0 && !e.negociou)
                UIFactory.Botao(linha, "Negociar adesão", () => { Toast(Parcerias.Negociar(info.id), UIFactory.Destaque, 5f); AtualizarTablet(); }, UIFactory.CartaoClaro, 230, 44, 18);
            UIFactory.Botao(linha, "Solicitar parceria", () => { Toast(Parcerias.Solicitar(info.id), UIFactory.Destaque, 5f); AtualizarTablet(); }, UIFactory.Destaque, 240, 44, 18);
        }
    }
}
