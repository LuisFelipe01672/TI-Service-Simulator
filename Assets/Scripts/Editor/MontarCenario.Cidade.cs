using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte do MontarCenario (Etapa 10E-2): a cidade em volta do bairro de antes. Lotes do Loteamento (casas e sobrados do
// Sketchfab com cor de parede, muro, portão e recuo sorteados; comércios com placa; terrenos baldios), a praça e o
// desempenho para placa de 4 GB: camadas com distância de corte, objetos parados estáticos (juntados em lotes de
// desenho) e occlusion culling calculado no fim da montagem.
public static partial class MontarCenario
{
    // Camadas de corte por distância (o CorteDistancia põe as distâncias na câmera em jogo)
    public const int CamadaDetalhe = CorteDistancia.CamadaDetalhe, CamadaMiudo = CorteDistancia.CamadaMiudo, CamadaImovel = CorteDistancia.CamadaImovel;

    // Paredes que ganham cor (material de cada modelo) e a paleta (multiplica a textura)
    static readonly Dictionary<string, string[]> ParedesDosModelos = new Dictionary<string, string[]>
    {
        ["Casa_Comum_Brasileira"] = new[] { "parede_casa" },
        ["Casa_Terrea"] = new[] { "Parede_Ext" },
        ["Sobrado_Tijolo"] = new[] { "Material" },
        ["Sobrado_Porta_Enrolar"] = new[] { "Materiais.001" },
    };
    static readonly Color[] PaletaParede =
    {
        Color.white, new Color(1f, 0.93f, 0.78f), new Color(0.82f, 0.95f, 0.9f), new Color(1f, 0.82f, 0.74f),
        new Color(0.8f, 0.88f, 1f), new Color(1f, 0.97f, 0.7f), new Color(0.92f, 0.84f, 0.95f), new Color(0.9f, 0.9f, 0.88f),
    };
    static readonly Color[] PaletaMuro =
    {
        new Color(0.86f, 0.8f, 0.68f), new Color(0.95f, 0.95f, 0.92f), new Color(0.72f, 0.72f, 0.7f),
        new Color(0.93f, 0.78f, 0.62f), new Color(0.75f, 0.85f, 0.78f), new Color(0.98f, 0.9f, 0.65f),
    };
    static Material[] mMuros, mPortoes;
    static Material mPisoQuintal;
    static readonly Dictionary<string, Material> variantes = new Dictionary<string, Material>();

    const string PastaVariantes = "Assets/Materials/Cenario/Variantes";

    static Material Variante(Material original, int cor)
    {
        if (original == null || cor <= 0) return original;
        string chave = original.name + "_" + cor;
        if (variantes.TryGetValue(chave, out var v) && v != null) return v;
        if (!AssetDatabase.IsValidFolder(PastaVariantes)) AssetDatabase.CreateFolder("Assets/Materials/Cenario", "Variantes");
        string caminho = $"{PastaVariantes}/{chave.Replace('.', '_')}.mat";
        v = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (v == null) { v = new Material(original); AssetDatabase.CreateAsset(v, caminho); }
        else { v.shader = original.shader; v.CopyPropertiesFromMaterial(original); }
        var c = PaletaParede[cor % PaletaParede.Length];
        if (v.HasProperty("baseColorFactor")) v.SetColor("baseColorFactor", original.GetColor("baseColorFactor") * c);
        else if (v.HasProperty("_Color")) v.color = original.color * c;
        EditorUtility.SetDirty(v);
        variantes[chave] = v;
        return v;
    }

    static void CriarMateriaisCidade()
    {
        variantes.Clear();
        mMuros = new Material[PaletaMuro.Length];
        for (int i = 0; i < PaletaMuro.Length; i++)
            mMuros[i] = i == 2 ? MatPBR("Muro_Chapisco", "Concrete034", PaletaMuro[i], 0.05f) : MatPBR("Muro_" + i, "PaintedPlaster017", PaletaMuro[i], 0.1f);
        mPortoes = new[]
        {
            Mat("Portao_Chapa_Branco", new Color(0.88f, 0.88f, 0.86f), 0.35f, 0.4f),
            Mat("Portao_Chapa_Marrom", new Color(0.32f, 0.2f, 0.13f), 0.3f, 0.3f),
            Mat("Portao_Chapa_Cinza", new Color(0.35f, 0.37f, 0.4f), 0.35f, 0.5f),
        };
        mPisoQuintal = MatPBR("Piso_Quintal", "Concrete034", new Color(0.7f, 0.69f, 0.66f), 0.08f);
    }

    // ---------------- Imóveis da cidade ----------------

    static void MontarImoveis(Transform viz)
    {
        CriarMateriaisCidade();
        Loteamento.Esquecer();
        var lotes = Loteamento.Todos();
        int casas = 0, comercios = 0, baldios = 0;
        foreach (var l in lotes)
        {
            if (l.fixo)
            {
                // as de antes, do mesmo jeito (direto na Vizinhanca)
                var go = Imovel(viz, l.modelo.nome, l.modelo.lado, l.modelo.frente, l.viradoPara, l.alinhamento);
                if (go == null) continue;
                if (l.marcador == "fornecedor") PlacaFachada(viz, go, l.viradoPara, "DISTRIBUIDORA DE PEÇAS", "Informática · Atacado", 3.2f);
                if (l.marcador == "coleta") PlacaFachada(viz, go, l.viradoPara, "ECOPONTO", "Lixo eletrônico · Pilhas", 3.4f);
                continue;
            }
            MontarLote(viz, l);
            if (l.baldio) baldios++; else if (l.comercio) comercios++; else casas++;
        }
        Debug.Log($"[Cidade] Lotes novos: {casas} casas e sobrados, {comercios} comércios, {baldios} terrenos baldios ({lotes.Count(x => x.fixo)} imóveis de antes).");
    }

    static void MontarLote(Transform viz, Loteamento.Lote l)
    {
        var raizLote = new GameObject(l.comercio ? "Lote_Comercio" : l.baldio ? "Lote_Baldio" : "Lote").transform;
        raizLote.SetParent(viz, false);
        raizLote.localPosition = new Vector3(l.alinhamento.x, 0f, l.alinhamento.y);
        raizLote.localRotation = Quaternion.Euler(0f, l.viradoPara, 0f);   // +z local = para a rua; o terreno fica em -z

        if (!l.baldio)
        {
            var casa = Imovel(viz, l.modelo.nome, l.modelo.lado, l.modelo.frente, l.viradoPara, l.FrenteCasa);
            if (casa != null)
            {
                // pivô no meio da planta, girado como o lote: espelhar troca só o lado (a planta continua a mesma)
                var pivo = new GameObject(l.espelhado ? "Casa_E" : "Casa").transform;
                pivo.SetParent(raizLote, false);
                pivo.localPosition = new Vector3(0f, 0f, -l.recuo - l.modelo.profundidade / 2f);
                casa.transform.SetParent(pivo, true);
                if (l.espelhado) pivo.localScale = new Vector3(-1f, 1f, 1f);
                // cor da parede
                if (ParedesDosModelos.TryGetValue(l.modelo.nome, out var paredes))
                    foreach (var r in casa.GetComponentsInChildren<Renderer>())
                    {
                        var mats = r.sharedMaterials;
                        bool trocou = false;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i] != null && paredes.Contains(mats[i].name)) { mats[i] = Variante(mats[i], l.corParede); trocou = true; }
                        if (trocou) r.sharedMaterials = mats;
                    }
                if (l.comercio) PlacaFachada(viz, casa, l.viradoPara, l.placa, l.subtitulo, 3.0f);
            }
        }
        if (l.muro) MuroDoLote(raizLote, l);
    }

    // Muro de 1,90 m na frente (com portão social de grade e, às vezes, o de garagem de chapa), nas laterais e no fundo;
    // piso cimentado no recuo. Coordenadas locais do lote: x ao longo da rua, z = 0 no alinhamento, terreno em -z.
    static void MuroDoLote(Transform lote, Loteamento.Lote l)
    {
        var g = new GameObject("Muro").transform;
        g.SetParent(lote, false);
        var mat = mMuros[l.corMuro % mMuros.Length];
        const float h = 1.9f, e = 0.15f;
        float w = l.largura / 2f, zf = -0.08f, fundo = -l.fundo + e / 2f;
        // vãos na frente: social (1 m) e garagem (2,8 m), se couber
        var vaos = new List<(float x0, float x1, bool garagem)>();
        if (!l.baldio)
        {
            float lado = l.espelhado ? -1f : 1f;
            vaos.Add((lado * (w - 1.9f) - 0.5f, lado * (w - 1.9f) + 0.5f, false));
            if (l.portao == 1 && l.recuo >= 2.5f && l.largura >= 8f) vaos.Add((-lado * (w - 2.2f) - 1.4f, -lado * (w - 2.2f) + 1.4f, true));
        }
        vaos.Sort((a, b) => a.x0.CompareTo(b.x0));
        float x = -w;
        foreach (var v in vaos)
        {
            if (v.x0 - x > 0.05f) Caixa(g, "Muro_Frente", new Vector3((x + v.x0) / 2f, h / 2f, zf), new Vector3(v.x0 - x, h, e), mat, 2.5f);
            Caixa(g, "Pilar", new Vector3(v.x0 - 0.1f, (h + 0.2f) / 2f, zf), new Vector3(0.2f, h + 0.2f, 0.25f), mat, 2.5f);
            Caixa(g, "Pilar", new Vector3(v.x1 + 0.1f, (h + 0.2f) / 2f, zf), new Vector3(0.2f, h + 0.2f, 0.25f), mat, 2.5f);
            float lv = v.x1 - v.x0;
            if (v.garagem)
                Caixa(g, "Portao_Garagem", new Vector3((v.x0 + v.x1) / 2f, 1f, zf), new Vector3(lv, 2f, 0.05f), mPortoes[(l.corMuro + l.corParede) % mPortoes.Length], 1f);
            else
            {
                Caixa(g, "Portao_Topo", new Vector3((v.x0 + v.x1) / 2f, 1.7f, zf), new Vector3(lv, 0.05f, 0.04f), mGrade, 1f, false);
                Caixa(g, "Portao_Base", new Vector3((v.x0 + v.x1) / 2f, 0.1f, zf), new Vector3(lv, 0.05f, 0.04f), mGrade, 1f, false);
                // barras numa malha só por portão (poucos objetos)
                var barras = new GameObject("Portao_Grade").transform;
                barras.SetParent(g, false);
                for (float bx = v.x0 + 0.06f; bx < v.x1; bx += 0.12f)
                    Caixa(barras, "Barra", new Vector3(bx, 0.9f, zf), new Vector3(0.02f, 1.6f, 0.02f), mGrade, 1f, false, false);
                JuntarFilhos(barras, mGrade, true);
            }
            x = v.x1;
        }
        if (w - x > 0.05f) Caixa(g, "Muro_Frente", new Vector3((x + w) / 2f, h / 2f, zf), new Vector3(w - x, h, e), mat, 2.5f);
        // laterais e fundo
        float comp = zf - fundo + e / 2f;
        Caixa(g, "Muro_Lado", new Vector3(-w + e / 2f, h / 2f, (zf + fundo) / 2f), new Vector3(e, h, comp), mat, 2.5f);
        Caixa(g, "Muro_Lado", new Vector3(w - e / 2f, h / 2f, (zf + fundo) / 2f), new Vector3(e, h, comp), mat, 2.5f);
        Caixa(g, "Muro_Fundo", new Vector3(0f, h / 2f, fundo), new Vector3(2f * w - 2f * e, h, e), mat, 2.5f);
        // piso cimentado no recuo
        if (!l.baldio && l.recuo > 0.3f)
            Caixa(g, "Piso_Quintal", new Vector3(0f, 0.015f, -(l.recuo + 0.08f) / 2f), new Vector3(2f * w - 2f * e, 0.03f, l.recuo - 0.1f), mPisoQuintal, 2f, false, false);
        if (l.baldio && l.portao == 1) PlacaVendeSe(g, new Vector3(0f, 1.3f, zf + e / 2f + 0.03f));
        JuntarGrupo(g);
        foreach (var t in g.GetComponentsInChildren<Transform>()) t.gameObject.layer = CamadaDetalhe;
    }

    // Plaquinha "VENDE-SE" no muro do terreno baldio
    static void PlacaVendeSe(Transform pai, Vector3 pos)
    {
        var p = new GameObject("Placa_VendeSe").transform;
        p.SetParent(pai, false);
        p.localPosition = pos;
        p.localRotation = Quaternion.Euler(0f, 180f, 0f);   // o texto olha para a rua (+z local do lote)
        Caixa(p, "Fundo", Vector3.zero, new Vector3(1.2f, 0.6f, 0.02f), Mat("Placa_Branca", new Color(0.95f, 0.95f, 0.93f), 0.3f), 1f, false, false);
        var t = new GameObject("Texto").AddComponent<TextMeshPro>();
        t.transform.SetParent(p, false);
        t.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        t.text = "VENDE-SE"; t.color = new Color(0.8f, 0.1f, 0.08f); t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 0.1f; t.fontSizeMax = 4f;
        t.rectTransform.sizeDelta = new Vector2(1.1f, 0.5f);
        t.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // Junta as malhas dos filhos (mesmo material) numa só, no próprio objeto (portões de grade, cercas)
    static void JuntarFilhos(Transform pai, Material mat, bool sombra)
    {
        var mfs = pai.GetComponentsInChildren<MeshFilter>();
        if (mfs.Length == 0) return;
        var ci = new List<CombineInstance>();
        var mundoParaPai = pai.worldToLocalMatrix;
        foreach (var mf in mfs) ci.Add(new CombineInstance { mesh = mf.sharedMesh, transform = mundoParaPai * mf.transform.localToWorldMatrix });
        var malha = new Mesh { name = pai.name };
        malha.CombineMeshes(ci.ToArray(), true, true);
        foreach (var mf in mfs) Object.DestroyImmediate(mf.gameObject);
        pai.gameObject.AddComponent<MeshFilter>().sharedMesh = malha;
        var mr = pai.gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    // Junta as caixas paradas de um grupo numa malha por material (e camada/sombra), no próprio grupo; as colisões
    // viram BoxColliders no grupo. Serve para caixas sem giro em relação ao grupo (muros, postes, faixas, barreiras).
    // Fica de fora: TextMeshPro, luzes, o que tem outro componente e os nomes em 'manter'. (Etapa 10E-2: menos
    // objetos na cena e menos chamadas de desenho.)
    static void JuntarGrupo(Transform g, params string[] manter)
    {
        var grupos = new Dictionary<(Material, int, ShadowCastingMode), List<MeshFilter>>();
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            var go = mf.gameObject;
            if (go.transform == g || manter.Contains(go.name) || mf.sharedMesh == null) continue;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterials.Length != 1) continue;
            bool simples = go.GetComponents<Component>().All(c => c is Transform || c is MeshFilter || c is MeshRenderer || c is BoxCollider);
            if (!simples) continue;
            if (Quaternion.Angle(Quaternion.Inverse(g.rotation) * go.transform.rotation, Quaternion.identity) > 0.5f) continue;
            var k = (mr.sharedMaterial, go.layer, mr.shadowCastingMode);
            if (!grupos.TryGetValue(k, out var l)) grupos[k] = l = new List<MeshFilter>();
            l.Add(mf);
        }
        var paraG = g.worldToLocalMatrix;
        foreach (var kv in grupos)
        {
            if (kv.Value.Count < 2) continue;
            var ci = kv.Value.Select(mf => new CombineInstance { mesh = mf.sharedMesh, transform = paraG * mf.transform.localToWorldMatrix }).ToArray();
            var malha = new Mesh { name = g.name + "_" + kv.Key.Item1.name, indexFormat = IndexFormat.UInt32 };
            malha.CombineMeshes(ci, true, true);
            if (malha.vertexCount < 65000) malha.indexFormat = IndexFormat.UInt16;
            var junto = new GameObject(g.name + "_Junto");
            junto.transform.SetParent(g, false);
            junto.layer = kv.Key.Item2;
            junto.AddComponent<MeshFilter>().sharedMesh = malha;
            var mr = junto.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kv.Key.Item1;
            mr.shadowCastingMode = kv.Key.Item3;
            foreach (var mf in kv.Value)
            {
                var go = mf.gameObject;
                var bc = go.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    var novo = g.gameObject.AddComponent<BoxCollider>();
                    novo.center = g.InverseTransformPoint(go.transform.TransformPoint(bc.center));
                    novo.size = Vector3.Scale(bc.size, go.transform.lossyScale);
                }
                if (go.transform.childCount == 0) Object.DestroyImmediate(go);
                else { Object.DestroyImmediate(bc); Object.DestroyImmediate(go.GetComponent<MeshRenderer>()); Object.DestroyImmediate(mf); }
            }
        }
    }

    // ---------------- Praça ----------------
    // Quarteirão inteiro: caminhos de cimento em cruz e em volta, canteiros, árvores (tronco e copa em formas simples),
    // bancos, coreto no meio, banca de jornal, lixeiras e quatro postes de praça com luz.
    static void MontarPraca()
    {
        var area = Loteamento.Praca;
        var p = new GameObject("Praca").transform;
        p.SetParent(raiz, false);
        p.localPosition = new Vector3(area.center.x, 0f, area.center.y);
        float w = area.width / 2f, d = area.height / 2f;
        var mCaminho = MatPBR("Praca_Caminho", "Concrete034", new Color(0.8f, 0.77f, 0.7f), 0.08f);
        var mCanteiro = MatPBR("Praca_Canteiro", "Concrete034", new Color(0.62f, 0.6f, 0.56f), 0.08f);
        var mTronco = Mat("Arvore_Tronco", new Color(0.3f, 0.22f, 0.15f), 0.1f);
        var mCopas = new[] { Mat("Arvore_Copa_1", new Color(0.2f, 0.38f, 0.16f), 0.15f), Mat("Arvore_Copa_2", new Color(0.26f, 0.44f, 0.2f), 0.15f), Mat("Arvore_Copa_3", new Color(0.33f, 0.4f, 0.14f), 0.15f) };
        var mBanco = Mat("Banco_Praca", new Color(0.45f, 0.3f, 0.18f), 0.25f);
        var mFerro = Mat("Banco_Ferro", new Color(0.15f, 0.25f, 0.18f), 0.35f, 0.5f);
        var mCoreto = MatPBR("Coreto", "PaintedPlaster017", new Color(0.97f, 0.96f, 0.93f), 0.1f);
        var mTelha = Mat("Coreto_Telhado", new Color(0.55f, 0.22f, 0.14f), 0.25f);

        // caminhos (2,5 m) em volta e em cruz, 4 cm acima da grama
        const float c = 2.5f, y = 0.02f;
        Caixa(p, "Caminho", new Vector3(0f, y, -d + c / 2f), new Vector3(2f * w, 0.04f, c), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(0f, y, d - c / 2f), new Vector3(2f * w, 0.04f, c), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(-w + c / 2f, y, 0f), new Vector3(c, 0.04f, 2f * d - 2f * c), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(w - c / 2f, y, 0f), new Vector3(c, 0.04f, 2f * d - 2f * c), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(0f, y, 0f), new Vector3(2f * w - 2f * c, 0.04f, c), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(0f, y, -d / 2f - 0.5f), new Vector3(c, 0.04f, d - 2f * c + 1f), mCaminho, 2f, false, false);
        Caixa(p, "Caminho", new Vector3(0f, y, d / 2f + 0.5f), new Vector3(c, 0.04f, d - 2f * c + 1f), mCaminho, 2f, false, false);

        // coreto no meio: base, 8 colunas e telhado em pirâmide
        Caixa(p, "Coreto_Base", new Vector3(0f, 0.3f, 0f), new Vector3(6f, 0.6f, 6f), mCoreto, 2f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Caixa(p, "Coreto_Coluna", new Vector3(Mathf.Cos(a) * 2.6f, 1.95f, Mathf.Sin(a) * 2.6f), new Vector3(0.18f, 2.7f, 0.18f), mCoreto, 1f);
        }
        Caixa(p, "Coreto_Forro", new Vector3(0f, 3.35f, 0f), new Vector3(6.4f, 0.15f, 6.4f), mCoreto, 2f, false);
        var tel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(tel.GetComponent<Collider>());
        tel.name = "Coreto_Telhado";
        tel.transform.SetParent(p, false);
        tel.transform.localPosition = new Vector3(0f, 3.9f, 0f);
        tel.transform.localScale = new Vector3(7f, 0.5f, 7f);
        tel.GetComponent<Renderer>().sharedMaterial = mTelha;
        var pico = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(pico.GetComponent<Collider>());
        pico.name = "Coreto_Pico";
        pico.transform.SetParent(p, false);
        pico.transform.localPosition = new Vector3(0f, 4.45f, 0f);
        pico.transform.localScale = new Vector3(4.5f, 1.4f, 4.5f);
        pico.GetComponent<Renderer>().sharedMaterial = mTelha;

        // árvores nos canteiros (quatro quadrantes), sem cair nos caminhos
        var rnd = new System.Random(77);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        int n = 0;
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                for (int k = 0; k < 5; k++)
                {
                    float x = sx * R(c + 2.5f, w - c - 1.8f), z = sz * R(c / 2f + 2f, d - c - 1.8f);
                    if (Mathf.Abs(x) < 5f && Mathf.Abs(z) < 5f) continue;   // longe do coreto
                    Arvore(p, new Vector3(x, 0f, z), R(5f, 8f), mTronco, mCopas[n++ % mCopas.Length], rnd);
                }
        // canteiros de mureta baixa em volta de algumas árvores do canto
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Caixa(p, "Canteiro", new Vector3(sx * (w - c - 1.2f), 0.2f, sz * (d - c - 1.2f)), new Vector3(1.6f, 0.4f, 1.6f), mCanteiro, 1f);

        // bancos ao longo do caminho em cruz
        foreach (float sx in new[] { -1f, 1f })
            for (float x = 7f; x < w - c - 2f; x += 6f)
                foreach (float sz in new[] { -1f, 1f })
                    Banco(p, new Vector3(sx * x, 0f, sz * (c / 2f + 0.55f)), sz > 0 ? 180f : 0f, mBanco, mFerro);
        foreach (float sz in new[] { -1f, 1f })
            for (float z = 7f; z < d - c - 2f; z += 6f)
                foreach (float sx in new[] { -1f, 1f })
                    Banco(p, new Vector3(sx * (c / 2f + 0.55f), 0f, sz * z), sx > 0 ? 270f : 90f, mBanco, mFerro);

        // banca de jornal no canto sudoeste (perto da avenida) e lixeiras
        var banca = new GameObject("Banca_Jornal").transform;
        banca.SetParent(p, false);
        banca.localPosition = new Vector3(-w + c + 2.2f, 0f, -d + c + 1.8f);
        Caixa(banca, "Corpo", new Vector3(0f, 1.25f, 0f), new Vector3(3f, 2.5f, 2f), Mat("Banca_Verde", new Color(0.12f, 0.4f, 0.24f), 0.4f, 0.3f), 1f);
        Caixa(banca, "Toldo", new Vector3(0f, 2.55f, -1.2f), new Vector3(3.2f, 0.08f, 0.6f), Mat("Banca_Toldo", new Color(0.85f, 0.75f, 0.2f), 0.3f), 1f, false);
        Placa(banca, new Vector3(0f, 2.15f, -1.02f), Quaternion.identity, 2.4f, 0.4f, "BANCA DE JORNAL", null);
        foreach (var pos in new[] { new Vector3(-4f, 0f, c / 2f + 0.5f), new Vector3(4f, 0f, -c / 2f - 0.5f), new Vector3(c / 2f + 0.5f, 0f, 8f) })
        {
            var li = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(li.GetComponent<Collider>());
            li.name = "Lixeira_Praca";
            li.transform.SetParent(p, false);
            li.transform.localPosition = pos + Vector3.up * 0.45f;
            li.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            li.GetComponent<Renderer>().sharedMaterial = Mat("Lixeira_Laranja", new Color(0.9f, 0.45f, 0.1f), 0.35f);
            li.layer = CamadaMiudo;
        }

        // postes de praça com luz (acendem à noite como os da rua)
        foreach (var pos in new[] { new Vector3(-w / 2f, 0f, c / 2f + 0.4f), new Vector3(w / 2f, 0f, -c / 2f - 0.4f), new Vector3(c / 2f + 0.4f, 0f, d / 2f), new Vector3(-c / 2f - 0.4f, 0f, -d / 2f) })
        {
            var poste = new GameObject("Poste_Praca").transform;
            poste.SetParent(p, false);
            poste.localPosition = pos;
            Caixa(poste, "Haste", new Vector3(0f, 2f, 0f), new Vector3(0.1f, 4f, 0.1f), mFerro, 1f);
            var lamp = Caixa(poste, "Luminaria", new Vector3(0f, 4.1f, 0f), new Vector3(0.45f, 0.35f, 0.45f), mLampadaAcesa, 1f, false, false);
            var go = new GameObject("Luz");
            go.transform.SetParent(poste, false);
            go.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            var luz = go.AddComponent<Light>();
            luz.type = LightType.Point; luz.color = new Color(1f, 0.85f, 0.65f); luz.intensity = 1.6f; luz.range = 12f; luz.shadows = LightShadows.None;
            var pl = go.AddComponent<PosteLuz>();
            pl.lampada = lamp.GetComponent<Renderer>(); pl.acesa = mLampadaAcesa; pl.apagada = mLampadaApagada;
        }
        foreach (var r in p.GetComponentsInChildren<Renderer>()) r.lightProbeUsage = LightProbeUsage.Off;
    }

    static void Arvore(Transform pai, Vector3 pos, float altura, Material tronco, Material copa, System.Random rnd)
    {
        var a = new GameObject("Arvore").transform;
        a.SetParent(pai, false);
        a.localPosition = pos;
        a.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        float ht = altura * 0.45f;
        var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        t.name = "Tronco";
        t.transform.SetParent(a, false);
        t.transform.localPosition = new Vector3(0f, ht / 2f, 0f);
        t.transform.localScale = new Vector3(0.32f, ht / 2f, 0.32f);
        t.GetComponent<Renderer>().sharedMaterial = tronco;
        Object.DestroyImmediate(t.GetComponent<Collider>());
        var cc = a.gameObject.AddComponent<CapsuleCollider>(); cc.radius = 0.25f; cc.height = ht; cc.center = new Vector3(0f, ht / 2f, 0f);
        float raio = altura * 0.32f;
        for (int i = 0; i < 3; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(s.GetComponent<Collider>());
            s.name = "Copa";
            s.transform.SetParent(a, false);
            float ang = i * 2.1f;
            s.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio * 0.35f, ht + raio * (0.6f + 0.25f * i), Mathf.Sin(ang) * raio * 0.35f);
            float k = raio * (1.5f - 0.2f * i);
            s.transform.localScale = new Vector3(k * 1.15f, k * 0.85f, k);
            s.GetComponent<Renderer>().sharedMaterial = copa;
        }
    }

    static void Banco(Transform pai, Vector3 pos, float giro, Material madeira, Material ferro)
    {
        var b = new GameObject("Banco").transform;
        b.SetParent(pai, false);
        b.localPosition = pos;
        b.localRotation = Quaternion.Euler(0f, giro, 0f);
        Caixa(b, "Assento", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.06f, 0.45f), madeira, 1f);
        Caixa(b, "Encosto", new Vector3(0f, 0.75f, 0.2f), new Vector3(1.6f, 0.35f, 0.05f), madeira, 1f, false);
        Caixa(b, "Pe", new Vector3(-0.7f, 0.22f, 0f), new Vector3(0.06f, 0.45f, 0.45f), ferro, 1f, false, false);
        Caixa(b, "Pe", new Vector3(0.7f, 0.22f, 0f), new Vector3(0.06f, 0.45f, 0.45f), ferro, 1f, false, false);
        foreach (var t in b.GetComponentsInChildren<Transform>()) t.gameObject.layer = CamadaMiudo;
    }

    // ---------------- Camadas e corte por distância ----------------

    // Garante os nomes das camadas no TagManager (Detalhe e Miúdo)
    static void GarantirCamadas()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var camadas = tm.FindProperty("layers");
        void Por(int i, string nome)
        {
            var p = camadas.GetArrayElementAtIndex(i);
            if (p.stringValue != nome) { p.stringValue = nome; }
        }
        Por(CamadaDetalhe, "Detalhe");
        Por(CamadaMiudo, "Miudo");
        Por(CamadaImovel, "Imovel");
        tm.ApplyModifiedProperties();
    }

    // Detalhes da rua (postes, fios, pinturas do PARE) nas camadas de corte por distância. As faixas pintadas ficam
    // na camada padrão: viram uma malha por rua, e o corte por camada mede até o meio do objeto (sumiria a rua toda).
    static void CamadasDaRua()
    {
        GarantirCamadas();
        var rua = raiz.Find("Rua");
        if (rua == null) return;
        foreach (var t in rua.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n == "Fio" || n == "Isolador" || n == "Caixa_Emenda" || n == "Suporte_Trafo") t.gameObject.layer = CamadaMiudo;
            else if (n.StartsWith("Poste") || n.StartsWith("Haste") || n == "Cruzeta" || n == "Braco"
                     || n == "Luminaria" || n == "Transformador") t.gameObject.layer = CamadaDetalhe;
        }
        // casas e sobrados da Vizinhanca (os modelos do Sketchfab; muros já estão em Detalhe)
        var viz = raiz.Find("Vizinhanca");
        if (viz != null)
            foreach (var r in viz.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.layer == 0 && r.GetComponent<TMP_Text>() == null) r.gameObject.layer = CamadaImovel;
        var transito = raiz.Find("Transito");
        if (transito != null)
            foreach (var t in transito.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Pintura_PARE") || t.name == "Linha_Retencao" || t.name.StartsWith("Texto_PARE")) t.gameObject.layer = CamadaMiudo;
    }

    // ---------------- Estáticos e occlusion culling ----------------

    // Fora dos cômodos: o que não se mexe vira estático (juntado em lotes de desenho; entra no occlusion culling).
    // Só os grandes (casas, muros, paredes) tampam a vista; tudo pode ser tampado. TextMeshPro fica de fora (a malha
    // dele muda), e o gramado não tampa nada.
    static void MarcarEstaticosDeFora(Renderer r)
    {
        if (r is SkinnedMeshRenderer || r.GetComponent<TMP_Text>() != null || SeMexe(r.transform)) return;
        var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
        var s = r.bounds.size;
        bool grande = (s.x > 2.5f || s.z > 2.5f) && s.y > 1.7f && r.name != "Gramado";
        if (grande) flags |= StaticEditorFlags.OccluderStatic;
        if (r.name == "Gramado") flags = StaticEditorFlags.BatchingStatic;
        GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
    }

    [MenuItem("TI Service/3c. Calcular occlusion culling", false, 1002)]
    public static void CalcularOclusao()
    {
        double t0 = EditorApplication.timeSinceStartup;
        StaticOcclusionCulling.smallestOccluder = 4f;
        StaticOcclusionCulling.smallestHole = 0.3f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        bool ok = StaticOcclusionCulling.Compute();
        Debug.Log($"[Oclusão] {(ok ? "Calculado" : "Falhou")} em {EditorApplication.timeSinceStartup - t0:0} s.");
    }
}
