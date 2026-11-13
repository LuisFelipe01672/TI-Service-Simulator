using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Funções para montar a interface por código (Canvas, painéis, textos, botões).
// Centraliza cores e tamanhos para o jogo inteiro ter o mesmo visual.
public static class UIFactory
{
    // Paleta do jogo
    public static readonly Color Fundo        = Hex("#151A22F2");
    public static readonly Color Cartao       = Hex("#1F2632");
    public static readonly Color CartaoClaro  = Hex("#2A3342");
    public static readonly Color CorTexto     = Hex("#E8ECF2");
    public static readonly Color CorSuave     = Hex("#9AA6B8");
    public static readonly Color Destaque     = Hex("#3FA9F5");
    public static readonly Color Sucesso      = Hex("#3CCB7F");
    public static readonly Color Alerta       = Hex("#F5B83F");
    public static readonly Color Perigo       = Hex("#F2545B");
    public static readonly Color Overlay      = new Color(0f, 0f, 0f, 0.55f);

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    public static Canvas CriarCanvas(string nome, int ordem = 0)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        GarantirEventSystem();
        return canvas;
    }

    public static void GarantirEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(es);
    }

    public static RectTransform Painel(Transform pai, string nome, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(pai, false);
        go.GetComponent<Image>().color = cor;
        return (RectTransform)go.transform;
    }

    public static RectTransform Vazio(Transform pai, string nome)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    public static TextMeshProUGUI Texto(Transform pai, string texto, float tamanho, Color cor,
                                         FontStyles estilo = FontStyles.Normal,
                                         TextAlignmentOptions alinhamento = TextAlignmentOptions.Left)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.fontStyle = estilo;
        t.alignment = alinhamento;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    public static Button Botao(Transform pai, string rotulo, Action aoClicar, Color? cor = null,
                               float largura = 220, float altura = 52, float tamanhoFonte = 24)
    {
        var rt = Painel(pai, "Botao_" + rotulo, cor ?? Destaque);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = largura;
        le.preferredHeight = altura;
        le.minHeight = altura;
        rt.sizeDelta = new Vector2(largura, altura);

        var botao = rt.gameObject.AddComponent<Button>();
        var cores = botao.colors;
        cores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cores.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        botao.colors = cores;
        botao.onClick.AddListener(() => SomManager.Tocar("clique", 0.6f));
        if (aoClicar != null) botao.onClick.AddListener(() => aoClicar());

        var t = Texto(rt, rotulo, tamanhoFonte, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Esticar(t.rectTransform);
        return botao;
    }

    // Barra deslizante (volume, sensibilidade...): fundo, preenchimento e alça
    public static Slider Barra(Transform pai, float min, float max, float valor, Action<float> aoMudar, float largura = 360)
    {
        var raiz = Vazio(pai, "Barra");
        var le = raiz.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = largura; le.minWidth = largura;
        le.preferredHeight = 34; le.minHeight = 34;

        var fundo = Painel(raiz, "Fundo", CartaoClaro);
        fundo.anchorMin = new Vector2(0, 0.35f); fundo.anchorMax = new Vector2(1, 0.65f);
        fundo.offsetMin = fundo.offsetMax = Vector2.zero;

        var areaFill = Vazio(raiz, "AreaFill");
        areaFill.anchorMin = new Vector2(0, 0.35f); areaFill.anchorMax = new Vector2(1, 0.65f);
        areaFill.offsetMin = areaFill.offsetMax = Vector2.zero;
        var fill = Painel(areaFill, "Fill", Destaque);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        var areaAlca = Vazio(raiz, "AreaAlca");
        areaAlca.anchorMin = Vector2.zero; areaAlca.anchorMax = Vector2.one;
        areaAlca.offsetMin = new Vector2(10, 0); areaAlca.offsetMax = new Vector2(-10, 0);
        var alca = Painel(areaAlca, "Alca", Color.white);
        alca.anchorMin = new Vector2(0, 0); alca.anchorMax = new Vector2(0, 1);
        alca.sizeDelta = new Vector2(20, 0);

        var s = raiz.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = alca;
        s.targetGraphic = alca.GetComponent<Image>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max;
        s.SetValueWithoutNotify(valor);
        if (aoMudar != null) s.onValueChanged.AddListener(v => aoMudar(v));
        return s;
    }

    public static VerticalLayoutGroup Vertical(RectTransform rt, float espaco = 12, int margem = 0,
                                               TextAnchor alinhamento = TextAnchor.UpperLeft)
    {
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = espaco;
        v.padding = new RectOffset(margem, margem, margem, margem);
        v.childAlignment = alinhamento;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup Horizontal(RectTransform rt, float espaco = 12, int margem = 0,
                                                   TextAnchor alinhamento = TextAnchor.MiddleLeft)
    {
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = espaco;
        h.padding = new RectOffset(margem, margem, margem, margem);
        h.childAlignment = alinhamento;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return h;
    }

    public static void AjustarAoConteudo(RectTransform rt, bool vertical = true, bool horizontal = false)
    {
        var f = rt.gameObject.AddComponent<ContentSizeFitter>();
        f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
    }

    // Lista com rolagem; devolve o "content" onde os itens devem ser criados
    public static RectTransform ListaComRolagem(Transform pai, string nome)
    {
        var raiz = Painel(pai, nome, new Color(0, 0, 0, 0));
        var scroll = raiz.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 60f;

        var viewport = Painel(raiz, "Viewport", new Color(0, 0, 0, 0.01f));
        Esticar(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = Vazio(viewport, "Content");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        Vertical(content, 12, 4);
        AjustarAoConteudo(content);

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    public static void Esticar(RectTransform rt, float margem = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, margem);
        rt.offsetMax = new Vector2(-margem, -margem);
    }

    public static void Ancorar(RectTransform rt, Vector2 ancora, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = ancora;
        rt.anchorMax = ancora;
        rt.pivot = ancora;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    public static void Limpar(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }

    // Formata no padrão brasileiro: R$ 1.234,50
    public static string Dinheiro(float valor)
    {
        string s = valor.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        s = s.Replace(",", "#").Replace(".", ",").Replace("#", ".");
        return "R$ " + s;
    }
}
