using System.Collections.Generic;

// Etapa 6: o que o técnico pode fazer com um equipamento usado em cima da bancada (mirar + E, roda do mouse,
// segurar E — o mesmo painel das peças dos clientes). A regra está em EquipamentosUsados; o visual reage pelo
// aviso EquipamentosUsados.AoMudar (UsadosFisicos).
public static class AcoesBancada
{
    public const string Peca = "usado";

    public static List<AcaoFisica> Para(EquipUsado e)
    {
        var l = new List<AcaoFisica>();
        if (e == null) return l;
        AcoesFisicas.PecaAtual = Peca;

        l.Add(new AcaoFisica("Olhar o equipamento", () => Descrever(e)) { observacao = true });

        if (!e.testado)
            l.Add(new AcaoFisica("Ligar na tomada e testar", () => EquipamentosUsados.Testar(e), 2.5f));
        else
        {
            if (e.condicao == CondicaoUsado.DefeitoLeve && !e.diagnosticado)
            {
                l.Add(new AcaoFisica("Espetar a placa POST e ligar", () => EquipamentosUsados.DiagnosticarPost(e), 2f, Item.PlacaPOST));
                l.Add(new AcaoFisica("Ligar o testador de fonte no conector de 24 pinos", () => EquipamentosUsados.DiagnosticarFonte(e), 2f, Item.TestadorFonte));
                l.Add(new AcaoFisica("Trocar peças de bancada até achar o defeito (sem ferramenta, demora)", () => EquipamentosUsados.DiagnosticarNoChute(e), 4f));
            }
            if (e.condicao == CondicaoUsado.DefeitoLeve && e.diagnosticado)
            {
                var item = EquipamentosUsados.PecaDaMochila(e.defeito);
                string rotulo = item != null ? $"Trocar a peça ruim: {EquipamentosUsados.NomeDefeito(e.defeito)}"
                                             : $"Trocar a peça ruim: {EquipamentosUsados.NomeDefeito(e.defeito)} (peça do balcão {UIFactory.Dinheiro(e.custoConserto)})";
                l.Add(new AcaoFisica(rotulo, () => EquipamentosUsados.TrocarPeca(e), 3f, item));
            }
            if (e.condicao == CondicaoUsado.Sucata && e.tipo == TipoUsado.Computador && !e.pecasAproveitadas)
                l.Add(new AcaoFisica("Desmontar e aproveitar o pente de memória", () => EquipamentosUsados.AproveitarPecas(e), 2.5f));
            if (e.condicao != CondicaoUsado.Sucata && !e.limpo)
                l.Add(new AcaoFisica("Limpar (pincel, ar comprimido e álcool isopropílico)", () => EquipamentosUsados.Limpar(e), 2.5f));
            if (e.condicao == CondicaoUsado.Bom && e.PrecisaApagar)
                l.Add(new AcaoFisica(e.tipo == TipoUsado.Impressora ? "Apagar a memória e restaurar o padrão de fábrica (LGPD)"
                                                                     : "Apagar os dados do antigo dono (formatação segura, LGPD)",
                    () => EquipamentosUsados.ApagarDados(e), 2f));
            if (e.condicao == CondicaoUsado.Bom && e.patrimonio == 0)
                l.Add(new AcaoFisica("Colar etiqueta de patrimônio", () => EquipamentosUsados.Etiquetar(e), 1f));
            if (e.condicao == CondicaoUsado.Bom && !e.anunciado)
                l.Add(new AcaoFisica($"Fotografar e anunciar à venda ({UIFactory.Dinheiro(EquipamentosUsados.ValorVenda(e))})", () => EquipamentosUsados.Anunciar(e), 1.5f));
            if (e.anunciado)
                l.Add(new AcaoFisica("Tirar o anúncio", () => { e.anunciado = false; EquipamentosUsados.AoMudar?.Invoke(e, "anunciar"); return "Anúncio retirado."; }));
            if (e.tipo == TipoUsado.Impressora && e.condicao == CondicaoUsado.Bom && !e.anunciado)
                l.Add(new AcaoFisica("Colocar na frota de locação (patrimônio)", () => EquipamentosUsados.ColocarNaFrota(e), 1.5f));
        }

        l.Add(new AcaoFisica(e.condicao == CondicaoUsado.Sucata && e.testado ? "Tirar da bancada (levar ao lixo eletrônico)" : "Tirar da bancada (pegar)",
            () => { EquipamentosUsados.AoMudar?.Invoke(e, "pegar"); return e.local == LocalUsado.Mao ? $"{e.nome} na mão." : null; }));
        return l;
    }

    public static string Descrever(EquipUsado e)
    {
        var sb = new System.Text.StringBuilder($"<b>{e.nome}</b>");
        if (!e.testado) sb.Append(": ainda não foi ligado. Pode estar bom, com defeito ou ser sucata.");
        else
        {
            sb.Append(e.condicao == CondicaoUsado.Bom ? $": funcionando{(e.recuperado ? " (recuperado)" : "")}, vale {UIFactory.Dinheiro(EquipamentosUsados.ValorVenda(e))}."
                    : e.condicao == CondicaoUsado.Sucata ? $": sucata ({EquipamentosUsados.NomeDefeito(e.defeito)})."
                    : e.diagnosticado ? $": {EquipamentosUsados.NomeDefeito(e.defeito)}."
                    : ": com defeito, peça ainda não descoberta.");
            if (e.temDados) sb.Append(e.dadosApagados ? " Dados apagados." : " <color=#F5B83F>Ainda tem dados do antigo dono.</color>");
        }
        if (!e.limpo && e.condicao != CondicaoUsado.Sucata) sb.Append(" Poeira e marcas de dedo na carcaça.");
        if (e.patrimonio > 0) sb.Append($" Etiqueta {e.Etiqueta}.");
        if (e.anunciado) sb.Append(" Anunciado: o comprador busca amanhã.");
        return sb.ToString();
    }
}
