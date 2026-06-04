using UnityEngine;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance;

    public float saldo = 0f;
    public float reputacao = 50f;

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

    public void AlterarReputacao(float valor)
    {
        reputacao = Mathf.Clamp(reputacao + valor, 0f, 100f);
        Debug.Log($"[Economy] Reputação: {reputacao}");
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 180, 55), "");
        GUI.Label(new Rect(20, 15, 160, 25), $"💰 Saldo: R${saldo:F2}");
        GUI.Label(new Rect(20, 35, 160, 25), $"⭐ Reputação: {reputacao:F0}/100");
    }
}