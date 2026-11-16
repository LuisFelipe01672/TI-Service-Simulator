using PrimeTween;
using TMPro;
using UnityEngine;

// Etapa 4: ferramentas de diagnóstico na mão do técnico (como o testador de cabo). A cada medição a ferramenta
// sobe na frente da câmera e o resultado aparece NELA: o visor da placa POST corre os códigos e para; o testador
// de fonte acende os LEDs um a um e mostra as tensões; o multímetro procura o número até firmar (e bipa na
// continuidade); o pendrive é espetado e o resultado sai na tela do monitor (TelaComputador).
public partial class EquipamentoPecas
{
    static EstadoEquipamentos ferrEstado;
    static int ferrVistos;
    static Transform ferramenta;
    static FerramentaDiag ferrTipo;
    static TextMeshPro ferrVisor;
    static Renderer[] ferrLeds;
    static Transform ferrSeletor;
    static float ferrInicio;
    static string ferrTexto, ferrLedsAlvo, ferrUnidade;
    static float ferrValor;
    static bool ferrBipe, ferrBipou;
    static Material mPcb, mOuro, mCorpoAmarelo, mCorpoCinza, mLcd, mLcdVerde, mPontaVermelha, mPreta, mLedVermelho, mBranco, mAzulPendrive, mMetal;
    const float FerrDura = 7f;
    static readonly string[] CodigosAntes = { "FF", "19", "2b", "4F", "60", "9A" };

    static void AtualizarFerramentas(EstadoEquipamentos e)
    {
        if (e != ferrEstado)
        {
            ferrEstado = e;
            ferrVistos = e != null ? e.usosFerramenta : 0;
        }
        var cam = Camera.main;
        if (e != null && cam != null && e.usosFerramenta > ferrVistos)
        {
            ferrVistos = e.usosFerramenta;
            ferrTipo = e.ultimaFerramenta;
            ferrTexto = e.leituraTexto;
            ferrValor = e.leituraValor;
            ferrUnidade = e.leituraUnidade;
            ferrBipe = e.leituraBipe;
            ferrLedsAlvo = e.ledsFonte ?? "";
            MostrarFerramenta(cam);
        }
        if (ferramenta == null) return;

        float t = Time.time - ferrInicio - 0.6f;   // espera chegar na mão
        switch (ferrTipo)
        {
            case FerramentaDiag.PlacaPOST: AnimarPost(t); break;
            case FerramentaDiag.TestadorFonte: AnimarTestadorFonte(t); break;
            case FerramentaDiag.Multimetro: AnimarMultimetro(t); break;
        }
        float dura = ferrTipo == FerramentaDiag.Pendrive ? 1.6f : FerrDura;
        if (Time.time - ferrInicio > dura)
        {
            var x = ferramenta;
            ferramenta = null;
            Vector3 saida = ferrTipo == FerramentaDiag.Pendrive ? x.localPosition + new Vector3(0f, 0.05f, 0.5f)   // foi espetado
                                                                : x.localPosition + new Vector3(0f, -0.5f, 0f);
            Tween.LocalPosition(x, saida, 0.4f, Ease.InCubic).OnComplete(x, o => Destroy(o.gameObject));
        }
    }

    // ---------- Animações dos visores ----------

    static void AnimarPost(float t)
    {
        if (ferrVisor == null) return;
        if (string.IsNullOrEmpty(ferrTexto)) { ferrVisor.text = ""; Pintar(ferrLeds[0], mLedOff); return; }   // sem energia
        Pintar(ferrLeds[0], t >= 0f ? mLedVermelho : mLedOff);
        if (t < 0f) { ferrVisor.text = ""; return; }
        int i = Mathf.FloorToInt(t / 0.28f);
        ferrVisor.text = i < CodigosAntes.Length ? CodigosAntes[i] : ferrTexto;
        if (i == CodigosAntes.Length && !ferrBipou) { ferrBipou = true; SomManager.TocarEm("bip", ferramenta.position, 0.25f, 1.6f); }
    }

    static void AnimarTestadorFonte(float t)
    {
        if (ferrLeds == null) return;
        for (int i = 0; i < ferrLeds.Length; i++)
        {
            char c = i < ferrLedsAlvo.Length ? ferrLedsAlvo[i] : '-';
            bool aceso = t >= i * 0.18f && c != '-';
            Pintar(ferrLeds[i], !aceso ? mLedOff : c == 'r' ? mLedVermelho : mVerde);
        }
        if (ferrVisor != null) ferrVisor.text = t > 1.1f ? ferrTexto : "";
        if (t > 1.1f && !ferrBipou && !string.IsNullOrEmpty(ferrTexto))
        {
            ferrBipou = true;
            bool falha = ferrLedsAlvo.Contains("r");
            SomManager.TocarEm(falha ? "erro" : "bip", ferramenta.position, falha ? 0.3f : 0.25f, falha ? 1.3f : 1.8f);
        }
    }

    static void AnimarMultimetro(float t)
    {
        if (ferrSeletor != null)   // o seletor gira até a escala certa
        {
            float alvo = ferrUnidade == "V~" ? -60f : ferrUnidade == "Ω" ? 70f : -20f;
            ferrSeletor.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, alvo, Mathf.Clamp01((t + 0.6f) / 0.5f)));
        }
        if (ferrVisor == null) return;
        if (t < 0.2f) { ferrVisor.text = ferrUnidade == "Ω" ? "OL" : "0.00"; return; }
        if (!string.IsNullOrEmpty(ferrTexto)) { ferrVisor.text = ferrTexto; return; }   // "OL": circuito aberto
        // procura o número (oscila) e firma depois de ~1 s, como um multímetro de verdade
        float firmar = Mathf.Clamp01((t - 0.2f) / 1f);
        float ruido = (1f - firmar) * Mathf.Sin(t * 37f) * Mathf.Max(0.05f, Mathf.Abs(ferrValor) * 0.08f);
        float v = ferrValor * Mathf.SmoothStep(0.55f, 1f, firmar) + ruido;
        ferrVisor.text = ferrUnidade == "V~" ? v.ToString("0") : ferrUnidade == "Ω" ? v.ToString("0.0") : v.ToString("0.00");
        if (ferrBipe && !ferrBipou && t > 0.3f) { ferrBipou = true; SomManager.TocarEm("bip", ferramenta.position, 0.35f, 2.2f); }
    }

    // ---------- Montagem dos modelos (primitivas; ver ROADMAP: trocar por modelos CC-BY quando aprovados) ----------

    static void MostrarFerramenta(Camera cam)
    {
        if (ferramenta != null) Destroy(ferramenta.gameObject);
        CriarMateriaisFerramentas();
        ferramenta = new GameObject("Ferramenta_" + ferrTipo).transform;
        ferramenta.SetParent(cam.transform, false);
        ferrVisor = null; ferrLeds = null; ferrSeletor = null; ferrBipou = false;
        switch (ferrTipo)
        {
            case FerramentaDiag.PlacaPOST: MontarPlacaPost(ferramenta); break;
            case FerramentaDiag.TestadorFonte: MontarTestadorFonte(ferramenta); break;
            case FerramentaDiag.Multimetro: MontarMultimetro(ferramenta); break;
            default: MontarPendrive(ferramenta); break;
        }
        ferramenta.localPosition = new Vector3(0.02f, -0.5f, 0.36f);
        ferramenta.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        Tween.LocalPosition(ferramenta, new Vector3(0.02f, -0.07f, 0.36f), 0.55f, Ease.OutCubic);
        ferrInicio = Time.time;
        SomManager.TocarEm("mexer", cam.transform.position, 0.6f, 1.15f);
    }

    static void CriarMateriaisFerramentas()
    {
        mPcb ??= Mat(new Color(0.08f, 0.32f, 0.16f));
        mOuro ??= Mat(new Color(0.85f, 0.68f, 0.25f));
        mCorpoAmarelo ??= Mat(new Color(0.93f, 0.72f, 0.08f));
        mCorpoCinza ??= Mat(new Color(0.2f, 0.21f, 0.23f));
        mLcd ??= Mat(new Color(0.62f, 0.67f, 0.58f), new Color(0.12f, 0.13f, 0.11f));
        mLcdVerde ??= Mat(new Color(0.35f, 0.62f, 0.3f), new Color(0.15f, 0.3f, 0.12f));
        mPontaVermelha ??= Mat(new Color(0.8f, 0.1f, 0.1f));
        mPreta ??= Mat(new Color(0.05f, 0.05f, 0.06f));
        mLedOff ??= Mat(new Color(0.3f, 0.36f, 0.3f));
        mLedVermelho ??= Mat(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.15f, 0.1f) * 1.6f);
        mBranco ??= Mat(new Color(0.92f, 0.92f, 0.88f));
        mAzulPendrive ??= Mat(new Color(0.1f, 0.35f, 0.75f));
        mMetal ??= Mat(new Color(0.75f, 0.76f, 0.78f));
    }

    static TextMeshPro Visor(Transform pai, Vector3 pos, Vector2 tamanho, float fonte, Color cor, TextAlignmentOptions alinhar = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Visor", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.rectTransform.sizeDelta = tamanho;
        t.fontSize = fonte;
        t.color = cor;
        t.alignment = alinhar;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMax = fonte;
        t.fontSizeMin = fonte * 0.3f;
        t.text = "";
        return t;
    }

    // Placa POST: placa verde com conector dourado, visor de dois dígitos vermelho e LED de energia
    static void MontarPlacaPost(Transform f)
    {
        Cubo(f, "Placa", Vector3.zero, new Vector3(0.12f, 0.07f, 0.002f), mPcb);
        Cubo(f, "Conector", new Vector3(0.01f, -0.04f, 0f), new Vector3(0.08f, 0.012f, 0.0022f), mOuro);
        Cubo(f, "Chip", new Vector3(-0.035f, 0.005f, -0.0025f), new Vector3(0.022f, 0.022f, 0.003f), mPreta);
        Cubo(f, "Chip2", new Vector3(0.04f, 0.018f, -0.0025f), new Vector3(0.015f, 0.01f, 0.003f), mPreta);
        Cubo(f, "Moldura", new Vector3(0.008f, 0.012f, -0.003f), new Vector3(0.036f, 0.026f, 0.004f), mPreta);
        ferrVisor = Visor(f, new Vector3(0.008f, 0.012f, -0.0052f), new Vector2(0.034f, 0.024f), 0.22f, new Color(1f, 0.12f, 0.08f));
        ferrVisor.fontStyle = FontStyles.Bold;
        ferrLeds = new[] { Cubo(f, "LedEnergia", new Vector3(0.045f, -0.012f, -0.002f), new Vector3(0.004f, 0.004f, 0.002f), mLedOff).GetComponent<Renderer>() };
    }

    // Testador de fonte: caixa com visor de LCD e 6 LEDs, ligado ao conector branco de 24 pinos
    static void MontarTestadorFonte(Transform f)
    {
        Cubo(f, "Corpo", Vector3.zero, new Vector3(0.125f, 0.085f, 0.022f), mCorpoCinza);
        Cubo(f, "Lcd", new Vector3(-0.012f, 0f, -0.0115f), new Vector3(0.08f, 0.066f, 0.002f), mLcdVerde);
        ferrVisor = Visor(f, new Vector3(-0.012f, 0f, -0.0128f), new Vector2(0.076f, 0.062f), 0.14f, new Color(0.05f, 0.1f, 0.05f), TextAlignmentOptions.Left);
        ferrVisor.lineSpacing = -20f;
        ferrLeds = new Renderer[6];
        string[] rotulos = { "12", "5", "3", "-12", "SB", "PG" };
        for (int i = 0; i < 6; i++)
        {
            float y = 0.03f - i * 0.012f;
            ferrLeds[i] = Cubo(f, "Led_" + rotulos[i], new Vector3(0.047f, y, -0.0115f), new Vector3(0.006f, 0.005f, 0.002f), mLedOff).GetComponent<Renderer>();
        }
        Cubo(f, "Conector24", new Vector3(0f, 0.057f, 0f), new Vector3(0.052f, 0.014f, 0.012f), mBranco);
        Cubo(f, "Fios", new Vector3(0f, 0.08f, 0.002f), new Vector3(0.045f, 0.035f, 0.006f), mCorpoAmarelo);
    }

    // Multímetro: corpo amarelo, visor de LCD, seletor giratório e as duas pontas de prova
    static void MontarMultimetro(Transform f)
    {
        Cubo(f, "Corpo", Vector3.zero, new Vector3(0.08f, 0.15f, 0.03f), mCorpoAmarelo);
        Cubo(f, "Frente", new Vector3(0f, 0f, -0.0152f), new Vector3(0.066f, 0.135f, 0.001f), mCorpoCinza);
        Cubo(f, "Lcd", new Vector3(0f, 0.04f, -0.016f), new Vector3(0.056f, 0.032f, 0.002f), mLcd);
        ferrVisor = Visor(f, new Vector3(0.002f, 0.04f, -0.0172f), new Vector2(0.054f, 0.03f), 0.16f, new Color(0.05f, 0.05f, 0.05f), TextAlignmentOptions.Right);
        var seletor = new GameObject("Seletor").transform;
        seletor.SetParent(f, false);
        seletor.localPosition = new Vector3(0f, -0.02f, -0.017f);
        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(disco.GetComponent<Collider>());
        disco.transform.SetParent(seletor, false);
        disco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disco.transform.localScale = new Vector3(0.036f, 0.003f, 0.036f);
        disco.GetComponent<Renderer>().sharedMaterial = mPreta;
        Cubo(seletor, "Ponteiro", new Vector3(0f, 0.01f, -0.003f), new Vector3(0.004f, 0.018f, 0.002f), mLcd);
        ferrSeletor = seletor;
        Cubo(f, "Borne", new Vector3(-0.015f, -0.058f, -0.016f), new Vector3(0.007f, 0.007f, 0.003f), mPontaVermelha);
        Cubo(f, "BorneCom", new Vector3(0.015f, -0.058f, -0.016f), new Vector3(0.007f, 0.007f, 0.003f), mPreta);
        // pontas de prova saindo por cima, como se estivessem encostadas no ponto medido
        Cubo(f, "PontaVermelha", new Vector3(-0.055f, 0.06f, -0.01f), new Vector3(0.006f, 0.11f, 0.006f), mPontaVermelha).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        Cubo(f, "PontaPreta", new Vector3(0.055f, 0.06f, -0.01f), new Vector3(0.006f, 0.11f, 0.006f), mPreta).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
    }

    // Pendrive: espetado no gabinete; o resultado aparece no monitor
    static void MontarPendrive(Transform f)
    {
        Cubo(f, "Corpo", Vector3.zero, new Vector3(0.018f, 0.05f, 0.008f), mAzulPendrive);
        Cubo(f, "Usb", new Vector3(0f, 0.032f, 0f), new Vector3(0.012f, 0.016f, 0.0045f), mMetal);
        f.localScale = Vector3.one * 1.6f;
    }
}

// Monitor do cliente: tela do pendrive de diagnóstico e os sintomas que aparecem na tela
// (tela azul da memória com erro, tela apagando da fonte fraca, data de 2009 da bateria fraca)
public partial class EquipamentoPecas
{
    TelaComputador telaPc;
    EstadoEquipamentos telaEstado;
    int telasVistas;

    void AtualizarTelaDiagnostico(EstadoEquipamentos e)
    {
        if (telaPc == null && telaViva != null) telaPc = telaViva.GetComponentInChildren<TelaComputador>(true);
        if (telaPc == null) return;
        if (e != telaEstado) { telaEstado = e; telasVistas = e != null ? e.telasPendrive : 0; }
        telaPc.dataForcada = e != null && e.pcBateriaFraca ? "00:13  01/01/2009" : null;
        if (e == null) return;

        // Resultado do pendrive de diagnóstico (fica uns segundos e o computador volta para o sistema)
        if (e.telasPendrive > telasVistas)
        {
            telasVistas = e.telasPendrive;
            telaPc.MostrarSobreposicao(e.telaPendriveTitulo, e.telaPendriveProgresso, e.telaPendriveResultado,
                new Color(0.02f, 0.03f, 0.12f, 1f), 16f, 4f);
        }
        if (!e.pcLigado) { e.pcSegundosTelaAzul = 0f; e.pcSegundosFonteFraca = 0f; return; }

        // Memória com erro: tela azul de vez em quando
        if (e.pcMemoriaIntermitente)
        {
            e.pcSegundosTelaAzul += Time.deltaTime;
            if (e.pcSegundosTelaAzul > 50f)
            {
                e.pcSegundosTelaAzul = 0f;
                telaPc.MostrarSobreposicao(":(\nSeu computador encontrou um problema e precisa ser reiniciado.",
                    "Código de parada: MEMORY_MANAGEMENT", "Reiniciando...", new Color(0.05f, 0.3f, 0.7f, 1f), 6f, 0f);
                SomManager.TocarEm("erro", transform.position, 0.4f, 0.8f);
                GameUI.Instance?.Toast("O computador deu <b>tela azul</b> e reiniciou.", UIFactory.Alerta, 3f);
            }
        }
        // Fonte fraca: a tela apaga e o computador reinicia sozinho
        if (e.pcFonteFraca)
        {
            e.pcSegundosFonteFraca += Time.deltaTime;
            if (e.pcSegundosFonteFraca > 40f)
            {
                e.pcSegundosFonteFraca = 0f;
                telaPc.MostrarSobreposicao("", "", "", Color.black, 3f, 0f);
                SomManager.TocarEm("mexer", transform.position, 0.5f, 0.6f);
                GameUI.Instance?.Toast("A tela apagou e o computador <b>reiniciou sozinho</b>.", UIFactory.Alerta, 3f);
            }
        }
    }
}
