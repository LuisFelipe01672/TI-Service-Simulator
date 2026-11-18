using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Parte do MontarCenario (Etapa 9B): o bairro. Ruas, calçadas, zebras e postes saem da malha (Bairro.Malha); postes de
// concreto com cruzeta, transformador e fios caindo em curva; casas e sobrados do Sketchfab (CC-BY) nos lotes;
// fachada dos prédios com interior (platibanda, caixa d'água azul e placa); muro com portão na casa do jogador.
// Tudo andando: sem teleporte. As salas dos clientes ficam nos seus endereços (Bairro.Centro*).
public static partial class MontarCenario
{
    static Material mConcretoPoste, mFio, mCaixaDagua, mPlacaFundo, mMuroCasa, mGrade, mJanelaEscura;

    // Frente de cada modelo do Sketchfab (para onde a fachada aponta no arquivo), em graus:
    // 0 = +z, 90 = +x, 180 = -z, 270 = -x. Conferido nas fotos do menu "Fotos dos modelos do bairro".
    const float FrenteCasaComum = 270f, FrenteCasaTerrea = 90f, FrenteSobradoEnrolar = 180f,
                FrenteSobradoTijolo = 180f, FrentePredioGrande = 270f, FrenteOrelhao = 0f;
    // Maior lado de cada um, em metros (os sobrados vieram em escala dobrada: reduzidos para 7,5 m e 9 m de altura)
    const float LadoCasaComum = 12.01f, LadoCasaTerrea = 7.08f, LadoSobradoEnrolar = 7.78f,
                LadoSobradoTijolo = 9.0f, LadoPredioGrande = 32f, LadoOrelhao = 2.4f;

    static void CriarMateriaisBairro()
    {
        mConcretoPoste = MatPBR("Concreto_Poste", "Concrete034", new Color(0.78f, 0.77f, 0.74f), 0.08f);
        mFio = Mat("Fio_Preto", new Color(0.03f, 0.03f, 0.03f), 0.3f);
        mCaixaDagua = Mat("Caixa_Dagua_Azul", new Color(0.12f, 0.28f, 0.62f), 0.45f);
        mPlacaFundo = Mat("Placa_Fundo", new Color(0.1f, 0.12f, 0.15f), 0.5f);
        mMuroCasa = MatPBR("Muro_Casa", "PaintedPlaster017", new Color(0.86f, 0.8f, 0.68f), 0.1f);
        mGrade = Mat("Grade_Ferro", new Color(0.12f, 0.12f, 0.13f), 0.4f, 0.6f);
        mJanelaEscura = Mat("Janela_Andar", new Color(0.12f, 0.16f, 0.2f), 0.9f);
    }

    // ---------------- Ruas ----------------

    static void MontarRua()
    {
        CriarMateriaisBairro();
        var rua = new GameObject("Rua").transform;
        rua.SetParent(raiz, false);

        // Asfalto e calçadas (calçada 12 cm acima do asfalto: o meio-fio é o degrau)
        foreach (var p in Bairro.Pedacos())
        {
            var c = p.area.center;
            if (p.tipo == TipoPiso.Asfalto)
                Caixa(rua, "Asfalto", new Vector3(c.x, -0.01f, c.y), new Vector3(p.area.width, 0.04f, p.area.height), mAsfalto, 4f);
            else
                Caixa(rua, "Calcada", new Vector3(c.x, 0.05f, c.y), new Vector3(p.area.width, 0.14f, p.area.height), mCalcada, 1.5f);
        }

        // Faixa central tracejada em cada rua da malha, fora dos cruzamentos e das zebras (Etapa 10E-1)
        var m = Bairro.Malha;
        for (int i = 0; i < m.ruas.Count; i++)
        {
            var r = m.ruas[i];
            var cruz = m.CruzamentosDa(i);
            bool Livre(float a)
            {
                foreach (int k in cruz) if (Mathf.Abs(a - r.Ao(m.cruzamentos[k].centro)) < m.Transversal(k, i).meiaPista + 2f) return false;
                foreach (var fx in m.faixas) if (fx.rua == i && Mathf.Abs(a - fx.pos) <= 2.5f) return false;
                return true;
            }
            var tam = r.lesteOeste ? new Vector3(1.5f, 0.005f, 0.12f) : new Vector3(0.12f, 0.005f, 1.5f);
            var grupo = new GameObject("Faixa_Central").transform;   // um objeto por rua (Etapa 10E-2)
            grupo.SetParent(rua, false);
            for (float a = r.ini + 1.5f; a < r.fim - 1f; a += 3f)
                if (Livre(a)) { var p = r.Ponto(a); Caixa(grupo, "Faixa_Central", new Vector3(p.x, 0.012f, p.z), tam, mFaixa, 1f, false, false); }
            JuntarGrupo(grupo);
        }

        // Faixas de pedestre (zebras): listras de 40 cm atravessando a rua
        foreach (var fx in m.faixas)
        {
            var r = m.ruas[fx.rua];
            var tam = r.lesteOeste ? new Vector3(3f, 0.005f, 0.4f) : new Vector3(0.4f, 0.005f, 3f);
            var zebra = new GameObject("Faixa_Pedestre").transform;
            zebra.SetParent(rua, false);
            for (float l = r.meiaPista - 0.4f; l > -r.meiaPista + 0.3f; l -= 0.8f)
            {
                var p = r.Ponto(fx.pos, l);
                Caixa(zebra, "Faixa_Pedestre", new Vector3(p.x, 0.012f, p.z), tam, mFaixa, 1f, false, false);
            }
            JuntarGrupo(zebra);
        }

        MontarPostes(rua);
    }

    // ---------------- Postes e fios ----------------

    static void MontarPostes(Transform rua)
    {
        var postes = new GameObject("Postes").transform;
        postes.SetParent(rua, false);
        // Uma fileira de postes por calçada com poste (Rua.ladosPostes), junto ao meio-fio, braço para a rua
        var m = Bairro.Malha;
        var linhas = new List<List<Vector3>>();
        var paraRua = new List<Vector3>();
        for (int i = 0; i < m.ruas.Count; i++)
        {
            var r = m.ruas[i];
            var pos = r.postes ?? m.PostesPadrao(i);
            foreach (int lado in r.ladosPostes)
            {
                var l = new List<Vector3>();
                foreach (float a in pos) l.Add(r.Ponto(a, lado * (r.meiaPista + 0.35f)));
                linhas.Add(l);
                paraRua.Add(-lado * r.Normal);
            }
        }

        int n = 0;
        for (int l = 0; l < linhas.Count; l++)
        {
            List<Vector3[]> anterior = null;
            Vector3 ultimo = Vector3.zero;
            foreach (var p in linhas[l])
            {
                var pontos = PosteBR(postes, p, paraRua[l], n++ % 3 == 1);
                // Fios só entre postes vizinhos do mesmo lado (não atravessam o cruzamento)
                if (anterior != null && (p - ultimo).magnitude < 18f)
                    for (int k = 0; k < pontos.Count; k++)
                        Fio(postes, anterior[k][0], pontos[k][0], k < 3 ? 0.022f : 0.04f);
                anterior = pontos;
                ultimo = p;
            }
        }
    }

    // Poste de concreto (duplo T) com cruzeta de média tensão, cabos de telefone/internet mais baixos e o braço da
    // luminária para a rua. 'paraRua' = direção do meio da rua. Devolve os pontos de fixação dos fios (mundo).
    static List<Vector3[]> PosteBR(Transform pai, Vector3 base_, Vector3 paraRua, bool transformador)
    {
        var p = new GameObject("Poste").transform;
        p.SetParent(pai, false);
        p.localPosition = base_ + Vector3.up * 0.12f;    // em cima da calçada
        p.localRotation = Quaternion.LookRotation(paraRua);

        Caixa(p, "Haste_Base", new Vector3(0f, 2.5f, 0f), new Vector3(0.3f, 5f, 0.24f), mConcretoPoste, 1f);
        Caixa(p, "Haste_Topo", new Vector3(0f, 7f, 0f), new Vector3(0.22f, 4f, 0.18f), mConcretoPoste, 1f);
        Caixa(p, "Cruzeta", new Vector3(0f, 8.55f, 0.25f), new Vector3(0.1f, 0.1f, 2f), mMadeira, 1f, false);
        foreach (float z in new[] { -0.6f, 0.25f, 1.1f })
            Caixa(p, "Isolador", new Vector3(0f, 8.66f, z), new Vector3(0.07f, 0.12f, 0.07f), Mat("Isolador", new Color(0.55f, 0.6f, 0.62f), 0.7f), 1f, false, false);
        if (transformador)
        {
            Caixa(p, "Transformador", new Vector3(0f, 7.4f, -0.5f), new Vector3(0.6f, 0.9f, 0.55f), Mat("Transformador", new Color(0.45f, 0.47f, 0.48f), 0.4f, 0.3f), 1f, false);
            Caixa(p, "Suporte_Trafo", new Vector3(0f, 7.4f, -0.2f), new Vector3(0.12f, 0.12f, 0.3f), mGrade, 1f, false, false);
        }
        // Caixa de emenda dos cabos de internet (todo poste brasileiro tem uma pendurada)
        Caixa(p, "Caixa_Emenda", new Vector3(0.18f, 5.6f, 0.15f), new Vector3(0.12f, 0.35f, 0.18f), mFio, 1f, false, false);

        // Luminária: braço para a rua e lâmpada de vapor de sódio (acende no fim da tarde)
        const float yBraco = 7.1f;
        Caixa(p, "Braco", new Vector3(0f, yBraco + 0.15f, 0.85f), new Vector3(0.06f, 0.06f, 1.7f), mAluminio, 1f, false);
        var lampada = Caixa(p, "Luminaria", new Vector3(0f, yBraco + 0.1f, 1.7f), new Vector3(0.28f, 0.1f, 0.5f), mLampadaAcesa, 1f, false, false);
        var go = new GameObject("Luz");
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(0f, yBraco - 0.2f, 1.7f);
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.78f, 0.5f);
        luz.intensity = 2.2f;
        luz.range = 17f;
        luz.shadows = LightShadows.None;
        var poste = go.AddComponent<PosteLuz>();
        poste.lampada = lampada.GetComponent<Renderer>();
        poste.acesa = mLampadaAcesa;
        poste.apagada = mLampadaApagada;

        // Peças paradas do poste numa malha por material (a luminária fica à parte: troca de material à noite)
        JuntarGrupo(p, "Luminaria");

        // Fixações: 3 fios de média tensão na cruzeta e 2 cabos de telecom (mais grossos) a 6 m
        var locais = new[]
        {
            new Vector3(0f, 8.72f, -0.6f), new Vector3(0f, 8.72f, 0.25f), new Vector3(0f, 8.72f, 1.1f),
            new Vector3(0.16f, 6.1f, 0.05f), new Vector3(0.16f, 5.85f, 0.12f),
        };
        var lista = new List<Vector3[]>();
        foreach (var l in locais) lista.Add(new[] { p.TransformPoint(l) });
        return lista;
    }

    // Fio caindo em curva (catenária aproximada por parábola) entre dois pontos do mundo
    static void Fio(Transform pai, Vector3 a, Vector3 b, float largura)
    {
        var go = new GameObject("Fio");
        go.transform.SetParent(pai, false);
        var lr = go.AddComponent<LineRenderer>();
        const int n = 12;
        float flecha = 0.25f + 0.02f * (b - a).magnitude;
        lr.positionCount = n + 1;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            lr.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * flecha * 4f * t * (1f - t));
        }
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = largura;
        lr.sharedMaterial = mFio;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.lightProbeUsage = LightProbeUsage.Off;
    }

    // ---------------- Bairro: imóveis, fachadas e muros ----------------

    static void MontarBairro()
    {
        var viz = new GameObject("Vizinhanca").transform;
        viz.SetParent(raiz, false);

        // Imóveis: os postos à mão no bairro de antes e os lotes da cidade nova (Etapa 10E-2, Editor/Loteamento.cs)
        MontarImoveis(viz);

        // Orelhão na calçada perto do escritório, junto ao meio-fio (a calçada continua livre do lado das casas);
        // a colisão é a caixa inteira, para ninguém entrar no domo
        ModeloReal(SF("Orelhao"), viz, Bairro.Orelhao, LadoOrelhao, 180f - FrenteOrelhao);

        // Fachada dos prédios com interior
        Fachada("Escritorio", 6f, 6f, "ASSISTÊNCIA TÉCNICA", "Informática · Redes · Impressoras", false, new Color(0.95f, 0.92f, 0.86f));
        Fachada("Cliente", 10f, 8f, "ESCRITÓRIO CONTÁBIL", "Contabilidade · Departamento pessoal", false, new Color(0.88f, 0.91f, 0.94f));
        Fachada("Cliente_Clinica", 10f, 8f, "CLÍNICA", "Clínica médica · Exames", true, new Color(0.95f, 0.97f, 0.96f));
        Fachada("Cliente_Loja", 10f, 8f, "PAPELARIA & VARIEDADES", "Material escolar · Presentes", false, new Color(0.97f, 0.9f, 0.74f));
        Fachada("Casa", 8f, 6f, null, null, false, new Color(0.96f, 0.88f, 0.76f));
        MuroDaCasa();
        NumeroNaFachada("Casa", 8f, 6f, "128", 1.5f + 1.1f);
        MontarPraca();
    }

    // Modelo do Sketchfab num lote: a fachada vira para 'viradoPara' (graus, 0 = norte) e encosta no alinhamento
    // 'frente' (x,z): o ponto do meio da fachada, em cima da linha onde a calçada termina.
    static GameObject Imovel(Transform pai, string nome, float maiorLado, float frenteModelo, float viradoPara, Vector2 frente)
    {
        var go = ModeloReal(SF(nome), pai, Vector3.zero, maiorLado, viradoPara - frenteModelo);
        if (go == null) return null;
        var b = Limites(go);
        float a = viradoPara * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        var pos = go.transform.position;
        if (Mathf.Abs(dir.z) > 0.5f)
        {
            float frenteAtual = dir.z > 0 ? b.max.z : b.min.z;
            pos.x += frente.x - b.center.x;
            pos.z += frente.y - frenteAtual - dir.z * 0.05f;
        }
        else
        {
            float frenteAtual = dir.x > 0 ? b.max.x : b.min.x;
            pos.z += frente.y - b.center.z;
            pos.x += frente.x - frenteAtual - dir.x * 0.05f;
        }
        go.transform.position = pos;
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.lightProbeUsage = LightProbeUsage.Off;
        return go;
    }

    // Platibanda, caixa d'água e placa luminosa sobre a porta de um prédio montado por código (sala com a porta no
    // lado sul local). 'doisAndares' põe um andar de cima com janelas (a clínica fica num prédio de dois andares).
    static void Fachada(string nomeSala, float largura, float profundidade, string titulo, string subtitulo, bool doisAndares, Color cor)
    {
        var sala = raiz.Find(nomeSala);
        if (sala == null) return;
        var mat = MatPBR("Fachada_" + nomeSala, "PaintedPlaster017", cor, 0.1f);
        float w = largura / 2f + T, d = profundidade / 2f + T;
        var f = new GameObject("Fachada").transform;
        f.SetParent(sala, false);

        float topo = H + 0.1f;
        if (doisAndares)
        {
            const float andar = 3.1f;
            Caixa(f, "Andar_Superior", new Vector3(0f, topo + andar / 2f, 0f), new Vector3(2f * w, andar, 2f * d), mat, 2.5f);
            for (float x = -largura / 2f + 1.2f; x < largura / 2f - 0.8f; x += 2.2f)
            {
                Caixa(f, "Janela_Andar", new Vector3(x, topo + 1.5f, -d - 0.01f), new Vector3(1.3f, 1.2f, 0.02f), mJanelaEscura, 1f, false, false);
                Caixa(f, "Peitoril_Andar", new Vector3(x, topo + 0.88f, -d - 0.06f), new Vector3(1.45f, 0.05f, 0.12f), mRodape, 1f, false, false);
            }
            topo += andar;
        }
        // Platibanda (mureta que esconde a laje)
        const float hp = 0.8f;
        Caixa(f, "Platibanda_S", new Vector3(0f, topo + hp / 2f, -d + T / 2f), new Vector3(2f * w, hp, T), mat, 2.5f, false);
        Caixa(f, "Platibanda_N", new Vector3(0f, topo + hp / 2f, d - T / 2f), new Vector3(2f * w, hp, T), mat, 2.5f, false);
        Caixa(f, "Platibanda_L", new Vector3(w - T / 2f, topo + hp / 2f, 0f), new Vector3(T, hp, 2f * d - 2f * T), mat, 2.5f, false);
        Caixa(f, "Platibanda_O", new Vector3(-w + T / 2f, topo + hp / 2f, 0f), new Vector3(T, hp, 2f * d - 2f * T), mat, 2.5f, false);
        Caixa(f, "Pingadeira", new Vector3(0f, topo + hp + 0.02f, -d + T / 2f), new Vector3(2f * w + 0.1f, 0.04f, T + 0.08f), mRodape, 1f, false, false);

        // Caixa d'água de polietileno azul num canto da laje
        var caixa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        caixa.name = "Caixa_Dagua";
        Object.DestroyImmediate(caixa.GetComponent<Collider>());
        caixa.transform.SetParent(f, false);
        caixa.transform.localPosition = new Vector3(w - 1.2f, topo + 0.55f, d - 1.2f);
        caixa.transform.localScale = new Vector3(1.3f, 0.5f, 1.3f);
        caixa.GetComponent<Renderer>().sharedMaterial = mCaixaDagua;
        var tampa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tampa.name = "Tampa";
        Object.DestroyImmediate(tampa.GetComponent<Collider>());
        tampa.transform.SetParent(f, false);
        tampa.transform.localPosition = new Vector3(w - 1.2f, topo + 1.12f, d - 1.2f);
        tampa.transform.localScale = new Vector3(1.0f, 0.08f, 1.0f);
        tampa.GetComponent<Renderer>().sharedMaterial = mCaixaDagua;

        // Placa sobre a porta (de 2,10 m até o forro)
        if (titulo != null)
            Placa(f, new Vector3(0f, 2.48f, -d - 0.14f), Quaternion.identity, Mathf.Min(largura - 1f, 5f), 0.62f, titulo, subtitulo);
    }

    // Placa luminosa: fundo escuro e texto branco (TextMeshPro 3D). Fica de frente para -z local.
    static void Placa(Transform pai, Vector3 pos, Quaternion rot, float largura, float altura, string titulo, string subtitulo)
    {
        var p = new GameObject("Placa").transform;
        p.SetParent(pai, false);
        p.localPosition = pos;
        p.localRotation = rot;
        Caixa(p, "Fundo", Vector3.zero, new Vector3(largura, altura, 0.1f), mPlacaFundo, 1f, false, false);
        Caixa(p, "Moldura", new Vector3(0f, -altura / 2f - 0.02f, 0f), new Vector3(largura + 0.04f, 0.04f, 0.12f), mAluminio, 1f, false, false);

        TextMeshPro Texto(string nome, string texto, float y, float tamanho, Color cor, FontStyles estilo)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(p, false);
            go.transform.localPosition = new Vector3(0f, y, -0.056f);
            var t = go.AddComponent<TextMeshPro>();
            t.text = texto;
            t.fontSize = tamanho;
            t.fontStyle = estilo;
            t.color = cor;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(largura - 0.1f, altura);
            t.enableAutoSizing = true;
            t.fontSizeMin = 0.5f;
            t.fontSizeMax = tamanho;
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.lightProbeUsage = LightProbeUsage.Off;
            return t;
        }
        if (string.IsNullOrEmpty(subtitulo))
            Texto("Titulo", titulo, 0f, 3.2f, new Color(1f, 0.97f, 0.9f), FontStyles.Bold);
        else
        {
            Texto("Titulo", titulo, altura * 0.13f, 3.0f, new Color(1f, 0.97f, 0.9f), FontStyles.Bold);
            Texto("Subtitulo", subtitulo, -altura * 0.28f, 1.6f, new Color(0.95f, 0.75f, 0.3f), FontStyles.Normal);
        }
    }

    // Placa na fachada de um modelo do Sketchfab (fornecedor, ecoponto), virado para 'viradoPara' graus
    static void PlacaFachada(Transform pai, GameObject imovel, float viradoPara, string titulo, string subtitulo, float y)
    {
        var b = Limites(imovel);
        float a = viradoPara * Mathf.Deg2Rad;
        var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        float frente = Mathf.Abs(dir.z) > 0.5f ? (dir.z > 0 ? b.max.z : b.min.z) : (dir.x > 0 ? b.max.x : b.min.x);
        var pos = b.center;
        if (Mathf.Abs(dir.z) > 0.5f) pos.z = frente; else pos.x = frente;
        pos += dir * 0.12f;
        pos.y = y;
        // A placa olha para -z local: gira para que -z aponte para a rua
        Placa(pai, pos, Quaternion.LookRotation(-dir), 4.2f, 0.7f, titulo, subtitulo);
    }

    // Número da casa numa plaquinha ao lado da porta
    static void NumeroNaFachada(string nomeSala, float largura, float profundidade, string numero, float x)
    {
        var sala = raiz.Find(nomeSala);
        if (sala == null) return;
        Placa(sala, new Vector3(x, 1.75f, profundidade / 2f + T + 0.06f), Quaternion.Euler(0, 180, 0), 0.42f, 0.2f, numero, null);
    }

    // Muro de 1,80 m na divisa da casa com a calçada, com o vão do portão (de grade, aberto) na frente da porta
    static void MuroDaCasa()
    {
        var casa = raiz.Find("Casa");
        if (casa == null) return;
        var m = new GameObject("Muro").transform;
        m.SetParent(raiz, false);
        float zFrente = Bairro.FrenteSulAcacias - 0.08f;          // mureta logo depois da calçada
        float zCasa = Bairro.CentroCasa.z + 3f + T;                 // parede da frente da casa
        float xO = Bairro.CentroCasa.x - 4f - T, xL = Bairro.CentroCasa.x + 4f + T;
        float portao0 = Bairro.CentroCasa.x + 0.9f, portao1 = Bairro.CentroCasa.x + 2.1f;
        const float h = 1.8f, e = 0.15f;
        Caixa(m, "Muro_Frente", new Vector3((xO + portao0) / 2f, h / 2f, zFrente), new Vector3(portao0 - xO, h, e), mMuroCasa, 2.5f);
        Caixa(m, "Muro_Frente", new Vector3((portao1 + xL) / 2f, h / 2f, zFrente), new Vector3(xL - portao1, h, e), mMuroCasa, 2.5f);
        Caixa(m, "Muro_Lado", new Vector3(xO + e / 2f, h / 2f, (zFrente + zCasa) / 2f), new Vector3(e, h, zFrente - zCasa), mMuroCasa, 2.5f);
        Caixa(m, "Muro_Lado", new Vector3(xL - e / 2f, h / 2f, (zFrente + zCasa) / 2f), new Vector3(e, h, zFrente - zCasa), mMuroCasa, 2.5f);
        // Pilares do portão e o portão de grade aberto para dentro (sem colisão: não atrapalha a passagem)
        Caixa(m, "Pilar", new Vector3(portao0 - 0.1f, (h + 0.2f) / 2f, zFrente), new Vector3(0.2f, h + 0.2f, 0.25f), mMuroCasa, 2.5f);
        Caixa(m, "Pilar", new Vector3(portao1 + 0.1f, (h + 0.2f) / 2f, zFrente), new Vector3(0.2f, h + 0.2f, 0.25f), mMuroCasa, 2.5f);
        var portao = new GameObject("Portao").transform;
        portao.SetParent(m, false);
        portao.localPosition = new Vector3(portao0, 0f, zFrente);
        portao.localRotation = Quaternion.Euler(0, 95f, 0);           // aberto, encostado no muro por dentro
        float lp = portao1 - portao0 - 0.05f;
        Caixa(portao, "Quadro_Topo", new Vector3(lp / 2f, 1.7f, 0f), new Vector3(lp, 0.05f, 0.04f), mGrade, 1f, false);
        Caixa(portao, "Quadro_Base", new Vector3(lp / 2f, 0.1f, 0f), new Vector3(lp, 0.05f, 0.04f), mGrade, 1f, false);
        for (float x = 0.03f; x < lp; x += 0.11f)
            Caixa(portao, "Barra", new Vector3(x, 0.9f, 0f), new Vector3(0.02f, 1.6f, 0.02f), mGrade, 1f, false);
    }
}
