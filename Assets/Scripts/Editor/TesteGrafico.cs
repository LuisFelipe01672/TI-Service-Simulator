using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using PP = UnityEngine.Rendering.PostProcessing;

// Cômodo de prova (home office) para comparar pipelines gráficos (Fase 0 do ROADMAP).
// Menu: TI Service/Teste gráfico. Usa os mesmos modelos e texturas do jogo.
public static class TesteGrafico
{
    const string PastaCena = "Assets/Scenes/Testes";
    const string CaminhoCena = PastaCena + "/TesteGrafico_HomeOffice.unity";
    const string Mats = "Assets/Materials/Cenario/";
    const string Kenney = "Assets/Models/Kenney/Furniture/";
    const string Sketchfab = "Assets/Models/Sketchfab/";

    const float L = 4.0f, P = 3.6f, A = 2.7f;   // largura (x), profundidade (z), altura

    [MenuItem("TI Service/Teste gráfico/1. Montar cômodo de prova", priority = 1300)]
    public static void Montar()
    {
        if (!AssetDatabase.IsValidFolder(PastaCena)) AssetDatabase.CreateFolder("Assets/Scenes", "Testes");
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var raiz = new GameObject("HomeOffice").transform;

        // ---- Materiais (os mesmos do jogo, com repetição de textura por metro) ----
        var piso = Repetir(Carregar("Piso_Madeira"), 2.2f, 2f);
        var parede = Repetir(Carregar("Parede_Casa"), 2f, 1.5f); if (parede != null) parede.color = new Color(0.93f, 0.92f, 0.89f);   // branco gelo
        var teto = Repetir(Carregar("Parede_Casa"), 2f, 2f); if (teto != null) teto.color = new Color(0.96f, 0.96f, 0.95f);
        var madeira = Carregar("Madeira");
        var rodape = Carregar("Rodape");
        var vidro = Carregar("Vidro");
        var tapete = Repetir(Carregar("Piso_Carpete"), 2f, 1.3f);
        var tecido = NovoMat("Tecido_Cadeira", new Color(0.12f, 0.12f, 0.13f), 0.15f, 0f);
        var metal = Carregar("Metal_Escovado");
        var lampada = Carregar("Lampada_Acesa");

        // ---- Caixa do cômodo (janela na parede do fundo, z+) ----
        // peças sobrepostas nas quinas (só encostar deixa a luz do sol vazar pela fresta)
        Bloco(raiz, "Piso", new Vector3(0, -0.05f, 0), new Vector3(L + 0.3f, 0.1f, P + 0.3f), piso);
        Bloco(raiz, "Teto", new Vector3(0, A + 0.05f, 0), new Vector3(L + 0.3f, 0.1f, P + 0.3f), teto);
        Bloco(raiz, "Parede_Esq", new Vector3(-L / 2 - 0.05f, A / 2, 0), new Vector3(0.1f, A + 0.2f, P + 0.2f), parede);
        Bloco(raiz, "Parede_Dir", new Vector3(L / 2 + 0.05f, A / 2, 0), new Vector3(0.1f, A + 0.2f, P + 0.2f), parede);
        Bloco(raiz, "Parede_Frente", new Vector3(0, A / 2, -P / 2 - 0.05f), new Vector3(L + 0.2f, A + 0.2f, 0.1f), parede);
        // parede do fundo com vão de janela 1.6 x 1.3 (peitoril a 0.9 m)
        float jl = 1.6f, jh = 1.3f, peit = 0.9f, z = P / 2 + 0.05f;
        Bloco(raiz, "Fundo_Esq", new Vector3(-(L + jl) / 4 - 0.05f, A / 2, z), new Vector3((L - jl) / 2 + 0.1f, A + 0.2f, 0.1f), parede);
        Bloco(raiz, "Fundo_Dir", new Vector3((L + jl) / 4 + 0.05f, A / 2, z), new Vector3((L - jl) / 2 + 0.1f, A + 0.2f, 0.1f), parede);
        Bloco(raiz, "Fundo_Baixo", new Vector3(0, peit / 2, z), new Vector3(jl, peit, 0.1f), parede);
        float topo = A - peit - jh;
        Bloco(raiz, "Fundo_Cima", new Vector3(0, A - topo / 2, z), new Vector3(jl, topo, 0.1f), parede);
        Bloco(raiz, "Vidro", new Vector3(0, peit + jh / 2, z), new Vector3(jl, jh, 0.01f), vidro, sombra: false);
        Bloco(raiz, "Peitoril", new Vector3(0, peit, z - 0.06f), new Vector3(jl + 0.1f, 0.03f, 0.16f), madeira);
        // rodapés
        Bloco(raiz, "Rodape_Esq", new Vector3(-L / 2 + 0.01f, 0.05f, 0), new Vector3(0.02f, 0.1f, P), rodape);
        Bloco(raiz, "Rodape_Dir", new Vector3(L / 2 - 0.01f, 0.05f, 0), new Vector3(0.02f, 0.1f, P), rodape);
        Bloco(raiz, "Rodape_Frente", new Vector3(0, 0.05f, -P / 2 + 0.01f), new Vector3(L, 0.1f, 0.02f), rodape);
        Bloco(raiz, "Rodape_Fundo", new Vector3(0, 0.05f, P / 2 - 0.01f), new Vector3(L, 0.1f, 0.02f), rodape);

        // ---- Móveis e equipamentos ----
        var mesa = Modelo(raiz, Kenney + "desk.fbx", new Vector3(-1.1f, 0, 1.35f), 1.5f, 180, madeira);
        float tampo = TopoY(mesa);
        Modelo(raiz, Kenney + "chairDesk.fbx", new Vector3(-1.1f, 0, 0.6f), 1.05f, 0, tecido);
        Modelo(raiz, Sketchfab + "Computador_Mesa.glb", new Vector3(-1.2f, tampo, 1.4f), 0.85f, 180);
        Modelo(raiz, Sketchfab + "Telefone_Escritorio.glb", new Vector3(-0.68f, tampo, 1.3f), 0.22f, 200);
        Modelo(raiz, Sketchfab + "Cartucho_Toner.glb", new Vector3(-0.47f, tampo, 1.5f), 0.36f, 160);
        Modelo(raiz, Sketchfab + "Switch_5_Portas.glb", new Vector3(-1.75f, tampo, 1.55f), 0.11f, 180);

        var apoio = Modelo(raiz, Kenney + "sideTableDrawers.fbx", new Vector3(0.65f, 0, 1.5f), 0.55f, 180, madeira);
        Modelo(raiz, Sketchfab + "Impressora_MFP_Compacta.fbx", new Vector3(0.65f, TopoY(apoio), 1.5f), 0.42f, 180);

        Modelo(raiz, Kenney + "bookcaseOpen.fbx", new Vector3(-1.8f, 0, -0.6f), 1.9f, 90, madeira);
        Modelo(raiz, Kenney + "rugRectangle.fbx", new Vector3(-0.6f, 0.005f, 0.4f), 2.2f, 0, tapete);

        // lá fora: gramado, calçada, muro e prédios vizinhos (vista da janela)
        Bloco(raiz, "Gramado", new Vector3(0, -0.15f, 0), new Vector3(80f, 0.1f, 80f), Repetir(Carregar("Grama"), 40f, 40f), gi: false);
        Bloco(raiz, "Calcada", new Vector3(0, -0.09f, 6f), new Vector3(30f, 0.02f, 2.5f), Carregar("Calcada"), gi: false);
        Bloco(raiz, "Muro", new Vector3(0, 0.6f, 14f), new Vector3(40f, 1.2f, 0.2f), Carregar("Parede_Cliente"), gi: false);
        Bloco(raiz, "Predio_1", new Vector3(-9f, 4f, 30f), new Vector3(9f, 8f, 6f), Carregar("Predio_70_78"), gi: false);
        Bloco(raiz, "Predio_2", new Vector3(8f, 6f, 34f), new Vector3(8f, 12f, 7f), Carregar("Predio_78_68"), gi: false);

        // luminária de teto
        Bloco(raiz, "Luminaria", new Vector3(0, A - 0.03f, 0.2f), new Vector3(0.6f, 0.04f, 0.6f), lampada, sombra: false);

        // ---- Luz ----
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;

        var sol = new GameObject("Sol").AddComponent<Light>();
        sol.transform.SetParent(raiz);
        sol.type = LightType.Directional;
        sol.transform.rotation = Quaternion.Euler(48f, 205f, 0f);   // tarde: entra pela janela (z+) em diagonal
        sol.color = new Color(1f, 0.93f, 0.82f);
        sol.intensity = 2.2f;
        sol.shadows = LightShadows.Soft;
        sol.lightmapBakeType = LightmapBakeType.Mixed;
        RenderSettings.sun = sol;

        var luz = new GameObject("Luz_Teto").AddComponent<Light>();
        luz.transform.SetParent(raiz);
        luz.transform.position = new Vector3(0, A - 0.15f, 0.2f);
        luz.type = LightType.Point;
        luz.range = 6f;
        luz.color = new Color(1f, 0.86f, 0.7f);   // lâmpada quente (~3000 K)
        luz.intensity = 1.1f;
        luz.shadows = LightShadows.Soft;
        luz.lightmapBakeType = LightmapBakeType.Baked;

        var sonda = new GameObject("Reflexos").AddComponent<ReflectionProbe>();
        sonda.transform.SetParent(raiz);
        sonda.transform.position = new Vector3(0, 1.3f, 0);
        sonda.size = new Vector3(L, A, P);
        sonda.boxProjection = true;
        sonda.mode = ReflectionProbeMode.Baked;

        var sondasLuz = new GameObject("SondasDeLuz").AddComponent<LightProbeGroup>();
        sondasLuz.transform.SetParent(raiz);
        var pts = new System.Collections.Generic.List<Vector3>();
        for (float x = -L / 2 + 0.4f; x <= L / 2 - 0.3f; x += 0.8f)
            for (float y = 0.2f; y <= A - 0.2f; y += 0.8f)
                for (float zz = -P / 2 + 0.4f; zz <= P / 2 - 0.3f; zz += 0.8f)
                    pts.Add(new Vector3(x, y, zz));
        sondasLuz.probePositions = pts.ToArray();

        // ---- Câmera do jogador (altura dos olhos), olhando a mesa ----
        var camGo = new GameObject("Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.position = new Vector3(1.3f, 1.6f, -1.45f);
        camGo.transform.LookAt(new Vector3(-0.9f, 0.85f, 1.3f));
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.allowHDR = true;
        cam.renderingPath = RenderingPath.DeferredShading;
        camGo.AddComponent<AudioListener>();
        PosProcessamento(cam);
        var pose = new GameObject("PoseMesa").transform;   // segunda foto: close na mesa
        pose.position = new Vector3(-0.35f, 1.35f, 0.35f);
        pose.LookAt(new Vector3(-1.1f, 0.95f, 1.45f));
        var cap = camGo.AddComponent<CapturaTesteGrafico>();
        cap.prefixo = "BuiltIn";
        cap.poseExtra = pose;

        // ---- Iluminação pré-calculada (lightmaps) ----
        var ajustes = new LightingSettings
        {
            bakedGI = true,
            realtimeGI = false,
            lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
            lightmapResolution = 24,
            lightmapPadding = 2,
            maxBounces = 3,
            directSampleCount = 32,
            indirectSampleCount = 256,
            environmentSampleCount = 256,
            mixedBakeMode = MixedLightingMode.Shadowmask,
            ao = true,
            aoMaxDistance = 0.6f,
            indirectScale = 1.4f,
            albedoBoost = 1.2f,
        };
        AssetDatabase.CreateAsset(ajustes, PastaCena + "/TesteGrafico_Luz.lighting");
        Lightmapping.lightingSettings = ajustes;

        EditorSceneManager.SaveScene(cena, CaminhoCena);
        Debug.Log("[TesteGrafico] Cômodo de prova montado em " + CaminhoCena);
    }

    [MenuItem("TI Service/Teste gráfico/2. Calcular luz (bake)", priority = 1301)]
    public static void CalcularLuz()
    {
        Lightmapping.bakeCompleted -= AoTerminar;
        Lightmapping.bakeCompleted += AoTerminar;
        Debug.Log("[TesteGrafico] Calculando a luz...");
        Lightmapping.BakeAsync();
    }

    static void AoTerminar()
    {
        Lightmapping.bakeCompleted -= AoTerminar;
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TesteGrafico] Luz calculada.");
    }

    [MenuItem("TI Service/Teste gráfico/3. Capturar fotos e FPS (Play)", priority = 1302)]
    public static void Capturar()
    {
        if (EditorSceneManager.GetActiveScene().path != CaminhoCena) EditorSceneManager.OpenScene(CaminhoCena);
        EditorApplication.isPlaying = true;
    }

    // Lista no log a hierarquia (nome, tamanho em cm e posição) dos modelos de impressora/computador,
    // para saber quais peças existem separadas (tampas, gavetas) e podem ser animadas
    [MenuItem("TI Service/Teste gráfico/5. Listar peças dos modelos de equipamento", priority = 1320)]
    public static void ListarPecas()
    {
        foreach (var caminho in new[] { Sketchfab + "Eletrodomesticos.glb", Sketchfab + "Cozinha_Compacta.glb", Sketchfab + "Micro_Ondas.glb",
                                        Sketchfab + "Impressora_MFP_Compacta.fbx", Sketchfab + "Impressora_MFP_Escritorio.glb",
                                        Sketchfab + "Impressora_Interior.glb", Sketchfab + "Cartucho_Toner.glb" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null) { Debug.Log("[Peças] não achou " + caminho); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var total = Limites(go);
            var sb = new System.Text.StringBuilder($"[Peças] {caminho} — total {total.size * 100f} cm\n");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                sb.AppendLine($"   {CaminhoNoModelo(r.transform, go.transform)} | {b.size.x * 100:0}x{b.size.y * 100:0}x{b.size.z * 100:0} cm | centro {(b.center - total.center) * 100f}");
            }
            Debug.Log(sb.ToString());
            Object.DestroyImmediate(go);
        }
    }

    static string CaminhoNoModelo(Transform t, Transform raiz)
    {
        string s = t.name;
        while (t.parent != null && t.parent != raiz) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    [MenuItem("TI Service/Teste gráfico/0. Abrir cena do jogo", priority = 1299)]
    public static void AbrirCenaDoJogo()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
    }

    // Fotos da casa na cena do jogo, sem Play (para conferir posição e giro dos modelos). Builds/Capturas/Casa_*.png
    [MenuItem("TI Service/Teste gráfico/4. Fotos da casa (cena do jogo)", priority = 1310)]
    public static void FotosDaCasa()
    {
        var c = new Vector3(0f, 0f, -19.5f);   // MontarCenario.CentroCasa
        Foto("Casa_Quarto", c + new Vector3(0.3f, 1.65f, 0.4f), c + new Vector3(-2.3f, 0.6f, -2.0f));
        Foto("Casa_Escritorio", c + new Vector3(-1.4f, 1.6f, 0.3f), c + new Vector3(-3.0f, 0.9f, 2.6f));
        Foto("Casa_Estante", c + new Vector3(-1.9f, 1.6f, 0.2f), c + new Vector3(-0.3f, 0.9f, 2.6f));
        Foto("Casa_GuardaRoupa", c + new Vector3(-0.6f, 1.6f, -1.0f), c + new Vector3(-3.7f, 1.0f, -0.6f));
        Foto("Casa_PC", c + new Vector3(-3.05f, 1.25f, 1.9f), c + new Vector3(-3.1f, 1.05f, 2.65f));
        Foto("Casa_Interruptor", c + new Vector3(0.6f, 1.4f, 1.6f), c + new Vector3(0.85f, 1.15f, 3f));
        Foto("Cliente_Impressora", new Vector3(42.9f, 1.45f, 0.1f), new Vector3(44.45f, 0.95f, 0.8f));
        Foto("Cliente_Computador", new Vector3(37.2f, 1.45f, 1.9f), new Vector3(37.2f, 1.0f, 3.5f));
        Foto("Cliente_Roteador", new Vector3(36.4f, 1.0f, -1.6f), new Vector3(35.5f, 0.7f, -1.8f));
        Foto("Cliente_Roteador_Lado", new Vector3(35.6f, 1.0f, -0.8f), new Vector3(35.5f, 0.7f, -1.8f));
        Foto("Cliente_Impressora_Perto", new Vector3(43.75f, 1.35f, 0.75f), new Vector3(44.45f, 0.95f, 0.8f));
        Foto("Casa_Sala", c + new Vector3(2.4f, 1.6f, 1.6f), c + new Vector3(2.4f, 0.5f, -2.3f));
        Foto("Casa_TV", c + new Vector3(2.4f, 1.3f, -2.2f), c + new Vector3(2.4f, 0.9f, -0.2f));
        Foto("Casa_Cozinha", c + new Vector3(1.4f, 1.6f, 1.2f), c + new Vector3(3.6f, 0.8f, 1.4f));
        Foto("Casa_Entrada", c + new Vector3(1.5f, 1.65f, 2.7f), c + new Vector3(1.2f, 0.6f, -0.5f));
        Foto("Escritorio_Estoque", new Vector3(-0.6f, 1.5f, 0.3f), new Vector3(-2.7f, 0.9f, 0.5f));
        Foto("Cliente_Gabinete", new Vector3(37.9f, 1.35f, 2.2f), new Vector3(37.75f, 0.95f, 3.45f));
        Foto("Casa_Geral", c + new Vector3(3.6f, 2.5f, 2.6f), c + new Vector3(-1.5f, 0.5f, -0.8f));
        Debug.Log("[TesteGrafico] Fotos da casa salvas em Builds/Capturas");
    }

    static void Foto(string nome, Vector3 de, Vector3 para)
    {
        var go = new GameObject("CameraFoto") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        go.transform.position = de;
        go.transform.LookAt(para);
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;
        cam.allowHDR = false;
        var rt = new RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        cam.Render();
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        var pasta = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Builds", "Capturas");
        System.IO.Directory.CreateDirectory(pasta);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(pasta, nome + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    // ---------------- Auxiliares ----------------

    static void PosProcessamento(Camera cam)
    {
        var recursos = AssetDatabase.LoadAssetAtPath<PP.PostProcessResources>(
            "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
        var perfil = AssetDatabase.LoadAssetAtPath<PP.PostProcessProfile>("Assets/Settings/PosProcessamento.asset");
        if (recursos == null || perfil == null) { Debug.LogWarning("[TesteGrafico] Pós-processamento não encontrado."); return; }

        var camada = cam.gameObject.AddComponent<PP.PostProcessLayer>();
        camada.Init(recursos);
        camada.volumeTrigger = cam.transform;
        camada.volumeLayer = 1 << 0;
        camada.antialiasingMode = PP.PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

        var vol = new GameObject("PosProcessamento").AddComponent<PP.PostProcessVolume>();
        vol.isGlobal = true;
        vol.sharedProfile = perfil;
    }

    static Material Carregar(string nome)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(Mats + nome + ".mat");
        if (m == null) Debug.LogWarning("[TesteGrafico] Material não encontrado: " + nome);
        return m;
    }

    static Material Repetir(Material baseMat, float x, float y)
    {
        if (baseMat == null) return null;
        var m = new Material(baseMat) { name = baseMat.name + "_Teste" };
        m.mainTextureScale = new Vector2(x, y);
        return m;
    }

    static Material NovoMat(string nome, Color cor, float brilho, float metal)
    {
        var m = new Material(Shader.Find("Standard")) { name = nome, color = cor };
        m.SetFloat("_Glossiness", brilho);
        m.SetFloat("_Metallic", metal);
        return m;
    }

    static GameObject Bloco(Transform pai, string nome, Vector3 centro, Vector3 tam, Material mat, bool sombra = true, bool gi = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        go.transform.SetParent(pai);
        go.transform.position = centro;
        go.transform.localScale = tam;
        var r = go.GetComponent<MeshRenderer>();
        if (mat != null) r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        if (!gi) { GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic); return go; }
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic |
                                                   StaticEditorFlags.BatchingStatic);
        r.receiveGI = ReceiveGI.Lightmaps;
        return go;
    }

    // Instancia um modelo, normaliza pelo maior lado (metros), apoia a base em 'pos' e gira em Y.
    // Os modelos recebem luz pelas sondas de luz (não precisam de UV de lightmap).
    static GameObject Modelo(Transform pai, string caminho, Vector3 pos, float maiorLado, float rotY, Material trocar = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) { Debug.LogWarning("[TesteGrafico] Modelo não encontrado: " + caminho); return new GameObject("Faltando"); }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(pai);
        // soma o giro à rotação original do modelo (alguns vêm de pé com -90° em X)
        go.transform.rotation = Quaternion.Euler(0, rotY, 0) * prefab.transform.rotation;

        var b = Limites(go);
        float lado = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (lado > 0.0001f) go.transform.localScale = prefab.transform.localScale * (maiorLado / lado);
        b = Limites(go);
        go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);

        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (trocar != null) r.sharedMaterials = Enumerable.Repeat(trocar, r.sharedMaterials.Length).ToArray();
            r.shadowCastingMode = ShadowCastingMode.On;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI |
                                                                 StaticEditorFlags.ReflectionProbeStatic);
            if (r is MeshRenderer mr) mr.receiveGI = ReceiveGI.LightProbes;
        }
        return go;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static float TopoY(GameObject go) => Limites(go).max.y;
}
