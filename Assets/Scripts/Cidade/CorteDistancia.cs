using UnityEngine;

// Etapa 10E-2 (cidade ~7x, placa de 4 GB): distância de corte por camada na câmera principal. Muros, postes e faixas
// pintadas (camada "Detalhe") somem a 110 m; fios, lixeiras, bancos e pinturas do chão ("Miudo") a 55 m; o resto vai
// até o fim da câmera (as casas que ficam atrás de outras o occlusion culling já tira). Confere a câmera a cada segundo
// (a do carro e a do jogador podem trocar). Casas e sobrados dos lotes ("Imovel") somem a 150 m.
public class CorteDistancia : MonoBehaviour
{
    public const int CamadaDetalhe = 8, CamadaMiudo = 9, CamadaImovel = 10;
    public const float DistDetalhe = 110f, DistMiudo = 55f, DistImovel = 150f, FimCamera = 450f;

    Camera ultima;
    float proxima;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        if (FindFirstObjectByType<CorteDistancia>() != null) return;
        var go = new GameObject("CorteDistancia");
        DontDestroyOnLoad(go);
        go.AddComponent<CorteDistancia>();
    }

    public static float[] Distancias()
    {
        var d = new float[32];
        d[CamadaDetalhe] = DistDetalhe;
        d[CamadaMiudo] = DistMiudo;
        d[CamadaImovel] = DistImovel;
        return d;
    }

    void Update()
    {
        if (Time.unscaledTime < proxima) return;
        proxima = Time.unscaledTime + 1f;
        var cam = Camera.main;
        if (cam == null || cam == ultima) return;
        ultima = cam;
        cam.layerCullDistances = Distancias();
        cam.layerCullSpherical = true;
        if (cam.farClipPlane > FimCamera) cam.farClipPlane = FimCamera;
    }
}
