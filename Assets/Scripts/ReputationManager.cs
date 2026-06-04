using UnityEngine;
using UnityEngine.Events;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance;

    [Header("Configuração")]
    [Range(0f, 100f)] public float reputacaoInicial = 50f;
    public float punicaoErro = 20f;
    public float punicaoAtraso = 10f;

    [Header("Estado atual")]
    [SerializeField] private float reputacao;

    public int FaseAtual { get; private set; } = 1;

    public UnityEvent<float> onReputacaoAlterada;
    public UnityEvent<int>   onFaseDesbloqueada;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        reputacao = reputacaoInicial;
    }

    public void AvaliarChamado(float tempoGasto, float tempoLimite, bool cometeuErro)
    {
        float delta = 0f;

        if (cometeuErro)
        {
            delta = -punicaoErro;
            Debug.Log("[Reputação] Erro cometido: -" + punicaoErro);
        }
        else
        {
            float razao = Mathf.Clamp01(tempoGasto / tempoLimite);

            if (razao <= 0.5f)       delta = 15f;
            else if (razao <= 0.75f) delta = 10f;
            else if (razao <= 1.0f)  delta = 5f;
            else                     delta = -punicaoAtraso;

            Debug.Log($"[Reputação] Chamado ok. Razão tempo: {razao:F2} → {delta:+0;-0}");
        }

        AlterarReputacao(delta);
    }

    public void AlterarReputacao(float delta)
    {
        reputacao = Mathf.Clamp(reputacao + delta, 0f, 100f);
        onReputacaoAlterada?.Invoke(reputacao);
        VerificarFase();

        Debug.Log($"[Reputação] {reputacao:F1}/100 (fase {FaseAtual})");
    }

    void VerificarFase()
    {
        int novaFase = reputacao >= 60f ? 3 :
                       reputacao >= 30f ? 2 : 1;

        if (novaFase > FaseAtual)
        {
            FaseAtual = novaFase;
            onFaseDesbloqueada?.Invoke(FaseAtual);
            Debug.Log($"[Reputação] FASE {FaseAtual} DESBLOQUEADA!");
        }
    }

    public float GetReputacao() => reputacao;
}