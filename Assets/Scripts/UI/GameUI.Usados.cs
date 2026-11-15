using UnityEngine;

// Parte da GameUI: equipamentos usados na aba Loja (lote à venda e o que está no escritório para testar,
// recuperar, apagar dados, vender ou descartar).
public partial class GameUI
{
    static bool NoEscritorio()
    {
        var sala = GameObject.Find("Cenario/Escritorio");
        var jogador = GameObject.FindWithTag("Player");
        if (sala == null || jogador == null) return true;
        Vector3 l = sala.transform.InverseTransformPoint(jogador.transform.position);
        return Mathf.Abs(l.x) < 3.3f && Mathf.Abs(l.z) < 3.3f;
    }

    void MontarUsados()
    {
        bool lote = EquipamentosUsados.LoteDisponivel(out int itens, out float preco, out string origem);
        var estoque = EquipamentosUsados.Estoque;
        if (!lote && estoque.Count == 0) return;

        Secao("EQUIPAMENTOS USADOS");
        if (lote)
        {
            var c = Cartao(UIFactory.CartaoClaro);
            UIFactory.Texto(c, $"<b>Lote de {origem}</b>   {UIFactory.Dinheiro(preco)}", 22, UIFactory.CorTexto);
            UIFactory.Texto(c, $"{itens} equipamentos sem teste, no estado em que estão. Pode ter coisa boa, coisa com conserto e sucata. Disponível só hoje e amanhã.",
                18, UIFactory.CorSuave);
            UIFactory.Botao(LinhaBotoes(c), "Comprar o lote", () => { Toast(EquipamentosUsados.ComprarLote(), UIFactory.Destaque, 4f); AtualizarTablet(); },
                UIFactory.Destaque, 230, 44, 19);
        }
        if (estoque.Count == 0) return;

        bool aqui = NoEscritorio();
        if (!aqui) UIFactory.Texto(tabletConteudo, "<color=#F5B83F>Os usados estão no escritório: vá até lá para testar, recuperar e apagar os dados.</color>", 18, UIFactory.CorTexto);

        foreach (var eq in new System.Collections.Generic.List<EquipUsado>(estoque))
        {
            var e = eq;
            var c = Cartao();
            string estado = !e.testado ? "<color=#9AA6B8>não testado</color>"
                          : e.condicao == CondicaoUsado.Bom ? $"<color=#3CCB7F>funcionando{(e.recuperado ? " (recuperado)" : "")}</color> — vale {UIFactory.Dinheiro(e.valorBom)}"
                          : e.condicao == CondicaoUsado.DefeitoLeve ? $"<color=#F5B83F>defeito com conserto</color> — peças {UIFactory.Dinheiro(e.custoConserto)}, depois vale {UIFactory.Dinheiro(e.valorBom)}"
                          : "<color=#F2545B>sucata</color>";
            string dados = !e.temDados ? "" : e.dadosApagados ? "  ·  <color=#3CCB7F>dados apagados</color>" : "  ·  <color=#F5B83F>com dados do antigo dono</color>";
            UIFactory.Texto(c, $"<b>{e.nome}</b>   {estado}{dados}", 20, UIFactory.CorTexto);
            var linha = LinhaBotoes(c);
            void Acao(string rotulo, System.Func<string> f, Color cor, bool precisaEscritorio)
            {
                var b = UIFactory.Botao(linha, rotulo, () => { Toast(f(), UIFactory.Destaque, 4f); AtualizarTablet(); }, cor, 210, 42, 17);
                b.interactable = aqui || !precisaEscritorio;
            }
            if (!e.testado) Acao("Testar (20 min)", () => EquipamentosUsados.Testar(e), UIFactory.Destaque, true);
            else
            {
                if (e.condicao == CondicaoUsado.DefeitoLeve) Acao("Recuperar (45 min)", () => EquipamentosUsados.Recuperar(e), UIFactory.Destaque, true);
                if (e.temDados && !e.dadosApagados && e.condicao != CondicaoUsado.Sucata) Acao("Apagar dados (15 min)", () => EquipamentosUsados.ApagarDados(e), UIFactory.CartaoClaro, true);
                if (e.condicao == CondicaoUsado.Bom) Acao("Vender", () => EquipamentosUsados.Vender(e), UIFactory.Sucesso, false);
                if (e.condicao == CondicaoUsado.Sucata) Acao("Levar ao ponto de coleta", () => EquipamentosUsados.Descartar(e), UIFactory.CartaoClaro, false);
            }
        }
    }
}
