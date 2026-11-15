using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Inventário (tecla I): o que está na mochila, com o peso de cada coisa e o limite de carga. No escritório
// mostra também a estante de estoque, com botões para guardar e pegar.
public partial class GameUI
{
    RectTransform inventario, invConteudo;
    TextMeshProUGUI invTitulo;
    RectTransform invBarra;
    Image invBarraImg;

    void ConstruirInventario(RectTransform pai)
    {
        inventario = JanelaModal(pai, "Inventario", new Vector2(980, 760));
        var cab = UIFactory.Vazio(inventario, "Cabecalho");
        cab.anchorMin = new Vector2(0, 1); cab.anchorMax = new Vector2(1, 1); cab.pivot = new Vector2(0.5f, 1);
        cab.offsetMin = new Vector2(32, -100); cab.offsetMax = new Vector2(-32, -24);
        UIFactory.Vertical(cab, 8, 0);
        var linha = UIFactory.Vazio(cab, "Linha");
        UIFactory.Horizontal(linha, 12);
        invTitulo = UIFactory.Texto(linha, "MOCHILA", 32, UIFactory.CorTexto, FontStyles.Bold);
        invTitulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        UIFactory.Botao(linha, "X", FecharModal, UIFactory.CartaoClaro, 48, 44, 22);
        var fundo = UIFactory.Painel(cab, "BarraFundo", UIFactory.CartaoClaro);
        fundo.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;
        invBarra = UIFactory.Painel(fundo, "Barra", UIFactory.Sucesso);
        invBarraImg = invBarra.GetComponent<Image>();
        invBarra.anchorMin = Vector2.zero; invBarra.anchorMax = Vector2.one;
        invBarra.offsetMin = invBarra.offsetMax = Vector2.zero;

        var area = UIFactory.Vazio(inventario, "Area");
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(32, 28); area.offsetMax = new Vector2(-32, -112);
        invConteudo = UIFactory.ListaComRolagem(area, "Lista");
        UIFactory.Esticar((RectTransform)invConteudo.parent.parent);
    }

    public void AbrirInventario()
    {
        AbrirModal(Modal.Inventario, inventario);
        AtualizarInventario();
    }

    void AtualizarInventario()
    {
        var m = Mochila.Instance;
        if (m == null || inventario == null || !inventario.gameObject.activeSelf) return;
        UIFactory.Limpar(invConteudo);

        float kg = m.PesoAtual, frac = Mathf.Clamp01(kg / Mochila.CapacidadeKg);
        invTitulo.text = $"MOCHILA   <size=70%>{kg:0.0} / {Mochila.CapacidadeKg:0} kg</size>";
        invBarra.anchorMax = new Vector2(frac, 1f);
        invBarraImg.color = frac > 0.95f ? UIFactory.Perigo : frac > 0.75f ? UIFactory.Alerta : UIFactory.Sucesso;

        bool noEscritorio = NoEscritorio() && m.comVoce;   // sem a mochila com você não dá para guardar nem pegar
        if (!m.comVoce)
            UIFactory.Texto(invConteudo, "<color=#F5B83F>A mochila está no home office: você não está com ela.</color>", 19, UIFactory.CorTexto);

        // ---- Na mochila ----
        Secao2("NA MOCHILA");
        bool vazia = true;
        foreach (Item item in System.Enum.GetValues(typeof(Item)))
        {
            if (item == Item.CaixaCabo || item == Item.CaboRede) continue;
            int q = m.Quantidade(item);
            if (q <= 0) continue;
            vazia = false;
            var it = item;
            int lote = item == Item.ConectorRJ45 ? Mathf.Min(10, q) : 1;
            LinhaItem(Mochila.Info(item).nome, q, Mochila.Peso(item) * q,
                noEscritorio && item != Item.Notebook ? (lote > 1 ? $"Guardar {lote}" : "Guardar") : null,
                () => { m.Guardar(it, lote); AtualizarInventario(); });
        }
        foreach (var c in new List<CaboPronto>(m.cabos))
        {
            vazia = false;
            var cabo = c;
            LinhaItem($"Cabo de rede pronto {c.metros:0.#} m" + (c.Bom ? "" : "  <color=#F5B83F>(ponta ruim)</color>"), 1, Mochila.PesoCabo(c),
                noEscritorio ? "Guardar" : null, () => { m.GuardarCabo(cabo); AtualizarInventario(); });
        }
        if (vazia) UIFactory.Texto(invConteudo, "Vazia.", 19, UIFactory.CorSuave);

        // ---- Na estante do escritório ----
        Secao2("NA ESTANTE DO ESCRITÓRIO");
        if (!noEscritorio)
            UIFactory.Texto(invConteudo, "Para guardar ou pegar itens, vá até a estante de aço do escritório.", 18, UIFactory.CorSuave);
        bool semEstoque = true;
        foreach (Item item in System.Enum.GetValues(typeof(Item)))
        {
            int q = m.NoEstoque(item);
            if (q <= 0) continue;
            semEstoque = false;
            var it = item;
            int lote = item == Item.ConectorRJ45 ? Mathf.Min(10, q) : 1;
            LinhaItem(Mochila.Info(item).nome, q, Mochila.Peso(item) * q,
                noEscritorio ? (lote > 1 ? $"Pegar {lote}" : "Pegar") : null,
                () =>
                {
                    if (!m.Pegar(it, lote))
                        Toast(Mochila.Info(it).ferramenta && m.Quantidade(it) > 0 ? "Você já tem essa ferramenta na mochila."
                            : $"Não cabe: faltam {Mochila.Peso(it) * lote - m.KgLivres:0.0} kg de espaço na mochila.", UIFactory.Alerta, 3f);
                    AtualizarInventario();
                });
        }
        foreach (var c in new List<CaboPronto>(m.cabosEstoque))
        {
            semEstoque = false;
            var cabo = c;
            LinhaItem($"Cabo de rede pronto {c.metros:0.#} m" + (c.Bom ? "" : "  <color=#F5B83F>(ponta ruim)</color>"), 1, Mochila.PesoCabo(c),
                noEscritorio ? "Pegar" : null, () =>
                {
                    if (!m.PegarCaboDoEstoque(cabo)) Toast("Não cabe na mochila.", UIFactory.Alerta, 3f);
                    AtualizarInventario();
                });
        }
        if (m.MetrosNaCaixa > 0) { semEstoque = false; UIFactory.Texto(invConteudo, $"Caixa de cabo de rede: {m.MetrosNaCaixa} m (na bancada)", 19, UIFactory.CorTexto); }
        if (semEstoque) UIFactory.Texto(invConteudo, "Nada guardado.", 19, UIFactory.CorSuave);

        UIFactory.Texto(invConteudo, "<size=85%>I ou Esc: fechar</size>", 18, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    void Secao2(string titulo) => UIFactory.Texto(invConteudo, titulo, 17, UIFactory.CorSuave, FontStyles.Bold);

    void LinhaItem(string nome, int qtd, float kg, string rotuloBotao, System.Action acao)
    {
        var linha = UIFactory.Painel(invConteudo, "Item", UIFactory.Cartao);
        var h = UIFactory.Horizontal(linha, 12, 0);
        h.padding = new RectOffset(16, 12, 6, 6);
        var t = UIFactory.Texto(linha, qtd > 1 ? $"{nome}  <color=#9AA6B8>x{qtd}</color>" : nome, 20, UIFactory.CorTexto);
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        var p = UIFactory.Texto(linha, $"{kg:0.00} kg", 18, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Right);
        p.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;
        if (rotuloBotao != null) UIFactory.Botao(linha, rotuloBotao, acao, UIFactory.CartaoClaro, 150, 38, 17);
    }
}
