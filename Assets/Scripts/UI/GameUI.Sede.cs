using TMPro;
using UnityEngine;

// Parte da GameUI (aba Empresa): sede, móveis e painel de concorrentes.
public partial class GameUI
{
    void MontarSede()
    {
        var sede = SedeManager.Instance;
        if (sede == null) return;

        Secao("SEDE");
        var c = Cartao(UIFactory.CartaoClaro);
        UIFactory.Texto(c, $"<b>{sede.Imovel.nome}</b>   <size=18><color=#9AA6B8>{sede.Imovel.descricao}</color></size>", 22, UIFactory.CorTexto);
        if (sede.imovel > 0)
            UIFactory.Texto(c, $"Custo fixo: ~{UIFactory.Dinheiro(sede.CustoFixoMensal)}/mês (aluguel, condomínio/IPTU, luz, internet)   ·   Caução depositada: {UIFactory.Dinheiro(sede.caucaoPaga)}", 18, UIFactory.CorSuave);
        UIFactory.Texto(c, $"Estações de trabalho: {sede.Estacoes}/{sede.Imovel.maxEstacoes} (cada funcionário precisa de uma)   ·   Estoque de equipamentos: até {sede.CapacidadeEstoque}", 18, UIFactory.CorSuave);

        // Mudar de imóvel
        for (int i = 0; i < SedeManager.Imoveis.Length; i++)
        {
            if (i == sede.imovel) continue;
            int indice = i;
            var im = SedeManager.Imoveis[i];
            var card = Cartao();
            UIFactory.Texto(card, i == 0 ? "<b>Voltar a trabalhar de casa</b>   <size=18><color=#9AA6B8>a caução é devolvida</color></size>"
                : $"<b>{im.nome}</b>   {UIFactory.Dinheiro(im.aluguel)}/mês   <size=18><color=#9AA6B8>caução {UIFactory.Dinheiro(im.aluguel * SedeManager.CaucaoMeses)} · {im.descricao}</color></size>", 20, UIFactory.CorTexto);
            UIFactory.Botao(LinhaBotoes(card), i == 0 ? "Voltar para casa" : "Alugar", () => { Toast(sede.Alugar(indice), UIFactory.Destaque, 5f); AtualizarTablet(); },
                i == 0 ? UIFactory.CartaoClaro : UIFactory.Destaque, 220, 44, 19);
        }

        if (sede.imovel == 0) return;
        Secao("MÓVEIS E EQUIPAMENTOS DA SEDE");
        foreach (var m in SedeManager.Moveis)
        {
            var movel = m;
            var card = Cartao();
            UIFactory.Texto(card, $"<b>{m.nome}</b>   {UIFactory.Dinheiro(m.preco)}   <size=18><color=#9AA6B8>você tem: {sede.Qtd(m.tipo)} · {m.efeito}</color></size>", 19, UIFactory.CorTexto);
            UIFactory.Botao(LinhaBotoes(card), "Comprar", () => { Toast(sede.ComprarMovel(movel.tipo), UIFactory.Destaque, 3f); AtualizarTablet(); }, UIFactory.CartaoClaro, 180, 42, 18);
        }
    }

    void MontarBanco()
    {
        var b = BancoManager.Instance;
        if (b == null) return;
        Secao("BANCO");
        var c = Cartao();
        float saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        UIFactory.Texto(c, $"Juros: <b>{b.JurosMes * 100:0.#}% ao mês</b> ({(EmpresaManager.Instance != null && EmpresaManager.Instance.TemCnpj ? "capital de giro" : "crédito pessoal")})   ·   " +
            $"Pode pegar: <b>{UIFactory.Dinheiro(b.Limite)}</b>   ·   Devendo: {UIFactory.Dinheiro(b.SaldoDevedor)}", 19, UIFactory.CorTexto);
        if (saldo < 0f)
            UIFactory.Texto(c, $"<color=#F2545B>Conta no vermelho há {b.diasNoVermelho} dia(s). Com {BancoManager.DiasParaFalencia} a empresa quebra.</color>", 19, UIFactory.CorTexto);
        foreach (var e in b.emprestimos)
            UIFactory.Texto(c, $"• {UIFactory.Dinheiro(e.valor)} em {e.totalParcelas}x de {UIFactory.Dinheiro(e.parcela)} — faltam {e.Restantes} parcela(s)", 18, UIFactory.CorSuave);

        var linha = LinhaBotoes(c);
        foreach (float v in new[] { 1000f, 3000f, 10000f })
        {
            if (v > b.Limite) continue;
            foreach (int n in BancoManager.OpcoesParcelas)
            {
                float valor = v; int parcelas = n;
                UIFactory.Botao(linha, $"{UIFactory.Dinheiro(v)} em {n}x", () => { Toast(b.Tomar(valor, parcelas), UIFactory.Alerta, 6f); AtualizarTablet(); },
                    UIFactory.CartaoClaro, 200, 42, 16);
            }
        }
        if (b.Limite < 1000f) UIFactory.Texto(linha, "Sem crédito disponível agora.", 18, UIFactory.CorSuave);
    }

    void MontarConquistas()
    {
        Secao($"CONQUISTAS · {Conquistas.desbloqueadas.Count}/{Conquistas.Lista.Length}");
        foreach (var q in Conquistas.Lista)
        {
            bool tem = Conquistas.Tem(q.id);
            UIFactory.Texto(tabletConteudo, tem ? $"<color=#C9A227><b>{q.nome}</b></color> — {q.descricao}" : $"<color=#5A6578>{q.nome} — {q.descricao} (bloqueada)</color>", 17, UIFactory.CorTexto);
        }
    }

    void MontarConcorrencia()
    {
        Secao("MERCADO E CONCORRENTES");
        UIFactory.Texto(tabletConteudo, $"Propostas ganhas: {Concorrencia.ganhas}  ·  perdidas: {Concorrencia.perdidas}  ·  sua fatia estimada: {Concorrencia.fatiaJogador * 100:0.#}%", 19, UIFactory.CorTexto);
        if (Concorrencia.lista == null || Concorrencia.lista.Count == 0) Concorrencia.Iniciar();
        foreach (var c in Concorrencia.lista)
        {
            string preco = c.precoFator < 0.95f ? "<color=#3CCB7F>barata</color>" : c.precoFator > 1.05f ? "<color=#F2545B>cara</color>" : "preço de mercado";
            UIFactory.Texto(tabletConteudo, $"• <b>{c.nome}</b> — {c.perfil}  ·  {preco}  ·  fatia ~{c.fatia * 100:0}%  ·  ganhou de você {c.vitoriasSobreVoce}x, perdeu {c.derrotasParaVoce}x", 18, UIFactory.CorSuave);
        }
    }
}
