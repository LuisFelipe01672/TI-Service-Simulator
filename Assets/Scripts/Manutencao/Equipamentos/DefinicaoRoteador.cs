// Roteador do cliente. Estado: EstadoEquipamentos.Roteador.cs. Ações: AcoesFisicas.Roteador.
// Peças 3D: EquipamentoPecas.MontarRoteador.
public class DefinicaoRoteador : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.Roteador;
    public override string Nome => "Roteador";
    public override TipoChamado TipoChamado => TipoChamado.Rede;
    public override string[] Pecas => new[] { "luzes", "etiqueta", "fonte", "reset" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.Roteador(e, l);

    // Internet e Wi-Fi de todo mundo de volta
    public override bool Funcionando(EstadoEquipamentos e) =>
        e.PcNaInternet && e.wifiAtivo && e.rotConfigOk && !e.rotTravado && !e.dhcpDuplo;

    public override bool Fechado(EstadoEquipamentos e) => true;
    public override bool Ligado(EstadoEquipamentos e) => e.rotNaTomada;
}
