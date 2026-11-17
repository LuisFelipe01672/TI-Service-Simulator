using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Etapa 8: visual do rack de parede 12U do cliente, feito em código: corpo de aço, porta de vidro fumê com
// dobradiça, patch panel de 24 portas, organizador com anéis, switch de 24 portas com LED de link por porta,
// prateleira com o modem da operadora e régua de tomadas. Os cordões são tubos (MalhaTubo) refeitos a cada
// mexida: bagunçados (2,5 m pendurados para fora, cruzando) ou curtos pelo organizador. Os pontos de rede do
// projeto de cabeamento ficam na mesma parede: marca de lápis, caixa de sobrepor, canaleta, sobra de cabo,
// tomada com espelho e etiqueta; dentro do rack o cabo desce até a porta do patch panel.
public partial class EquipamentoPecas
{
    public static readonly Color[] CoresCordao =
    {
        new Color(0.16f, 0.38f, 0.85f), new Color(0.95f, 0.78f, 0.12f), new Color(0.6f, 0.62f, 0.66f), new Color(0.85f, 0.18f, 0.16f)
    };

    // Medidas internas (y em relação ao chão, z negativo para a sala)
    const float RkTopo = RackCliente.Base + RackCliente.Altura;
    const float RkPatchY = RkTopo - 0.06f, RkOrgY = RkPatchY - 0.0445f, RkSwitchY = RkOrgY - 0.0445f;
    const float RkFrente = -0.405f;          // face dos equipamentos nos trilhos
    const float RkPortaZ = -RackCliente.Profundidade;

    Transform rkPorta, rkCordoes, rkPontos, rkEtiquetas, rkVelcro, rkPunch;
    Renderer[] rkLeds;
    float rkAngulo;
    int rkVersaoVista = -1, rkPontosVersao = -1;
    EstadoEquipamentos rkEstadoVisto;
    readonly Dictionary<string, float> rkNasceu = new Dictionary<string, float>();
    readonly List<(MeshFilter mf, List<Vector3> caminho, float inicio)> rkCrescendo = new List<(MeshFilter, List<Vector3>, float)>();
    readonly List<PecaEquipamento> rkPecasPontos = new List<PecaEquipamento>();
    readonly Dictionary<int, bool> rkCanaletaVista = new Dictionary<int, bool>();
    readonly Dictionary<int, bool> rkPatchVisto = new Dictionary<int, bool>();
    static Material mRkAco, mRkVidro, mRkPainel, mRkPorta, mRkEtiqueta, mRkCanaleta, mRkMetal, mRkLapis, mRkModem;
    static Material[] mRkCordao;

    static float PortaX(int porta) => -0.207f + (porta - 1) * 0.018f;

    void MontarRack()
    {
        CriarMateriaisRack();
        var v = new GameObject("Visual").transform;
        v.SetParent(transform, false);
        visual = v;
        float b = RackCliente.Base, w = RackCliente.Largura, h = RackCliente.Altura, d = RackCliente.Profundidade;
        float yc = b + h / 2f;

        // Corpo: fundo, laterais, teto (com recorte para a canaleta) e piso
        Cubo(v, "Fundo", new Vector3(0f, yc, -0.006f), new Vector3(w, h, 0.012f), mRkAco);
        Cubo(v, "LateralE", new Vector3(-w / 2f + 0.006f, yc, -d / 2f), new Vector3(0.012f, h, d), mRkAco);
        Cubo(v, "LateralD", new Vector3(w / 2f - 0.006f, yc, -d / 2f), new Vector3(0.012f, h, d), mRkAco);
        Cubo(v, "Teto", new Vector3(0f, b + h - 0.006f, -d / 2f - 0.04f), new Vector3(w, 0.012f, d - 0.08f), mRkAco);
        Cubo(v, "Piso", new Vector3(0f, b + 0.006f, -d / 2f), new Vector3(w, 0.012f, d), mRkAco);
        // Moldura da frente
        Cubo(v, "MolduraCima", new Vector3(0f, b + h - 0.018f, -d + 0.006f), new Vector3(w, 0.036f, 0.012f), mRkAco);
        Cubo(v, "MolduraBaixo", new Vector3(0f, b + 0.018f, -d + 0.006f), new Vector3(w, 0.036f, 0.012f), mRkAco);
        // Trilhos 19" com furação
        foreach (float s in new[] { -1f, 1f })
        {
            Cubo(v, "Trilho", new Vector3(s * 0.2475f, yc, RkFrente + 0.008f), new Vector3(0.016f, h - 0.04f, 0.004f), mRkMetal);
            for (int u = 0; u < 12; u++)
                Cubo(v, "Furo", new Vector3(s * 0.2475f, b + 0.04f + u * 0.0445f, RkFrente + 0.005f), new Vector3(0.006f, 0.006f, 0.002f), mPreto);
        }

        // Patch panel 24 portas (1U)
        Cubo(v, "PatchPanel", new Vector3(0f, RkPatchY, RkFrente + 0.012f), new Vector3(0.483f, 0.043f, 0.024f), mRkPainel);
        for (int p = 1; p <= 24; p++)
        {
            Cubo(v, "PortaPatch", new Vector3(PortaX(p), RkPatchY - 0.004f, RkFrente - 0.0005f), new Vector3(0.0125f, 0.011f, 0.002f), mPreto);
            Cubo(v, "NumeroPatch", new Vector3(PortaX(p), RkPatchY + 0.013f, RkFrente - 0.0005f), new Vector3(0.006f, 0.003f, 0.001f), mRkMetal);
        }
        // Organizador horizontal (1U) com 5 anéis
        Cubo(v, "Organizador", new Vector3(0f, RkOrgY, RkFrente + 0.004f), new Vector3(0.483f, 0.043f, 0.008f), mRkPainel);
        for (int i = 0; i < 5; i++)
        {
            float x = -0.2f + i * 0.1f;
            Cubo(v, "Anel", new Vector3(x - 0.035f, RkOrgY, RkFrente - 0.02f), new Vector3(0.004f, 0.036f, 0.04f), mRkPainel);
            Cubo(v, "Anel", new Vector3(x + 0.035f, RkOrgY, RkFrente - 0.02f), new Vector3(0.004f, 0.036f, 0.04f), mRkPainel);
            Cubo(v, "Anel", new Vector3(x, RkOrgY + 0.017f, RkFrente - 0.038f), new Vector3(0.074f, 0.004f, 0.004f), mRkPainel);
        }
        // Switch 24 portas (1U), LED de link em cima de cada porta
        Cubo(v, "Switch", new Vector3(0f, RkSwitchY, RkFrente + 0.125f), new Vector3(0.44f, 0.043f, 0.25f), mRkModem);
        Cubo(v, "SwitchFrente", new Vector3(0f, RkSwitchY, RkFrente - 0.0005f), new Vector3(0.483f, 0.043f, 0.002f), mRkPainel);
        rkLeds = new Renderer[24];
        for (int p = 1; p <= 24; p++)
        {
            Cubo(v, "PortaSwitch", new Vector3(PortaX(p), RkSwitchY - 0.005f, RkFrente - 0.0015f), new Vector3(0.0125f, 0.011f, 0.002f), mPreto);
            rkLeds[p - 1] = Cubo(v, "LedSwitch", new Vector3(PortaX(p), RkSwitchY + 0.011f, RkFrente - 0.002f), new Vector3(0.004f, 0.003f, 0.002f), mApagado).GetComponent<Renderer>();
        }
        Cubo(v, "LedPower", new Vector3(0.226f, RkSwitchY + 0.01f, RkFrente - 0.002f), new Vector3(0.004f, 0.004f, 0.002f), mVerde);
        // Prateleira com o modem da operadora e régua de tomadas no fundo
        float yPrat = RkSwitchY - 0.12f;
        Cubo(v, "Prateleira", new Vector3(0f, yPrat, -0.2f), new Vector3(0.48f, 0.006f, 0.38f), mRkAco);
        Cubo(v, "Modem", new Vector3(-0.1f, yPrat + 0.02f, -0.22f), new Vector3(0.18f, 0.035f, 0.13f), mRkModem);
        Cubo(v, "ModemLed", new Vector3(-0.1f, yPrat + 0.032f, -0.286f), new Vector3(0.05f, 0.004f, 0.002f), mVerde);
        Cubo(v, "Regua", new Vector3(0f, b + 0.06f, RkFrente + 0.02f), new Vector3(0.483f, 0.043f, 0.04f), mRkPainel);
        for (int i = 0; i < 6; i++)
            Cubo(v, "Tomada", new Vector3(-0.17f + i * 0.068f, b + 0.06f, RkFrente - 0.001f), new Vector3(0.03f, 0.03f, 0.002f), mPreto);

        // Porta de vidro fumê com dobradiça à esquerda, fechadura à direita
        rkPorta = new GameObject("Dobradica").transform;
        rkPorta.SetParent(v, false);
        rkPorta.localPosition = new Vector3(-w / 2f, yc, RkPortaZ);
        Cubo(rkPorta, "Vidro", new Vector3(w / 2f, 0f, -0.004f), new Vector3(w - 0.03f, h - 0.03f, 0.004f), mRkVidro);
        Cubo(rkPorta, "QuadroE", new Vector3(0.012f, 0f, -0.006f), new Vector3(0.024f, h, 0.012f), mRkPorta);
        Cubo(rkPorta, "QuadroD", new Vector3(w - 0.012f, 0f, -0.006f), new Vector3(0.024f, h, 0.012f), mRkPorta);
        Cubo(rkPorta, "QuadroC", new Vector3(w / 2f, h / 2f - 0.012f, -0.006f), new Vector3(w, 0.024f, 0.012f), mRkPorta);
        Cubo(rkPorta, "QuadroB", new Vector3(w / 2f, -h / 2f + 0.012f, -0.006f), new Vector3(w, 0.024f, 0.012f), mRkPorta);
        Cubo(rkPorta, "Fechadura", new Vector3(w - 0.012f, 0f, -0.016f), new Vector3(0.014f, 0.03f, 0.01f), mRkMetal);

        rkCordoes = new GameObject("Cordoes").transform; rkCordoes.SetParent(v, false);
        rkEtiquetas = new GameObject("Etiquetas").transform; rkEtiquetas.SetParent(v, false);
        rkPontos = new GameObject("Pontos").transform; rkPontos.SetParent(v, false);
        rkVelcro = Cubo(v, "Velcro", new Vector3(0f, RkOrgY - 0.004f, RkFrente - 0.03f), new Vector3(0.44f, 0.016f, 0.022f), mPreto).transform;
        rkVelcro.gameObject.SetActive(false);

        // Peças (mirar + E)
        PecaEquipamento.Criar(v, eq, "rack", "Rack de parede", new Vector3(0f, b + 0.09f, -d / 2f), new Vector3(w + 0.02f, 0.16f, d + 0.04f));
        PecaEquipamento.Criar(v, eq, "porta", "Porta de vidro", new Vector3(w / 2f - 0.03f, yc - 0.1f, -d - 0.01f), new Vector3(0.08f, 0.2f, 0.04f));
        PecaEquipamento.Criar(v, eq, "patch", "Patch panel", new Vector3(0f, RkPatchY, RkFrente - 0.03f), new Vector3(0.483f, 0.044f, 0.07f));
        PecaEquipamento.Criar(v, eq, "organizador", "Organizador de cabos", new Vector3(0f, RkOrgY, RkFrente - 0.03f), new Vector3(0.483f, 0.044f, 0.07f));
        PecaEquipamento.Criar(v, eq, "switch", "Switch 24 portas", new Vector3(0f, RkSwitchY - 0.02f, RkFrente - 0.03f), new Vector3(0.483f, 0.085f, 0.07f));
    }

    static void CriarMateriaisRack()
    {
        if (mRkAco != null) return;
        mRkAco = Mat(new Color(0.2f, 0.205f, 0.215f));
        mRkAco.SetFloat("_Glossiness", 0.3f);
        mRkPainel = Mat(new Color(0.13f, 0.13f, 0.14f));
        mRkPainel.SetFloat("_Glossiness", 0.35f);
        mRkPorta = Mat(new Color(0.12f, 0.12f, 0.13f));
        mRkMetal = Mat(new Color(0.72f, 0.73f, 0.75f));
        mRkMetal.SetFloat("_Metallic", 0.8f);
        mRkEtiqueta = Mat(new Color(0.96f, 0.96f, 0.93f));
        mRkCanaleta = Mat(new Color(0.93f, 0.93f, 0.91f));
        mRkLapis = Mat(new Color(0.25f, 0.25f, 0.28f));
        mRkModem = Mat(new Color(0.32f, 0.33f, 0.35f));
        // Vidro fumê: transparente (modo Fade do Standard)
        mRkVidro = Mat(new Color(0.12f, 0.14f, 0.16f, 0.35f));
        mRkVidro.SetFloat("_Mode", 2f);
        mRkVidro.SetOverrideTag("RenderType", "Transparent");
        mRkVidro.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mRkVidro.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mRkVidro.SetInt("_ZWrite", 0);
        mRkVidro.DisableKeyword("_ALPHATEST_ON");
        mRkVidro.EnableKeyword("_ALPHABLEND_ON");
        mRkVidro.renderQueue = 3000;
        mRkVidro.SetFloat("_Glossiness", 0.9f);
        mRkCordao = new Material[CoresCordao.Length];
        for (int i = 0; i < CoresCordao.Length; i++) { mRkCordao[i] = Mat(CoresCordao[i]); mRkCordao[i].SetFloat("_Glossiness", 0.45f); }
    }

    // ---------- Estado no modelo ----------

    void AtualizarRack(EstadoEquipamentos e)
    {
        if (rkPorta == null) return;
        bool aberta = e != null && e.rackPortaAberta;
        rkAngulo = Mathf.MoveTowards(rkAngulo, aberta ? 105f : 0f, 200f * Time.deltaTime);
        rkPorta.localRotation = Quaternion.Euler(0f, rkAngulo, 0f);

        int versao = e != null ? e.rackVersao : 0;
        if (e != rkEstadoVisto || versao != rkVersaoVista || RackCliente.Versao != rkPontosVersao)
        {
            bool novoEstado = e != rkEstadoVisto;
            if (novoEstado) { rkNasceu.Clear(); rkCanaletaVista.Clear(); rkPatchVisto.Clear(); }
            rkEstadoVisto = e;
            rkVersaoVista = versao;
            rkPontosVersao = RackCliente.Versao;
            RefazerCordoes(e, novoEstado);
            RefazerPontos(e, novoEstado);
        }

        // Cordão novo "escorrega" da porta do patch até o switch
        for (int i = rkCrescendo.Count - 1; i >= 0; i--)
        {
            var c = rkCrescendo[i];
            if (c.mf == null) { rkCrescendo.RemoveAt(i); continue; }
            float t = Mathf.Clamp01((Time.time - c.inicio) / 0.45f);
            int n = Mathf.Max(2, Mathf.RoundToInt(c.caminho.Count * t));
            MalhaTubo.Criar(c.caminho.GetRange(0, n), 0.0028f, 6, c.mf.sharedMesh);
            if (t >= 1f) rkCrescendo.RemoveAt(i);
        }

        // LEDs de link: porta com cordão pisca (tráfego)
        var cordoes = CordoesVisiveis(e);
        for (int p = 1; p <= 24; p++)
        {
            bool link = cordoes.Exists(c => c.portaSwitch == p);
            bool acesa = link && Mathf.Repeat(Time.time * (2.3f + p * 0.37f), 1f) < 0.8f;   // pisca com o tráfego
            Pintar(rkLeds[p - 1], acesa ? mVerde : mApagado);
        }
    }

    static readonly List<CordaoRack> cordoesPadrao = MontarPadrao();
    static List<CordaoRack> MontarPadrao()
    {
        var l = new List<CordaoRack>();
        for (int p = 1; p <= EstadoEquipamentos.PortasRack; p++) l.Add(new CordaoRack { portaPatch = p, portaSwitch = p, metros = 0.3f, cor = 0 });
        return l;
    }

    // Fora do chamado do rack, ele fica arrumado (cordões curtos, organizador, velcro, etiquetas)
    static List<CordaoRack> CordoesVisiveis(EstadoEquipamentos e) => e != null && e.rackServico ? e.rackCordoes : cordoesPadrao;

    void RefazerCordoes(EstadoEquipamentos e, bool semAnimacao)
    {
        foreach (Transform c in rkCordoes) Destroy(c.gameObject);
        foreach (Transform c in rkEtiquetas) Destroy(c.gameObject);
        rkCrescendo.Clear();
        bool servico = e != null && e.rackServico;
        bool org = !servico || e.rackNoOrganizador;
        bool velcro = !servico || e.rackVelcro;
        var cordoes = CordoesVisiveis(e);
        var vistos = new HashSet<string>();

        foreach (var c in cordoes)
        {
            string chave = $"{c.portaPatch}-{c.portaSwitch}-{c.metros}";
            vistos.Add(chave);
            var caminho = MalhaTubo.Curva(CaminhoCordao(c, org), 6);
            var go = new GameObject("Cordao_P" + c.portaPatch);
            go.transform.SetParent(rkCordoes, false);
            var mf = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial = mRkCordao[Mathf.Clamp(c.cor, 0, mRkCordao.Length - 1)];
            mf.sharedMesh = MalhaTubo.Criar(caminho, 0.0028f);
            // capinhas dos conectores RJ45 nas duas pontas
            var mCapa = mRkCordao[Mathf.Clamp(c.cor, 0, mRkCordao.Length - 1)];
            Cubo(go.transform, "Capa", new Vector3(PortaX(c.portaPatch), RkPatchY - 0.004f, RkFrente - 0.012f), new Vector3(0.012f, 0.01f, 0.022f), mCapa);
            Cubo(go.transform, "Capa", new Vector3(PortaX(c.portaSwitch), RkSwitchY - 0.005f, RkFrente - 0.013f), new Vector3(0.012f, 0.01f, 0.022f), mCapa);
            bool novo = !semAnimacao && !rkNasceu.ContainsKey(chave);
            if (!rkNasceu.ContainsKey(chave)) rkNasceu[chave] = Time.time;
            if (novo)
            {
                rkCrescendo.Add((mf, caminho, Time.time));
                SomManager.TocarEm("mexer", go.transform.position, 0.5f, 1.4f);
            }
        }
        // Cordões que saíram: some da memória (se voltar, anima de novo)
        var sairam = new List<string>();
        foreach (var k in rkNasceu.Keys) if (!vistos.Contains(k)) sairam.Add(k);
        foreach (var k in sairam) rkNasceu.Remove(k);

        // Etiquetas em cima das portas do patch panel
        var etiquetas = servico ? e.rackEtiquetas : null;
        for (int p = 1; p <= EstadoEquipamentos.PortasRack; p++)
        {
            if (etiquetas != null && !etiquetas.Contains(p)) continue;
            var et = Cubo(rkEtiquetas, "Etiqueta_P" + p, new Vector3(PortaX(p), RkPatchY + 0.0125f, RkFrente - 0.0015f), new Vector3(0.015f, 0.007f, 0.001f), mRkEtiqueta).transform;
            if (etiquetas != null && etiquetas.Count > 0 && etiquetas[etiquetas.Count - 1] == p && !semAnimacao)
            {
                Vector3 s = et.localScale;
                et.localScale = s * 0.1f;
                Tween.Scale(et, s, 0.25f, Ease.OutBack);
            }
        }
        // Pontos do projeto: etiqueta na porta do patch também
        if (e != null)
            foreach (var pt in e.pontos)
                if (pt.etiquetado)
                    Cubo(rkEtiquetas, "Etiqueta_" + pt.Nome, new Vector3(PortaX(pt.portaPatch), RkPatchY + 0.0125f, RkFrente - 0.0015f), new Vector3(0.015f, 0.007f, 0.001f), mRkEtiqueta);

        bool velcroAntes = rkVelcro.gameObject.activeSelf;
        rkVelcro.gameObject.SetActive(velcro && org);
        if (velcro && org && !velcroAntes && !semAnimacao)
        {
            rkVelcro.localScale = new Vector3(0.44f, 0.016f, 0.002f);
            Tween.Scale(rkVelcro, new Vector3(0.44f, 0.016f, 0.022f), 0.4f, Ease.OutCubic);
        }
    }

    // Caminho de um cordão (pontos de controle, coordenadas do Visual)
    static List<Vector3> CaminhoCordao(CordaoRack c, bool organizador)
    {
        Vector3 p = new Vector3(PortaX(c.portaPatch), RkPatchY - 0.004f, RkFrente - 0.02f);
        Vector3 s = new Vector3(PortaX(c.portaSwitch), RkSwitchY - 0.005f, RkFrente - 0.022f);
        var l = new List<Vector3> { p + new Vector3(0f, 0f, 0.008f), p + new Vector3(0f, 0f, -0.012f) };
        if (c.Curto)
        {
            if (organizador)
                l.Add(new Vector3(Mathf.Lerp(p.x, s.x, 0.5f), RkOrgY, RkFrente - 0.025f));
            else
                l.Add(new Vector3(Mathf.Lerp(p.x, s.x, 0.5f), RkOrgY - 0.004f, RkFrente - 0.075f));
        }
        else
        {
            // 2,5 m: sai pela frente, despenca abaixo do rack, dá uma volta e sobe cruzando os outros
            float r1 = Pseudo(c.portaPatch * 3 + 1), r2 = Pseudo(c.portaPatch * 7 + 2), r3 = Pseudo(c.portaSwitch * 5 + 3);
            float baixo = RackCliente.Base - 0.18f - r1 * 0.35f;
            l.Add(new Vector3(p.x + (r2 - 0.5f) * 0.1f, RkPatchY - 0.05f, RkFrente - 0.11f - r3 * 0.06f));
            l.Add(new Vector3(-0.25f + r2 * 0.5f, baixo + 0.12f, RkPortaZ - 0.1f - r1 * 0.12f));
            l.Add(new Vector3(-0.2f + r3 * 0.4f, baixo, RkPortaZ - 0.07f - r2 * 0.1f));
            l.Add(new Vector3(-0.28f + r1 * 0.56f, baixo + 0.1f + r3 * 0.08f, RkPortaZ - 0.02f - r3 * 0.12f));
            l.Add(new Vector3(s.x + (r1 - 0.5f) * 0.12f, RackCliente.Base + 0.05f, RkFrente - 0.13f - r2 * 0.05f));
            l.Add(new Vector3(s.x, RkSwitchY - 0.04f, RkFrente - 0.08f));
        }
        l.Add(s + new Vector3(0f, 0f, -0.012f));
        l.Add(s + new Vector3(0f, 0f, 0.008f));
        return l;
    }

    static float Pseudo(int n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f) * 43758.5453f, 1f);

    // ---------- Pontos de rede do projeto ----------

    void RefazerPontos(EstadoEquipamentos e, bool semAnimacao)
    {
        foreach (Transform c in rkPontos) Destroy(c.gameObject);
        foreach (var pc in rkPecasPontos) if (pc != null) Destroy(pc.gameObject);
        rkPecasPontos.Clear();
        if (e == null || e.pontos.Count == 0) return;

        float maxDir = 0f, maxEsq = 0f;
        for (int i = 0; i < e.pontos.Count && i < 4; i++)
        {
            var p = e.pontos[i];
            float x = i < RackCliente.PontosLocais.Count ? RackCliente.PontosLocais[i] : (i % 2 == 0 ? 1.1f : -1.1f) * (1 + i / 2);
            if (p.canaleta) { if (x > 0) maxDir = Mathf.Max(maxDir, x); else maxEsq = Mathf.Min(maxEsq, x); }
            MontarPonto(e, p, i, x, semAnimacao);
            rkPecasPontos.Add(PecaEquipamento.Criar(visual, eq, "ponto" + (i + 1), $"Ponto de rede {p.Nome}",
                new Vector3(x, RackCliente.AlturaTomada + 0.03f, -0.1f), new Vector3(0.3f, 0.45f, 0.24f)));
        }
        // Canaleta horizontal de cada lado, a 2,25 m, e a descida até o teto do rack
        foreach (float max in new[] { maxDir, maxEsq })
        {
            if (Mathf.Abs(max) < 0.01f) continue;
            float sinal = Mathf.Sign(max);
            float x0 = sinal * 0.2f;
            float comp = Mathf.Abs(max - x0) + 0.01f;
            var hz = Cubo(rkPontos, "CanaletaHorizontal", new Vector3((x0 + max) / 2f, RackCliente.AlturaCanaleta, -0.008f), new Vector3(comp, 0.02f, 0.016f), mRkCanaleta).transform;
            Cubo(rkPontos, "CanaletaDescida", new Vector3(x0, (RackCliente.AlturaCanaleta + RkTopo) / 2f, -0.008f), new Vector3(0.02f, RackCliente.AlturaCanaleta - RkTopo, 0.016f), mRkCanaleta);
            Cubo(rkPontos, "Curva", new Vector3(max, RackCliente.AlturaCanaleta, -0.008f), new Vector3(0.03f, 0.03f, 0.02f), mRkCanaleta);
            Cubo(rkPontos, "Curva", new Vector3(x0, RackCliente.AlturaCanaleta, -0.008f), new Vector3(0.03f, 0.03f, 0.02f), mRkCanaleta);
        }
    }

    void MontarPonto(EstadoEquipamentos e, PontoRede p, int i, float x, bool semAnimacao)
    {
        float yT = RackCliente.AlturaTomada;
        if (!p.canaleta)
        {
            // Marca de lápis na parede: um X onde vai a caixa
            Cubo(rkPontos, "Lapis", new Vector3(x, yT, -0.0015f), new Vector3(0.05f, 0.002f, 0.001f), mRkLapis).transform.localRotation = Quaternion.Euler(0, 0, 45);
            Cubo(rkPontos, "Lapis", new Vector3(x, yT, -0.0015f), new Vector3(0.05f, 0.002f, 0.001f), mRkLapis).transform.localRotation = Quaternion.Euler(0, 0, -45);
            return;
        }
        // Caixa de sobrepor e canaleta subindo até 2,25 m
        Cubo(rkPontos, "Caixa_" + p.Nome, new Vector3(x, yT, -0.022f), new Vector3(0.075f, 0.12f, 0.044f), mRkCanaleta);
        float baseC = yT + 0.06f, alto = RackCliente.AlturaCanaleta - baseC;
        var sub = Cubo(rkPontos, "CanaletaSubida", new Vector3(x, baseC + alto / 2f, -0.008f), new Vector3(0.02f, alto, 0.016f), mRkCanaleta).transform;
        bool viuAntes = rkCanaletaVista.TryGetValue(i, out bool v) && v;
        rkCanaletaVista[i] = true;
        if (!viuAntes && !semAnimacao)
        {
            // cresce de baixo para cima, como se fosse sendo fixada
            sub.localScale = new Vector3(0.02f, 0.01f, 0.016f);
            sub.localPosition = new Vector3(x, baseC, -0.008f);
            Tween.Scale(sub, new Vector3(0.02f, alto, 0.016f), 0.9f, Ease.OutCubic);
            Tween.LocalPosition(sub, new Vector3(x, baseC + alto / 2f, -0.008f), 0.9f, Ease.OutCubic);
            SomManager.TocarEm("mexer", transform.TransformPoint(new Vector3(x, yT, 0f)), 0.6f, 0.8f);
        }

        if (p.caboPassado && !p.crimpado)
        {
            // Sobra de cabo saindo da caixa, sem conector
            var sobra = new List<Vector3> { new Vector3(x, yT - 0.05f, -0.03f), new Vector3(x + 0.04f, yT - 0.16f, -0.06f), new Vector3(x, yT - 0.24f, -0.08f),
                                            new Vector3(x - 0.05f, yT - 0.17f, -0.06f), new Vector3(x - 0.015f, yT - 0.09f, -0.05f) };
            Tubo(rkPontos, "Sobra_" + p.Nome, MalhaTubo.Curva(sobra, 6), 0.003f, mRkCordao[0]);
        }
        if (p.crimpado)
        {
            Cubo(rkPontos, "Espelho_" + p.Nome, new Vector3(x, yT, -0.046f), new Vector3(0.08f, 0.125f, 0.004f), mRkCanaleta);
            Cubo(rkPontos, "Keystone", new Vector3(x, yT - 0.015f, -0.049f), new Vector3(0.016f, 0.014f, 0.003f), mPreto);
            if (p.etiquetado) Cubo(rkPontos, "Etiqueta_" + p.Nome, new Vector3(x, yT + 0.035f, -0.049f), new Vector3(0.04f, 0.012f, 0.002f), mRkEtiqueta);
        }
        if (p.caboPassado)
        {
            // Dentro do rack: desce do teto até atrás da porta do patch panel (solto até o punch-down)
            float x0 = Mathf.Sign(x) * 0.2f;
            float xp = PortaX(p.portaPatch);
            var dentro = new List<Vector3> { new Vector3(x0, RkTopo + 0.02f, -0.02f), new Vector3(x0, RkTopo - 0.03f, -0.05f) };
            if (p.noPatch) { dentro.Add(new Vector3(xp, RkPatchY + 0.01f, RkFrente + 0.06f)); dentro.Add(new Vector3(xp, RkPatchY, RkFrente + 0.026f)); }
            else { dentro.Add(new Vector3(x0 * 0.6f, RkPatchY - 0.02f, -0.12f)); dentro.Add(new Vector3(xp, RkSwitchY - 0.06f, RkFrente + 0.03f)); }
            Tubo(rkPontos, "CaboRack_" + p.Nome, MalhaTubo.Curva(dentro, 6), 0.003f, mRkCordao[0]);

            bool patchAntes = rkPatchVisto.TryGetValue(i, out bool pv) && pv;
            rkPatchVisto[i] = p.noPatch;
            if (p.noPatch && !patchAntes && !semAnimacao) AnimarPunchDown(xp);
        }
    }

    // Ferramenta de inserção batendo duas vezes na porta do patch panel (clack, clack)
    void AnimarPunchDown(float xp)
    {
        if (rkPunch != null) Destroy(rkPunch.gameObject);
        rkPunch = new GameObject("PunchDown").transform;
        rkPunch.SetParent(visual, false);
        var mCabo = Mat(new Color(0.95f, 0.75f, 0.1f));
        Cubo(rkPunch, "Cabo", new Vector3(0f, 0f, -0.06f), new Vector3(0.028f, 0.028f, 0.11f), mCabo);
        Cubo(rkPunch, "Lamina", new Vector3(0f, 0f, 0.005f), new Vector3(0.004f, 0.012f, 0.03f), mRkMetal);
        Vector3 longe = new Vector3(xp, RkPatchY, RkFrente - 0.09f), perto = new Vector3(xp, RkPatchY, RkFrente - 0.03f);
        rkPunch.localPosition = longe;
        var alvo = rkPunch;
        Sequence.Create()
            .Chain(Tween.LocalPosition(alvo, perto, 0.18f, Ease.InCubic))
            .ChainCallback(alvo, t => SomManager.TocarEm("clique", t.position, 0.8f, 0.8f))
            .Chain(Tween.LocalPosition(alvo, longe, 0.2f, Ease.OutCubic))
            .Chain(Tween.LocalPosition(alvo, perto, 0.18f, Ease.InCubic))
            .ChainCallback(alvo, t => SomManager.TocarEm("clique", t.position, 0.8f, 0.8f))
            .Chain(Tween.LocalPosition(alvo, longe + new Vector3(0f, 0f, -0.2f), 0.35f, Ease.InCubic))
            .ChainCallback(alvo, t => Destroy(t.gameObject));
    }

    static void Tubo(Transform pai, string nome, List<Vector3> caminho, float raio, Material mat)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.AddComponent<MeshFilter>().sharedMesh = MalhaTubo.Criar(caminho, raio);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
