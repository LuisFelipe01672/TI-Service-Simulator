using System.Collections;
using System.IO;
using UnityEngine;
using PP = UnityEngine.Rendering.PostProcessing;

// Só para o cômodo de prova gráfico (Assets/Scenes/Testes). No Play: mede o FPS por alguns segundos,
// salva fotos 1920x1080 com e sem pós-processamento em Builds/Capturas e encerra o Play.
public class CapturaTesteGrafico : MonoBehaviour
{
    public string prefixo = "BuiltIn";
    public float segundosMedicao = 6f;
    public Transform poseExtra;   // segunda foto (ex.: close na mesa)

    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);

        int quadros = 0;
        float t = 0f, pior = 0f;
        while (t < segundosMedicao)
        {
            yield return null;
            quadros++;
            t += Time.unscaledDeltaTime;
            pior = Mathf.Max(pior, Time.unscaledDeltaTime);
        }
        float media = quadros / t;
        Debug.Log($"[TesteGrafico] {prefixo}: FPS médio {media:0} (pior quadro {pior * 1000f:0.0} ms) " +
                  $"na janela {Screen.width}x{Screen.height} — GPU {SystemInfo.graphicsDeviceName}");

        var cam = GetComponent<Camera>();
        var camada = GetComponent<PP.PostProcessLayer>();
        var pasta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "Capturas");
        Directory.CreateDirectory(pasta);

        yield return Fotografar(cam, camada, pasta, "geral");
        if (poseExtra != null)
        {
            transform.SetPositionAndRotation(poseExtra.position, poseExtra.rotation);
            yield return null;
            yield return Fotografar(cam, camada, pasta, "mesa");
        }
        Debug.Log($"[TesteGrafico] Capturas salvas em {pasta}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator Fotografar(Camera cam, PP.PostProcessLayer camada, string pasta, string vista)
    {
        yield return Salvar(cam, Path.Combine(pasta, $"{prefixo}_{vista}_com_efeitos.png"));
        if (camada == null) yield break;
        camada.enabled = false;
        yield return null;
        yield return Salvar(cam, Path.Combine(pasta, $"{prefixo}_{vista}_sem_efeitos.png"));
        camada.enabled = true;
        yield return null;
    }

    // A câmera passa a desenhar na textura 1920x1080 por alguns quadros normais do jogo (um por vez —
    // a exposição automática e o TAA do HDRP precisam de histórico) e então a imagem é gravada.
    static IEnumerator Salvar(Camera cam, string caminho)
    {
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 1 };
        var anterior = cam.targetTexture;
        cam.targetTexture = rt;
        bool srp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        if (srp) for (int i = 0; i < 45; i++) yield return null;
        else cam.Render();
        yield return new WaitForEndOfFrame();

        // Converte HDR → LDR para gravar o PNG
        var ldr = RenderTexture.GetTemporary(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(rt, ldr);
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var ativo = RenderTexture.active;
        RenderTexture.active = ldr;
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        RenderTexture.active = ativo;
        File.WriteAllBytes(caminho, tex.EncodeToPNG());

        cam.targetTexture = anterior;
        RenderTexture.ReleaseTemporary(ldr);
        rt.Release();
        Destroy(rt);
        Destroy(tex);
    }
}
