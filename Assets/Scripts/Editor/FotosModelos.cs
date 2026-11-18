using System.IO;
using UnityEditor;
using UnityEngine;

// Ferramenta de conferência: põe cada modelo glTF de uma lista longe do cenário, mede o tamanho e tira fotos dos
// quatro lados (de +z, +x, -z, -x) em Builds/Capturas/Modelos, para descobrir para onde fica a frente de cada um.
public static class FotosModelos
{
    static readonly string[] Modelos =
    {
        "Casa_Comum_Brasileira", "Casa_Terrea", "Sobrado_Porta_Enrolar", "Sobrado_Tijolo", "Predio_Hospital", "Orelhao",
    };

    // Fotos ortográficas da fachada de um imóvel de cada modelo do bairro (80 px por metro, enquadrando os limites do
    // modelo), em Builds/Capturas/Fachadas: servem para medir onde ficam as janelas (luz acesa à noite, Etapa 9D).
    [MenuItem("TI Service/Teste gráfico/Fotos das fachadas (ortográficas)")]
    static void FotosFachadas()
    {
        var viz = GameObject.Find("Cenario/Vizinhanca");
        if (viz == null) { Debug.LogWarning("[Fotos] Monte o cenário antes."); return; }
        string pasta = Path.Combine(Application.dataPath, "../Builds/Capturas/Fachadas");
        Directory.CreateDirectory(pasta);
        var feitos = new System.Collections.Generic.HashSet<string>();
        var camGo = new GameObject("CamFotoFachada");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1f, 0f, 1f);
        cam.farClipPlane = 80f;
        const float pxm = 80f;
        foreach (Transform t in viz.transform)
        {
            if (feitos.Contains(t.name)) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 dir = MontarCenario.FachadaDe(b.center);
            if (dir == Vector3.zero) continue;
            feitos.Add(t.name);
            Vector3 direita = Vector3.Cross(Vector3.up, -dir);
            float largura = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(direita.x), 0f, Mathf.Abs(direita.z))));
            int w = Mathf.RoundToInt(largura * pxm), h = Mathf.RoundToInt(b.size.y * pxm);
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.orthographicSize = b.size.y / 2f;
            float fundo = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z))));
            cam.nearClipPlane = 0.01f; cam.farClipPlane = fundo * 2f + 0.6f;
            camGo.transform.SetPositionAndRotation(b.center + dir * (fundo + 0.3f), Quaternion.LookRotation(-dir));
            cam.Render();
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(pasta, t.name + ".png"), tex.EncodeToPNG());
            Debug.Log($"[Fotos] Fachada {t.name}: {largura:F2} x {b.size.y:F2} m ({w}x{h} px), base y {b.min.y:F2}, frente para {dir}");
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
        }
        Object.DestroyImmediate(camGo);
        Debug.Log("[Fotos] Fachadas em Builds/Capturas/Fachadas");
    }

    // Fotos do bairro montado na cena aberta (sem Play): vista de cima e da calçada, em Builds/Capturas/Bairro
    [MenuItem("TI Service/Teste gráfico/Fotos do bairro (cena aberta)")]
    static void FotosBairro()
    {
        string pasta = Path.Combine(Application.dataPath, "../Builds/Capturas/Bairro");
        Directory.CreateDirectory(pasta);
        var rt = new RenderTexture(1600, 900, 24);
        var camGo = new GameObject("CamFotoBairro");
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.fieldOfView = 60f;
        cam.farClipPlane = 600f;
        var principal = Camera.main;
        if (principal != null) { cam.clearFlags = principal.clearFlags; cam.backgroundColor = principal.backgroundColor; }
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var vistas = new (string nome, Vector3 de, Vector3 para)[]
        {
            ("01_aerea", new Vector3(0, 70, -75), new Vector3(0, 0, -5)),
            ("02_aerea_oeste", new Vector3(-45, 45, -55), new Vector3(-35, 0, -5)),
            ("03_aerea_leste", new Vector3(45, 45, -55), new Vector3(35, 0, -5)),
            ("04_porta_escritorio_leste", new Vector3(-1, 1.7f, -4.3f), new Vector3(30, 2, -6)),
            ("05_porta_escritorio_oeste", new Vector3(1, 1.7f, -4.3f), new Vector3(-30, 2, -6)),
            ("06_casa_muro", new Vector3(4, 1.7f, -7), new Vector3(0.5f, 1.5f, -16)),
            ("07_contabilidade", new Vector3(36, 1.7f, -10), new Vector3(42, 2.5f, -3)),
            ("08_loja", new Vector3(48, 1.7f, -8), new Vector3(42, 2, -15)),
            ("09_clinica", new Vector3(-36, 1.7f, -10), new Vector3(-42, 3, -3)),
            ("10_fornecedor", new Vector3(24, 1.7f, -24), new Vector3(32, 3, -30)),
            ("11_coleta", new Vector3(-24, 1.7f, -24), new Vector3(-32, 3, -30)),
            ("12_cruzamento_ipe", new Vector3(-20, 1.7f, -14), new Vector3(-30, 4, 10)),
        };
        foreach (var (nome, de, para) in vistas)
        {
            camGo.transform.position = de;
            camGo.transform.LookAt(para);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(pasta, nome + ".png"), tex.EncodeToPNG());
        }
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(tex);
        rt.Release();
        Debug.Log("[Fotos] Fotos do bairro em Builds/Capturas/Bairro");
    }

    [MenuItem("TI Service/Teste gráfico/Fotos dos modelos do bairro (4 lados)")]
    static void Fotografar()
    {
        string pasta = Path.Combine(Application.dataPath, "../Builds/Capturas/Modelos");
        Directory.CreateDirectory(pasta);
        var rt = new RenderTexture(800, 600, 24);
        var camGo = new GameObject("CamFotoModelo");
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.6f, 0.65f);
        var luzGo = new GameObject("LuzFotoModelo");
        var luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.2f;
        luzGo.transform.rotation = Quaternion.Euler(50, 30, 0);
        var tex = new Texture2D(800, 600, TextureFormat.RGB24, false);

        foreach (var nome in Modelos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/Sketchfab/{nome}/{nome}.gltf");
            if (prefab == null) { Debug.LogWarning("[Fotos] Sem modelo: " + nome); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(0, -500, 0);
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log($"[Fotos] {nome}: tamanho {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} (x, y, z) · {rs.Length} malhas");
            float raio = b.extents.magnitude;
            var lados = new (string n, Vector3 d)[] { ("de_+z", Vector3.forward), ("de_+x", Vector3.right), ("de_-z", Vector3.back), ("de_-x", Vector3.left) };
            foreach (var (n, d) in lados)
            {
                camGo.transform.position = b.center + d * raio * 2.2f + Vector3.up * raio * 0.5f;
                camGo.transform.LookAt(b.center);
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(pasta, $"{nome}_{n}.png"), tex.EncodeToPNG());
            }
            Object.DestroyImmediate(go);
        }
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(luzGo);
        Object.DestroyImmediate(tex);
        rt.Release();
        Debug.Log("[Fotos] Fotos em Builds/Capturas/Modelos");
    }
}
