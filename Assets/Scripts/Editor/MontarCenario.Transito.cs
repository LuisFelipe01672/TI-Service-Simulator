using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte do MontarCenario (Etapa 9D, vida na rua): o molde do carro dos vizinhos (o mesmo hatch do técnico, numa malha
// só, sem porta recortada nem física), o semáforo do cruzamento da Jacarandás, o PARE da Ipê e as linhas de retenção.
// Quem põe os carros e os pedestres para circular é o Cidade/VidaNaRua.cs (em tempo de jogo).
public static partial class MontarCenario
{
    static void MontarTransito()
    {
        var pai = new GameObject("Transito").transform;
        pai.SetParent(raiz, false);
        MontarMoldeCarroNPC(pai);
        MontarSemaforo(pai);
        MontarPare(pai);
        MontarJanelasNoturnas();
    }

    // ---------------- Molde do carro dos vizinhos ----------------

    static void MontarMoldeCarroNPC(Transform pai)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SF("Carro_Milano95"));
        if (prefab == null) { Debug.LogError("[Cenário] Modelo do carro não encontrado (carro dos vizinhos)."); return; }

        var carro = new GameObject("Molde_Carro_Vizinho");
        var ct = carro.transform;
        var suporte = new GameObject("Modelo").transform;
        suporte.SetParent(ct, false);
        suporte.localRotation = Quaternion.Euler(0f, 90f, 0f);
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var rs = modelo.GetComponentsInChildren<Renderer>(true).ToList();
        float chao = rs.Where(r => r.name.Contains("WheelStock")).Min(r => r.bounds.min.y);
        var b = Limites(modelo);
        suporte.localPosition = new Vector3(-b.center.x, -chao, -b.center.z);

        var c = carro.AddComponent<CarroNPC>();
        // Rodas: pivô no centro (giram com a distância andada)
        string[] nomesRoda = { "WheelStock_FL", "WheelStock_FR", "WheelStock_RL", "WheelStock_RR" };
        var rodas = new List<Transform>();
        foreach (var nome in nomesRoda)
        {
            var rr = rs.First(r => r.name.Contains(nome));
            c.raioRoda = rr.bounds.size.y / 2f;
            rodas.Add(Pivo("Roda", rr, ct, Quaternion.identity));
            rr.shadowCastingMode = ShadowCastingMode.Off;   // a sombra da carroceria já cobre as rodas
            rr.lightProbeUsage = LightProbeUsage.Off;
        }
        c.rodas = rodas.ToArray();

        // Materiais: pintura (cores comuns na rua), faróis e lanternas acendendo pelo script
        var cores = new (string nome, Color cor, float metal)[]
        {
            ("Branco", new Color(0.92f, 0.92f, 0.9f), 0.1f), ("Prata", new Color(0.62f, 0.64f, 0.66f), 0.6f),
            ("Preto", new Color(0.04f, 0.04f, 0.05f), 0.3f), ("Cinza", new Color(0.32f, 0.33f, 0.35f), 0.5f),
            ("Vermelho", new Color(0.55f, 0.06f, 0.05f), 0.3f), ("Azul", new Color(0.07f, 0.16f, 0.38f), 0.4f),
        };
        var pinturas = new List<Material>();
        foreach (var cor in cores) pinturas.Add(Mat("CarroNPC_Pintura_" + cor.nome, cor.cor, 0.78f, cor.metal));
        // mais carros brancos, prata e pretos (como na rua de verdade)
        c.pinturas = new[] { pinturas[0], pinturas[0], pinturas[0], pinturas[1], pinturas[1], pinturas[1], pinturas[2], pinturas[2], pinturas[3], pinturas[3], pinturas[4], pinturas[5] };
        c.farolApagado = Mat("CarroNPC_Farol_Apagado", new Color(0.85f, 0.85f, 0.82f), 0.9f, 0.2f);
        Brilhar(c.farolApagado, Color.black);
        c.farolAceso = Mat("CarroNPC_Farol_Aceso", new Color(0.95f, 0.95f, 0.9f), 0.9f, 0.2f);
        Brilhar(c.farolAceso, new Color(1f, 0.95f, 0.8f) * 2.2f);
        c.freioApagado = Mat("CarroNPC_Lanterna_Apagada", new Color(0.4f, 0.03f, 0.02f), 0.8f);
        Brilhar(c.freioApagado, Color.black);
        c.lanterna = Mat("CarroNPC_Lanterna_Acesa", new Color(0.5f, 0.04f, 0.03f), 0.8f);
        Brilhar(c.lanterna, new Color(1f, 0.05f, 0.02f) * 0.6f);
        c.freioAceso = Mat("CarroNPC_Freio_Aceso", new Color(0.6f, 0.05f, 0.03f), 0.8f);
        Brilhar(c.freioAceso, new Color(1f, 0.05f, 0.02f) * 2.4f);

        // Malha única da carroceria, uma submalha por material (poucas chamadas de desenho por carro)
        var grupos = new List<(Material mat, List<CombineInstance> partes)>();
        void Somar(Material m, CombineInstance ci)
        {
            var g = grupos.FirstOrDefault(x => x.mat == m);
            if (g.mat == null) { g = (m, new List<CombineInstance>()); grupos.Add(g); }
            g.partes.Add(ci);
        }
        var paraCarro = ct.worldToLocalMatrix;
        foreach (var r in rs)
        {
            if (r.name.Contains("WheelStock") || r.name.Contains("Reverse") || r.name.Contains("Blinkers")) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
            {
                var m = r.sharedMaterials[Mathf.Min(s, r.sharedMaterials.Length - 1)];
                if (r.name.Contains("Headlights_UCB")) m = c.farolApagado;
                else if (r.name.Contains("Brakelights")) m = c.freioApagado;
                else if (m != null && m.name.Contains("Bodymat")) m = pinturas[0];
                Somar(m, new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = paraCarro * r.transform.localToWorldMatrix });
            }
        }
        var porMaterial = new List<CombineInstance>();
        foreach (var g in grupos)
        {
            var mg = new Mesh { indexFormat = IndexFormat.UInt32 };
            mg.CombineMeshes(g.partes.ToArray(), true, true);
            porMaterial.Add(new CombineInstance { mesh = mg, subMeshIndex = 0, transform = Matrix4x4.identity });
        }
        var malha = new Mesh { name = "Carro_Vizinho", indexFormat = IndexFormat.UInt32 };
        malha.CombineMeshes(porMaterial.ToArray(), false, false);
        malha.RecalculateBounds();
        malha.UploadMeshData(false);
        foreach (var ci in porMaterial) Object.DestroyImmediate(ci.mesh);
        Object.DestroyImmediate(suporte.gameObject);   // as rodas já saíram de dentro (viraram filhas dos pivôs)

        var corpo = new GameObject("Carroceria");
        corpo.transform.SetParent(ct, false);
        corpo.AddComponent<MeshFilter>().sharedMesh = malha;
        var mr = corpo.AddComponent<MeshRenderer>();
        var mats = grupos.Select(g => g.mat).ToArray();
        mr.sharedMaterials = mats;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.lightProbeUsage = LightProbeUsage.Off;
        c.carroceria = mr;
        c.indicePintura = System.Array.IndexOf(mats, pinturas[0]);
        c.indiceFarol = System.Array.IndexOf(mats, c.farolApagado);
        c.indiceFreio = System.Array.IndexOf(mats, c.freioApagado);
        Debug.Log($"[Cenário] Carro dos vizinhos: {malha.vertexCount} vértices, {mats.Length} materiais (pintura {c.indicePintura}, farol {c.indiceFarol}, freio {c.indiceFreio}).");

        // Placas (o texto muda em cada carro): mede a traseira e a frente com raios na malha
        var mc = corpo.AddComponent<MeshCollider>();
        mc.sharedMesh = malha;
        Physics.SyncTransforms();
        float Raio(Vector3 de, Vector3 dir, float padrao)
        {
            foreach (var h in Physics.RaycastAll(de, dir, 5f).OrderBy(h => h.distance)) if (h.collider == mc) return h.distance;
            return padrao;
        }
        float zTras = -3.5f + Raio(new Vector3(0f, 0.86f, -3.5f), Vector3.forward, 1.55f);
        float zFrente = 3.5f - Raio(new Vector3(0f, 0.4f, 3.5f), Vector3.back, 1.6f);
        Object.DestroyImmediate(mc);
        PlacaMercosul(ct, new Vector3(0f, 0.86f, zTras - 0.012f), Quaternion.identity, "ABC1D23");
        PlacaMercosul(ct, new Vector3(0f, 0.4f, zFrente + 0.012f), Quaternion.Euler(0f, 180f, 0f), "ABC1D23");
        c.placas = carro.GetComponentsInChildren<TextMeshPro>().Where(t => t.text != "BRASIL").Cast<TMP_Text>().ToArray();

        // Farol de verdade (só os carros mais perto da câmera acendem, à noite)
        var lg = new GameObject("Farol").AddComponent<Light>();
        lg.transform.SetParent(ct, false);
        lg.transform.localPosition = new Vector3(0f, 0.64f, 1.9f);
        lg.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        lg.type = LightType.Spot;
        lg.range = 22f; lg.spotAngle = 75f; lg.innerSpotAngle = 35f; lg.intensity = 2f;
        lg.color = new Color(1f, 0.95f, 0.84f);
        lg.shadows = LightShadows.None;
        lg.lightmapBakeType = LightmapBakeType.Realtime;
        lg.enabled = false;
        c.farol = lg;

        // Colisão (o técnico não atravessa o carro; o carro dele bate)
        var bc1 = carro.AddComponent<BoxCollider>();
        bc1.center = new Vector3(0f, 0.62f, 0f); bc1.size = new Vector3(1.62f, 0.68f, 3.82f);
        var bc2 = carro.AddComponent<BoxCollider>();
        bc2.center = new Vector3(0f, 1.18f, -0.35f); bc2.size = new Vector3(1.36f, 0.46f, 2.3f);
        var rb = carro.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (var t in carro.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        ct.SetParent(pai, false);
        ct.localPosition = new Vector3(0f, -30f, 0f);
        carro.SetActive(false);   // molde: o VidaNaRua copia
    }

    // ---------------- Semáforo (cruzamento da Jacarandás) ----------------

    static void MontarSemaforo(Transform pai)
    {
        var sem = new GameObject("Semaforo_Jacarandas").transform;
        sem.SetParent(pai, false);
        var mPoste = Mat("Semaforo_Poste", new Color(0.2f, 0.21f, 0.22f), 0.35f, 0.5f);
        var mCaixa = Mat("Semaforo_Caixa", new Color(0.05f, 0.05f, 0.05f), 0.3f);
        Material Lente(string nome, Color cor, bool acesa)
        {
            var m = Mat("Semaforo_" + nome + (acesa ? "_Aceso" : "_Apagado"), acesa ? cor : cor * 0.18f, 0.85f);
            Brilhar(m, acesa ? cor * 2.2f : Color.black);
            return m;
        }
        var cores = new[] { new Color(1f, 0.08f, 0.04f), new Color(1f, 0.62f, 0.05f), new Color(0.1f, 1f, 0.45f) };
        string[] nomes = { "Vermelho", "Amarelo", "Verde" };
        var acesos = new Material[3]; var apagados = new Material[3];
        for (int i = 0; i < 3; i++) { acesos[i] = Lente(nomes[i], cores[i], true); apagados[i] = Lente(nomes[i], cores[i], false); }

        Vector3 I = RotasTransito.CentroCruzamento(RotasTransito.CruzamentoJacarandas);
        foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
        {
            // na esquina à direita de quem chega, virado para ele
            float canto = Bairro.MeiaPista + 0.6f;
            Vector3 pos = I + RotasTransito.Direita(d) * canto - d * canto;
            var p = new GameObject("Semaforo").transform;
            p.SetParent(sem, false);
            p.localPosition = pos + Vector3.up * 0.12f;
            p.localRotation = Quaternion.LookRotation(-d);
            Caixa(p, "Coluna", new Vector3(0f, 1.6f, 0f), new Vector3(0.11f, 3.2f, 0.11f), mPoste, 1f);
            Caixa(p, "Caixa", new Vector3(0f, 3.05f, 0f), new Vector3(0.32f, 0.92f, 0.22f), mCaixa, 1f, false);
            var luz = p.gameObject.AddComponent<SemaforoLuzes>();
            luz.eixoAcacias = Mathf.Abs(d.x) > 0.5f;
            luz.acesos = acesos; luz.apagados = apagados;
            luz.lentes = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 3.33f - i * 0.28f;
                luz.lentes[i] = Caixa(p, "Lente_" + nomes[i], new Vector3(0f, y, 0.115f), new Vector3(0.2f, 0.2f, 0.02f), apagados[i], 1f, false, false).GetComponent<Renderer>();
                Caixa(p, "Pala", new Vector3(0f, y + 0.12f, 0.17f), new Vector3(0.24f, 0.02f, 0.12f), mCaixa, 1f, false, false);
            }
            // Linha de retenção (onde não há zebra antes do cruzamento)
            LinhaRetencao(sem, RotasTransito.CruzamentoJacarandas, d);
        }
    }

    // Faixa branca de 40 cm atravessando a mão de quem chega, logo à frente de onde o carro para
    static void LinhaRetencao(Transform pai, int cruzamento, Vector3 d)
    {
        float dist = RotasTransito.DistanciaParada(cruzamento, d);
        if (dist > Bairro.MeiaPista + RotasTransito.FolgaParada + 0.1f) return;   // a zebra já marca
        Vector3 I = RotasTransito.CentroCruzamento(cruzamento);
        Vector3 c = I + RotasTransito.Direita(d) * (Bairro.MeiaPista / 2f) - d * (dist - 0.45f);
        Vector3 tam = Mathf.Abs(d.x) > 0.5f ? new Vector3(0.4f, 0.005f, Bairro.MeiaPista - 0.2f) : new Vector3(Bairro.MeiaPista - 0.2f, 0.005f, 0.4f);
        Caixa(pai, "Linha_Retencao", new Vector3(c.x, 0.012f, c.z), tam, mFaixa, 1f, false, false);
    }

    // ---------------- PARE (cruzamento da Ipê: quem vem pela Ipê para) ----------------

    static void MontarPare(Transform pai)
    {
        var g = new GameObject("Pare_Ipe").transform;
        g.SetParent(pai, false);
        var mVermelho = Mat("Placa_Pare", new Color(0.75f, 0.05f, 0.05f), 0.4f);
        var mPoste = Mat("Semaforo_Poste", new Color(0.2f, 0.21f, 0.22f), 0.35f, 0.5f);
        Vector3 I = RotasTransito.CentroCruzamento(RotasTransito.CruzamentoIpe);
        foreach (var d in new[] { Vector3.forward, Vector3.back })
        {
            float canto = Bairro.MeiaPista + 0.6f;
            var p = new GameObject("Placa_PARE").transform;
            p.SetParent(g, false);
            p.localPosition = I + RotasTransito.Direita(d) * canto - d * canto + Vector3.up * 0.12f;
            p.localRotation = Quaternion.LookRotation(-d);
            Caixa(p, "Coluna", new Vector3(0f, 1.15f, 0f), new Vector3(0.06f, 2.3f, 0.06f), mPoste, 1f);
            var oct = new GameObject("Octogono");
            oct.transform.SetParent(p, false);
            oct.transform.localPosition = new Vector3(0f, 2.15f, 0.04f);
            oct.AddComponent<MeshFilter>().sharedMesh = Octogono(0.33f);
            oct.AddComponent<MeshRenderer>().sharedMaterial = mVermelho;
            var t = new GameObject("Texto_PARE").AddComponent<TextMeshPro>();
            t.transform.SetParent(p, false);
            t.transform.localPosition = new Vector3(0f, 2.15f, 0.055f);
            t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            t.text = "PARE"; t.color = Color.white; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 0.1f; t.fontSizeMax = 3f;
            t.rectTransform.sizeDelta = new Vector2(0.5f, 0.2f);

            // "PARE" pintado no asfalto e a linha de retenção
            LinhaRetencao(g, RotasTransito.CruzamentoIpe, d);
            float dist = RotasTransito.DistanciaParada(RotasTransito.CruzamentoIpe, d);
            var chao = new GameObject("Pintura_PARE").AddComponent<TextMeshPro>();
            chao.transform.SetParent(g, false);
            Vector3 pc = I + RotasTransito.Direita(d) * (Bairro.MeiaPista / 2f) - d * (dist + 1.6f);
            chao.transform.localPosition = new Vector3(pc.x, 0.015f, pc.z);
            chao.transform.localRotation = Quaternion.LookRotation(Vector3.down, d);
            chao.text = "PARE"; chao.color = new Color(0.92f, 0.92f, 0.9f); chao.fontStyle = FontStyles.Bold; chao.alignment = TextAlignmentOptions.Center;
            chao.enableAutoSizing = true; chao.fontSizeMin = 1f; chao.fontSizeMax = 30f;
            chao.rectTransform.sizeDelta = new Vector2(2.6f, 1.4f);
            chao.transform.localScale = new Vector3(1f, 1.8f, 1f);   // letra alongada, como a pintura de rua
            chao.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    // Octógono em pé, com a frente para +z (para quem chega) e o verso para -z (vértices separados: normais certas)
    static Mesh Octogono(float raio)
    {
        var m = new Mesh { name = "Octogono_PARE" };
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
        foreach (float lado in new[] { 1f, -1f })
        {
            int b0 = v.Count;
            v.Add(Vector3.zero); n.Add(Vector3.forward * lado);
            for (int i = 0; i < 8; i++) { float a = (22.5f + 45f * i) * Mathf.Deg2Rad; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * raio); n.Add(Vector3.forward * lado); }
            for (int i = 0; i < 8; i++)
            {
                int x = b0 + 1 + i, y = b0 + 1 + (i + 1) % 8;
                if (lado > 0) { tris.Add(b0); tris.Add(x); tris.Add(y); } else { tris.Add(b0); tris.Add(y); tris.Add(x); }
            }
        }
        m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }
    // ---------------- Janelas acesas à noite ----------------

    // Para onde fica a frente de um imóvel do bairro (a rua mais perto do centro dele): Vector3.zero se não for lote
    public static Vector3 FachadaDe(Vector3 c)
    {
        var opcoes = new List<(float d, Vector3 dir)>();
        if (c.z > Bairro.FrenteNorteAcacias) opcoes.Add((c.z - Bairro.FrenteNorteAcacias, Vector3.back));
        if (c.z < Bairro.FrenteSulAcacias) opcoes.Add((Bairro.FrenteSulAcacias - c.z, Vector3.forward));
        if (c.x < Bairro.FrenteOesteIpe) opcoes.Add((Bairro.FrenteOesteIpe - c.x, Vector3.right));
        if (c.x > Bairro.FrenteLesteIpe && c.x < 0f) opcoes.Add((c.x - Bairro.FrenteLesteIpe, Vector3.left));
        if (c.x < Bairro.FrenteOesteJacarandas && c.x > 0f) opcoes.Add((Bairro.FrenteOesteJacarandas - c.x, Vector3.right));
        if (c.x > Bairro.FrenteLesteJacarandas) opcoes.Add((c.x - Bairro.FrenteLesteJacarandas, Vector3.left));
        if (opcoes.Count == 0) return Vector3.zero;
        return opcoes.OrderBy(o => o.d).First().dir;
    }

    // Janelas de cada modelo, medidas nas fotos ortográficas da fachada (menu Teste gráfico/Fotos das fachadas):
    // (u0, v0, u1, v1) em metros, a partir do canto de baixo à esquerda de quem olha da rua
    static Dictionary<string, Vector4[]> JanelasDosModelos => new Dictionary<string, Vector4[]>
    {
        ["Casa_Comum_Brasileira"] = new[] { new Vector4(5.80f, 4.34f, 7.79f, 5.51f), new Vector4(9.16f, 4.34f, 11.14f, 5.51f), new Vector4(9.16f, 1.53f, 11.14f, 2.70f) },
        ["Casa_Terrea"] = new[] { new Vector4(1.79f, 2.11f, 2.73f, 3.08f) },
        ["Sobrado_Tijolo"] = new[] { new Vector4(0.88f, 4.38f, 2.23f, 5.36f), new Vector4(3.55f, 4.38f, 4.25f, 5.36f) },
        ["Sobrado_Porta_Enrolar"] = new[] { new Vector4(0.56f, 4.33f, 2.03f, 5.75f), new Vector4(3.81f, 4.33f, 5.23f, 5.75f) },
        ["Predio_Hospital"] = JanelasHospital(),
    };

    // Hospital: 3 andares com 11 janelas cada (a cada 2,09 m)
    static Vector4[] JanelasHospital()
    {
        var l = new List<Vector4>();
        foreach (var (v0, v1) in new[] { (5.46f, 6.61f), (8.64f, 9.79f), (11.81f, 12.96f) })
            for (int i = 0; i < 11; i++) { float u0 = (150f + 167.4f * i) / 80f; l.Add(new Vector4(u0, v0, u0 + 1.52f, v1)); }
        return l.ToArray();
    }

    static void MontarJanelasNoturnas()
    {
        var viz = raiz.Find("Vizinhanca");
        if (viz == null) return;
        var mAcesa = Mat("Janela_Acesa", new Color(0.35f, 0.3f, 0.22f), 0.6f);
        Brilhar(mAcesa, new Color(1f, 0.78f, 0.48f) * 1.15f);
        var mTv = Mat("Janela_Acesa_TV", new Color(0.2f, 0.24f, 0.32f), 0.6f);
        Brilhar(mTv, new Color(0.55f, 0.7f, 1f) * 0.9f);
        var pai = new GameObject("Janelas_Noturnas").transform;
        pai.SetParent(raiz, false);
        var lista = new List<Renderer>();
        int n = 0;
        foreach (Transform t in viz)
        {
            if (!JanelasDosModelos.TryGetValue(t.name, out var janelas)) continue;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 dir = FachadaDe(b.center);
            if (dir == Vector3.zero) continue;
            Vector3 direita = Vector3.Cross(Vector3.up, -dir);
            float largura = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(direita.x), 0f, Mathf.Abs(direita.z))));
            Vector3 cantoFrente = b.center + dir * (Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z))))) - direita * (largura / 2f);
            cantoFrente.y = b.min.y;
            // colisão temporária para achar a parede (a janela fica rente, atrás da grade se tiver)
            var temp = new List<MeshCollider>();
            foreach (var r in rs) if (r.GetComponent<MeshFilter>() is MeshFilter mf && mf.sharedMesh != null && r.GetComponent<Collider>() == null) { var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); }
            Physics.SyncTransforms();
            foreach (var j in janelas)
            {
                Vector3 meio = cantoFrente + direita * ((j.x + j.z) / 2f) + Vector3.up * ((j.y + j.w) / 2f);
                float fundo = -1f;
                foreach (var off in new[] { Vector2.zero, new Vector2(0.15f, 0.15f), new Vector2(-0.15f, -0.15f), new Vector2(0.15f, -0.15f), new Vector2(-0.15f, 0.15f) })
                {
                    Vector3 de = meio + direita * off.x + Vector3.up * off.y + dir * 3f;
                    // primeira coisa deste imóvel que o raio toca (marquise do hospital pode ficar a mais de 3 m da parede)
                    float primeira = float.MaxValue;
                    foreach (var h in Physics.RaycastAll(de, -dir, 25f))
                        if (h.collider is MeshCollider && h.collider.transform.IsChildOf(t)) primeira = Mathf.Min(primeira, h.distance);
                    if (primeira < float.MaxValue) fundo = Mathf.Max(fundo, primeira);
                }
                if (fundo < 0f) { Debug.LogWarning($"[Cenário] Janela sem parede em {t.name} ({meio})."); continue; }
                // entre os raios, o toque mais fundo (vidro): a luz fica atrás da grade/caixilho
                Vector3 pos = meio + dir * (3f - fundo + 0.015f);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "Janela_Acesa";
                q.transform.SetParent(pai, true);
                q.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-dir));
                q.transform.localScale = new Vector3((j.z - j.x) * 0.94f, (j.w - j.y) * 0.94f, 1f);
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = (n++ % 7 == 3) ? mTv : mAcesa;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.enabled = false;
                GameObjectUtility.SetStaticEditorFlags(q, 0);
                lista.Add(mr);
            }
            foreach (var mc in temp) Object.DestroyImmediate(mc);
        }
        // Janelas do andar de cima da clínica (prédio montado por código)
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name != "Janela_Andar") continue;
            var q = Object.Instantiate(r.gameObject, pai, true);
            q.name = "Janela_Acesa";
            var c = q.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mAcesa; mr.enabled = false; mr.shadowCastingMode = ShadowCastingMode.Off;
            q.transform.localScale = Vector3.Scale(q.transform.localScale, new Vector3(1f, 1f, 1.5f));
            GameObjectUtility.SetStaticEditorFlags(q, 0);
            lista.Add(mr);
        }
        var jn = pai.gameObject.AddComponent<JanelasNoturnas>();
        jn.janelas = lista.ToArray();
        Debug.Log($"[Cenário] Janelas que acendem à noite: {lista.Count}.");
    }
}
