using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: serviços recorrentes (na aba Contratos), a aba Central (alertas de monitoramento)
// e a preparação de notebooks/mini PCs na bancada.
public partial class GameUI
{
    // Escolhas da proposta de serviço: [prazo, desconto]
    readonly Dictionary<int, int[]> propostasServico = new Dictionary<int, int[]>();
    static readonly float[] descontos = { 0f, 0.05f, 0.1f, 0.15f };

    // ---------- Aba Contratos: serviços ----------

    void MontarServicosNaAbaContratos()
    {
        var sm = ServicosManager.Instance;
        if (sm == null) return;

        Secao($"SERVIÇOS MENSAIS · receita recorrente {UIFactory.Dinheiro(sm.Mrr)}/mês");
        foreach (var info in CatalogoServicos.Servicos)
        {
            if (sm.Liberado(info.tipo, out string motivo)) continue;
            UIFactory.Texto(tabletConteudo, $"<color=#9AA6B8>{info.nome}: {motivo}</color>", 17, UIFactory.CorTexto);
            break;   // um aviso basta
        }

        // Contatos
        if (sm.leads.Count == 0) UIFactory.Texto(tabletConteudo, "Nenhum contato de serviço agora.", 19, UIFactory.CorSuave);
        foreach (var l in new List<LeadServico>(sm.leads))
        {
            var lead = l;
            var info = CatalogoServicos.Info(l.tipo);
            if (!propostasServico.TryGetValue(l.id, out var esc)) { esc = new[] { 2, 0 }; propostasServico[l.id] = esc; }
            int meses = info.porPrazo ? new[] { 12, 24, 36 }[esc[0]] : 12;
            float tabela = CatalogoServicos.Mensal(l.tipo, l.unidades, meses);
            float oferta = tabela * (1f - descontos[esc[1]]);
            float custo = CatalogoServicos.CustoMensal(l.tipo, l.unidades);

            var card = Cartao();
            UIFactory.Texto(card, $"<b>{l.empresa}</b>   <size=18><color=#9AA6B8>{info.nome} · {ServicosManager.DescreverUnidades(l.tipo, l.unidades)} · responde até {DiaManager.DataDoDia(l.expiraNoDia):dd/MM}</color></size>", 21, UIFactory.CorTexto);
            UIFactory.Texto(card, info.descricao, 17, UIFactory.CorSuave);
            var linha = LinhaBotoes(card);
            if (info.porPrazo)
                UIFactory.Botao(linha, $"Prazo: {meses} meses", () => { esc[0] = (esc[0] + 1) % 3; AtualizarTablet(); }, UIFactory.CartaoClaro, 220, 44, 17);
            UIFactory.Botao(linha, $"Desconto: {descontos[esc[1]] * 100:0}%", () => { esc[1] = (esc[1] + 1) % descontos.Length; AtualizarTablet(); }, UIFactory.CartaoClaro, 200, 44, 17);

            string equip = "";
            if (info.exigeEquipamento)
            {
                float compra = info.custoEquipamento * l.unidades;
                equip = $"   ·   Equipamentos: {UIFactory.Dinheiro(compra)} (prontos: {sm.Prontos(l.tipo)}/{l.unidades})";
            }
            UIFactory.Texto(card, $"Mensal: <b>{UIFactory.Dinheiro(oferta)}</b>   ·   Seu custo: ~{UIFactory.Dinheiro(custo)}/mês   ·   Implantação: {UIFactory.Dinheiro(CatalogoServicos.Implantacao(l.tipo, l.unidades))}{equip}", 18, UIFactory.CorTexto);
            UIFactory.Botao(LinhaBotoes(card), "Enviar proposta", () =>
            {
                string r = sm.EnviarProposta(lead, esc[0], descontos[esc[1]]);
                propostasServico.Remove(lead.id);
                Toast(r, r.StartsWith("Fechado") ? UIFactory.Sucesso : UIFactory.Alerta, 5f);
                AtualizarTablet();
            }, UIFactory.Destaque, 240, 46, 20);
        }

        // Contratos de serviço
        foreach (var c in sm.contratos)
        {
            if (!c.ativo) continue;
            var contrato = c;
            var info = CatalogoServicos.Info(c.tipo);
            var card = Cartao();
            string sat = c.satisfacao >= 60 ? "#3CCB7F" : c.satisfacao >= 35 ? "#F5B83F" : "#F2545B";
            UIFactory.Texto(card, $"<b>{c.empresa}</b>   {info.nome}   {UIFactory.Dinheiro(c.mensal)}/mês   <size=18><color={sat}>satisfação {c.satisfacao:F0}</color></size>", 20, UIFactory.CorTexto);
            string extra = c.tipo == TipoServico.Backup ? $"  ·  backup: {(c.backupOk ? "<color=#3CCB7F>ok</color>" : $"<color=#F2545B>falhando há {c.diasBackupFalhando} dia(s)</color>")}" : "";
            UIFactory.Texto(card, $"{ServicosManager.DescreverUnidades(c.tipo, c.unidades)}  ·  {c.meses} meses{extra}", 17, UIFactory.CorSuave);
            if (!c.Implantado)
                UIFactory.Botao(LinhaBotoes(card), info.exigeEquipamento ? $"Entregar e instalar (1 h)" : $"Implantar ({ServicosManager.MinutosImplantacao(c) / 60f:0.#} h)",
                    () => { Toast(sm.Implantar(contrato), UIFactory.Destaque, 4f); AtualizarTablet(); }, UIFactory.Destaque, 300, 44, 18);
        }

        // Compra de notebooks e mini PCs
        foreach (var info in CatalogoServicos.Servicos)
        {
            if (!info.exigeEquipamento) continue;
            var tipo = info.tipo;
            var card = Cartao();
            UIFactory.Texto(card, $"<b>{info.nome}</b>   <size=18><color=#9AA6B8>novos (preparar na bancada): {sm.Novos(tipo)} · prontos: {sm.Prontos(tipo)}</color></size>", 20, UIFactory.CorTexto);
            UIFactory.Botao(LinhaBotoes(card), $"Comprar 1 {info.unidade} ({UIFactory.Dinheiro(info.custoEquipamento)})",
                () => { Toast(sm.ComprarEquipamento(tipo), UIFactory.Destaque, 4f); AtualizarTablet(); }, UIFactory.CartaoClaro, 360, 44, 18);
        }
    }

    // ---------- Aba Central ----------

    void MontarAbaCentral()
    {
        var sm = ServicosManager.Instance;
        if (sm == null) return;
        Secao($"CENTRAL DE MONITORAMENTO · {sm.alertas.Count} alerta(s)");
        UIFactory.Texto(tabletConteudo, "Resolva os alertas antes do cliente perceber. Alerta esquecido derruba a satisfação; no suporte gerenciado ele vira chamado presencial.", 18, UIFactory.CorSuave);
        if (sm.alertas.Count == 0) UIFactory.Texto(tabletConteudo, "Tudo verde por aqui.", 21, UIFactory.Sucesso);

        int hoje = DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
        var ordenados = new List<Alerta>(sm.alertas);
        ordenados.Sort((a, b) => (b.critico ? 1 : 0).CompareTo(a.critico ? 1 : 0));
        foreach (var a in ordenados)
        {
            var alerta = a;
            int idade = hoje - a.criadoNoDia;
            var card = Cartao(a.critico ? UIFactory.Hex("#3A1F22") : (Color?)null);
            UIFactory.Texto(card, $"<b>{a.titulo}</b>   <size=18><color=#9AA6B8>{a.empresa} · {(idade == 0 ? "hoje" : $"há {idade} dia(s)")}</color></size>", 21, UIFactory.CorTexto);
            UIFactory.Texto(card, a.detalhe, 17, UIFactory.CorSuave);
            UIFactory.Botao(LinhaBotoes(card), a.visita ? $"Ir até o cliente ({a.minutos} min)" : $"Resolver à distância ({a.minutos} min)", () =>
            {
                Toast(sm.ResolverAlerta(alerta), UIFactory.Destaque, 4f);
                AtualizarTablet();
            }, a.visita ? UIFactory.Alerta : UIFactory.Destaque, 320, 44, 18);
        }
    }

    // ---------- Bancada: preparar notebooks e mini PCs ----------

    void MontarPreparacaoNaBancada()
    {
        var sm = ServicosManager.Instance;
        if (sm == null) return;
        foreach (var tipo in new[] { TipoServico.Notebook, TipoServico.MiniPc })
        {
            var info = CatalogoServicos.Info(tipo);
            if (sm.Novos(tipo) == 0) continue;
            var t = tipo;
            UIFactory.Texto(bancadaConteudo, $"PREPARAR {info.unidade.ToUpper()}S PARA ENTREGA ({sm.Novos(tipo)} esperando)", 17, UIFactory.Destaque, FontStyles.Bold);
            var b = UIFactory.Botao(bancadaConteudo, $"Preparar 1 {info.unidade}: formatar, imagem, antivírus, Office, testar",
                () => rotinaSegurar = StartCoroutine(Preparar(t)), UIFactory.CartaoClaro, 680, 48, 18);
            b.interactable = !segurando;
        }
    }

    IEnumerator Preparar(TipoServico tipo)
    {
        segurando = true;
        string[] etapas = { "Formatando o disco", "Instalando a imagem padrão", "Instalando o antivírus", "Instalando o Office", "Testando tudo" };
        float[] tempos = { 3f, 2f, 1f, 1f, 1f };
        var txt = UIFactory.Texto(bancadaConteudo, "", 20, UIFactory.Alerta, FontStyles.Bold);
        for (int i = 0; i < etapas.Length; i++)
        {
            float t = 0f;
            while (t < tempos[i])
            {
                t += Time.unscaledDeltaTime;
                int barras = Mathf.RoundToInt(16f * t / tempos[i]);
                if (txt != null) txt.text = $"{i + 1}/{etapas.Length} {etapas[i]}...  [{new string('|', barras)}{new string('.', 16 - barras)}]";
                yield return null;
            }
        }
        segurando = false;
        if (ServicosManager.Instance != null && ServicosManager.Instance.PrepararUnidade(tipo))
            Toast($"{CatalogoServicos.Info(tipo).unidade} pronto para entregar!", UIFactory.Sucesso, 2.5f);
        if (modal == Modal.Bancada) MontarBancada();
    }
}
