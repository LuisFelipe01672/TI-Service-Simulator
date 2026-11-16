using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela "ligada" de um computador 3D: um canvas no próprio monitor com o sistema fictício do jogo (TI OS) —
// papel de parede, ícones e barra de tarefas com a hora do jogo. Cobre a textura original do modelo.
// O objeto deve ficar no centro da tela, com o eixo Z local apontando para fora do monitor.
public class TelaComputador : MonoBehaviour
{
    public Vector2 tamanho = new Vector2(0.5f, 0.3f);   // metros (largura x altura da área visível)
    public string usuario = "Técnico";

    TextMeshProUGUI relogio;
    RectTransform canvasRt;

    // Tela por cima do sistema (Etapa 4): pendrive de diagnóstico, tela azul. Some sozinha.
    RectTransform sobreposicao;
    TextMeshProUGUI sobTitulo, sobTexto;
    float sobInicio, sobFim;
    string sobProgresso, sobResultado;
    // Bateria da placa-mãe fraca: o relógio mostra a data que a placa perdeu (null = hora do jogo)
    [System.NonSerialized] public string dataForcada;
    const float PixelsPorMetro = 2000f;
    static Texture2D papelDeParede;

    void Start() { if (canvasRt == null) Montar(); }

    void Montar()
    {
        var go = new GameObject("CanvasTela", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0, 180f, 0);   // o canvas olha para -Z: vira para fora da tela
        go.transform.localScale = Vector3.one / PixelsPorMetro;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = tamanho * PixelsPorMetro;
        canvasRt = rt;

        // Papel de parede: degradê azul-petróleo com a marca do sistema
        var fundo = new GameObject("PapelDeParede", typeof(RectTransform)).AddComponent<RawImage>();
        fundo.transform.SetParent(rt, false);
        UIFactory.Esticar(fundo.rectTransform);
        fundo.texture = PapelDeParede();
        fundo.raycastTarget = false;

        var marca = UIFactory.Texto(rt, "TI OS", 120, new Color(1f, 1f, 1f, 0.22f), FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Esticar(marca.rectTransform);

        // Ícones na esquerda
        var icones = UIFactory.Vazio(rt, "Icones");
        icones.anchorMin = new Vector2(0, 1); icones.anchorMax = new Vector2(0, 1); icones.pivot = new Vector2(0, 1);
        icones.anchoredPosition = new Vector2(24, -24);
        icones.sizeDelta = new Vector2(140, 600);
        UIFactory.Vertical(icones, 22, 0, TextAnchor.UpperLeft);
        foreach (var (nome, cor) in new[] { ("Chamados", UIFactory.Destaque), ("Loja", UIFactory.Sucesso),
                                            ("Banco", UIFactory.Alerta), ("Acesso remoto", UIFactory.Perigo) })
            Icone(icones, nome, cor);

        // Barra de tarefas com o relógio
        var barra = UIFactory.Painel(rt, "BarraTarefas", new Color(0.06f, 0.08f, 0.11f, 0.92f));
        barra.anchorMin = new Vector2(0, 0); barra.anchorMax = new Vector2(1, 0); barra.pivot = new Vector2(0.5f, 0);
        barra.sizeDelta = new Vector2(0, 54);
        var iniciar = UIFactory.Texto(barra, "  Iniciar", 26, UIFactory.CorTexto, FontStyles.Bold);
        UIFactory.Esticar(iniciar.rectTransform);
        iniciar.alignment = TextAlignmentOptions.Left;
        relogio = UIFactory.Texto(barra, "", 24, UIFactory.CorTexto, FontStyles.Normal, TextAlignmentOptions.Right);
        UIFactory.Esticar(relogio.rectTransform, 0);
        relogio.margin = new Vector4(0, 0, 18, 0);
    }

    static void Icone(RectTransform pai, string nome, Color cor)
    {
        var item = UIFactory.Vazio(pai, "Icone_" + nome);
        item.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
        var quadrado = UIFactory.Painel(item, "Quadrado", cor);
        quadrado.anchorMin = quadrado.anchorMax = new Vector2(0.5f, 1); quadrado.pivot = new Vector2(0.5f, 1);
        quadrado.sizeDelta = new Vector2(64, 64);
        var t = UIFactory.Texto(item, nome, 20, Color.white, FontStyles.Normal, TextAlignmentOptions.Bottom);
        UIFactory.Esticar(t.rectTransform);
    }

    static Texture2D PapelDeParede()
    {
        if (papelDeParede != null) return papelDeParede;
        const int w = 64, h = 36;
        papelDeParede = new Texture2D(w, h, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var a = new Color(0.04f, 0.22f, 0.32f);
        var b = new Color(0.07f, 0.45f, 0.52f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = Mathf.Clamp01((x / (float)w) * 0.6f + (y / (float)h) * 0.4f);
                float onda = Mathf.Sin(x * 0.18f + y * 0.1f) * 0.04f;
                papelDeParede.SetPixel(x, y, Color.Lerp(a, b, t + onda));
            }
        papelDeParede.Apply();
        return papelDeParede;
    }

    void Update()
    {
        if (relogio != null && !string.IsNullOrEmpty(dataForcada)) relogio.text = dataForcada + "  ";
        else if (relogio != null && DiaManager.Instance != null)
            relogio.text = DiaManager.Instance.HoraTexto() + "  ";
        AtualizarSobreposicao();
    }

    // Mostra uma tela inteira por cima do sistema: primeiro o progresso (com barra), depois o resultado
    public void MostrarSobreposicao(string titulo, string progresso, string resultado, Color fundo, float segundos, float segundosProgresso = 3f)
    {
        if (canvasRt == null) Montar();
        if (sobreposicao == null)
        {
            sobreposicao = UIFactory.Painel(canvasRt, "Sobreposicao", fundo);
            UIFactory.Esticar(sobreposicao);
            sobTitulo = UIFactory.Texto(sobreposicao, "", 58, Color.white, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            UIFactory.Esticar(sobTitulo.rectTransform, 40);
            sobTexto = UIFactory.Texto(sobreposicao, "", 46, new Color(0.85f, 0.9f, 0.95f), FontStyles.Normal, TextAlignmentOptions.Left);
            UIFactory.Esticar(sobTexto.rectTransform, 40);
        }
        sobreposicao.GetComponent<Image>().color = fundo;
        sobreposicao.gameObject.SetActive(true);
        sobreposicao.SetAsLastSibling();
        sobTitulo.text = titulo;
        sobProgresso = progresso;
        sobResultado = resultado;
        sobInicio = Time.time;
        sobFim = Time.time + segundos;
        sobFimProgresso = segundosProgresso;
        AtualizarSobreposicao();
    }

    float sobFimProgresso = 3f;

    void AtualizarSobreposicao()
    {
        if (sobreposicao == null || !sobreposicao.gameObject.activeSelf) return;
        if (Time.time > sobFim) { sobreposicao.gameObject.SetActive(false); return; }
        float t = Time.time - sobInicio;
        if (sobFimProgresso <= 0f) { sobTexto.text = string.IsNullOrEmpty(sobProgresso) ? sobResultado : sobProgresso + "\n\n" + sobResultado; return; }
        if (t < sobFimProgresso)
        {
            int barras = Mathf.RoundToInt(30f * t / sobFimProgresso);
            sobTexto.text = $"{sobProgresso}\n\n[{new string('#', barras)}{new string('.', 30 - barras)}]  {Mathf.RoundToInt(100f * t / sobFimProgresso)}%";
        }
        else sobTexto.text = $"{sobProgresso}\n\n[{new string('#', 30)}]  100%\n\n{sobResultado}";
    }

    // Telas menores (notebook): o texto encolhe para caber na janela
    static void Encolher(TextMeshProUGUI t, float max) { t.enableAutoSizing = true; t.fontSizeMax = max; t.fontSizeMin = 8; }

    public bool SobreposicaoVisivel => sobreposicao != null && sobreposicao.gameObject.activeSelf;

    // ---------- Etapa 7: pistas de software (ficam até o problema sumir) ----------
    RectTransform pista;
    string chavePista;

    // Mostra uma janela do sistema por cima da área de trabalho (ou a tela inteira, no bloqueio) e propagandas pulando.
    // chave null = sem pista. Só refaz quando a chave muda.
    public void Pista(string chave, string titulo, string texto, Color barra, bool telaCheia = false, int popups = 0)
    {
        if (chave == chavePista) return;
        chavePista = chave;
        if (canvasRt == null) Montar();
        if (pista != null) Destroy(pista.gameObject);
        pista = null;
        if (chave == null) return;

        pista = UIFactory.Vazio(canvasRt, "Pista");
        UIFactory.Esticar(pista);
        if (telaCheia)
        {
            var fundo = UIFactory.Painel(pista, "Bloqueio", new Color(0.03f, 0.1f, 0.16f, 1f));
            UIFactory.Esticar(fundo);
            var t = UIFactory.Texto(fundo, $"<size=140%><b>{titulo}</b></size>\n\n{texto}", 44, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.Esticar(t.rectTransform, 60);
            Encolher(t, 44);
        }
        else if (!string.IsNullOrEmpty(titulo))
        {
            var jan = UIFactory.Painel(pista, "Janela", new Color(0.93f, 0.94f, 0.96f, 1f));
            jan.anchorMin = new Vector2(0.18f, 0.25f); jan.anchorMax = new Vector2(0.82f, 0.8f);
            jan.offsetMin = jan.offsetMax = Vector2.zero;
            var b = UIFactory.Painel(jan, "Barra", barra);
            b.anchorMin = new Vector2(0, 1); b.anchorMax = new Vector2(1, 1); b.pivot = new Vector2(0.5f, 1);
            b.sizeDelta = new Vector2(0, 70);
            var tt = UIFactory.Texto(b, "  " + titulo, 34, Color.white, FontStyles.Bold);
            UIFactory.Esticar(tt.rectTransform);
            tt.alignment = TextAlignmentOptions.Left;
            var corpo = UIFactory.Texto(jan, texto, 32, new Color(0.1f, 0.14f, 0.2f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UIFactory.Esticar(corpo.rectTransform, 24);
            corpo.margin = new Vector4(10, 80, 10, 10);
            Encolher(corpo, 32);
            Encolher(tt, 34);
        }
        string[] anuncios = { "PARABÉNS!!! Você ganhou um celular", "Seu PC está LENTO! Clique aqui", "OFERTA 90% OFF só hoje",
                              "Vírus detectado! Baixe o Limpador Pro", "Ganhe dinheiro em casa" };
        for (int i = 0; i < popups; i++)
        {
            var p = UIFactory.Painel(pista, "Propaganda" + i, i % 2 == 0 ? new Color(0.95f, 0.85f, 0.1f, 1f) : new Color(0.85f, 0.12f, 0.15f, 1f));
            float x = 0.05f + (i * 0.37f) % 0.62f, y = 0.12f + (i * 0.29f) % 0.6f;
            p.anchorMin = new Vector2(x, y); p.anchorMax = new Vector2(x + 0.33f, y + 0.24f);
            p.offsetMin = p.offsetMax = Vector2.zero;
            var t = UIFactory.Texto(p, $"<b>{anuncios[i % anuncios.Length]}</b>\n<size=70%>[ X ]</size>", 30,
                                    i % 2 == 0 ? new Color(0.5f, 0f, 0f) : Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.Esticar(t.rectTransform, 10);
            Encolher(t, 30);
        }
        if (sobreposicao != null) sobreposicao.SetAsLastSibling();
    }
}
