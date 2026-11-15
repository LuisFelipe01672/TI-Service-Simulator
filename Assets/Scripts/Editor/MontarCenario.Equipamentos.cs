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
        // gaveta de papel na frente, embaixo; saída do papel entre o corpo e o scanner; porta de rede atrás, embaixo
        Ancora(v, "Ancora_Gaveta", new Vector3(0f, bInf.min.y + bInf.size.y * 0.3f, bInf.min.z), new Vector3(bInf.size.x * 0.8f, 0.09f, 0.08f));
        Ancora(v, "Ancora_Saida", new Vector3(0f, bSup.min.y - 0.012f, bInf.min.z + 0.02f), Vector3.one);
        Ancora(v, "Ancora_PortaRede", new Vector3(bTudo.size.x * 0.25f, bInf.min.y + 0.05f, bTudo.max.z), Vector3.one);

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
        MontarGabinete(v, new Vector3(bMon.max.x + 0.2f, 0f, bMon.center.z + 0.05f));
    }

    // Gabinete (torre) ao lado do monitor, feito de peças: carcaça, tampa lateral que sai, placa-mãe, dois
    // pentes de memória, cooler com dissipador, fonte, HD e a poeira. Frente em -Z; a tampa removível fica em -X
    // (o jogo vira o gabinete para ela ficar de frente para o técnico). Origem na base, no centro.
    static void MontarGabinete(Transform v, Vector3 pos)
    {
        const float W = 0.2f, Hh = 0.42f, D = 0.42f, e = 0.008f;
        var preto = Mat("Gabinete_Preto", new Color(0.07f, 0.07f, 0.08f), 0.4f);
        var chapa = Mat("Gabinete_Chapa", new Color(0.5f, 0.52f, 0.55f), 0.5f, 0.6f);
        var placa = Mat("PlacaMae", new Color(0.05f, 0.25f, 0.12f), 0.3f);
        var pente = Mat("Pente_Memoria", new Color(0.08f, 0.32f, 0.16f), 0.35f);
        var escuro = Mat("Cooler_Preto", new Color(0.13f, 0.13f, 0.15f), 0.3f);
        var metal = Mat("Fonte_PC", new Color(0.32f, 0.32f, 0.34f), 0.5f, 0.5f);
        var prata = Mat("HD_Prata", new Color(0.62f, 0.64f, 0.67f), 0.6f, 0.7f);
        var po = Mat("Poeira", new Color(0.45f, 0.42f, 0.38f), 0.02f);

        var g = new GameObject("Gabinete").transform;
        g.SetParent(v, false);
        g.localPosition = pos;

        Cubo(g, "Base", new Vector3(0, e / 2, 0), new Vector3(W, e, D), chapa);
        Cubo(g, "Topo", new Vector3(0, Hh - e / 2, 0), new Vector3(W, e, D), preto);
        Cubo(g, "Frente", new Vector3(0, Hh / 2, -D / 2 + e / 2), new Vector3(W, Hh, e), preto);
        Cubo(g, "Tras", new Vector3(0, Hh / 2, D / 2 - e / 2), new Vector3(W, Hh, e), chapa);
        Cubo(g, "LadoFixo", new Vector3(W / 2 - e / 2, Hh / 2, 0), new Vector3(e, Hh, D), preto);
        Cubo(g, "TampaLateral", new Vector3(-W / 2 + e / 2, Hh / 2, 0), new Vector3(e, Hh - 0.004f, D - 0.004f), preto);
        // frente: baia do leitor, botão e LED
        Cubo(g, "Baia", new Vector3(0, Hh - 0.05f, -D / 2 - 0.001f), new Vector3(W * 0.8f, 0.035f, 0.002f), escuro);
        Cubo(g, "BotaoPower", new Vector3(-0.04f, Hh - 0.11f, -D / 2 - 0.002f), new Vector3(0.022f, 0.022f, 0.004f), chapa);
        Cubo(g, "LedGabinete", new Vector3(0.0f, Hh - 0.11f, -D / 2 - 0.002f), new Vector3(0.008f, 0.008f, 0.004f), escuro);

        // por dentro (presos no lado fixo, +X)
        float xPlaca = W / 2 - e - 0.004f;
        Cubo(g, "PlacaMae", new Vector3(xPlaca, 0.2f, 0.02f), new Vector3(0.004f, 0.32f, 0.3f), placa);
        Cubo(g, "Pente1", new Vector3(xPlaca - 0.018f, 0.27f, -0.075f), new Vector3(0.03f, 0.13f, 0.006f), pente);
        Cubo(g, "Pente2", new Vector3(xPlaca - 0.018f, 0.27f, -0.058f), new Vector3(0.03f, 0.13f, 0.006f), pente);
        // processador e a pasta térmica ficam embaixo do dissipador (aparecem quando o cooler sai)
        Cubo(g, "Processador", new Vector3(xPlaca - 0.004f, 0.27f, 0.05f), new Vector3(0.004f, 0.04f, 0.04f), prata);
        Cubo(g, "Pasta", new Vector3(xPlaca - 0.007f, 0.27f, 0.05f), new Vector3(0.002f, 0.034f, 0.034f), po);
        Cubo(g, "Dissipador", new Vector3(xPlaca - 0.026f, 0.27f, 0.05f), new Vector3(0.035f, 0.085f, 0.085f), chapa);
        var cooler = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(cooler.GetComponent<Collider>());
        cooler.name = "Cooler";
        cooler.transform.SetParent(g, false);
        cooler.transform.localPosition = new Vector3(xPlaca - 0.045f, 0.27f, 0.05f);
        cooler.transform.localRotation = Quaternion.Euler(0, 0, 90);
        cooler.transform.localScale = new Vector3(0.085f, 0.008f, 0.085f);
        cooler.GetComponent<Renderer>().sharedMaterial = escuro;
        Cubo(g, "Fonte", new Vector3(0.005f, Hh - e - 0.047f, D / 2 - e - 0.078f), new Vector3(W - 0.04f, 0.09f, 0.15f), metal);
        Cubo(g, "HD", new Vector3(0.01f, 0.07f, -D / 2 + 0.1f), new Vector3(0.1f, 0.026f, 0.146f), prata);
        Cubo(g, "CabosInternos", new Vector3(0.02f, 0.17f, 0.12f), new Vector3(0.02f, 0.2f, 0.03f), escuro);

        // poeira: manta no fundo, tufos no cooler, na fonte e nos pentes
        var poeira = new GameObject("Poeira").transform;
        poeira.SetParent(g, false);
        Cubo(poeira, "Manta", new Vector3(0, e + 0.004f, 0), new Vector3(W - 0.03f, 0.008f, D - 0.04f), po);
        Cubo(poeira, "NoCooler", new Vector3(xPlaca - 0.052f, 0.27f, 0.05f), new Vector3(0.006f, 0.08f, 0.08f), po);
        Cubo(poeira, "NaFonte", new Vector3(0.005f, Hh - e - 0.096f, D / 2 - 0.09f), new Vector3(W - 0.05f, 0.006f, 0.14f), po);
        Cubo(poeira, "NosPentes", new Vector3(xPlaca - 0.036f, 0.27f, -0.066f), new Vector3(0.006f, 0.12f, 0.03f), po);
        Cubo(poeira, "NoHD", new Vector3(0.01f, 0.086f, -D / 2 + 0.1f), new Vector3(0.098f, 0.006f, 0.14f), po);

        Ancora(v, "Ancora_Gabinete", pos + new Vector3(0, Hh / 2, 0), new Vector3(W + 0.03f, Hh + 0.03f, D + 0.03f));
    }

    // Roteador realista: corpo do modelo + 4 LEDs na frente (o jogo acende conforme o estado), com âncoras da
    // fonte (atrás, à direita) e do reset (atrás, à esquerda). RotRoteador acerta a frente do modelo para -Z.
    const float RotRoteador = 0f;
    static void VisualRoteadorReal(Transform v)
    {
        var r = ModeloReal(SF("Roteador"), v, Vector3.zero, 0.3f, RotRoteador, Colisao.Nenhuma);
        if (r == null) { VisualRoteador(v); return; }
        var b = LimitesLocal(r, v);
        // altura do corpo (sem as antenas): a parte mais baixa e larga; estimada em 18% da altura total, mín. 3,5 cm
        float corpo = Mathf.Max(0.035f, b.size.y * 0.18f);
        var led = Mat("LED_Verde", new Color(0.2f, 0.9f, 0.45f), 0.6f);
        for (int i = 0; i < 4; i++)
            Cubo(v, "LED" + i, new Vector3(-0.075f + i * 0.05f, b.min.y + corpo * 0.6f, b.min.z - 0.002f), new Vector3(0.012f, 0.006f, 0.004f), led);
        Ancora(v, "Ancora_Luzes", new Vector3(0f, b.min.y + corpo * 0.6f, b.min.z - 0.01f), new Vector3(0.26f, 0.05f, 0.05f));
        Ancora(v, "Ancora_Corpo", new Vector3(0f, b.min.y + corpo / 2f, b.center.z), new Vector3(b.size.x, corpo + 0.02f, b.size.z * 0.8f));
        Ancora(v, "Ancora_Fonte", new Vector3(b.size.x * 0.28f, b.min.y + corpo + 0.05f, b.max.z + 0.02f), new Vector3(0.1f, 0.14f, 0.06f));
        Ancora(v, "Ancora_Reset", new Vector3(-b.size.x * 0.28f, b.min.y + corpo + 0.05f, b.max.z + 0.02f), new Vector3(0.1f, 0.14f, 0.06f));
        Ancora(v, "Ancora_Plugue", new Vector3(b.size.x * 0.28f, b.min.y + corpo * 0.5f, b.max.z + 0.015f), new Vector3(0.03f, 0.025f, 0.03f));
        Ancora(v, "Ancora_FuroReset", new Vector3(-b.size.x * 0.28f, b.min.y + corpo * 0.5f, b.max.z + 0.001f), new Vector3(0.008f, 0.008f, 0.002f));
        Debug.Log($"[Cenário] Roteador real: {b.size * 100f} cm");
    }

    // Estante de aço de estoque na parede oeste do escritório + caixa de logística reversa (Fase 2).
    // As prateleiras são medidas por raio (de cima para baixo, em 3 pontos da largura) e viram "vagas" onde o
    // EstoqueVisual mostra o que há na mochila/estoque.
    static void MontarEstoque(Transform sala)
    {
        var estante = ModeloReal(PH("steel_frame_shelves_03"), sala, new Vector3(-2.68f, 0f, 0.6f), 1.8f, 90f);
        if (estante == null) { Modelo("bookcaseOpen", sala, new Vector3(-2.7f, 0, 0.6f), 90); return; }
        estante.name = "Estoque";

        var temporarios = new System.Collections.Generic.List<MeshCollider>();
        foreach (var mf in estante.GetComponentsInChildren<MeshFilter>())
        {
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            temporarios.Add(mc);
        }
        var caixaColisor = estante.GetComponent<BoxCollider>();
        if (caixaColisor != null) caixaColisor.enabled = false;
        Physics.SyncTransforms();

        var achadas = new System.Collections.Generic.List<Vector3>();
        // um raio por coluna da estante, descendo de prateleira em prateleira (cada raio só acha a primeira
        // superfície de um colisor, então recomeça logo abaixo de cada acerto)
        foreach (float dz in new[] { -0.45f, 0.45f })
        {
            Vector3 origem = sala.TransformPoint(new Vector3(-2.68f, 2.6f, 0.6f + dz));
            for (int passo = 0; passo < 12; passo++)
            {
                if (!Physics.Raycast(origem, Vector3.down, out var hit, 3f)) break;
                origem = hit.point + Vector3.down * 0.03f;
                if (hit.normal.y < 0.7f || !temporarios.Contains(hit.collider as MeshCollider)) continue;
                float y = hit.point.y - sala.position.y;
                if (y < 0.15f || y > 1.55f) continue;   // nem o chão, nem alto demais para ver
                if (achadas.Exists(p => Mathf.Abs(p.y - hit.point.y) < 0.2f && Mathf.Abs(p.z - hit.point.z) < 0.1f)) continue;
                achadas.Add(hit.point);
            }
        }
        foreach (var mc in temporarios) Object.DestroyImmediate(mc);
        if (caixaColisor != null) caixaColisor.enabled = true;

        // mais altas primeiro (altura dos olhos), da esquerda para a direita
        achadas.Sort((a, b) => Mathf.Abs(a.y - b.y) > 0.06f ? b.y.CompareTo(a.y) : a.z.CompareTo(b.z));
        for (int i = 0; i < achadas.Count; i++)
        {
            var v = new GameObject($"Vaga_{i:00}").transform;
            v.SetParent(estante.transform, true);
            v.position = achadas[i] + Vector3.up * 0.002f;
            v.rotation = Quaternion.LookRotation(-sala.right);   // -Z da vaga (frente das etiquetas) para dentro da sala
        }
        estante.AddComponent<EstoqueVisual>();
        Debug.Log($"[Cenário] Estoque: {achadas.Count} vagas na estante.");

        // Logística reversa: caixa no chão, perto da porta
        var descarte = new GameObject("LogisticaReversa");
        descarte.transform.SetParent(sala, false);
        descarte.transform.localPosition = new Vector3(-2.6f, 0f, -1.75f);
        descarte.transform.localRotation = Quaternion.LookRotation(-Vector3.right);
        descarte.AddComponent<DescarteEletronico>();
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
