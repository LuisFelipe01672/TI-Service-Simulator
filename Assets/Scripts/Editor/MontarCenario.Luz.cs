using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Luz pré-calculada nos interiores (Etapa 2.2). Regras, por causa do ciclo de dia e dos interruptores:
// - O sol continua em tempo real (o DiaManager gira e apaga) e fica fora do cálculo.
// - Lâmpadas do teto: Mixed no modo Baked Indirect — a luz direta segue ligando/desligando e com sombra
//   (no máximo uma com sombra por cômodo); só o rebote nas paredes é pré-calculado.
// - Casca dos cômodos (piso, forro, paredes, rodapés) vai para o lightmap; móveis contribuem para o cálculo
//   e recebem luz pelas sondas de luz (grade por cômodo); objetos que se mexem ficam de fora.
// - Fora dos cômodos (rua, prédios, gramado) nada usa sonda: segue a luz ambiente do DiaManager.
// - Reflexos: uma sonda pré-calculada por cômodo, com projeção em caixa.
public static partial class MontarCenario
{
    const string CaminhoAjustesLuz = "Assets/Settings/Luz_Cenario.lighting";

    struct Comodo { public string nome; public Bounds interior; }

    static List<Comodo> Comodos()
    {
        var lista = new List<Comodo>();
        void Add(string nome, Vector3 centro, float largura, float prof)
        {
            if (raiz == null || raiz.Find(nome) == null) return;
            lista.Add(new Comodo { nome = nome, interior = new Bounds(centro + new Vector3(0, H / 2f, 0), new Vector3(largura, H, prof)) });
        }
        Add("Escritorio", Vector3.zero, 6f, 6f);
        Add("Cliente", new Vector3(40, 0, 0), 10f, 8f);
        Add("Cliente_Clinica", CentroClinica, 10f, 8f);
        Add("Cliente_Loja", CentroLoja, 10f, 8f);
        Add("Casa", CentroCasa, 8f, 6f);
        Add("Banheiro", CentroBanheiro, LarguraBanheiro, ProfBanheiro);
        return lista;
    }

    static void PrepararLuzPreCalculada()
    {
        var comodos = Comodos();
        int casca = 0, moveis = 0, fora = 0;

        // Bounds com a parede inteira (o interior + espessura), para pegar os blocos de parede
        bool Dentro(Bounds b, Bounds interior, float folga)
        {
            var e = interior; e.Expand(new Vector3(folga, 0.4f, folga) * 2f);
            return e.Contains(b.center);
        }

        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer) continue;
            var c = comodos.FirstOrDefault(k => Dentro(r.bounds, k.interior, T + 0.05f));
            if (c.nome == null || r.name == "Bloco_Fora")
            {
                // Rua, prédios, gramado, caminho da calçada: sem sonda, segue a luz ambiente do dia
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
                fora++;
                continue;
            }
            if (SeMexe(r.transform)) continue;   // portas, interruptor, mochila, cafeteira...: sonda de luz (padrão)

            var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;
            if (EhCasca(r.transform) && r is MeshRenderer mr && r.GetComponent<MeshFilter>() is MeshFilter mf && mf.sharedMesh != null)
            {
                Unwrapping.GenerateSecondaryUVSet(mf.sharedMesh);
                flags |= StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                mr.receiveGI = ReceiveGI.Lightmaps;
                mr.scaleInLightmap = 1f;
                casca++;
            }
            else
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                if (r is MeshRenderer m2) m2.receiveGI = ReceiveGI.LightProbes;
                moveis++;
            }
        }

        // Lâmpadas do teto (as LuzTeto): direto em tempo real, rebote pré-calculado. Sol e postes: tempo real.
        foreach (var luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            bool teto = luz.type == LightType.Point && luz.transform.IsChildOf(raiz) && luz.GetComponent<PosteLuz>() == null && luz.GetComponentInParent<Abajur>() == null
                        && comodos.Any(k => k.interior.Contains(luz.transform.position));
            luz.lightmapBakeType = teto ? LightmapBakeType.Mixed : LightmapBakeType.Realtime;
            if (teto) luz.bounceIntensity = 1.2f;
        }

        // Sondas de luz e de reflexo, uma grade por cômodo
        foreach (var c in comodos)
        {
            var sala = raiz.Find(c.nome);
            var g = new GameObject("SondasDeLuz").AddComponent<LightProbeGroup>();
            g.transform.SetParent(sala, false);
            var pts = new List<Vector3>();
            Vector3 tam = c.interior.size;
            int nx = Mathf.Max(2, Mathf.CeilToInt(tam.x / 1.1f)), nz = Mathf.Max(2, Mathf.CeilToInt(tam.z / 1.1f));
            foreach (float y in new[] { 0.25f, 1.0f, 1.8f, H - 0.25f })
                for (int i = 0; i < nx; i++)
                    for (int k = 0; k < nz; k++)
                    {
                        float x = Mathf.Lerp(-tam.x / 2f + 0.2f, tam.x / 2f - 0.2f, i / (float)(nx - 1));
                        float z = Mathf.Lerp(-tam.z / 2f + 0.2f, tam.z / 2f - 0.2f, k / (float)(nz - 1));
                        pts.Add(new Vector3(x, y, z));
                    }
            g.probePositions = pts.ToArray();

            foreach (var rp in sala.GetComponentsInChildren<ReflectionProbe>())
            {
                rp.mode = ReflectionProbeMode.Baked;
                rp.boxProjection = true;
                rp.resolution = 256;
                rp.hdr = true;
            }
        }

        // Céu do cálculo: o mesmo céu de dia do DiaManager, mais fraco (é a luz que entra pelas janelas;
        // em jogo o DiaManager troca o ambiente a cada minuto e só afeta o que não tem lightmap)
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.60f, 0.68f, 0.82f) * 0.7f;
        RenderSettings.ambientEquatorColor = new Color(0.50f, 0.50f, 0.48f) * 0.7f;
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.24f) * 0.7f;
        RenderSettings.ambientIntensity = 1f;

        var ajustes = AssetDatabase.LoadAssetAtPath<LightingSettings>(CaminhoAjustesLuz);
        if (ajustes == null)
        {
            ajustes = new LightingSettings();
            AssetDatabase.CreateAsset(ajustes, CaminhoAjustesLuz);
        }
        ajustes.bakedGI = true;
        ajustes.realtimeGI = false;
        ajustes.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        ajustes.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ajustes.lightmapResolution = 14;          // texels por metro: cômodos pequenos, lightmap leve
        ajustes.lightmapPadding = 4;
        ajustes.lightmapMaxSize = 1024;
        ajustes.lightmapCompression = LightmapCompression.NormalQuality;
        ajustes.directionalityMode = LightmapsMode.NonDirectional;
        ajustes.maxBounces = 3;
        ajustes.directSampleCount = 32;
        ajustes.indirectSampleCount = 256;
        ajustes.environmentSampleCount = 256;
        ajustes.filteringMode = LightingSettings.FilterMode.Auto;
        ajustes.ao = true;                         // cantos e pés dos móveis escurecem: nada parece flutuar
        ajustes.aoMaxDistance = 0.6f;
        ajustes.aoExponentIndirect = 1.2f;
        ajustes.aoExponentDirect = 0f;
        ajustes.indirectScale = 1.15f;
        ajustes.albedoBoost = 1.1f;
        EditorUtility.SetDirty(ajustes);
        Lightmapping.lightingSettings = ajustes;

        Debug.Log($"[Luz] Preparado: {casca} peças no lightmap, {moveis} móveis nas sondas, {fora} de fora (luz ambiente do dia).");
    }

    // Peça da casca do cômodo: piso, forro, blocos de parede, rodapé (malhas feitas pela Caixa, sem nada que se mexa)
    static bool EhCasca(Transform t)
    {
        string n = t.name;
        if (n == "Piso" || n == "Forro") return true;
        return (n == "Bloco" || n == "Rodape") && t.parent != null && t.parent.name.StartsWith("Parede_");
    }

    // Objetos que mudam de lugar ou de forma durante o jogo não podem entrar no cálculo
    static readonly string[] NomesQueSeMexem = { "Porta", "Dobradica", "Tecla", "Persiana", "Gaveta", "Tampa" };

    static bool SeMexe(Transform t)
    {
        for (var p = t; p != null && p != raiz; p = p.parent)
        {
            if (NomesQueSeMexem.Any(n => p.name.StartsWith(n))) return true;
            if (p.GetComponent<Rigidbody>() != null) return true;
            foreach (var mb in p.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Assembly.GetName().Name == "Assembly-CSharp" && !(mb is LightProbeGroup)) return true;
        }
        return false;
    }

    [MenuItem("TI Service/3b. Calcular luz do cenário (bake)", false, 1001)]
    public static void CalcularLuz()
    {
        if (Lightmapping.isRunning) { Debug.Log("[Luz] Cálculo já em andamento."); return; }
        Lightmapping.bakeCompleted -= AoTerminarLuz;
        Lightmapping.bakeCompleted += AoTerminarLuz;
        inicioLuz = EditorApplication.timeSinceStartup;
        Debug.Log("[Luz] Calculando a luz dos interiores...");
        if (!Lightmapping.BakeAsync()) Debug.LogError("[Luz] Não foi possível iniciar o cálculo.");
    }

    static double inicioLuz;

    static void AoTerminarLuz()
    {
        Lightmapping.bakeCompleted -= AoTerminarLuz;
        EditorSceneManager.SaveOpenScenes();
        int n = LightmapSettings.lightmaps?.Length ?? 0;
        Debug.Log($"[Luz] Luz calculada em {EditorApplication.timeSinceStartup - inicioLuz:0} s: {n} lightmap(s). Cena salva.");
    }
}
