using TMPro;
using UnityEngine;

// Parte da GameUI: aba "Loja e mochila" do tablet.
public partial class GameUI
{
    int fornecedorLoja = Mercado.FornecedorPadrao;

    void MontarAbaLoja()
    {
        var m = Mochila.Instance;
        if (m == null) return;

        Secao("SUA MOCHILA");
        var c = Cartao(UIFactory.CartaoClaro);
        var ferramentas = new System.Collections.Generic.List<string> { "Notebook" };
        foreach (var info in Mochila.Catalogo)
            if (info.ferramenta && m.Tem(info.item)) ferramentas.Add(info.nome);
        UIFactory.Texto(c, "Ferramentas: " + string.Join(", ", ferramentas), 20, UIFactory.CorTexto);
        UIFactory.Texto(c, $"Conectores RJ45: {m.Quantidade(Item.ConectorRJ45)}  ·  Toner: {m.Quantidade(Item.Toner)}  ·  Cabos prontos: {m.ListaCabos()}", 19, UIFactory.CorSuave);
        UIFactory.Texto(c, $"Na sede: caixa de cabo com {m.MetrosNaCaixa} m (use a bancada para fazer cabos)", 19, UIFactory.CorSuave);
        if (m.entregas.Count > 0)
        {
            var nomes = new System.Collections.Generic.List<string>();
            foreach (var e in m.entregas) nomes.Add(Mochila.Info(e.item).nome + (e.naPorta ? " <color=#3CCB7F>(na porta de casa)</color>" : e.reposicao ? " (troca na garantia)" : ""));
            UIFactory.Texto(c, "<color=#F5B83F>A caminho:</color> " + string.Join(", ", nomes), 19, UIFactory.CorTexto);
        }

        MontarUsados();

        Secao("FORNECEDORES");
        var abas = LinhaBotoes(tabletConteudo);
        for (int i = 0; i < Mercado.Fornecedores.Length; i++)
        {
            int idx = i;
            UIFactory.Botao(abas, Mercado.Fornecedores[i].nome, () => { fornecedorLoja = idx; AtualizarTablet(); },
                fornecedorLoja == i ? UIFactory.Destaque : UIFactory.CartaoClaro, 320, 44, 18);
        }
        var forn = Mercado.Fornecedores[fornecedorLoja];
        string prazo = forn.prazoDias == 1 ? "chega amanhã de manhã" : $"chega em {forn.prazoDias} dias";
        UIFactory.Texto(tabletConteudo, $"<b>{forn.nome}</b> — {forn.perfil}\nEntrega normal grátis, {prazo}." +
            (forn.expressa ? $"  ·  Expressa (motoboy): +{UIFactory.Dinheiro(Mochila.TaxaExpressa)}, {Mochila.MinutosEntregaExpressa:F0} min." : "  ·  Sem entrega expressa."),
            18, UIFactory.CorSuave);

        foreach (var info in Mochila.Catalogo)
        {
            var item = info;
            var card = Cartao();
            string tem = item.ferramenta
                ? (m.Tem(item.item) ? "  <color=#3CCB7F><size=18>VOCÊ JÁ TEM</size></color>" : m.ACaminho(item.item) ? "  <color=#F5B83F><size=18>A CAMINHO</size></color>" : "")
                : $"  <color=#9AA6B8><size=18>você tem: {(item.item == Item.CaixaCabo ? m.MetrosNaCaixa + " m" : item.item == Item.CaboRede ? m.cabos.Count.ToString() : m.Quantidade(item.item).ToString())}</size></color>";
            UIFactory.Texto(card, $"<b>{item.nome}</b>   {UIFactory.Dinheiro(Mercado.Preco(item, fornecedorLoja))}  <size=17>{Mercado.VariacaoTexto(item.item)}</size>{tem}", 22, UIFactory.CorTexto);
            UIFactory.Texto(card, item.descricao, 18, UIFactory.CorSuave);

            bool podeComprar = !(item.ferramenta && (m.Tem(item.item) || m.ACaminho(item.item)));
            var linha = LinhaBotoes(card);
            var normal = UIFactory.Botao(linha, forn.prazoDias == 1 ? "Comprar (amanhã)" : $"Comprar ({forn.prazoDias} dias)",
                () => { Toast(m.Comprar(item.item, false, fornecedorLoja), UIFactory.Destaque, 3f); AtualizarTablet(); },
                UIFactory.CartaoClaro, 230, 44, 19);
            normal.interactable = podeComprar;
            if (forn.expressa)
            {
                var expressa = UIFactory.Botao(linha, "Expressa (30 min)", () => { Toast(m.Comprar(item.item, true, fornecedorLoja), UIFactory.Destaque, 3f); AtualizarTablet(); },
                    UIFactory.Destaque, 230, 44, 19);
                expressa.interactable = podeComprar;
            }
        }
    }
}
