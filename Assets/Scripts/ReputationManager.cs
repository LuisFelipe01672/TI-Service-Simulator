using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance;

    [Header("Configuração")]
    [Range(0f, 100f)] public float reputacaoInicial = 20f;
    public float punicaoErro = 20f;
    public float punicaoAtraso = 10f;

    [Header("Ganho por chamado resolvido")]
    [Tooltip("Resolveu usando até metade do prazo")] public float ganhoRapido = 6f;
    [Tooltip("Resolveu usando até 75% do prazo")] public float ganhoNormal = 4f;
    [Tooltip("Resolveu perto do fim do prazo")] public float ganhoNoLimite = 2f;

    [Header("Fases (reputação mínima)")]
    public float reputacaoFase2 = 30f;
    public float reputacaoFase3 = 60f;

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
        FaseAtual = CalcularFase(reputacao); // fase coerente com a reputação inicial, sem disparar aviso
    }

    // multiplicador: clientes VIP valem mais (ganho e perda)
    public void AvaliarChamado(float tempoGasto, float tempoLimite, bool cometeuErro, float multiplicador = 1f)
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

            if (razao <= 0.5f)       delta = ganhoRapido;
            else if (razao <= 0.75f) delta = ganhoNormal;
            else if (razao < 1.0f)   delta = ganhoNoLimite;
            else                     delta = -punicaoAtraso;

            Debug.Log($"[Reputação] Chamado ok. Razão tempo: {razao:F2} → {delta:+0;-0}");
        }

        AlterarReputacao(delta * multiplicador);
    }

    public void AlterarReputacao(float delta)
    {
        float anterior = reputacao;
        reputacao = Mathf.Clamp(reputacao + delta, 0f, 100f);
        onReputacaoAlterada?.Invoke(reputacao);
        EventBus<ReputacaoAlteradaEvent>.Raise(new ReputacaoAlteradaEvent { reputacao = reputacao, delta = reputacao - anterior });
        VerificarFase();

        Debug.Log($"[Reputação] {reputacao:F1}/100 (fase {FaseAtual})");
    }

    void VerificarFase()
    {
        int novaFase = CalcularFase(reputacao);

        if (novaFase > FaseAtual)
        {
            FaseAtual = novaFase;
            onFaseDesbloqueada?.Invoke(FaseAtual);
            EventBus<FaseDesbloqueadaEvent>.Raise(new FaseDesbloqueadaEvent { fase = FaseAtual });
            Debug.Log($"[Reputação] FASE {FaseAtual} DESBLOQUEADA!");
        }
    }

    public float GetReputacao() => reputacao;

    int CalcularFase(float rep) => rep >= reputacaoFase3 ? 3 : rep >= reputacaoFase2 ? 2 : 1;

    // Usado pelo SaveManager ao carregar o jogo
    public void DefinirEstado(float novaReputacao, int fase)
    {
        float anterior = reputacao;
        reputacao = Mathf.Clamp(novaReputacao, 0f, 100f);
        FaseAtual = Mathf.Clamp(fase, 1, 3);
        onReputacaoAlterada?.Invoke(reputacao);
        EventBus<ReputacaoAlteradaEvent>.Raise(new ReputacaoAlteradaEvent { reputacao = reputacao, delta = reputacao - anterior });
    }

    [Button("Teste: +20 reputação")]
    void DebugMaisReputacao() => AlterarReputacao(20f);

    [Button("Teste: -20 reputação")]
    void DebugMenosReputacao() => AlterarReputacao(-20f);
}