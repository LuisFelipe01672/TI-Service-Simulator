using UnityEngine;
using UnityEngine.Serialization;

// Etapa 9D: grupo focal de um semáforo. Acende a lente da cor do momento (o mesmo relógio que os carros dos vizinhos
// respeitam: RotasTransito.CorSemaforo com VidaNaRua.TempoSemaforo). 'eixoPrincipal': virado para quem vem pela rua
// principal do cruzamento (a do verde longo).
public class SemaforoLuzes : MonoBehaviour
{
    [FormerlySerializedAs("eixoAcacias")] public bool eixoPrincipal;
    public Renderer[] lentes;          // vermelho, amarelo, verde
    public Material[] acesos, apagados;
    int corAtual = -1;
    float proxima;

    void Update()
    {
        if (Time.time < proxima) return;
        proxima = Time.time + 0.1f;
        int cor = RotasTransito.CorSemaforo(eixoPrincipal,VidaNaRua.TempoSemaforo) switch
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
