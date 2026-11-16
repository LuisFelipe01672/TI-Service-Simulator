using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

// Etapa 6: o corpo 3D de um equipamento usado (frente em -Z, base em y = 0) e as pistas de cada ação na bancada:
// LED e tela acendendo no teste, fumaça na sucata, folha com listra na impressora, peça velha saindo e nova entrando,
// poeira sumindo na limpeza, etiqueta de patrimônio e plaquinha "À VENDA". Impressora, roteador e switch usam os
// modelos que o projeto já tem (ModelosUsados); o resto é montado com primitivas.
public class UsadoVisual : MonoBehaviour
{
    public EquipUsado e;
    public Bounds limites;        // no espaço local da raiz
    Transform visual, poeira, etiqueta, placaVenda, saidaFolha, calombo;
    TextMeshPro txtEtiqueta, txtVenda;
    readonly List<Renderer> leds = new List<Renderer>();
    Renderer tela;                // tela própria (monitor e notebook)
    readonly List<GameObject> folhas = new List<GameObject>();
    float piscarAte;              // monitor com capacitor estufado / sucata de rede: pisca
    bool ligado;

    static Material mGrafite, mPreto, mCinza, mPrata, mClaro, mLedOff, mLedVerde, mLedLaranja, mTelaOff, mTelaLigada,
        mPo, mBranco, mFumaca, mVerdePcb, mFolha, mPapel;

    // ---------- Montagem ----------

    public static UsadoVisual Montar(Transform raiz, EquipUsado e)
    {
        CriarMateriais();
        var v = raiz.gameObject.AddComponent<UsadoVisual>();
        v.e = e;
        v.visual = new GameObject("Visual").transform;
        v.visual.SetParent(raiz, false);
        switch (e.tipo)
        {
            case TipoUsado.Computador: v.Gabinete(); break;
            case TipoUsado.Notebook: v.Notebook(); break;
            case TipoUsado.Monitor: v.Monitor(); break;
            case TipoUsado.Impressora: v.Impressora(); break;
            case TipoUsado.Roteador: v.Rede(true); break;
            default: v.Rede(false); break;
        }
        v.limites = ModelosFerramentas.Limites(v.visual, raiz);
        if (v.limites.size == Vector3.zero) v.limites = new Bounds(Vector3.up * 0.15f, Vector3.one * 0.3f);
        v.MontarPoeira();
        v.MontarEtiqueta();
        v.MontarPlacaVenda();
        v.Atualizar();
        return v;
    }

    static void CriarMateriais()
    {
        if (mGrafite != null) return;
        mGrafite = Mat(new Color(0.13f, 0.135f, 0.145f), 0.3f);
        mPreto = Mat(new Color(0.045f, 0.045f, 0.05f), 0.45f);
        mCinza = Mat(new Color(0.25f, 0.26f, 0.28f), 0.3f);
        mPrata = Mat(new Color(0.62f, 0.63f, 0.65f), 0.5f);
        mClaro = Mat(new Color(0.84f, 0.85f, 0.86f), 0.35f);
        mLedOff = Mat(new Color(0.12f, 0.14f, 0.12f), 0.6f);
        mLedVerde = Mat(new Color(0.2f, 0.95f, 0.45f), 0.6f, new Color(0.2f, 0.95f, 0.45f) * 2f);
        mLedLaranja = Mat(new Color(1f, 0.55f, 0.1f), 0.6f, new Color(1f, 0.55f, 0.1f) * 2f);
        mTelaOff = Mat(new Color(0.02f, 0.022f, 0.025f), 0.85f);
        mTelaLigada = Mat(new Color(0.2f, 0.35f, 0.6f), 0.6f, new Color(0.25f, 0.45f, 0.8f) * 0.9f);
        mPo = Mat(new Color(0.52f, 0.5f, 0.45f), 0.02f);
        mBranco = Mat(new Color(0.96f, 0.96f, 0.94f), 0.15f);
        mFumaca = Mat(new Color(0.35f, 0.35f, 0.36f), 0f);
        mVerdePcb = Mat(new Color(0.08f, 0.35f, 0.15f), 0.4f);
        mPapel = Mat(new Color(0.97f, 0.9f, 0.45f), 0.1f);
        mFolha = new Material(Shader.Find("Standard"));
        mFolha.SetFloat("_Glossiness", 0.05f);
    }

    static Material Mat(Color cor, float brilho, Color? emissao = null)
    {
        var m = new Material(Shader.Find("Standard")) { color = cor };
        m.SetFloat("_Glossiness", brilho);
        if (emissao != null) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emissao.Value); }
        return m;
    }

    static GameObject Cubo(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat, PrimitiveType tipo = PrimitiveType.Cube)
    {
        var c = GameObject.CreatePrimitive(tipo);
        c.name = nome;
        Object.Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localPosition = pos;
        c.transform.localScale = tam;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c;
    }

    static TextMeshPro Texto3D(Transform pai, Vector3 pos, Vector2 tamanho, float fonte, Color cor)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.rectTransform.sizeDelta = tamanho;
        t.fontSize = fonte;
        t.color = cor;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMax = fonte;
        t.fontSizeMin = fonte * 0.3f;
        return t;
    }

    void Led(Vector3 pos, Vector3 tam) => leds.Add(Cubo(visual, "LED" + leds.Count, pos, tam, mLedOff).GetComponent<Renderer>());

    // Gabinete torre de escritório (19 x 42 x 42 cm)
    void Gabinete()
    {
        const float W = 0.19f, H = 0.42f, D = 0.42f;
        Cubo(visual, "Corpo", new Vector3(0, H / 2, 0), new Vector3(W, H, D), mGrafite);
        Cubo(visual, "Frente", new Vector3(0, H / 2, -D / 2 - 0.003f), new Vector3(W - 0.008f, H - 0.008f, 0.006f), mPreto);
        Cubo(visual, "BaiaDVD", new Vector3(0, H - 0.06f, -D / 2 - 0.0065f), new Vector3(W - 0.04f, 0.042f, 0.002f), mCinza);
        var bt = Cubo(visual, "BotaoPower", new Vector3(0, H - 0.13f, -D / 2 - 0.007f), new Vector3(0.026f, 0.002f, 0.026f), mCinza, PrimitiveType.Cylinder);
        bt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Led(new Vector3(0.045f, H - 0.13f, -D / 2 - 0.0068f), new Vector3(0.008f, 0.008f, 0.002f));
        Cubo(visual, "USB1", new Vector3(-0.022f, H - 0.18f, -D / 2 - 0.0065f), new Vector3(0.014f, 0.006f, 0.002f), mCinza);
        Cubo(visual, "USB2", new Vector3(0.022f, H - 0.18f, -D / 2 - 0.0065f), new Vector3(0.014f, 0.006f, 0.002f), mCinza);
        for (int i = 0; i < 6; i++)   // grade de ventilação na lateral
            Cubo(visual, "Grade" + i, new Vector3(W / 2 + 0.001f, 0.12f + i * 0.025f, 0.08f), new Vector3(0.002f, 0.006f, 0.16f), mPreto);
    }

    void Notebook()
    {
        Cubo(visual, "Base", new Vector3(0, 0.011f, 0), new Vector3(0.34f, 0.022f, 0.24f), mPrata);
        Cubo(visual, "Teclado", new Vector3(0, 0.0222f, 0.025f), new Vector3(0.30f, 0.001f, 0.11f), mPreto);
        Cubo(visual, "Touchpad", new Vector3(0, 0.0222f, -0.075f), new Vector3(0.1f, 0.001f, 0.06f), mCinza);
        if (e.defeito == DefeitoUsado.Bateria)   // bateria estufada: a carcaça levanta embaixo do touchpad
            calombo = Cubo(visual, "Estufado", new Vector3(0, 0.021f, -0.07f), new Vector3(0.16f, 0.006f, 0.09f), mPrata).transform;
        var dobradica = new GameObject("Dobradica").transform;
        dobradica.SetParent(visual, false);
        dobradica.localPosition = new Vector3(0, 0.022f, 0.118f);
        dobradica.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        Cubo(dobradica, "Tampa", new Vector3(0, 0.11f, 0), new Vector3(0.34f, 0.22f, 0.008f), mPrata);
        tela = Cubo(dobradica, "Tela", new Vector3(0, 0.115f, -0.0045f), new Vector3(0.31f, 0.19f, 0.001f), mTelaOff).GetComponent<Renderer>();
        Led(new Vector3(0.15f, 0.0225f, -0.105f), new Vector3(0.006f, 0.001f, 0.004f));
    }

    void Monitor()
    {
        Cubo(visual, "Base", new Vector3(0, 0.005f, 0.02f), new Vector3(0.2f, 0.01f, 0.15f), mPreto, PrimitiveType.Cylinder).transform.localScale = new Vector3(0.2f, 0.005f, 0.15f);
        Cubo(visual, "Haste", new Vector3(0, 0.08f, 0.03f), new Vector3(0.045f, 0.15f, 0.02f), mPreto);
        Cubo(visual, "Painel", new Vector3(0, 0.255f, 0), new Vector3(0.43f, 0.28f, 0.035f), mPreto);
        tela = Cubo(visual, "Tela", new Vector3(0, 0.26f, -0.018f), new Vector3(0.40f, 0.24f, 0.001f), mTelaOff).GetComponent<Renderer>();
        Led(new Vector3(0.19f, 0.124f, -0.018f), new Vector3(0.006f, 0.004f, 0.002f));
    }

    void Impressora()
    {
        var m = ModelosUsados.Carregar();
        var modelo = m != null && m.impressora != null ? ModelosFerramentas.Instanciar(m.impressora, visual, 0.42f, Quaternion.Euler(0f, -90f, 0f)) : null;
        if (modelo != null)
        {
            Assentar(modelo);
            foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = r.name.Contains("upper") || r.name.Contains("lid") ? mClaro : mGrafite;
                r.sharedMaterials = mats;
            }
        }
        else
        {
            Cubo(visual, "Corpo", new Vector3(0, 0.12f, 0), new Vector3(0.40f, 0.24f, 0.36f), mGrafite);
            Cubo(visual, "Scanner", new Vector3(0, 0.27f, 0.01f), new Vector3(0.40f, 0.06f, 0.34f), mClaro);
            Cubo(visual, "Saida", new Vector3(0, 0.2f, -0.17f), new Vector3(0.3f, 0.02f, 0.02f), mPreto);
        }
        var b = ModelosFerramentas.Limites(visual, visual);
        saidaFolha = new GameObject("SaidaFolha").transform;
        saidaFolha.SetParent(visual, false);
        saidaFolha.localPosition = new Vector3(0f, b.min.y + b.size.y * 0.62f, b.min.z + 0.12f);
        Led(new Vector3(b.max.x - 0.04f, b.max.y + 0.001f, b.min.z + 0.04f), new Vector3(0.008f, 0.002f, 0.008f));
    }

    void Rede(bool roteador)
    {
        var m = ModelosUsados.Carregar();
        var prefab = m == null ? null : roteador ? m.roteador : m.switchRede;
        var modelo = prefab != null ? ModelosFerramentas.Instanciar(prefab, visual, roteador ? 0.24f : 0.2f, Quaternion.Euler(0f, roteador ? 0f : 180f, 0f)) : null;
        if (modelo != null) Assentar(modelo);
        else
        {
            Cubo(visual, "Corpo", new Vector3(0, roteador ? 0.0175f : 0.015f, 0), new Vector3(roteador ? 0.22f : 0.2f, roteador ? 0.035f : 0.03f, roteador ? 0.15f : 0.11f), roteador ? mPreto : mCinza);
            if (roteador)
                foreach (float x in new[] { -0.08f, 0.08f })
                    Cubo(visual, "Antena", new Vector3(x, 0.1f, 0.07f), new Vector3(0.012f, 0.13f, 0.012f), mPreto);
        }
        var b = ModelosFerramentas.Limites(visual, visual);
        float corpo = roteador ? Mathf.Max(0.03f, b.size.y * 0.18f) : b.size.y;
        int n = roteador ? 4 : 8;
        float passo = roteador ? 0.045f : 0.018f;
        for (int i = 0; i < n; i++)
            Led(new Vector3((i - (n - 1) / 2f) * passo, b.min.y + corpo * 0.6f, b.min.z - 0.002f), new Vector3(0.01f, 0.005f, 0.003f));
    }

    // Base do modelo no chão (y = 0), centrado em X e Z
    void Assentar(Transform modelo)
    {
        var b = ModelosFerramentas.Limites(modelo, visual);
        modelo.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
    }

    // Altura do "teto" onde a poeira assenta (sem antenas)
    float Topo()
    {
        switch (e.tipo)
        {
            case TipoUsado.Notebook: return 0.0232f;
            case TipoUsado.Monitor: return 0.396f;
            case TipoUsado.Roteador: return Mathf.Max(0.03f, limites.size.y * 0.18f) + limites.min.y;
            default: return limites.max.y;
        }
    }

    void MontarPoeira()
    {
        poeira = new GameObject("Poeira").transform;
        poeira.SetParent(transform, false);
        float y = Topo();
        Vector3 tam = e.tipo == TipoUsado.Monitor ? new Vector3(0.42f, 0.0015f, 0.03f)
                    : e.tipo == TipoUsado.Notebook ? new Vector3(0.3f, 0.0015f, 0.2f)
                    : new Vector3(limites.size.x * 0.92f, 0.0015f, limites.size.z * 0.88f);
        Cubo(poeira, "Manta", new Vector3(limites.center.x, y + 0.001f, e.tipo == TipoUsado.Notebook ? 0f : limites.center.z), tam, mPo);
        var rnd = new System.Random(e.id * 31 + 7);
        for (int i = 0; i < 6; i++)   // tufos
        {
            float x = limites.center.x + ((float)rnd.NextDouble() - 0.5f) * tam.x * 0.8f;
            float z = (e.tipo == TipoUsado.Notebook ? 0f : limites.center.z) + ((float)rnd.NextDouble() - 0.5f) * tam.z * 0.8f;
            float s = 0.008f + (float)rnd.NextDouble() * 0.012f;
            Cubo(poeira, "Tufo" + i, new Vector3(x, y + 0.003f, z), new Vector3(s, 0.004f, s * 1.4f), mPo, PrimitiveType.Sphere);
        }
    }

    // Etiqueta de patrimônio: papel branco com "PATRIMÔNIO" e o número, colada na frente (ou em cima, nos baixinhos)
    void MontarEtiqueta()
    {
        etiqueta = new GameObject("EtiquetaPatrimonio").transform;
        etiqueta.SetParent(transform, false);
        bool emCima = e.tipo == TipoUsado.Roteador || e.tipo == TipoUsado.Switch || e.tipo == TipoUsado.Notebook;
        if (emCima)
        {
            float y = e.tipo == TipoUsado.Notebook ? 0.0235f : Topo() + 0.0012f;
            etiqueta.localPosition = new Vector3(limites.min.x + 0.05f, y, (e.tipo == TipoUsado.Notebook ? -0.1f : limites.min.z + 0.025f));
            etiqueta.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            float x = e.tipo == TipoUsado.Monitor ? -0.15f : limites.min.x + Mathf.Min(0.05f, limites.size.x * 0.3f);
            float y = e.tipo == TipoUsado.Monitor ? 0.128f : limites.min.y + limites.size.y * 0.18f;
            float z = e.tipo == TipoUsado.Computador ? limites.min.z - 0.0005f : e.tipo == TipoUsado.Monitor ? -0.0182f : limites.min.z - 0.002f;
            etiqueta.localPosition = new Vector3(x, y, z);
        }
        Cubo(etiqueta, "Papel", Vector3.zero, new Vector3(0.064f, 0.026f, 0.0006f), mBranco);
        for (int i = 0; i < 9; i++)   // código de barras
            Cubo(etiqueta, "Barra" + i, new Vector3(-0.024f + i * 0.0045f + (i % 3) * 0.0006f, -0.0065f, -0.0004f), new Vector3(i % 2 == 0 ? 0.0012f : 0.0022f, 0.008f, 0.0002f), mPreto);
        txtEtiqueta = Texto3D(etiqueta, new Vector3(0f, 0.004f, -0.0005f), new Vector2(0.06f, 0.014f), 0.12f, new Color(0.05f, 0.07f, 0.12f));
        txtEtiqueta.fontStyle = FontStyles.Bold;
        etiqueta.gameObject.SetActive(false);
    }

    // Plaquinha amarela "À VENDA" pendurada na frente
    void MontarPlacaVenda()
    {
        placaVenda = new GameObject("PlacaVenda").transform;
        placaVenda.SetParent(transform, false);
        placaVenda.localPosition = new Vector3(limites.max.x - 0.06f, Topo() + 0.05f, limites.min.z + 0.02f);
        Cubo(placaVenda, "Placa", Vector3.zero, new Vector3(0.1f, 0.06f, 0.002f), mPapel);
        Cubo(placaVenda, "Haste", new Vector3(0f, -0.04f, 0.002f), new Vector3(0.003f, 0.04f, 0.003f), mCinza);
        txtVenda = Texto3D(placaVenda, new Vector3(0f, 0f, -0.0015f), new Vector2(0.09f, 0.05f), 0.25f, new Color(0.12f, 0.08f, 0.02f));
        txtVenda.fontStyle = FontStyles.Bold;
        placaVenda.gameObject.SetActive(false);
    }

    // ---------- Estado ----------

    public void Atualizar()
    {
        if (poeira != null && !poeiraSumindo) poeira.gameObject.SetActive(!e.limpo && e.condicao != CondicaoUsado.Sucata);
        if (etiqueta != null)
        {
            etiqueta.gameObject.SetActive(e.patrimonio > 0);
            if (e.patrimonio > 0) txtEtiqueta.text = $"<size=60%>PATRIMÔNIO</size>\n{e.Etiqueta}";
        }
        if (placaVenda != null)
        {
            placaVenda.gameObject.SetActive(e.anunciado);
            if (e.anunciado) txtVenda.text = $"À VENDA\n<size=70%>{UIFactory.Dinheiro(EquipamentosUsados.ValorVenda(e))}</size>";
        }
    }

    bool poeiraSumindo;

    void Update()
    {
        if (piscarAte > Time.time)
        {
            bool aceso = Mathf.Repeat(Time.time, 0.6f) < 0.3f;
            if (tela != null) tela.sharedMaterial = aceso ? mTelaLigada : mTelaOff;
            if (e.tipo == TipoUsado.Roteador || e.tipo == TipoUsado.Switch) PintarLeds(aceso ? mLedLaranja : mLedOff);
        }
        else if (piscarAte > 0f)
        {
            piscarAte = 0f;
            Ligar(false);
        }
    }

    void PintarLeds(Material m) { foreach (var l in leds) if (l != null) l.sharedMaterial = m; }

    void Ligar(bool sim)
    {
        ligado = sim;
        PintarLeds(sim ? mLedVerde : mLedOff);
        if (tela != null) tela.sharedMaterial = sim ? mTelaLigada : mTelaOff;
    }

    // ---------- Pistas das ações (chamadas por UsadosFisicos) ----------

    public void Animar(string o, TelaBancada monitor)
    {
        Vector3 p = transform.TransformPoint(limites.center);
        switch (o)
        {
            case "testar": Testar(monitor); break;
            case "post": EquipamentoPecas.MostrarNaMao(FerramentaDiag.PlacaPOST, e.defeito == DefeitoUsado.Memoria ? "55" : ""); break;
            case "fonte":
                EquipamentoPecas.MostrarNaMao(FerramentaDiag.TestadorFonte, e.defeito == DefeitoUsado.Fonte ? "" : "12.1 5.0 3.3 PG",
                    e.defeito == DefeitoUsado.Fonte ? "------" : "vvvvvv");
                break;
            case "chute":
                SomManager.TocarEm("mexer", p, 0.8f, 0.9f);
                monitor?.Mostrar(TelaBancada.Modo.Texto, "Trocando peças de bancada...\n<size=70%>memória · fonte · cabo de vídeo</size>");
                break;
            case "limpar": Limpar(); break;
            case "apagar": monitor?.Progresso(e.tipo == TipoUsado.Impressora ? "Painel da impressora (USB)\nRestaurando padrão de fábrica" : "Apagamento seguro do disco\n3 passagens (LGPD)", 9f); break;
            case "etiqueta": ColarEtiqueta(); break;
            case "anunciar": Fotografar(); break;
            case "aproveitar": PecaVoando(DefeitoUsado.Memoria, false); break;
            default:
                if (o.StartsWith("trocar:") && System.Enum.TryParse(o.Substring(7), out DefeitoUsado df)) Trocar(df, monitor);
                break;
        }
        Atualizar();
    }

    void Testar(TelaBancada monitor)
    {
        Vector3 p = transform.TransformPoint(limites.center);
        SomManager.TocarEm("clique", p, 0.6f, 0.8f);
        bool bom = e.condicao == CondicaoUsado.Bom, sucata = e.condicao == CondicaoUsado.Sucata;
        switch (e.tipo)
        {
            case TipoUsado.Computador:
            case TipoUsado.Notebook:
                if (bom) { Ligar(true); monitor?.Desktop(true); }
                else if (sucata)
                {
                    monitor?.Mostrar(TelaBancada.Modo.SemSinal);
                    Fumaca(transform.TransformPoint(new Vector3(limites.center.x, limites.max.y, limites.center.z)));
                    SomManager.TocarEm("erro", p, 0.5f, 0.6f);
                }
                else if (e.defeito == DefeitoUsado.Memoria)
                {
                    Ligar(true);
                    monitor?.Mostrar(TelaBancada.Modo.SemSinal);
                    Tween.Delay(this, 0.6f, t => t.Bipes(3));
                }
                else if (e.defeito == DefeitoUsado.Bateria) { Ligar(true); monitor?.Desktop(true); }
                else monitor?.Mostrar(TelaBancada.Modo.SemSinal);   // fonte queimada: nada acende
                break;
            case TipoUsado.Impressora:
                if (sucata) { SomManager.TocarEm("erro", p, 0.6f, 0.5f); SomManager.TocarEm("mexer", p, 1f, 0.5f); PintarLeds(mLedLaranja); }
                else { Ligar(true); Tween.Delay(this, 1.2f, t => t.SoltarFolha(t.e.defeito == DefeitoUsado.Cilindro ? ResultadoPagina.Listra : ResultadoPagina.Perfeita)); }
                break;
            case TipoUsado.Monitor:
                if (bom) Ligar(true);
                else if (sucata) { PintarLeds(mLedVerde); if (tela != null) tela.sharedMaterial = mTelaOff; Rachar(); }
                else { piscarAte = Time.time + 6f; PintarLeds(mLedVerde); }
                break;
            default:
                if (bom) Ligar(true);
                else if (sucata) piscarAte = Time.time + 8f;
                else
                {
                    Ligar(false);
                    EquipamentoPecas.MostrarNaMao(FerramentaDiag.Multimetro, "", "", 0f, "V");
                }
                break;
        }
    }

    void Bipes(int n)
    {
        var s = Sequence.Create();
        for (int i = 0; i < n; i++)
            s.ChainCallback(this, t => SomManager.TocarEm("bip", t.transform.position, 0.5f, 0.7f)).ChainDelay(0.45f);
    }

    void SoltarFolha(ResultadoPagina r)
    {
        if (saidaFolha == null) return;
        var folha = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(folha.GetComponent<Collider>());
        folha.name = "Folha_" + r;
        folha.transform.SetParent(visual, false);
        folha.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        folha.transform.localScale = new Vector3(0.21f, 0.297f, 1f);
        Vector3 dentro = saidaFolha.localPosition + new Vector3(0f, folhas.Count * 0.0015f, 0.12f);
        Vector3 fora = saidaFolha.localPosition + new Vector3(0f, folhas.Count * 0.0015f - 0.004f, -0.12f);
        folha.transform.localPosition = dentro;
        folha.GetComponent<Renderer>().material = new Material(mFolha) { mainTexture = EquipamentoPecas.TexturaFolha(r) };
        Tween.LocalPosition(folha.transform, fora, 1.4f, Ease.OutQuad);
        SomManager.TocarEm("impressora", transform.position, 0.8f);
        folhas.Add(folha);
        if (folhas.Count > 3) { Object.Destroy(folhas[0]); folhas.RemoveAt(0); }
    }

    void Rachar()
    {
        if (tela == null) return;
        var t = tela.transform;
        for (int i = 0; i < 5; i++)
        {
            var r = Cubo(t.parent, "Rachadura" + i, t.localPosition + new Vector3(0.02f, 0.01f, -0.0008f), new Vector3(0.002f, 0.12f + i * 0.02f, 0.0005f), mBranco);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, -60f + i * 30f);
            r.transform.localPosition += r.transform.localRotation * Vector3.up * 0.05f;
        }
    }

    // Fumaça cinza subindo e sumindo (sucata em curto)
    void Fumaca(Vector3 de)
    {
        for (int i = 0; i < 7; i++)
        {
            var f = Cubo(null, "Fumaca", de + Random.insideUnitSphere * 0.03f, Vector3.one * 0.03f, mFumaca, PrimitiveType.Sphere).transform;
            float dt = 0.02f + i * 0.18f;
            Vector3 alvo = f.position + new Vector3(Random.Range(-0.08f, 0.08f), 0.45f + Random.value * 0.2f, Random.Range(-0.08f, 0.08f));
            Sequence.Create()
                .ChainDelay(dt)
                .Chain(Tween.Position(f, alvo, 2.2f, Ease.OutQuad))
                .Group(Tween.Scale(f, Vector3.one * 0.14f, 2.2f, Ease.OutQuad))
                .Chain(Tween.Scale(f, Vector3.zero, 0.4f))
                .OnComplete(f, x => Object.Destroy(x.gameObject));
        }
    }

    void Limpar()
    {
        if (poeira == null || !poeira.gameObject.activeSelf) return;
        poeiraSumindo = true;
        SomManager.TocarEm("mexer", transform.position, 0.6f, 1.4f);
        Fumaca(transform.TransformPoint(new Vector3(limites.center.x, Topo() + 0.02f, limites.center.z)));   // nuvem de pó
        Tween.Scale(poeira, new Vector3(1f, 0.01f, 1f), 1.4f, Ease.InQuad).OnComplete(this, v =>
        {
            v.poeira.gameObject.SetActive(false);
            v.poeira.localScale = Vector3.one;
            v.poeiraSumindo = false;
        });
    }

    void ColarEtiqueta()
    {
        if (etiqueta == null) return;
        SomManager.TocarEm("clique", etiqueta.position, 0.5f, 1.3f);
        etiqueta.gameObject.SetActive(true);
        var final = etiqueta.localPosition;
        etiqueta.localScale = Vector3.one * 0.3f;
        etiqueta.localPosition = final + new Vector3(0f, 0.08f, -0.15f);
        Tween.LocalPosition(etiqueta, final, 0.5f, Ease.OutCubic);
        Tween.Scale(etiqueta, Vector3.one, 0.5f, Ease.OutBack);
    }

    void Fotografar()
    {
        // flash do celular: uma luz branca forte que pisca duas vezes
        var luz = new GameObject("Flash").AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 2.5f;
        luz.color = Color.white;
        luz.intensity = 0f;
        var cam = Camera.main;
        luz.transform.position = cam != null ? cam.transform.position : transform.position + Vector3.up;
        Sequence.Create()
            .ChainCallback(luz, l => { l.intensity = 6f; SomManager.TocarEm("clique", l.transform.position, 0.7f, 2f); })
            .ChainDelay(0.08f).ChainCallback(luz, l => l.intensity = 0f)
            .ChainDelay(0.35f).ChainCallback(luz, l => { l.intensity = 6f; SomManager.TocarEm("clique", l.transform.position, 0.7f, 2f); })
            .ChainDelay(0.08f).ChainCallback(luz, l => Object.Destroy(l.gameObject));
    }

    // A peça velha sai do equipamento e vai para a mochila (câmera); a nova vem da mão e entra no lugar
    void Trocar(DefeitoUsado df, TelaBancada monitor)
    {
        PecaVoando(df, true);
        Tween.Delay(this, 1.6f, v => v.PecaVoando(df, false, true));
        if (calombo != null && df == DefeitoUsado.Bateria) Tween.Scale(calombo, new Vector3(calombo.localScale.x, 0.0001f, calombo.localScale.z), 1.2f).OnComplete(calombo, c => c.gameObject.SetActive(false));
        Tween.Delay(this, 3.2f, v =>
        {
            v.piscarAte = 0f;
            v.Ligar(true);
            if (v.e.tipo == TipoUsado.Computador || v.e.tipo == TipoUsado.Notebook) monitor?.Desktop(v.e.PrecisaApagar);
            if (v.e.tipo == TipoUsado.Impressora) v.SoltarFolha(ResultadoPagina.Perfeita);
        });
    }

    void PecaVoando(DefeitoUsado df, bool saindo, bool entrando = false)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var peca = new GameObject("Peca_" + df).transform;
        switch (df)
        {
            case DefeitoUsado.Memoria:
                Cubo(peca, "Placa", Vector3.zero, new Vector3(0.133f, 0.03f, 0.002f), mVerdePcb);
                for (int i = 0; i < 4; i++) Cubo(peca, "Chip", new Vector3(-0.045f + i * 0.03f, 0.002f, -0.002f), new Vector3(0.02f, 0.012f, 0.002f), mPreto);
                break;
            case DefeitoUsado.Fonte: Cubo(peca, "Fonte", Vector3.zero, new Vector3(0.15f, 0.086f, 0.14f), mCinza); break;
            case DefeitoUsado.Cilindro:
                Cubo(peca, "Cilindro", Vector3.zero, new Vector3(0.03f, 0.15f, 0.03f), mVerdePcb, PrimitiveType.Cylinder).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case DefeitoUsado.Capacitor:
                for (int i = 0; i < 3; i++) Cubo(peca, "Capacitor", new Vector3(i * 0.015f, 0f, 0f), new Vector3(0.01f, 0.012f, 0.01f), mPreto, PrimitiveType.Cylinder);
                break;
            case DefeitoUsado.Bateria: Cubo(peca, "Bateria", Vector3.zero, new Vector3(0.2f, 0.012f, 0.07f), mPreto); break;
            default: Cubo(peca, "FonteExterna", Vector3.zero, new Vector3(0.07f, 0.04f, 0.05f), mPreto); break;
        }
        Vector3 equip = transform.TransformPoint(limites.center + Vector3.up * limites.extents.y * 0.6f);
        Vector3 mao = cam.transform.TransformPoint(new Vector3(0.12f, -0.25f, 0.45f));
        bool sai = saindo && !entrando;
        peca.position = sai ? equip : mao;
        peca.rotation = cam.transform.rotation;
        SomManager.TocarEm("mexer", equip, 0.6f, sai ? 0.9f : 1.1f);
        var seq = Sequence.Create();
        if (sai) seq.Chain(Tween.Position(peca, equip + Vector3.up * 0.2f, 0.5f, Ease.OutCubic));
        else seq.ChainDelay(0.3f);
        seq.Chain(Tween.Position(peca, sai ? mao : equip, 0.8f, Ease.InOutCubic))
            .Chain(Tween.Scale(peca, Vector3.zero, 0.25f))
            .OnComplete(peca, x => Object.Destroy(x.gameObject));
    }
}
