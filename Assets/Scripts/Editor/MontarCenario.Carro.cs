using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte do MontarCenario (Etapa 9C): o carro do técnico, o posto de combustível e o fim das ruas.
// Carro: modelo "Milano '95" (CC-BY, Sketchfab) sem emblemas nem placas originais; rodas e volante viram pivôs; a
// porta do motorista e a tampa traseira são recortadas da carroceria (triângulos dentro de uma caixa) e ganham
// dobradiça; faróis com luz de verdade; placas Mercosul fictícias; WheelCollider nas 4 rodas; porta-malas com a
// escada (CC-BY) dobrada e a maleta grande. Coordenadas do carro: frente +z, motorista à esquerda (-x), chão y = 0.
public static partial class MontarCenario
{
    // Recortes medidos nas fotos do modelo (menu Tmp/Fotos do modelo do carro), no espaço do carro
    const float PortaZFrente = 0.84f, PortaZTras = -0.56f, PortaXFora = -0.55f, PortaY0 = 0.2f, PortaY1 = 1.34f;
    const float TampaZ = -1.6f, TampaY0 = 0.64f, TampaMeiaLargura = 0.6f;
    public static Vector3 OlhoMotorista => new Vector3(-0.33f, 1.13f, -0.2f);

    static void MontarCarroEPosto()
    {
        var pai = new GameObject("Cidade_Carro").transform;
        pai.SetParent(raiz, false);
        MontarPosto(pai);
        MontarFimDasRuas(pai);
        var v = Bairro.VagaDe(TipoEndereco.Casa);
        MontarCarro(pai, v.centro, v.giro);
    }

    // ---------------- Carro ----------------

    static void MontarCarro(Transform pai, Vector3 pos, float giro)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SF("Carro_Milano95"));
        if (prefab == null) { Debug.LogError("[Cenário] Modelo do carro não encontrado."); return; }

        var carro = new GameObject("CarroTecnico");
        var ct = carro.transform;   // monta na origem, sem giro; posiciona no fim
        var suporte = new GameObject("Modelo").transform;
        suporte.SetParent(ct, false);
        suporte.localRotation = Quaternion.Euler(0f, 90f, 0f);   // o arquivo tem a frente para -x
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var rs = modelo.GetComponentsInChildren<Renderer>(true).ToList();
        Renderer R(string parte) => rs.FirstOrDefault(r => r.name.Contains(parte));
        // Chão no pé dos pneus e centro do carro na origem
        float chao = rs.Where(r => r.name.Contains("WheelStock")).Min(r => r.bounds.min.y);
        var b = Limites(modelo);
        suporte.localPosition = new Vector3(-b.center.x, -chao, -b.center.z);
        foreach (var r in rs) { r.shadowCastingMode = ShadowCastingMode.On; r.lightProbeUsage = LightProbeUsage.Off; }

        // Rodas: pivô no centro de cada uma (a física move o pivô)
        string[] nomesRoda = { "WheelStock_FL", "WheelStock_FR", "WheelStock_RL", "WheelStock_RR" };
        string[] siglas = { "FE", "FD", "TE", "TD" };
        var c = carro.AddComponent<CarroTecnico>();
        var rb = carro.AddComponent<Rigidbody>();
        rb.mass = 1050f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = true;
        c.centroMassa = new Vector3(0f, 0.36f, 0.12f);
        for (int i = 0; i < 4; i++)
        {
            var rr = R(nomesRoda[i]);
            var piv = Pivo("Roda_" + siglas[i], rr, ct, Quaternion.identity);
            c.rodasVisuais[i] = piv;
            float raio = rr.bounds.size.y / 2f;
            var wgo = new GameObject("Roda_Fisica_" + siglas[i]);
            wgo.transform.SetParent(ct, false);
            wgo.transform.localPosition = piv.localPosition + Vector3.up * 0.09f;
            var wc = wgo.AddComponent<WheelCollider>();
            wc.radius = raio;
            wc.mass = 18f;
            wc.suspensionDistance = 0.17f;
            wc.forceAppPointDistance = 0.12f;
            wc.wheelDampingRate = 0.6f;
            wc.suspensionSpring = new JointSpring { spring = 32000f, damper = 4200f, targetPosition = 0.5f };
            wc.forwardFriction = new WheelFrictionCurve { extremumSlip = 0.4f, extremumValue = 1f, asymptoteSlip = 0.8f, asymptoteValue = 0.6f, stiffness = 1.6f };
            wc.sidewaysFriction = new WheelFrictionCurve { extremumSlip = 0.25f, extremumValue = 1f, asymptoteSlip = 0.5f, asymptoteValue = 0.75f, stiffness = 1.9f };
            c.rodas[i] = wc;
        }

        // Volante: o eixo da coluna aponta para o painel, um pouco para baixo (~18° de inclinação)
        var vol = R("Steering_Wheel");
        Vector3 eixo = new Vector3(0f, -Mathf.Sin(18f * Mathf.Deg2Rad), Mathf.Cos(18f * Mathf.Deg2Rad));
        c.volante = Pivo("Volante", vol, ct, Quaternion.LookRotation(eixo, Vector3.up));
        c.volanteBase = c.volante.localRotation;

        // Colisão temporária para medir com raios (assoalho do porta-malas, lugar das placas)
        var temp = new List<MeshCollider>();
        foreach (var r in rs) if (r.GetComponent<MeshFilter>() is MeshFilter mf && mf.sharedMesh != null) { var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); }
        Physics.SyncTransforms();
        // só os colisores do carro contam (ele é montado na origem, em cima do escritório)
        float Raio(Vector3 de, Vector3 dir, float padrao)
        {
            float melhor = padrao;
            bool achou = false;
            foreach (var h in Physics.RaycastAll(de, dir, 5f))
                if (temp.Contains(h.collider as MeshCollider) && (!achou || h.distance < melhor)) { melhor = h.distance; achou = true; }
            return melhor;
        }
        float zPlacaTras = -3.5f + Raio(new Vector3(0f, 0.86f, -3.5f), Vector3.forward, 1.55f);
        float zPlacaFrente = 3.5f - Raio(new Vector3(0f, 0.4f, 3.5f), Vector3.back, 1.6f);
        float pisoMalas = 0.85f - Raio(new Vector3(0f, 0.85f, -1.45f), Vector3.down, 0.35f);   // abaixo do tampão
        foreach (var mc in temp) Object.DestroyImmediate(mc);
        Debug.Log($"[Cenário] Carro: placa traseira z {zPlacaTras:F2}, dianteira z {zPlacaFrente:F2}, assoalho do porta-malas y {pisoMalas:F2}.");

        // Porta do motorista e tampa traseira recortadas da carroceria
        var porta = new GameObject("Porta_Motorista").transform;
        porta.SetParent(ct, false);
        porta.localPosition = new Vector3(-0.8f, 0.75f, PortaZFrente);
        bool NaPorta(Vector3 p, bool interior) => p.x < (interior ? -0.6f : PortaXFora) && p.z < PortaZFrente && p.z > PortaZTras && p.y > PortaY0 && p.y < PortaY1;
        int nPorta = 0;
        foreach (var nome in new[] { "Milano95_Body_", "StockKit", "Stock_Trim" }) nPorta += Recortar(R(nome), ct, porta, p => NaPorta(p, false));
        nPorta += Recortar(R("interior"), ct, porta, p => NaPorta(p, true));
        var vidro = R("Glass_Driver");
        if (vidro != null) vidro.transform.SetParent(porta, true);
        c.porta = porta;
        c.portaAberta = new Vector3(0f, 62f, 0f);

        var tampa = new GameObject("Tampa_Traseira").transform;
        tampa.SetParent(ct, false);
        tampa.localPosition = new Vector3(0f, 1.37f, -1.45f);
        bool NaTampa(Vector3 p) => p.z < TampaZ && p.y > TampaY0 && Mathf.Abs(p.x) < TampaMeiaLargura;
        int nTampa = 0;
        foreach (var nome in new[] { "Milano95_Body_", "StockKit" }) nTampa += Recortar(R(nome), ct, tampa, NaTampa);
        var vidroTras = R("Glass_Rear");
        if (vidroTras != null) vidroTras.transform.SetParent(tampa, true);
        c.tampa = tampa;
        c.tampaAberta = new Vector3(78f, 0f, 0f);
        Debug.Log($"[Cenário] Carro: porta com {nPorta} triângulos, tampa com {nTampa}.");

        // Luzes: materiais próprios com emissão (o script acende/apaga) e dois faróis de verdade
        var mFarol = Mat("Carro_Farol", new Color(0.92f, 0.92f, 0.88f), 0.9f, 0.2f);
        Brilhar(mFarol, Color.black);
        var mFreio = Mat("Carro_LuzFreio", new Color(0.45f, 0.03f, 0.02f), 0.8f);
        Brilhar(mFreio, Color.black);
        var mRe = Mat("Carro_LuzRe", new Color(0.85f, 0.85f, 0.85f), 0.8f);
        Brilhar(mRe, Color.black);
        Renderer[] Trocar(string parte, Material m) { var r = R(parte); if (r == null) return new Renderer[0]; r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray(); return new[] { r }; }
        c.lampadasFarol = Trocar("Milano95_Headlights_UCB", mFarol);
        c.lampadasFreio = Trocar("Brakelights", mFreio);
        c.lampadasRe = Trocar("Reverse", mRe);
        var farois = new List<Light>();
        foreach (float x in new[] { -0.55f, 0.55f })
        {
            var lg = new GameObject("Farol").AddComponent<Light>();
            lg.transform.SetParent(ct, false);
            lg.transform.localPosition = new Vector3(x, 0.64f, 1.85f);
            lg.transform.localRotation = Quaternion.Euler(7f, x * 6f, 0f);
            lg.type = LightType.Spot;
            lg.range = 28f;
            lg.spotAngle = 68f;
            lg.innerSpotAngle = 30f;
            lg.intensity = 2.4f;
            lg.color = new Color(1f, 0.95f, 0.84f);
            lg.shadows = LightShadows.None;
            lg.renderMode = LightRenderMode.ForcePixel;
            lg.lightmapBakeType = LightmapBakeType.Realtime;
            lg.enabled = false;
            farois.Add(lg);
        }
        c.farois = farois.ToArray();

        // Placas Mercosul (fictícias)
        PlacaMercosul(tampa, tampa.InverseTransformPoint(ct.TransformPoint(new Vector3(0f, 0.86f, zPlacaTras - 0.012f))), Quaternion.identity, "TIS4C26");   // a placa de trás sobe com a tampa
        PlacaMercosul(ct, new Vector3(0f, 0.4f, zPlacaFrente + 0.012f), Quaternion.Euler(0f, 180f, 0f), "TIS4C26");

        // Colisão da carroceria (baixa: passa no meio-fio de 12 cm sem raspar) e da cabine
        var bc1 = carro.AddComponent<BoxCollider>();
        bc1.center = new Vector3(0f, 0.62f, 0f); bc1.size = new Vector3(1.62f, 0.68f, 3.82f);
        var bc2 = carro.AddComponent<BoxCollider>();
        bc2.center = new Vector3(0f, 1.18f, -0.35f); bc2.size = new Vector3(1.36f, 0.46f, 2.3f);

        // Pontos do motorista
        var assento = new GameObject("Assento_Motorista").transform;
        assento.SetParent(ct, false);
        assento.localPosition = OlhoMotorista;
        c.pontoAssento = assento;
        var saida = new GameObject("Saida_Motorista").transform;
        saida.SetParent(ct, false);
        saida.localPosition = new Vector3(-1.5f, 0f, 0.1f);
        c.pontoSaida = saida;

        var pc = new GameObject("Porta_E").AddComponent<PortaCarro>();
        pc.transform.SetParent(ct, false);
        pc.transform.localPosition = new Vector3(-0.86f, 0.95f, 0.15f);
        var pcCol = pc.gameObject.AddComponent<BoxCollider>();
        pcCol.isTrigger = true; pcCol.size = new Vector3(0.12f, 0.7f, 1.1f);
        pc.carro = c;
        pc.distanciaInteracao = 1.8f;

        // Porta-malas: vagas no assoalho, escada dobrada encostada no banco e a maleta no canto
        var pm = new GameObject("PortaMalas_E").AddComponent<PortaMalas>();
        pm.transform.SetParent(ct, false);
        pm.transform.localPosition = new Vector3(0f, 0.92f, -1.98f);
        var pmCol = pm.gameObject.AddComponent<BoxCollider>();
        pmCol.isTrigger = true; pmCol.size = new Vector3(1.1f, 0.5f, 0.12f);
        pm.carro = c;
        pm.distanciaInteracao = 1.9f;
        var vagasPm = new GameObject("Vagas_PortaMalas").transform;
        vagasPm.SetParent(ct, false);
        Transform Vaga(string nome, Vector3 p, Quaternion r) { var t = new GameObject(nome).transform; t.SetParent(vagasPm, false); t.localPosition = p; t.localRotation = r; return t; }
        float y = pisoMalas + 0.01f;
        pm.vagas = new[]
        {
            Vaga("Vaga_1", new Vector3(-0.4f, y, -1.46f), Quaternion.identity),
            Vaga("Vaga_2", new Vector3(-0.05f, y, -1.46f), Quaternion.identity),
            Vaga("Vaga_3", new Vector3(-0.4f, y + 0.34f, -1.46f), Quaternion.identity),
            Vaga("Vaga_4", new Vector3(-0.05f, y + 0.34f, -1.46f), Quaternion.identity),
        };
        pm.vagaEscada = Vaga("Vaga_Escada", new Vector3(0.6f, y + 0.23f, -1.14f), Quaternion.Euler(0f, 0f, 90f));
        pm.vagaMaleta = Vaga("Vaga_Maleta", new Vector3(0.34f, y, -1.45f), Quaternion.identity);
        pm.escada = Escada(ct);
        pm.maleta = Maleta(ct);

        foreach (var t in carro.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        ct.SetParent(pai, false);
        ct.SetPositionAndRotation(pos + Vector3.up * 0.02f, Quaternion.Euler(0f, giro, 0f));
    }

    // Pivô no centro do renderer, com a rotação dada (no espaço do carro); o renderer vira filho sem sair do lugar
    static Transform Pivo(string nome, Renderer r, Transform carro, Quaternion rotLocal)
    {
        var p = new GameObject(nome).transform;
        p.SetParent(carro, false);
        p.position = r.bounds.center;
        p.localRotation = rotLocal;
        r.transform.SetParent(p, true);
        return p;
    }

    // Tira da malha do renderer os triângulos cujo centro (no espaço do carro) cai na região, e põe esses triângulos
    // numa malha nova, filha de 'destino', no mesmo lugar. Devolve quantos triângulos saíram.
    static int Recortar(Renderer r, Transform carro, Transform destino, System.Func<Vector3, bool> regiao)
    {
        if (r == null) return 0;
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 0;
        var original = mf.sharedMesh;
        var verts = original.vertices;
        var paraCarro = carro.worldToLocalMatrix * r.transform.localToWorldMatrix;
        var fica = Object.Instantiate(original); fica.name = original.name + "_sem_" + destino.name;
        var sai = Object.Instantiate(original); sai.name = original.name + "_" + destino.name;
        int n = 0;
        for (int s = 0; s < original.subMeshCount; s++)
        {
            var tris = original.GetTriangles(s);
            var a = new List<int>(); var bb = new List<int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 centro = paraCarro.MultiplyPoint3x4((verts[tris[i]] + verts[tris[i + 1]] + verts[tris[i + 2]]) / 3f);
                var lista = regiao(centro) ? bb : a;
                lista.Add(tris[i]); lista.Add(tris[i + 1]); lista.Add(tris[i + 2]);
            }
            fica.SetTriangles(a, s);
            sai.SetTriangles(bb, s);
            n += bb.Count / 3;
        }
        if (n == 0) { Object.DestroyImmediate(fica); Object.DestroyImmediate(sai); return 0; }
        mf.sharedMesh = fica;
        var go = new GameObject(r.name + "_parte");
        go.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
        go.transform.localScale = r.transform.lossyScale;
        go.transform.SetParent(destino, true);
        go.AddComponent<MeshFilter>().sharedMesh = sai;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = r.sharedMaterials;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.lightProbeUsage = LightProbeUsage.Off;
        return n;
    }

    // Placa Mercosul: fundo branco, faixa azul com "BRASIL", letras pretas (40 x 13 cm)
    static void PlacaMercosul(Transform carro, Vector3 pos, Quaternion rot, string texto)
    {
        var p = new GameObject("Placa_Mercosul").transform;
        p.SetParent(carro, false);
        p.localPosition = pos;
        p.localRotation = rot;
        Caixa(p, "Fundo", Vector3.zero, new Vector3(0.4f, 0.13f, 0.006f), Mat("Placa_Branca", new Color(0.95f, 0.95f, 0.95f), 0.5f), 1f, false, false);
        Caixa(p, "Faixa_Azul", new Vector3(0f, 0.05f, -0.0035f), new Vector3(0.4f, 0.03f, 0.002f), Mat("Placa_Azul", new Color(0.05f, 0.22f, 0.6f), 0.5f), 1f, false, false);
        TextoPlaca(p, "BRASIL", new Vector3(0f, 0.05f, -0.0048f), 0.12f, Color.white, 0.018f);
        TextoPlaca(p, texto, new Vector3(0f, -0.012f, -0.0048f), 0.38f, Color.black, 0.075f);
    }

    static void TextoPlaca(Transform pai, string s, Vector3 pos, float largura, Color cor, float altura)
    {
        var go = new GameObject("Texto_" + s);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.text = s;
        t.color = cor;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 0.05f; t.fontSizeMax = 2f;
        t.rectTransform.sizeDelta = new Vector2(largura, altura);
        t.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // Escada de alumínio (CC-BY): no porta-malas fica dobrada (achatada), no chão abre
    static CargaFisica Escada(Transform carro)
    {
        var go = new GameObject("Carga_Escada");
        go.transform.SetParent(carro, false);
        var m = ModeloReal(SF("Escada_Aluminio"), go.transform, Vector3.zero, 1.19f, 0f, Colisao.Nenhuma);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.6f, 0f); bc.size = new Vector3(0.46f, 1.2f, 0.3f);
        var c = go.AddComponent<CargaFisica>();
        c.item = new ItemCarga { tipo = TipoCarga.Escada, rotulo = "Escada de alumínio (4 degraus)" };
        c.tamanho = new Vector3(0.46f, 1.2f, 0.12f);
        c.distanciaInteracao = 1.9f;
        if (m != null)
        {
            c.modelo = m.transform;
            c.escalaAberta = m.transform.localScale;
            c.escalaDobrada = Vector3.Scale(m.transform.localScale, new Vector3(1f, 1f, 0.18f));
        }
        return c;
    }

    // Maleta grande de ferramentas (plástico laranja e preto, alça e travas)
    static CargaFisica Maleta(Transform carro)
    {
        var go = new GameObject("Carga_Maleta");
        go.transform.SetParent(carro, false);
        var laranja = Mat("Maleta_Laranja", new Color(0.85f, 0.38f, 0.07f), 0.45f);
        var preto = Mat("Maleta_Preto", new Color(0.06f, 0.06f, 0.07f), 0.35f);
        Caixa(go.transform, "Corpo", new Vector3(0f, 0.1f, 0f), new Vector3(0.26f, 0.2f, 0.5f), preto, 1f, false);
        Caixa(go.transform, "Tampa", new Vector3(0f, 0.23f, 0f), new Vector3(0.27f, 0.06f, 0.51f), laranja, 1f, false);
        Caixa(go.transform, "Alca", new Vector3(0f, 0.285f, 0f), new Vector3(0.04f, 0.03f, 0.2f), preto, 1f, false);
        foreach (float z in new[] { -0.17f, 0.17f })
            Caixa(go.transform, "Trava", new Vector3(0.137f, 0.2f, z), new Vector3(0.01f, 0.05f, 0.04f), mAluminio, 1f, false, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.14f, 0f); bc.size = new Vector3(0.28f, 0.3f, 0.52f);
        var c = go.AddComponent<CargaFisica>();
        c.item = new ItemCarga { tipo = TipoCarga.Maleta, rotulo = "Maleta grande de ferramentas" };
        c.tamanho = new Vector3(0.28f, 0.3f, 0.52f);
        c.distanciaInteracao = 1.9f;
        return c;
    }

    // ---------------- Posto de combustível ----------------

    static void MontarPosto(Transform pai)
    {
        var p = new GameObject("Posto").transform;
        p.SetParent(pai, false);
        var t = Bairro.TerrenoPosto;
        var mPiso = MatPBR("Piso_Posto", "Concrete034", new Color(0.7f, 0.7f, 0.68f), 0.15f);
        var mBranco = Mat("Posto_Branco", new Color(0.92f, 0.93f, 0.92f), 0.4f);
        var mVerde = Mat("Posto_Verde", new Color(0.1f, 0.45f, 0.28f), 0.45f);
        var mAmarelo = Mat("Posto_Amarelo", new Color(0.95f, 0.75f, 0.1f), 0.4f);
        Caixa(p, "Piso_Patio", new Vector3(t.center.x, -0.01f, t.center.y), new Vector3(t.width, 0.04f, t.height), mPiso, 2f);

        // Ilha e bomba (o visor olha para a vaga, ao sul)
        var bomba = Bairro.BombaPosto;
        Caixa(p, "Ilha", new Vector3(bomba.x, 0.11f, bomba.z), new Vector3(3.4f, 0.22f, 1.0f), mCalcada, 1.5f);
        Caixa(p, "Ilha_Faixa", new Vector3(bomba.x, 0.11f, bomba.z - 0.505f), new Vector3(3.4f, 0.2f, 0.01f), mAmarelo, 1f, false, false);
        var b = new GameObject("Bomba").transform;
        b.SetParent(p, false);
        b.localPosition = bomba + Vector3.up * 0.22f;
        Caixa(b, "Gabinete", new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 1.8f, 0.5f), mBranco, 1f);
        Caixa(b, "Faixa_Verde", new Vector3(0f, 1.62f, -0.252f), new Vector3(0.95f, 0.28f, 0.01f), mVerde, 1f, false, false);
        Caixa(b, "Visor_Fundo", new Vector3(0f, 1.25f, -0.253f), new Vector3(0.6f, 0.3f, 0.01f), mPlacaFundo, 1f, false, false);
        var visor = new GameObject("Visor").AddComponent<TextMeshPro>();
        visor.transform.SetParent(b, false);
        visor.transform.localPosition = new Vector3(0f, 1.25f, -0.26f);
        visor.rectTransform.sizeDelta = new Vector2(0.56f, 0.26f);
        visor.text = "R$ 0,00\n0,00 L";
        visor.color = new Color(1f, 0.45f, 0.2f);
        visor.alignment = TextAlignmentOptions.Center;
        visor.enableAutoSizing = true; visor.fontSizeMin = 0.1f; visor.fontSizeMax = 1.2f;
        var nome = new GameObject("Nome").AddComponent<TextMeshPro>();
        nome.transform.SetParent(b, false);
        nome.transform.localPosition = new Vector3(0f, 1.62f, -0.26f);
        nome.rectTransform.sizeDelta = new Vector2(0.9f, 0.22f);
        nome.text = "GASOLINA COMUM";
        nome.color = Color.white; nome.fontStyle = FontStyles.Bold;
        nome.alignment = TextAlignmentOptions.Center;
        nome.enableAutoSizing = true; nome.fontSizeMin = 0.1f; nome.fontSizeMax = 1f;
        // Bico na lateral e a mangueira pendurada
        var bico = Caixa(b, "Bico", new Vector3(0.52f, 1.0f, -0.05f), new Vector3(0.06f, 0.22f, 0.08f), Mat("Bico_Preto", new Color(0.05f, 0.05f, 0.05f), 0.4f), 1f, false, false).transform;
        for (int i = 0; i < 6; i++)
        {
            float a = i / 5f;
            Caixa(b, "Mangueira", new Vector3(0.52f + Mathf.Sin(a * Mathf.PI) * 0.12f, 1.6f - a * 0.55f, -0.05f), new Vector3(0.035f, 0.12f, 0.035f), mFio, 1f, false, false);
        }
        var posto = b.gameObject.AddComponent<PostoCombustivel>();
        posto.visor = visor;
        posto.bico = bico;
        posto.distanciaInteracao = 2.6f;

        // Cobertura sobre a ilha, em dois pilares
        Caixa(p, "Cobertura", new Vector3(bomba.x, 4.9f, bomba.z - 0.8f), new Vector3(9f, 0.55f, 7.5f), mBranco, 2f);
        Caixa(p, "Cobertura_Faixa", new Vector3(bomba.x, 4.9f, bomba.z - 4.56f), new Vector3(9.02f, 0.4f, 0.02f), mVerde, 1f, false, false);
        foreach (float dx in new[] { -1.4f, 1.4f })
            Caixa(p, "Pilar", new Vector3(bomba.x + dx, 2.4f, bomba.z), new Vector3(0.35f, 4.8f, 0.35f), mBranco, 1f);
        PlacaCobertura(p, new Vector3(bomba.x, 4.9f, bomba.z - 4.58f), "AUTO POSTO IPÊ");

        // Lojinha de conveniência no fundo do terreno
        float lx = t.xMax - 2.6f, lz = t.center.y;
        Caixa(p, "Conveniencia", new Vector3(lx, 1.6f, lz), new Vector3(5f, 3.2f, 6f), mBranco, 2f);
        Caixa(p, "Conveniencia_Vitrine", new Vector3(lx - 2.51f, 1.25f, lz), new Vector3(0.02f, 2f, 4.6f), mJanelaEscura, 1f, false, false);
        Placa(p, new Vector3(lx - 2.62f, 2.75f, lz), Quaternion.Euler(0f, 90f, 0f), 3.4f, 0.5f, "CONVENIÊNCIA", null);

        // Totem com o preço, na esquina do terreno
        float tx = t.xMin + 0.8f, tz = t.yMax - 0.4f;
        Caixa(p, "Totem_Haste", new Vector3(tx, 1.6f, tz), new Vector3(0.18f, 3.2f, 0.18f), mAluminio, 1f);
        Placa(p, new Vector3(tx, 3.5f, tz), Quaternion.Euler(0f, 90f, 0f), 1.4f, 0.8f, "GASOLINA", $"R$ {PostoCombustivel.PrecoLitro:0.00}".Replace('.', ','));
    }

    static void PlacaCobertura(Transform pai, Vector3 pos, string s)
    {
        var t = new GameObject("Nome_Posto").AddComponent<TextMeshPro>();
        t.transform.SetParent(pai, false);
        t.transform.localPosition = pos + new Vector3(0f, 0f, -0.02f);
        t.rectTransform.sizeDelta = new Vector2(6f, 0.38f);
        t.text = s; t.color = Color.white; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 0.1f; t.fontSizeMax = 4f;
    }

    // ---------------- Fim das ruas ----------------
    // Barreiras de concreto zebradas no fim de cada rua e um muro invisível em volta do bairro (o carro não sai)
    static void MontarFimDasRuas(Transform pai)
    {
        var f = new GameObject("Fim_Das_Ruas").transform;
        f.SetParent(pai, false);
        var mZebra = Mat("Barreira_Amarela", new Color(0.95f, 0.78f, 0.1f), 0.3f);
        void Barreira(Vector3 centro, bool aoLongoDeX, float comprimento)
        {
            int n = Mathf.RoundToInt(comprimento / 1f);
            for (int i = 0; i < n; i++)
            {
                float o = -comprimento / 2f + 0.5f + i;
                Vector3 pos = centro + (aoLongoDeX ? new Vector3(o, 0.4f, 0f) : new Vector3(0f, 0.4f, o));
                Caixa(f, "Barreira", pos, aoLongoDeX ? new Vector3(0.95f, 0.8f, 0.45f) : new Vector3(0.45f, 0.8f, 0.95f), i % 2 == 0 ? mZebra : mCalcada, 1f);
            }
        }
        // uma barreira em cada ponta da malha (fim de rua na borda do mapa)
        var m = Bairro.Malha;
        foreach (var pt in m.pontas)
        {
            var r = m.ruas[pt.rua];
            Barreira(r.Ponto(pt.lado < 0 ? r.ini + 0.3f : r.fim - 0.3f), !r.lesteOeste, 2f * r.meiaPista);
        }
        // Muro invisível em volta (só colisão)
        var lim = m.limites;
        float w = lim.width + 4f, xMeio = lim.center.x, zMeio = lim.center.y, h = lim.height + 4f;
        void Invisivel(Vector3 c, Vector3 tam) { var g = new GameObject("Limite_Invisivel"); g.transform.SetParent(f, false); g.transform.localPosition = c; g.AddComponent<BoxCollider>().size = tam; }
        Invisivel(new Vector3(xMeio, 2f, lim.yMin - 2f), new Vector3(w, 4f, 0.5f));
        Invisivel(new Vector3(xMeio, 2f, lim.yMax + 2f), new Vector3(w, 4f, 0.5f));
        Invisivel(new Vector3(lim.xMin - 2f, 2f, zMeio), new Vector3(0.5f, 4f, h));
        Invisivel(new Vector3(lim.xMax + 2f, 2f, zMeio), new Vector3(0.5f, 4f, h));
    }
}
