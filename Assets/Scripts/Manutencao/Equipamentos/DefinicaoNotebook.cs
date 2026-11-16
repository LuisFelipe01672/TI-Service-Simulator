// Notebook do cliente (Etapa 7). Fica na mesa do computador da sala do cliente (SalaCliente).
// Estado: EstadoEquipamentos.Notebook.cs (+ .Software.cs). Ações: AcoesFisicas.Notebook.cs.
// Peças 3D e visual: EquipamentoPecas.Notebook.cs (criado em código; NotebookCliente cria o objeto na cena).
public class DefinicaoNotebook : DefinicaoEquipamento
{
    public override TipoEquipamento Tipo => TipoEquipamento.Notebook;
    public override string Nome => "Notebook";
    public override TipoChamado TipoChamado => TipoChamado.Notebook;
    public override string[] Pecas => new[] { "tela", "teclado", "energia", "fundo", "carregador", "saida_ar", "dobradica" };

    public override void Acoes(EstadoEquipamentos e, ListaAcoes l) => AcoesFisicas.Notebook(e, l);

    // Liga, carrega, digita, a tela fica de pé, não esquenta e o sistema está em ordem
    public override bool Funcionando(EstadoEquipamentos e) => e.nbLigado && e.NbHardwareOk && e.NbCarregando && e.SoftwareOk;

    public override bool Fechado(EstadoEquipamentos e) => !e.NbAberto && !e.nbBateriaFora;
    public override bool Ligado(EstadoEquipamentos e) => e.nbLigado;
}
