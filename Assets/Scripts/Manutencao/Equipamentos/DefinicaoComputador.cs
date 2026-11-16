// Computador de mesa do cliente (gabinete + monitor). Estado: EstadoEquipamentos.Computador.cs (+ .Saude.cs).
// Ações: AcoesFisicas.Computador (+ .Diagnostico.cs). Peças 3D: EquipamentoPecas.MontarComputador.
public class DefinicaoComputador : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.Computador;
    public override string Nome => "Computador";
    public override TipoChamado TipoChamado => TipoChamado.Notebook;
    public override string[] Pecas => new[] { "tela", "traseira", "energia", "gabinete" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.Computador(e, l);

    // Navega, sem travar, com o hardware em ordem (e, no check-up, com o laudo inteiro)
    public override bool Funcionando(EstadoEquipamentos e) =>
        e.PcNaInternet && e.memoria < 0.8f && e.PcHardwareOk && (!e.checkup || e.LaudoCompleto);

    public override bool Fechado(EstadoEquipamentos e) => !e.pcGabineteAberto && !e.pcCoolerFora;
    public override bool Ligado(EstadoEquipamentos e) => e.pcLigado;
}
