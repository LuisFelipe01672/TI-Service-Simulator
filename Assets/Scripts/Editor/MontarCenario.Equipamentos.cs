using System.Linq;
using UnityEditor;
using UnityEngine;

// Parte do MontarCenario: visuais realistas dos equipamentos do cliente (Fase 3).
// O "Visual" segue a convenção do jogo: frente em -Z, base em y = 0. Além das peças do modelo, cria
// "âncoras" (objetos vazios com posição e tamanho em localScale) que o EquipamentoPecas usa para colocar as
// peças de mira (visor, botões, traseira, toner) e a dobradiça da tampa.
public static partial class MontarCenario
{
    // Multifuncional laser compacta: a parte de cima (scanner + tampa) levanta para acessar o toner.
    static void VisualImpressoraReal(Transform v)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaSketchfab + "Impressora_MFP_Compacta.fbx");
        if (prefab == null) { Debug.LogWarning("[Cenário] Modelo da impressora não encontrado — usando o simples."); VisualImpressora(v); return; }

        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        modelo.name = "Modelo";
        modelo.transform.SetParent(v, false);
        // No arquivo a frente (visor) fica em -X: -90° em Y vira a frente para -Z
        modelo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f) * modelo.transform.localRotation;
        var b = LimitesLocal(modelo, v);
        modelo.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

        Transform Peca(string nome) => modelo.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == nome);
        var superior = Peca("upper_part_buttons");
        var tampaScanner = Peca("lid");
        var visor = Peca("display");
        var inferior = Peca("lower_part_shutter");

        // Marca fictícia: corpo grafite fosco (sem logotipo do fabricante)
        var mCorpo = Mat("Impressora_Real_Corpo", new Color(0.2f, 0.21f, 0.23f), 0.32f);
        var mClaro = Mat("Impressora_Real_Claro", new Color(0.86f, 0.87f, 0.88f), 0.35f);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            r.sharedMaterial = r.transform == superior || r.transform == tampaScanner ? mClaro : mCorpo;

        // "Tampa" = conjunto que levanta (parte de cima + tampa do scanner + visor)
        var tampa = new GameObject("Tampa").transform;
        tampa.SetParent(v, false);
        foreach (var p in new[] { superior, tampaScanner, visor })
            if (p != null) p.SetParent(tampa, true);

        // Visor: o jogo pinta de verde/laranja conforme o estado (EquipamentoPecas procura "Painel")
        if (visor != null) visor.name = "Painel";

        var bSup = superior != null ? LimitesLocal(superior.gameObject, v) : LimitesLocal(modelo, v);
        var bInf = inferior != null ? LimitesLocal(inferior.gameObject, v) : LimitesLocal(modelo, v);
        var bTudo = LimitesLocal(modelo, v);
        var bVisor = visor != null ? LimitesLocal(visor.gameObject, v) : new Bounds(new Vector3(bSup.max.x - 0.08f, bSup.max.y, bSup.min.z + 0.05f), Vector3.one * 0.05f);

        // Âncoras (posição local no Visual; tamanho em localScale)
        Ancora(v, "Ancora_Dobradica", new Vector3(0f, bSup.min.y, bSup.max.z), Vector3.one);
        Ancora(v, "Ancora_Visor", bVisor.center, Vector3.Max(bVisor.size + new Vector3(0.04f, 0.04f, 0.04f), new Vector3(0.1f, 0.05f, 0.08f)));
        Ancora(v, "Ancora_BotaoTeste", new Vector3(bVisor.center.x + 0.07f, bSup.max.y, bVisor.center.z), new Vector3(0.07f, 0.05f, 0.07f));
        Ancora(v, "Ancora_Energia", new Vector3(bInf.min.x + 0.05f, bInf.center.y + bInf.extents.y * 0.4f, bInf.min.z), new Vector3(0.08f, 0.08f, 0.05f));
        Ancora(v, "Ancora_Traseira", new Vector3(0f, bTudo.center.y + 0.1f, bTudo.max.z + 0.02f), new Vector3(bTudo.size.x, bTudo.size.y + 0.2f, 0.1f));
        Ancora(v, "Ancora_Cartucho", new Vector3(0f, bSup.min.y - 0.035f, bTudo.center.z), new Vector3(bTudo.size.x * 0.75f, 0.09f, 0.16f));
        Ancora(v, "Ancora_Tampa", new Vector3(0f, bSup.center.y, bSup.center.z), new Vector3(bSup.size.x, bSup.size.y, bSup.size.z));

        // Toner realista (aparece com a tampa aberta): o EquipamentoPecas procura "Cartucho"
        var toner = AssetDatabase.LoadAssetAtPath<GameObject>(PastaSketchfab + "Cartucho_Toner.glb");
        if (toner != null)
        {
            var suporte = new GameObject("Cartucho").transform;
            suporte.SetParent(v, false);
            var t = (GameObject)PrefabUtility.InstantiatePrefab(toner);
            t.transform.SetParent(suporte, false);
            // material próprio (a textura original tem o logotipo do fabricante)
            var mToner = Mat("Toner_Generico", new Color(0.07f, 0.07f, 0.08f), 0.3f);
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(mToner, r.sharedMaterials.Length).ToArray();
            var bt = LimitesLocal(t, suporte);
            float lado = Mathf.Max(bt.size.x, bt.size.z);
            if (lado > 0.0001f) t.transform.localScale *= bTudo.size.x * 0.7f / lado;
            bt = LimitesLocal(t, suporte);
            // deitado de lado a lado (o maior lado no eixo X)
            if (bt.size.z > bt.size.x) { t.transform.localRotation = Quaternion.Euler(0, 90, 0) * t.transform.localRotation; bt = LimitesLocal(t, suporte); }
            t.transform.localPosition -= bt.center;
            suporte.localPosition = new Vector3(0f, bSup.min.y - bt.size.y / 2f + 0.01f, bTudo.center.z);
        }
    }

    // Computador do cliente: monitor, teclado e mouse realistas, tela viva (apaga com o PC desligado) e âncoras
    // da tela, do botão liga/desliga (embaixo, à direita do monitor) e da traseira (cabos).
    static void VisualComputadorReal(Transform v)
    {
        var pc = ModeloReal(SF("Computador_Mesa"), v, Vector3.zero, 0.85f, 180f, Colisao.Nenhuma);
        if (pc == null) { Modelo("computerScreen", v, Vector3.zero, 0, Colisao.Nenhuma); return; }
        var tela = TelaNoMonitor(pc, out var bMundo);
        tela.name = "TelaViva";

        // limites do monitor no espaço do Visual (o Visual ainda está na origem, sem giro)
        var bMon = new Bounds(v.InverseTransformPoint(bMundo.center), bMundo.size);
        float zFrente = bMon.min.z, zTras = bMon.max.z;   // a tela olha para -Z
        Ancora(v, "Ancora_Tela", new Vector3(bMon.center.x, bMon.center.y, zFrente), new Vector3(bMon.size.x * 0.92f, bMon.size.y * 0.85f, 0.06f));
        Ancora(v, "Ancora_Energia", new Vector3(bMon.max.x - 0.04f, bMon.min.y + 0.02f, zFrente), new Vector3(0.08f, 0.06f, 0.06f));
        Ancora(v, "Ancora_Traseira", new Vector3(bMon.center.x, bMon.center.y + 0.12f, zTras + 0.03f), new Vector3(bMon.size.x, bMon.size.y + 0.25f, 0.1f));
    }

    static void Ancora(Transform pai, string nome, Vector3 pos, Vector3 tam)
    {
        var a = new GameObject(nome).transform;
        a.SetParent(pai, false);
        a.localPosition = pos;
        a.localScale = tam;
    }

    // Limites de um objeto no espaço local de 'referencia'
    static Bounds LimitesLocal(GameObject go, Transform referencia)
    {
        bool tem = false;
        var b = new Bounds();
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var lb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var canto = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = referencia.InverseTransformPoint(r.transform.TransformPoint(canto));
                if (!tem) { b = new Bounds(p, Vector3.zero); tem = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }
}
