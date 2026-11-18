// Etapa 10: access point do projeto de Wi-Fi corporativo. Estado: EstadoEquipamentos.WiFi.cs.
// Ações: AcoesFisicas.WiFi. Peças 3D: EquipamentoPecas.WiFi. O objeto é criado em código (AccessPointCliente).
public class DefinicaoAccessPoint : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.AccessPoint;
    public override string Nome => "Access point";
    public override TipoChamado TipoChamado => TipoChamado.Rede;
    public override string[] Pecas => new[] { "forro", "caixa" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.AccessPoint(e, l);

    // Fora da visita do Wi-Fi não há AP; na visita, ele precisa estar no teto e no ar
    public override bool Funcionando(EstadoEquipamentos e) => !e.wifiServico || (e.apInstalado && e.ApLigado);

    // O rack (onde o cabo do AP chega) precisa ficar fechado
    public override bool Fechado(EstadoEquipamentos e) => !e.rackPortaAberta;
    public override bool Ligado(EstadoEquipamentos e) => !e.wifiServico || !e.apInstalado || e.apNoSwitch;
}
