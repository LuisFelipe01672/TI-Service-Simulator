using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Close 3D da crimpagem: o cabo, os 8 fios, o conector RJ45 e o alicate, numa mini-cena isolada
// (camada própria, longe do cenário) filmada por uma câmera que desenha numa textura.
// A janela da crimpagem mostra essa textura e chama as animações a cada etapa.
public class CenaCrimpagem : MonoBehaviour
{
    const int Camada = 30;                 // camada só desta cena (a câmera do jogo não a vê)
    static readonly Vector3 Origem = new Vector3(0f, -500f, 0f);

    // Medidas (em "unidades de close": o cabo é desenhado ~10x maior que o real)
    const float XBase = -0.25f;            // onde a capa termina depois de descascar (início dos fios)
    const float CompFio = 0.25f, CompAparado = 0.13f, RaioFio = 0.0045f;

    static CenaCrimpagem instancia;

    public RenderTexture Textura { get; private set; }
    Camera cam;
    Transform raiz, capaPonta, pontaVelha, conector, mandibulaCima, mandibulaBaixo, laminaCima, laminaBaixo;
    readonly Transform[] fios = new Transform[8];       // pivô na base de cada fio
    readonly Transform[] corpoFio = new Transform[8];   // cilindro (para aparar)
    readonly Vector3[] alvoPos = new Vector3[8];
    readonly Quaternion[] alvoRot = new Quaternion[8];
    readonly List<Transform> pinos = new List<Transform>();
    readonly List<Material> mats = new List<Material>();
    Renderer rendConector;

    public static CenaCrimpagem Obter()
    {
        if (instancia != null) return instancia;
        var go = new GameObject("CenaCrimpagem");
        instancia = go.AddComponent<CenaCrimpagem>();
        instancia.Construir();
        return instancia;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
        if (Textura != null) Textura.Release();
        foreach (var m in mats) if (m != null) Destroy(m);
    }

    // ---------- Montagem ----------

    void Construir()
    {
        // A câmera do jogo deixa de ver esta camada
        foreach (var c in Camera.allCameras) c.cullingMask &= ~(1 << Camada);

        raiz = new GameObject("Raiz").transform;
        raiz.SetParent(transform, false);
        raiz.position = Origem;

        Textura = new RenderTexture(1120, 300, 24) { antiAliasing = 4, name = "Crimpagem" };
        var goCam = new GameObject("CameraCrimpagem");
        goCam.transform.SetParent(raiz, false);
        // Bem de cima (~60°): a fileira dos 8 fios aparece espalhada, como o técnico vê na mão
        goCam.transform.localPosition = new Vector3(-0.13f, 0.50f, -0.29f);
        goCam.transform.LookAt(raiz.TransformPoint(new Vector3(-0.13f, 0f, 0.0f)));
        cam = goCam.AddComponent<Camera>();
        cam.cullingMask = 1 << Camada;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.13f, 0.17f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.02f;
        cam.farClipPlane = 5f;
        cam.targetTexture = Textura;
        cam.enabled = false;

        Luz(new Vector3(0.3f, 0.8f, -0.4f), 2.2f);
        Luz(new Vector3(-0.8f, 0.4f, 0.3f), 0.9f);

        // Bancada (fundo)
        Caixa("Bancada", new Vector3(-0.1f, -0.05f, 0.05f), new Vector3(1.6f, 0.02f, 0.8f), new Color(0.42f, 0.3f, 0.2f), 0.2f);

        // Cabo (capa azul de Cat6), da esquerda até XBase, mais a ponta que vai ser descascada
        var azul = new Color(0.2f, 0.42f, 0.78f);
        Cilindro("Capa", new Vector3(-0.55f, 0f, 0f), 0.6f, 0.032f, azul);
        capaPonta = Cilindro("CapaPonta", new Vector3(XBase + 0.075f, 0f, 0f), 0.15f, 0.032f, azul);

        // Ponta velha (pedaço de cabo + conector amarelado), cortada na primeira etapa
        pontaVelha = new GameObject("PontaVelha").transform;
        pontaVelha.SetParent(raiz, false);
        Cilindro("CaboVelho", new Vector3(-0.05f, 0f, 0f), 0.1f, 0.032f, azul).SetParent(pontaVelha, true);
        Caixa("ConectorVelho", new Vector3(0.06f, 0f, 0f), new Vector3(0.12f, 0.05f, 0.07f), new Color(0.85f, 0.8f, 0.55f, 1f), 0.6f).SetParent(pontaVelha, true);

        // Fios (escondidos dentro da capa até descascar)
        for (int i = 0; i < 8; i++)
        {
            var pivo = new GameObject("Fio_" + i).transform;
            pivo.SetParent(raiz, false);
            var cor = UIFactory.Hex(Crimpagem.CoresHex[i]);
            var c = Cilindro("Corpo", Vector3.zero, CompFio, RaioFio, cor);
            c.SetParent(pivo, false);
            c.localPosition = new Vector3(CompFio / 2f, 0f, 0f);
            fios[i] = pivo;
            corpoFio[i] = c;
        }

        // Conector RJ45 novo (transparente, com 8 pinos dourados em cima)
        conector = new GameObject("Conector").transform;
        conector.SetParent(raiz, false);
        var corpo = Caixa("Corpo", Vector3.zero, new Vector3(0.17f, 0.05f, 0.09f), new Color(0.85f, 0.92f, 0.95f, 0.35f), 0.9f, true);
        corpo.SetParent(conector, false);
        rendConector = corpo.GetComponent<Renderer>();
        Caixa("Trava", new Vector3(-0.02f, -0.032f, 0f), new Vector3(0.09f, 0.012f, 0.03f), new Color(0.85f, 0.92f, 0.95f, 0.5f), 0.9f, true).SetParent(conector, false);
        for (int i = 0; i < 8; i++)
        {
            var p = Caixa("Pino", new Vector3(0.05f, 0.03f, -0.0315f + i * 0.009f), new Vector3(0.04f, 0.012f, 0.004f), new Color(0.95f, 0.75f, 0.25f), 0.85f);
            p.SetParent(conector, false);
            pinos.Add(p);
        }

        // Alicate de crimpar (duas mandíbulas) e lâmina de corte
        var ferro = new Color(0.18f, 0.19f, 0.21f);
        mandibulaCima = Caixa("MandibulaCima", Vector3.zero, new Vector3(0.2f, 0.05f, 0.14f), ferro, 0.6f);
        mandibulaBaixo = Caixa("MandibulaBaixo", Vector3.zero, new Vector3(0.2f, 0.05f, 0.14f), ferro, 0.6f);
        laminaCima = Caixa("LaminaCima", Vector3.zero, new Vector3(0.02f, 0.08f, 0.12f), new Color(0.7f, 0.72f, 0.75f), 0.9f);
        laminaBaixo = Caixa("LaminaBaixo", Vector3.zero, new Vector3(0.02f, 0.08f, 0.12f), new Color(0.7f, 0.72f, 0.75f), 0.9f);

        Reiniciar();
    }

    void Luz(Vector3 pos, float intensidade)
    {
        var go = new GameObject("Luz");
        go.transform.SetParent(raiz, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 3f;
        l.intensity = intensidade;
        l.cullingMask = 1 << Camada;
        l.color = new Color(1f, 0.96f, 0.9f);
    }

    Material Mat(Color cor, float brilho, bool transparente = false)
    {
        var m = new Material(Shader.Find("Standard")) { color = cor };
        m.SetFloat("_Glossiness", brilho);
        if (transparente)
        {
            m.SetFloat("_Mode", 3);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
        mats.Add(m);
        return m;
    }

    Transform Prim(PrimitiveType tipo, string nome, Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nome;
        Destroy(go.GetComponent<Collider>());
        go.layer = Camada;
        go.transform.SetParent(raiz, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    // Cilindro deitado ao longo do eixo x
    Transform Cilindro(string nome, Vector3 centro, float comprimento, float raio, Color cor)
    {
        var t = Prim(PrimitiveType.Cylinder, nome, centro, new Vector3(raio * 2f, comprimento / 2f, raio * 2f), Mat(cor, 0.35f));
        t.localRotation = Quaternion.Euler(0f, 0f, 90f);
        return t;
    }

    Transform Caixa(string nome, Vector3 centro, Vector3 tam, Color cor, float brilho, bool transparente = false) =>
        Prim(PrimitiveType.Cube, nome, centro, tam, Mat(cor, brilho, transparente));

    // ---------- Estado ----------

    public void Mostrar(bool sim) { if (cam != null) cam.enabled = sim; }

    // Volta ao começo: ponta velha no lugar, capa inteira, fios escondidos, conector longe
    public void Reiniciar()
    {
        foreach (var t in new[] { pontaVelha, capaPonta, conector, mandibulaCima, mandibulaBaixo, laminaCima, laminaBaixo }) Tween.StopAll(t);
        for (int i = 0; i < 8; i++) { Tween.StopAll(fios[i]); Tween.StopAll(corpoFio[i]); }
        foreach (var p in pinos) Tween.StopAll(p);
        pontaVelha.gameObject.SetActive(true);
        pontaVelha.localPosition = Vector3.zero;
        pontaVelha.localRotation = Quaternion.identity;
        capaPonta.gameObject.SetActive(true);
        capaPonta.localPosition = new Vector3(XBase + 0.075f, 0f, 0f);
        for (int i = 0; i < 8; i++)
        {
            fios[i].gameObject.SetActive(false);
            corpoFio[i].localScale = new Vector3(RaioFio * 2f, CompFio / 2f, RaioFio * 2f);
            corpoFio[i].localPosition = new Vector3(CompFio / 2f, 0f, 0f);
            PoseTrancada(i, out alvoPos[i], out alvoRot[i]);
            fios[i].localPosition = alvoPos[i];
            fios[i].localRotation = alvoRot[i];
        }
        conector.gameObject.SetActive(false);
        conector.localPosition = new Vector3(0.45f, 0f, 0f);
        foreach (var p in pinos) p.localPosition = new Vector3(p.localPosition.x, 0.03f, p.localPosition.z);
        rendConector.sharedMaterial.color = new Color(0.85f, 0.92f, 0.95f, 0.35f);
        PosicionarFerramentas(false);
    }

    void PosicionarFerramentas(bool visivel)
    {
        mandibulaCima.gameObject.SetActive(visivel);
        mandibulaBaixo.gameObject.SetActive(visivel);
        mandibulaCima.localPosition = new Vector3(-0.165f, 0.16f, 0f);
        mandibulaBaixo.localPosition = new Vector3(-0.165f, -0.16f, 0f);
        laminaCima.gameObject.SetActive(false);
        laminaBaixo.gameObject.SetActive(false);
    }

    // 4 pares trançados saindo da capa (antes de ordenar)
    static void PoseTrancada(int fio, out Vector3 pos, out Quaternion rot)
    {
        int par = fio / 2;
        float ang = par * 90f * Mathf.Deg2Rad + (fio % 2 == 0 ? 0f : 0.5f);
        float r = 0.013f;
        pos = new Vector3(XBase, Mathf.Sin(ang) * r, Mathf.Cos(ang) * r);
        rot = Quaternion.Euler(0f, (fio % 2 == 0 ? 4f : -4f) + (par - 1.5f) * 3f, (par - 1.5f) * 2f);
    }

    // Atualiza o leque de fios soltos e a fileira dos já colocados (anima o que mudou)
    public void Sincronizar(Crimpagem c)
    {
        if (c == null) return;
        if (c.etapa < Crimpagem.Etapa.Ordenar) return;

        for (int slot = 0; slot < c.ordem.Count; slot++)
        {
            int fio = c.ordem[slot];
            Mover(fio, new Vector3(XBase, 0f, -0.0315f + slot * 0.009f), Quaternion.identity);
        }
        if (c.etapa == Crimpagem.Etapa.Ordenar)
            for (int k = 0; k < c.fiosSoltos.Count; k++)
            {
                int fio = c.fiosSoltos[k];
                float t = c.fiosSoltos.Count <= 1 ? 0.5f : k / (float)(c.fiosSoltos.Count - 1);
                // Leque aberto acima da fileira, cada fio num ângulo
                float yaw = Mathf.Lerp(-28f, 28f, t);
                Mover(fio, new Vector3(XBase, 0.02f, Mathf.Lerp(0.03f, -0.03f, t)), Quaternion.Euler(0f, yaw, 8f));
            }
    }

    void Mover(int fio, Vector3 pos, Quaternion rot)
    {
        if ((alvoPos[fio] - pos).sqrMagnitude < 1e-8f && Quaternion.Angle(alvoRot[fio], rot) < 0.1f) return;
        alvoPos[fio] = pos;
        alvoRot[fio] = rot;
        Tween.LocalPosition(fios[fio], pos, 0.35f, Ease.OutCubic);
        Tween.LocalRotation(fios[fio], rot, 0.35f, Ease.OutCubic);
    }

    // ---------- Animações de cada etapa (chamadas depois que a etapa acontece) ----------

    public void AnimarEtapa(Crimpagem.Etapa feita, Crimpagem c)
    {
        switch (feita)
        {
            case Crimpagem.Etapa.Cortar: AnimarCorte(); break;
            case Crimpagem.Etapa.Descascar: AnimarDescascar(); break;
            case Crimpagem.Etapa.Ordenar: Sincronizar(c); break;
            case Crimpagem.Etapa.Aparar: AnimarAparar(); break;
            case Crimpagem.Etapa.Inserir: AnimarInserir(); break;
            case Crimpagem.Etapa.Crimpar: AnimarCrimpar(c != null && c.sucesso); break;
        }
    }

    void AnimarCorte()
    {
        laminaCima.gameObject.SetActive(true);
        laminaBaixo.gameObject.SetActive(true);
        laminaCima.localPosition = new Vector3(-0.1f, 0.14f, 0f);
        laminaBaixo.localPosition = new Vector3(-0.1f, -0.14f, 0f);
        Sequence.Create()
            .Group(Tween.LocalPositionY(laminaCima, 0.04f, 0.18f, Ease.InQuad))
            .Group(Tween.LocalPositionY(laminaBaixo, -0.04f, 0.18f, Ease.InQuad))
            .Chain(Tween.LocalPositionY(pontaVelha, -0.25f, 0.45f, Ease.InQuad))
            .Group(Tween.LocalRotation(pontaVelha, Quaternion.Euler(0f, 0f, -35f), 0.45f))
            .Group(Tween.LocalPositionY(laminaCima, 0.14f, 0.2f))
            .Group(Tween.LocalPositionY(laminaBaixo, -0.14f, 0.2f))
            .ChainCallback(() =>
            {
                pontaVelha.gameObject.SetActive(false);
                laminaCima.gameObject.SetActive(false);
                laminaBaixo.gameObject.SetActive(false);
            });
    }

    void AnimarDescascar()
    {
        for (int i = 0; i < 8; i++) fios[i].gameObject.SetActive(true);
        Sequence.Create()
            .Chain(Tween.LocalPositionX(capaPonta, XBase + 0.075f + 0.35f, 0.5f, Ease.InOutQuad))
            .Chain(Tween.LocalPositionY(capaPonta, -0.25f, 0.35f, Ease.InQuad))
            .ChainCallback(() => capaPonta.gameObject.SetActive(false));
    }

    void AnimarAparar()
    {
        for (int i = 0; i < 8; i++)
        {
            Tween.Scale(corpoFio[i], new Vector3(RaioFio * 2f, CompAparado / 2f, RaioFio * 2f), 0.3f, Ease.OutQuad);
            Tween.LocalPositionX(corpoFio[i], CompAparado / 2f, 0.3f, Ease.OutQuad);
        }
        laminaCima.gameObject.SetActive(true);
        laminaBaixo.gameObject.SetActive(true);
        laminaCima.localPosition = new Vector3(XBase + CompAparado, 0.12f, 0f);
        laminaBaixo.localPosition = new Vector3(XBase + CompAparado, -0.12f, 0f);
        Sequence.Create()
            .Group(Tween.LocalPositionY(laminaCima, 0.02f, 0.15f))
            .Group(Tween.LocalPositionY(laminaBaixo, -0.02f, 0.15f))
            .Chain(Tween.LocalPositionY(laminaCima, 0.12f, 0.2f))
            .Group(Tween.LocalPositionY(laminaBaixo, -0.12f, 0.2f))
            .ChainCallback(() => { laminaCima.gameObject.SetActive(false); laminaBaixo.gameObject.SetActive(false); });
    }

    void AnimarInserir()
    {
        conector.gameObject.SetActive(true);
        conector.localPosition = new Vector3(0.45f, 0f, 0f);
        // A trava fica para baixo; o fundo do conector encosta na ponta dos fios
        Tween.LocalPositionX(conector, XBase + 0.085f, 0.7f, Ease.OutCubic);
    }

    void AnimarCrimpar(bool ok)
    {
        PosicionarFerramentas(true);
        float x = XBase + 0.085f;
        mandibulaCima.localPosition = new Vector3(x, 0.16f, 0f);
        mandibulaBaixo.localPosition = new Vector3(x, -0.16f, 0f);
        var seq = Sequence.Create()
            .Group(Tween.LocalPositionY(mandibulaCima, 0.05f, 0.35f, Ease.InQuad))
            .Group(Tween.LocalPositionY(mandibulaBaixo, -0.05f, 0.35f, Ease.InQuad));
        foreach (var p in pinos) seq.Group(Tween.LocalPositionY(p, 0.02f, 0.35f, Ease.InQuad));
        seq.Chain(Tween.LocalPositionY(mandibulaCima, 0.16f, 0.3f))
           .Group(Tween.LocalPositionY(mandibulaBaixo, -0.16f, 0.3f))
           .ChainCallback(() =>
           {
               PosicionarFerramentas(false);
               // brilho rápido no conector: verde se ficou bom, âmbar se não (só o testador confirma no jogo)
               rendConector.sharedMaterial.color = ok ? new Color(0.6f, 1f, 0.7f, 0.5f) : new Color(1f, 0.85f, 0.5f, 0.5f);
           });
    }
}
