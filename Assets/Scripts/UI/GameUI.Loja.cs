using TMPro;
using UnityEngine.UI;
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
        // Etapa 10D: as ferramentas que você tem, em fotos
        var fotos = UIFactory.Vazio(c, "Ferramentas");
        UIFactory.Horizontal(fotos, 8);
        var nomesFerr = new System.Collections.Generic.List<string>();
        foreach (var info in Mochila.Catalogo)
        {
            if (!info.ferramenta || !m.Tem(info.item)) continue;
            nomesFerr.Add(info.nome);
            var tex = UIFactory.Icone(info.item);
            if (tex != null) UIFactory.Imagem(fotos, tex, 56);
        }
        if (nomesFerr.Count == 0) UIFactory.Texto(fotos, "Nenhuma ferramenta ainda: compre abaixo.", 18, UIFactory.CorSuave);
        if (nomesFerr.Count > 0) UIFactory.Texto(c, "Ferramentas: " + string.Join(", ", nomesFerr), 17, UIFactory.CorSuave);
        UIFactory.Texto(c, $"Conectores RJ45: {m.Quantidade(Item.ConectorRJ45)}  ·  Toner: {m.Quantidade(Item.Toner)}  ·  Cabos prontos: {m.ListaCabos()}", 19, UIFactory.CorSuave);
        UIFactory.Texto(c, $"Na sede: caixa de cabo com {m.MetrosNaCaixa} m (use a bancada de cabos)", 19, UIFactory.CorSuave);
        if (m.entregas.Count > 0)
        {
            UIFactory.Texto(c, "<color=#F5B83F>A caminho:</color>", 19, UIFactory.CorTexto);
            var cam = UIFactory.Vazio(c, "ACaminho");
            UIFactory.Horizontal(cam, 10);
            foreach (var e in m.entregas)
            {
                var ent = UIFactory.Vazio(cam, "Entrega");
                UIFactory.Vertical(ent, 2, 0, TextAnchor.UpperCenter);
                ent.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                var tex = UIFactory.Icone(e.item);
                if (tex != null) UIFactory.Imagem(ent, tex, 64);
                UIFactory.Texto(ent, Mochila.Info(e.item).nome + (e.naPorta ? "\n<color=#3CCB7F>na porta de casa</color>" : e.reposicao ? "\n(troca na garantia)" : ""),
                    14, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
            }
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

        // Etapa 10D: vitrine em grade, com a foto de cada item
        var grade = UIFactory.Vazio(tabletConteudo, "Vitrine");
        var g = grade.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        g.cellSize = new Vector2(338, 372);
        g.spacing = new Vector2(14, 14);
        g.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;
        foreach (var info in Mochila.Catalogo)
        {
            var item = info;
            var card = UIFactory.Painel(grade, "Produto_" + item.item, UIFactory.Cartao);
            UIFactory.Arredondar(card.GetComponent<UnityEngine.UI.Image>(), 14f);
            UIFactory.Sombra(card);
            UIFactory.Vertical(card, 4, 14, TextAnchor.UpperCenter);

            // Foto num fundo claro
            var vitr = UIFactory.Painel(card, "Foto", UIFactory.Hex("#2E3848"));
            UIFactory.Arredondar(vitr.GetComponent<UnityEngine.UI.Image>(), 12f);
            var leV = vitr.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            leV.preferredHeight = leV.minHeight = 140;
            var tex = UIFactory.Icone(item.item);
            if (tex != null)
            {
                var foto = UIFactory.Imagem(vitr, tex, 132);
                var rtf = foto.rectTransform;
                rtf.anchorMin = rtf.anchorMax = new Vector2(0.5f, 0.5f);
                rtf.sizeDelta = new Vector2(132, 132);
            }
            bool tenho = item.ferramenta && m.Tem(item.item), caminho = item.ferramenta && m.ACaminho(item.item);
            if (tenho || caminho)
            {
                var selo = UIFactory.Painel(vitr, "Selo", tenho ? UIFactory.Sucesso : UIFactory.Alerta);
                UIFactory.Ancorar(selo, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(118f, 28f));
                var ts = UIFactory.Texto(selo, tenho ? "VOCÊ TEM" : "A CAMINHO", 14, UIFactory.Hex("#10151D"), FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.Esticar(ts.rectTransform);
            }

            var nome = UIFactory.Texto(card, $"<b>{item.nome}</b>", 18, UIFactory.CorTexto, FontStyles.Normal, TextAlignmentOptions.Center);
            nome.overflowMode = TextOverflowModes.Ellipsis;
            nome.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 46;
            UIFactory.Texto(card, $"<b>{UIFactory.Dinheiro(Mercado.Preco(item, fornecedorLoja))}</b>  <size=14><color=#9AA6B8>{Mercado.VariacaoTexto(item.item)}</color></size>",
                22, UIFactory.Sucesso, FontStyles.Normal, TextAlignmentOptions.Center);
            string qtd = item.ferramenta ? "" : $"você tem: {(item.item == Item.CaixaCabo ? m.MetrosNaCaixa + " m" : item.item == Item.CaboRede ? m.cabos.Count.ToString() : m.Quantidade(item.item).ToString())}";
            var desc = UIFactory.Texto(card, item.descricao + (qtd != "" ? $"  <color=#9AA6B8>({qtd})</color>" : ""), 14, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
            desc.overflowMode = TextOverflowModes.Ellipsis;
            var leD = desc.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            leD.preferredHeight = 58; leD.flexibleHeight = 1;

            bool podeComprar = !(tenho || caminho);
            var linha = UIFactory.Vazio(card, "Botoes");
            UIFactory.Horizontal(linha, 8, 0, TextAnchor.MiddleCenter);
            var normal = UIFactory.Botao(linha, forn.prazoDias == 1 ? "Comprar (amanhã)" : $"Comprar ({forn.prazoDias} dias)",
                () => { Toast(m.Comprar(item.item, false, fornecedorLoja), UIFactory.Destaque, 3f); AtualizarTablet(); },
                UIFactory.CartaoClaro, forn.expressa ? 150 : 300, 40, 15);
            normal.interactable = podeComprar;
            if (forn.expressa)
            {
                var expressa = UIFactory.Botao(linha, "Expressa 30 min", () => { Toast(m.Comprar(item.item, true, fornecedorLoja), UIFactory.Destaque, 3f); AtualizarTablet(); },
                    UIFactory.Destaque, 150, 40, 15);
                expressa.interactable = podeComprar;
            }
        }
    }
}
