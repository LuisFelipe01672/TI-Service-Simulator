using UnityEngine;

// Etapa 9D: grupo focal do semáforo da Jacarandás. Acende a lente da cor do momento (o mesmo relógio que os carros
// dos vizinhos respeitam: RotasTransito.CorSemaforo com VidaNaRua.TempoSemaforo).
public class SemaforoLuzes : MonoBehaviour
{
    public bool eixoAcacias;
    public Renderer[] lentes;          // vermelho, amarelo, verde
    public Material[] acesos, apagados;
    int corAtual = -1;
    float proxima;

    void Update()
    {
        if (Time.time < proxima) return;
        proxima = Time.time + 0.1f;
        int cor = RotasTransito.CorSemaforo(eixoAcacias, VidaNaRua.TempoSemaforo) switch
        {
            RotasTransito.Cor.Vermelho => 0,
            RotasTransito.Cor.Amarelo => 1,
            _ => 2,
        };
        if (cor == corAtual || lentes == null) return;
        corAtual = cor;
        for (int i = 0; i < lentes.Length && i < 3; i++)
            if (lentes[i] != null) lentes[i].sharedMaterial = i == cor ? acesos[i] : apagados[i];
    }
}
