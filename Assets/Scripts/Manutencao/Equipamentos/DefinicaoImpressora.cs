// Impressora do cliente (multifuncional laser). Estado: EstadoEquipamentos.Impressora.cs (+ .Locacao.cs).
// Ações: AcoesFisicas.Impressora. Peças 3D: EquipamentoPecas.MontarImpressora.
public class DefinicaoImpressora : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.Impressora;
    public override string Nome => "Impressora";
    public override string Artigo => "a";
    public override TipoChamado TipoChamado => TipoChamado.Impressora;
    public override string[] Pecas => new[] { "visor", "traseira", "tampa", "cartucho", "botao", "energia", "gaveta" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.Impressora(e, l);

    // Imprime pela rede, do computador, e a folha sai perfeita
    public override bool Funcionando(EstadoEquipamentos e) =>
        e.impLigada && !e.impTampaAberta && e.impCartuchoPresente && !e.impCartuchoComLacre && e.impToner >= 0.1f &&
        !e.impPapelAtolado && !e.impSemPapel && !e.impGavetaAberta && !e.impCilindroRiscado &&
        !e.impTampaMalFechada && !e.impIpMudou && !e.impDriverErrado &&
        e.spoolerRodando && !e.FilaTravada && e.ImpressoraOnline && e.PcNaRede;

    public override bool Fechado(EstadoEquipamentos e) => !e.impTampaAberta && !e.impGavetaAberta && !e.cartuchoNaMao;
    public override bool Ligado(EstadoEquipamentos e) => e.impLigada;
}
