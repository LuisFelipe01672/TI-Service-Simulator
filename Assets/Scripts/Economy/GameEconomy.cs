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
        Debug.Log($"[Economy] Pagamento recebido: R${valor} | Saldo: R${saldo}");
    }

    // Mantido para não quebrar chamadas antigas, mas agora delega ao ReputationManager
    public void AlterarReputacao(float valor)
    {
        ReputationManager.Instance?.AlterarReputacao(valor);
    }

    void OnGUI()
    {
        float rep = ReputationManager.Instance != null
                    ? ReputationManager.Instance.GetReputacao() : 0f;

        GUI.Box(new Rect(10, 10, 180, 55), "");
        GUI.Label(new Rect(20, 15, 160, 25), $"💰 Saldo: R${saldo:F2}");
        GUI.Label(new Rect(20, 35, 160, 25), $"⭐ Reputação: {rep:F0}/100");
    }
}
