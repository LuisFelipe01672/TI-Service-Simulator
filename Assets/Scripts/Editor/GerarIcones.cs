using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Etapa 10D: fotos dos itens da loja (pedido do colega testador: "imagens do que está comprando"). Regra "pronto
// antes do zero": usa os modelos 3D que o jogo já tem (notebook, multímetro, pendrive e cartucho do Sketchfab, CC-BY,
// ver CREDITOS.md) e, para o resto, miniaturas em primitivas com a cara da peça de verdade. Cada item é montado no
// alto do céu, fotografado com fundo transparente e salvo em Assets/Resources/Icones/<Item>.png (256 x 256).
// Menu: TI Service/Gerar ícones da loja (com o Play desligado).
public static class GerarIcones
{
    const string Pasta = "Assets/Resources/Icones";
    const string PastaSketchfab = "Assets/Models/Sketchfab/";
    static readonly Vector3 Palco = new Vector3(0f, 600f, 0f);

    static Material M(Color c, float brilho = 0.45f, float metal = 0f)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Glossiness", brilho);
        m.SetFloat("_Metallic", metal);
        return m;
    }

    static Transform P(Transform pai, PrimitiveType t, Vector3 pos, Vector3 esc, Material m, Vector3? giro = null)
    {
        var g = GameObject.CreatePrimitive(t);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;
        if (giro != null) g.transform.localRotation = Quaternion.Euler(giro.Value);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }

    // Modelo do Sketchfab na escala real: o maior lado vira 'comprimento' metros, centrado
    static Transform Modelo(Transform pai, string nome, float comprimento, float giroY = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaSketchfab + nome + "/" + nome + ".gltf");
        if (prefab == null) return null;
        return ModelosFerramentas.Instanciar(prefab, pai, comprimento, Quaternion.Euler(0f, giroY, 0f));
    }

    // Limites pelas malhas (renderer.bounds fica velho no editor logo depois de criar/mover)
    static Bounds LimitesMundo(Transform t)
    {
        bool tem = false;
        var b = new Bounds();
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var canto = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = r.transform.TransformPoint(canto);
                if (!tem) { b = new Bounds(p, Vector3.zero); tem = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }

    [MenuItem("TI Service/Gerar ícones da loja", false, 52)]
    public static void Gerar()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Ícones] Pare o Play antes."); return; }
        Directory.CreateDirectory(Pasta);
        var preto = M(new Color(0.07f, 0.07f, 0.08f), 0.5f);
        var cinza = M(new Color(0.35f, 0.36f, 0.38f), 0.4f);
        var metal = M(new Color(0.78f, 0.79f, 0.81f), 0.7f, 0.85f);
        var ouro = M(new Color(0.9f, 0.72f, 0.25f), 0.7f, 0.9f);
        var verdePcb = M(new Color(0.08f, 0.38f, 0.18f), 0.5f);
        var azul = M(new Color(0.18f, 0.38f, 0.85f), 0.45f);
        var amarelo = M(new Color(0.96f, 0.76f, 0.1f), 0.45f);
        var vermelho = M(new Color(0.85f, 0.16f, 0.12f), 0.45f);
        var papelao = M(new Color(0.66f, 0.5f, 0.32f), 0.1f);
        var branco = M(new Color(0.94f, 0.94f, 0.93f), 0.4f);
        var vidro = M(new Color(0.85f, 0.9f, 0.95f), 0.9f);
        var ledVerde = M(new Color(0.2f, 0.95f, 0.4f), 0.6f);
        ledVerde.EnableKeyword("_EMISSION"); ledVerde.SetColor("_EmissionColor", new Color(0.2f, 0.95f, 0.4f) * 1.5f);
        var visor = M(new Color(0.55f, 0.75f, 0.55f), 0.3f);

        var receitas = new Dictionary<Item, System.Action<Transform>>
        {
            [Item.Notebook] = t => { var m = Modelo(t, "Notebook", 0.34f, 180f); if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.34f, 0.02f, 0.24f), cinza); },
            [Item.Multimetro] = t => { var m = Modelo(t, "Multimetro", 0.16f, 180f); if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.16f, 0.04f), amarelo); },
            [Item.PendriveDrivers] = t => { var m = Modelo(t, "Pendrive", 0.06f); if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.02f, 0.06f, 0.008f), azul); },
            [Item.PendriveDiagnostico] = t =>
            {
                var m = Modelo(t, "Pendrive", 0.06f);
                if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.02f, 0.06f, 0.008f), vermelho);
                P(t, PrimitiveType.Cube, new Vector3(0.03f, -0.03f, 0f), new Vector3(0.02f, 0.02f, 0.004f), vermelho);   // etiqueta vermelha "DIAG"
            },
            [Item.Toner] = t => { var m = Modelo(t, "Cartucho_Toner", 0.32f); if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.32f, 0.1f, 0.12f), preto); },
            [Item.TonerUsado] = t =>
            {
                var m = Modelo(t, "Cartucho_Toner", 0.32f); if (m == null) P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.32f, 0.1f, 0.12f), preto);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.004f, 0.06f), M(new Color(0.55f, 0.53f, 0.5f), 0.05f));   // pó
            },
            [Item.Cilindro] = t =>
            {
                P(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.05f, 0.16f, 0.05f), M(new Color(0.1f, 0.5f, 0.25f), 0.8f), new Vector3(0f, 0f, 90f));
                P(t, PrimitiveType.Cylinder, new Vector3(0.165f, 0f, 0f), new Vector3(0.03f, 0.008f, 0.03f), preto, new Vector3(0f, 0f, 90f));
                P(t, PrimitiveType.Cylinder, new Vector3(-0.165f, 0f, 0f), new Vector3(0.03f, 0.008f, 0.03f), preto, new Vector3(0f, 0f, 90f));
            },
            [Item.TestadorCabo] = t =>
            {
                P(t, PrimitiveType.Cube, new Vector3(-0.05f, 0f, 0f), new Vector3(0.08f, 0.13f, 0.03f), cinza);
                P(t, PrimitiveType.Cube, new Vector3(0.05f, -0.02f, 0f), new Vector3(0.05f, 0.09f, 0.03f), cinza);
                for (int i = 0; i < 8; i++) P(t, PrimitiveType.Cube, new Vector3(-0.07f + (i % 2) * 0.04f, 0.045f - (i / 2) * 0.022f, -0.016f), new Vector3(0.008f, 0.008f, 0.003f), ledVerde);
                P(t, PrimitiveType.Cube, new Vector3(-0.05f, 0.075f, 0f), new Vector3(0.02f, 0.015f, 0.016f), vidro);
            },
            [Item.AlicateCrimpagem] = t =>
            {
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.06f, 0.06f, 0.02f), metal);
                P(t, PrimitiveType.Cube, new Vector3(-0.02f, -0.04f, 0f), new Vector3(0.022f, 0.17f, 0.022f), vermelho, new Vector3(0f, 0f, -10f));
                P(t, PrimitiveType.Cube, new Vector3(0.025f, -0.04f, 0f), new Vector3(0.022f, 0.17f, 0.022f), preto, new Vector3(0f, 0f, 12f));
            },
            [Item.ConectorRJ45] = t =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var c = P(t, PrimitiveType.Cube, new Vector3(-0.03f + i * 0.03f, 0f, i * 0.01f), new Vector3(0.014f, 0.012f, 0.022f), vidro, new Vector3(0f, i * 25f, 0f));
                    P(c, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.2f), new Vector3(0.8f, 0.05f, 0.4f), ouro);
                }
            },
            [Item.CaboRede] = t =>
            {
                for (int i = 0; i < 18; i++)
                {
                    float a = i / 18f * Mathf.PI * 2f;
                    P(t, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.07f, Mathf.Sin(a) * 0.07f, 0f), new Vector3(0.008f, 0.0135f, 0.008f), azul, new Vector3(0f, 0f, a * Mathf.Rad2Deg));
                }
                P(t, PrimitiveType.Cube, new Vector3(0.09f, -0.06f, 0f), new Vector3(0.014f, 0.022f, 0.012f), vidro);
            },
            [Item.Clipe] = t =>
            {
                P(t, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.004f, 0.04f, 0.004f), metal);
                P(t, PrimitiveType.Cylinder, new Vector3(0.012f, 0.005f, 0f), new Vector3(0.004f, 0.035f, 0.004f), metal);
                P(t, PrimitiveType.Cylinder, new Vector3(0.006f, 0.04f, 0f), new Vector3(0.004f, 0.006f, 0.004f), metal, new Vector3(0f, 0f, 90f));
                P(t, PrimitiveType.Cylinder, new Vector3(0.006f, -0.04f, 0f), new Vector3(0.004f, 0.006f, 0.004f), metal, new Vector3(0f, 0f, 90f));
            },
            [Item.CaixaCabo] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.36f, 0.2f), papelao);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.101f), new Vector3(0.22f, 0.12f, 0.002f), branco);
                P(t, PrimitiveType.Cylinder, new Vector3(0.1f, -0.12f, -0.12f), new Vector3(0.012f, 0.05f, 0.012f), azul, new Vector3(90f, 0f, 0f));
            },
            [Item.Memoria] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.13f, 0.03f, 0.004f), verdePcb);
                for (int i = 0; i < 8; i++) P(t, PrimitiveType.Cube, new Vector3(-0.055f + i * 0.0157f, 0.002f, -0.003f), new Vector3(0.011f, 0.012f, 0.002f), preto);
                P(t, PrimitiveType.Cube, new Vector3(0f, -0.0135f, -0.0021f), new Vector3(0.12f, 0.003f, 0.0005f), ouro);
            },
            [Item.Fonte] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.15f, 0.086f, 0.14f), M(new Color(0.15f, 0.15f, 0.16f), 0.5f, 0.4f));
                P(t, PrimitiveType.Cylinder, new Vector3(0f, 0.044f, 0f), new Vector3(0.11f, 0.002f, 0.11f), cinza);
                P(t, PrimitiveType.Cylinder, new Vector3(0f, 0.046f, 0f), new Vector3(0.03f, 0.002f, 0.03f), preto);
                P(t, PrimitiveType.Cube, new Vector3(0.09f, -0.01f, -0.04f), new Vector3(0.06f, 0.02f, 0.02f), M(new Color(0.9f, 0.85f, 0.1f)));
            },
            [Item.ArComprimido] = t =>
            {
                P(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.06f, 0.08f, 0.06f), azul);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.025f, 0.02f, 0.03f), preto);
                P(t, PrimitiveType.Cylinder, new Vector3(0f, 0.095f, -0.05f), new Vector3(0.004f, 0.04f, 0.004f), vermelho, new Vector3(90f, 0f, 0f));
            },
            [Item.PastaTermica] = t =>
            {
                P(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.018f, 0.05f, 0.018f), M(new Color(0.75f, 0.76f, 0.78f), 0.8f), new Vector3(0f, 0f, 70f));
                P(t, PrimitiveType.Cylinder, new Vector3(0.055f, 0.02f, 0f), new Vector3(0.006f, 0.012f, 0.006f), branco, new Vector3(0f, 0f, 70f));
                P(t, PrimitiveType.Cylinder, new Vector3(-0.07f, -0.026f, 0f), new Vector3(0.014f, 0.025f, 0.014f), branco, new Vector3(0f, 0f, 70f));
            },
            [Item.SSD] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.07f, 0.1f, 0.008f), M(new Color(0.08f, 0.08f, 0.1f), 0.6f));
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.01f, -0.0045f), new Vector3(0.05f, 0.04f, 0.001f), M(new Color(0.25f, 0.55f, 0.95f)));
            },
            [Item.PlacaPOST] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.12f, 0.06f, 0.003f), verdePcb);
                P(t, PrimitiveType.Cube, new Vector3(0.02f, 0.008f, -0.003f), new Vector3(0.04f, 0.022f, 0.003f), M(new Color(0.9f, 0.15f, 0.1f), 0.6f));
                P(t, PrimitiveType.Cube, new Vector3(0f, -0.032f, -0.001f), new Vector3(0.09f, 0.006f, 0.001f), ouro);
            },
            [Item.TestadorFonte] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.09f, 0.13f, 0.03f), amarelo);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.035f, -0.016f), new Vector3(0.06f, 0.03f, 0.002f), visor);
                for (int i = 0; i < 5; i++) P(t, PrimitiveType.Cube, new Vector3(-0.03f + i * 0.015f, -0.01f, -0.016f), new Vector3(0.008f, 0.008f, 0.003f), ledVerde);
                P(t, PrimitiveType.Cube, new Vector3(0f, -0.05f, -0.016f), new Vector3(0.07f, 0.015f, 0.004f), preto);
            },
            [Item.BateriaCMOS] = t => P(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.05f, 0.0035f, 0.05f), metal, new Vector3(70f, 0f, 0f)),
            [Item.PecaVelha] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.12f, 0.06f, 0.1f), M(new Color(0.4f, 0.38f, 0.34f), 0.05f));
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.032f, 0f), new Vector3(0.1f, 0.006f, 0.08f), M(new Color(0.55f, 0.52f, 0.47f), 0.02f));
            },
            [Item.BateriaNotebook] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.012f, 0.07f), M(new Color(0.15f, 0.17f, 0.2f), 0.5f));
                P(t, PrimitiveType.Cube, new Vector3(-0.04f, 0.007f, 0f), new Vector3(0.08f, 0.001f, 0.04f), branco);
                P(t, PrimitiveType.Cube, new Vector3(0.1f, 0.008f, 0.02f), new Vector3(0.02f, 0.004f, 0.012f), M(new Color(0.9f, 0.9f, 0.9f)));
            },
            [Item.CarregadorNotebook] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.11f, 0.03f, 0.05f), preto);
                P(t, PrimitiveType.Cube, new Vector3(-0.01f, 0.016f, 0f), new Vector3(0.05f, 0.001f, 0.03f), cinza);
                P(t, PrimitiveType.Cylinder, new Vector3(0.1f, 0f, 0f), new Vector3(0.006f, 0.05f, 0.006f), preto, new Vector3(0f, 0f, 90f));
                P(t, PrimitiveType.Cylinder, new Vector3(0.155f, 0f, 0f), new Vector3(0.01f, 0.01f, 0.01f), metal, new Vector3(0f, 0f, 90f));
            },
            [Item.DobradicaNotebook] = t =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    P(t, PrimitiveType.Cube, new Vector3(s * 0.04f, 0.02f, 0f), new Vector3(0.025f, 0.05f, 0.004f), metal);
                    P(t, PrimitiveType.Cube, new Vector3(s * 0.04f, -0.008f, 0.015f), new Vector3(0.025f, 0.004f, 0.035f), metal);
                    P(t, PrimitiveType.Cylinder, new Vector3(s * 0.04f, -0.006f, 0f), new Vector3(0.01f, 0.014f, 0.01f), cinza, new Vector3(0f, 0f, 90f));
                }
            },
            [Item.Etiquetadora] = t =>
            {
                P(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.08f, 0.17f, 0.04f), cinza);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.021f), new Vector3(0.06f, 0.03f, 0.002f), visor);
                for (int i = 0; i < 12; i++) P(t, PrimitiveType.Cube, new Vector3(-0.024f + (i % 4) * 0.016f, 0.0f - (i / 4) * 0.018f, -0.021f), new Vector3(0.012f, 0.012f, 0.003f), preto);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.03f, 0.012f, 0.004f), branco);
            },
            [Item.PunchDown] = t =>
            {
                P(t, PrimitiveType.Cylinder, new Vector3(0f, -0.03f, 0f), new Vector3(0.035f, 0.07f, 0.035f), amarelo);
                P(t, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.012f, 0.03f, 0.012f), metal);
                P(t, PrimitiveType.Cube, new Vector3(0f, 0.095f, 0f), new Vector3(0.004f, 0.012f, 0.01f), metal);
            },
        };

        // Palco: câmera, luz principal e de contorno
        var raiz = new GameObject("PalcoIcones").transform;
        raiz.position = Palco;
        var camGo = new GameObject("CamIcone");
        camGo.transform.SetParent(raiz, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView = 25f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 20f;
        cam.allowHDR = false;
        cam.enabled = false;
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(raiz, false);
        luz.type = LightType.Directional;
        luz.intensity = 1.1f;
        luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        var luz2 = new GameObject("Contorno").AddComponent<Light>();
        luz2.transform.SetParent(raiz, false);
        luz2.type = LightType.Directional;
        luz2.intensity = 0.5f;
        luz2.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        int feitos = 0;
        try
        {
            foreach (var kv in receitas)
            {
                var obj = new GameObject("Icone_" + kv.Key).transform;
                obj.SetParent(raiz, false);
                kv.Value(obj);
                // Enquadra: centraliza e ajusta a distância pelo tamanho (vista de 3/4, de cima)
                if (obj.GetComponentsInChildren<Renderer>().Length == 0) { Object.DestroyImmediate(obj.gameObject); continue; }
                var b = LimitesMundo(obj);
                obj.position -= b.center - Palco;
                float raio = b.extents.magnitude;
                // de frente (-z), um pouco de lado e de cima
                Vector3 dir = Quaternion.Euler(24f, 28f, 0f) * Vector3.back;
                cam.transform.position = Palco + dir * (raio / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.0f;
                cam.transform.LookAt(Palco);
                cam.targetTexture = rt;
                cam.Render();
                var antes = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                tex.Apply();
                RenderTexture.active = antes;
                File.WriteAllBytes($"{Pasta}/{kv.Key}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(obj.gameObject);
                feitos++;
            }
        }
        finally
        {
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(raiz.gameObject);
        }
        AssetDatabase.Refresh();
        foreach (var kv in receitas)
        {
            var imp = AssetImporter.GetAtPath($"{Pasta}/{kv.Key}.png") as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Default;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 256;
            imp.SaveAndReimport();
        }
        int faltam = System.Enum.GetValues(typeof(Item)).Length - feitos;
        Debug.Log($"[Ícones] {feitos} ícones em {Pasta}" + (faltam > 0 ? $" ({faltam} item(ns) sem receita: ficam com o ícone padrão)" : "."));
    }
}
