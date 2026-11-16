using UnityEngine;

// Parte da GameUI: equipamentos usados na aba Loja. Comprar o lote é pelo tablet; o trabalho é físico (Etapa 6):
// as caixas chegam no escritório, cada equipamento vai para a bancada (testar, consertar, limpar, apagar dados,
// etiquetar, anunciar ou pôr na frota) e a sucata vai para o lixo eletrônico. Aqui só aparece o estado.
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
            UIFactory.Texto(c, $"{itens} equipamentos sem teste, no estado em que estão. Pode ter coisa boa, coisa com conserto e sucata. Disponível só hoje e amanhã. " +
                "Chega em caixas no escritório.", 18, UIFactory.CorSuave);
            UIFactory.Botao(LinhaBotoes(c), "Comprar o lote", () => { Toast(EquipamentosUsados.ComprarLote(), UIFactory.Destaque, 4f); AtualizarTablet(); },
                UIFactory.Destaque, 230, 44, 19);
        }
        if (estoque.Count == 0) return;

        UIFactory.Texto(tabletConteudo, "<color=#9AA6B8>O trabalho é na bancada do escritório: pegue o equipamento (E), ponha na bancada e mire nele. " +
            "Sucata vai para a caixa de lixo eletrônico.</color>", 17, UIFactory.CorTexto);

        foreach (var e in estoque)
        {
            var c = Cartao();
            string estado = !e.testado ? "<color=#9AA6B8>não testado</color>"
                          : e.condicao == CondicaoUsado.Bom ? $"<color=#3CCB7F>funcionando{(e.recuperado ? " (recuperado)" : "")}</color> — vale {UIFactory.Dinheiro(EquipamentosUsados.ValorVenda(e))}"
                          : e.condicao == CondicaoUsado.DefeitoLeve ? (e.diagnosticado ? $"<color=#F5B83F>{EquipamentosUsados.NomeDefeito(e.defeito)}</color> — peça {UIFactory.Dinheiro(EquipamentosUsados.CustoPeca(e))}, depois vale {UIFactory.Dinheiro(e.valorBom)}"
                                                                                       : "<color=#F5B83F>com defeito, peça não descoberta</color>")
                          : "<color=#F2545B>sucata</color>";
            string dados = !e.temDados || !e.testado || e.condicao == CondicaoUsado.Sucata ? "" : e.dadosApagados ? "  ·  <color=#3CCB7F>dados apagados</color>" : "  ·  <color=#F5B83F>com dados do antigo dono</color>";
            UIFactory.Texto(c, $"<b>{e.nome}</b>   {estado}{dados}", 20, UIFactory.CorTexto);
            string onde = e.local == LocalUsado.Caixa ? "na caixa do lote, no escritório" : e.local == LocalUsado.Bancada ? "na bancada"
                        : e.local == LocalUsado.Mao ? "na sua mão" : "no chão do escritório";
            var extras = new System.Collections.Generic.List<string> { onde };
            if (e.limpo) extras.Add("limpo");
            if (e.patrimonio > 0) extras.Add("etiqueta " + e.Etiqueta);
            if (e.anunciado) extras.Add("<color=#3FA9F5>anunciado: o comprador busca amanhã</color>");
            UIFactory.Texto(c, $"{string.Join("  ·  ", extras)}\n<color=#9AA6B8>Próximo passo: {ProximoPasso(e)}</color>", 17, UIFactory.CorSuave);
        }
    }

    static string ProximoPasso(EquipUsado e)
    {
        if (e.local == LocalUsado.Caixa) return "abrir a caixa e levar à bancada.";
        if (!e.testado) return e.local == LocalUsado.Bancada ? "ligar e testar." : "levar à bancada e testar.";
        if (e.condicao == CondicaoUsado.Sucata) return e.tipo == TipoUsado.Computador && !e.pecasAproveitadas ? "aproveitar o pente de memória e levar ao lixo eletrônico." : "levar ao lixo eletrônico.";
        if (e.condicao == CondicaoUsado.DefeitoLeve) return e.diagnosticado ? "trocar a peça ruim." : "descobrir a peça (placa POST, testador de fonte ou troca na bancada).";
        if (e.anunciado) return "esperar o comprador.";
        if (e.PrecisaApagar) return "apagar os dados do antigo dono (LGPD).";
        if (!e.limpo) return "limpar, etiquetar e anunciar.";
        if (e.patrimonio == 0) return "colar a etiqueta de patrimônio.";
        return e.tipo == TipoUsado.Impressora ? "anunciar ou colocar na frota de locação." : "anunciar à venda.";
    }
}
