// Etapa 8: rack de parede do cliente (patch panel, organizador, switch) e os pontos de rede novos do projeto de
// cabeamento. Estado: EstadoEquipamentos.Rack.cs. Ações: AcoesFisicas.Rack. Peças 3D: EquipamentoPecas.Rack.
// O objeto é criado em código e fica na parede perto do roteador (RackCliente).
public class DefinicaoRack : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.Rack;
    public override string Nome => "Rack de rede";
    public override TipoChamado TipoChamado => TipoChamado.Rede;
    public override string[] Pecas => new[] { "rack", "porta", "patch", "organizador", "switch", "ponto1", "ponto2", "ponto3", "ponto4" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.Rack(e, l);

    // A rede do escritório passa pelo rack: todo cordão no lugar
    public override bool Funcionando(EstadoEquipamentos e) => e.RackFuncionando;

    public override bool Fechado(EstadoEquipamentos e) => !e.rackPortaAberta;
    public override bool Ligado(EstadoEquipamentos e) => true;
}
