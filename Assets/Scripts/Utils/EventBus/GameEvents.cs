// Eventos do TI Service Simulator.
// Qualquer script pode "ouvir" um evento sem depender de quem o dispara:
//   var binding = new EventBinding<SaldoAlteradoEvent>(e => Debug.Log(e.saldo));
//   EventBus<SaldoAlteradoEvent>.Register(binding);   // no OnEnable
//   EventBus<SaldoAlteradoEvent>.Deregister(binding); // no OnDisable

public interface IEvent { }

public struct SaldoAlteradoEvent : IEvent
{
    public float saldo;
    public float delta;
}

public struct ReputacaoAlteradaEvent : IEvent
{
    public float reputacao;
    public float delta;
}

public struct FaseDesbloqueadaEvent : IEvent
{
    public int fase;
}

public struct ChamadoAceitoEvent : IEvent
{
    public Chamado chamado;
}

public struct ChamadoConcluidoEvent : IEvent
{
    public Chamado chamado;
    public float tempoGasto;
    public bool cometeuErro;
    public bool porFuncionario;
}

public struct AcaoErradaEvent : IEvent
{
    public string acao;
    public float penalidade;
}

public struct FuncionarioContratadoEvent : IEvent
{
    public Funcionario funcionario;
}
