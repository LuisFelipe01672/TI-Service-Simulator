using NaughtyAttributes;
using UnityEngine;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance;

    public float saldo = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ReceberPagamento(float valor)
    {
        saldo += valor;
        EventBus<SaldoAlteradoEvent>.Raise(new SaldoAlteradoEvent { saldo = saldo, delta = valor });
        Debug.Log($"[Economy] Pagamento recebido: R${valor} | Saldo: R${saldo}");
    }

    // Pagar uma despesa (salários, aluguel...). O saldo pode ficar negativo: é dívida.
    public void Gastar(float valor)
    {
        if (valor <= 0f) return;
        saldo -= valor;
        EventBus<SaldoAlteradoEvent>.Raise(new SaldoAlteradoEvent { saldo = saldo, delta = -valor });
        Debug.Log($"[Economy] Despesa: R${valor} | Saldo: R${saldo}");
    }

    public void DefinirSaldo(float valor)
    {
        float delta = valor - saldo;
        saldo = valor;
        EventBus<SaldoAlteradoEvent>.Raise(new SaldoAlteradoEvent { saldo = saldo, delta = delta });
    }

    [Button("Teste: +R$ 1.000")]
    void DebugMaisDinheiro() => ReceberPagamento(1000f);

    // Mantido para não quebrar chamadas antigas, mas agora delega ao ReputationManager
    public void AlterarReputacao(float valor)
    {
        ReputationManager.Instance?.AlterarReputacao(valor);
    }

    // O HUD agora é desenhado pela GameUI (Assets/Scripts/UI/GameUI.cs)
}
