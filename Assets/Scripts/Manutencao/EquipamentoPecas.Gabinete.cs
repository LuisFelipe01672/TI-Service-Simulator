using UnityEngine;

// Gabinete do computador (Fase 4), acompanhando o estado do chamado: o técnico vira o gabinete na mesa e
// deita a tampa lateral ao lado; por dentro, a poeira aparece conforme o estado e some ao limpar; o LED da frente
// acende com o PC ligado.
// Etapa 10C: tudo que é feito segurando E anda junto com a barra (AnimacaoPecas): os 2 parafusos da tampa giram e
// saem antes de o gabinete virar; as travas da memória abrem, o pente sobe, sai e volta; a fonte e o disco deslizam
// para fora e para dentro (o SSD entra no lugar do HD); os 4 parafusos do cooler soltam em X e o cooler sai; a pasta
// velha some com o pano e a nova cresce num grão; a poeira encolhe enquanto o ar comprimido sopra.
public partial class EquipamentoPecas
{
    Transform gabinete, tampaLateral, poeiraGab, pente1, fontePc, coolerPc, dissipadorPc, hdPc, pastaPc;
    Transform travaCima, travaBaixo;
    Transform[] parafusosTampa, parafusosCooler;
    Vector3 coolerPos, dissipadorPos, hdPos, hdEscala;
    Material hdMat, penteMat, fonteMat;
    float coolerFora;
    static Material mPastaSeca, mPastaNova, mSsd, mParafuso, mTrava, mPenteNovo, mFonteNova;
    Renderer ledGabinete;
    Vector3 gabPos, tampaPos, pentePos, fontePos;
    float aberturaGab;
    EstadoEquipamentos estadoPcVisto;
    int trocasMemVistas, trocasFonteVistas;
    bool penteNovo, fonteNova;

    void MontarGabinete()
    {
        gabinete = visual.Find("Gabinete");
        if (gabinete == null) return;
        tampaLateral = gabinete.Find("TampaLateral");
        poeiraGab = gabinete.Find("Poeira");
        pente1 = gabinete.Find("Pente1");
        fontePc = gabinete.Find("Fonte");
        var led = gabinete.Find("LedGabinete");
        if (led != null) ledGabinete = led.GetComponent<Renderer>();
        gabPos = gabinete.localPosition;
        if (tampaLateral != null) tampaPos = tampaLateral.localPosition;
        if (pente1 != null) { pentePos = pente1.localPosition; penteMat = pente1.GetComponent<Renderer>().sharedMaterial; }
        if (fontePc != null) { fontePos = fontePc.localPosition; fonteMat = fontePc.GetComponent<Renderer>().sharedMaterial; }
        coolerPc = gabinete.Find("Cooler");
        dissipadorPc = gabinete.Find("Dissipador");
        hdPc = gabinete.Find("HD");
        pastaPc = gabinete.Find("Pasta");
        if (coolerPc != null) coolerPos = coolerPc.localPosition;
        if (dissipadorPc != null) dissipadorPos = dissipadorPc.localPosition;
        if (hdPc != null) { hdPos = hdPc.localPosition; hdEscala = hdPc.localScale; hdMat = hdPc.GetComponent<Renderer>().sharedMaterial; }
        mPastaSeca ??= Mat(new Color(0.5f, 0.47f, 0.4f));
        mPastaNova ??= Mat(new Color(0.93f, 0.93f, 0.95f));
        mSsd ??= Mat(new Color(0.1f, 0.1f, 0.12f));
        if (mParafuso == null) { mParafuso = Mat(new Color(0.7f, 0.71f, 0.73f)); mParafuso.SetFloat("_Metallic", 0.8f); }
        mTrava ??= Mat(new Color(0.92f, 0.92f, 0.9f));
        mPenteNovo ??= Mat(new Color(0.1f, 0.35f, 0.18f));
        if (mFonteNova == null) { mFonteNova = Mat(new Color(0.12f, 0.12f, 0.13f)); mFonteNova.SetFloat("_Metallic", 0.4f); }

        // Parafusos da tampa lateral, na traseira (cabeça para fora)
        if (tampaLateral != null)
        {
            Vector3 s = tampaLateral.localScale;
            parafusosTampa = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                parafusosTampa[i] = Parafuso(gabinete, "ParafusoTampa" + i);
                parafusosTampa[i].localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
        // Travas brancas nas pontas do pente de memória
        if (pente1 != null)
        {
            float meia = pente1.localScale.y / 2f + 0.004f;
            travaCima = Cubo(gabinete, "TravaCima", pentePos + new Vector3(0.006f, meia, 0f), new Vector3(0.012f, 0.008f, 0.008f), mTrava).transform;
            travaBaixo = Cubo(gabinete, "TravaBaixo", pentePos + new Vector3(0.006f, -meia, 0f), new Vector3(0.012f, 0.008f, 0.008f), mTrava).transform;
        }
        // Parafusos do cooler (4 cantos do dissipador)
        if (dissipadorPc != null)
        {
            parafusosCooler = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                parafusosCooler[i] = Parafuso(gabinete, "ParafusoCooler" + i);
                parafusosCooler[i].localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }
    }

    static Transform Parafuso(Transform pai, string nome)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        c.name = nome;
        Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localScale = new Vector3(0.008f, 0.004f, 0.008f);
        c.GetComponent<Renderer>().sharedMaterial = mParafuso;
        return c.transform;
    }

    // Ordem em X dos parafusos do cooler: canto 0, oposto 3, canto 1, oposto 2
    static readonly int[] OrdemX = { 0, 3, 1, 2 };
    static readonly Vector2[] CantosCooler = { new Vector2(0.034f, 0.034f), new Vector2(-0.034f, 0.034f), new Vector2(0.034f, -0.034f), new Vector2(-0.034f, -0.034f) };

    void AtualizarGabinete(EstadoEquipamentos e)
    {
        if (gabinete == null) return;

        // Chamado novo: peças originais de volta
        if (e != estadoPcVisto)
        {
            estadoPcVisto = e;
            trocasMemVistas = e != null ? e.pcTrocasMemoria : 0;
            trocasFonteVistas = e != null ? e.pcTrocasFonte : 0;
            penteNovo = fonteNova = false;
            if (poeiraGab != null) poeiraGab.localScale = Vector3.one;
        }
        if (e != null && e.pcTrocasMemoria > trocasMemVistas) { trocasMemVistas = e.pcTrocasMemoria; }
        if (e != null && e.pcTrocasFonte > trocasFonteVistas) { trocasFonteVistas = e.pcTrocasFonte; fonteNova = true; }

        float p;
        // ---------- Abrir / fechar ----------
        bool aberto = e != null && e.pcGabineteAberto;
        float parafTampa = aberto ? 1f : 0f;   // 0 = apertados, 1 = fora
        if (AnimacaoPecas.Segurando(eq, "gab_abrir", out p))
        {
            parafTampa = AnimacaoPecas.Trecho(p, 0f, 0.6f);
            aberturaGab = Mathf.Max(aberturaGab, AnimacaoPecas.Trecho(p, 0.6f, 1f) * 0.45f);
        }
        else if (AnimacaoPecas.Segurando(eq, "gab_fechar", out p))
        {
            aberturaGab = Mathf.Min(aberturaGab, 1f - AnimacaoPecas.Trecho(p, 0f, 0.6f));
            parafTampa = 1f - AnimacaoPecas.Trecho(p, 0.6f, 1f);
        }
        else aberturaGab = Mathf.MoveTowards(aberturaGab, aberto ? 1f : 0f, 1.3f * Time.deltaTime);
        float t = Mathf.SmoothStep(0f, 1f, aberturaGab);
        gabinete.localPosition = gabPos + new Vector3(0.1f, 0f, -0.14f) * t;
        gabinete.localRotation = Quaternion.Euler(0f, -90f * t, 0f);
        if (tampaLateral != null)
        {
            float tt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, aberturaGab));
            // a tampa fica de pé, encostada do outro lado do gabinete (não cai da mesa)
            tampaLateral.localPosition = Vector3.Lerp(tampaPos, new Vector3(0.135f, tampaPos.y, 0f), tt) + new Vector3(0f, 0f, -0.5f * Mathf.Sin(tt * Mathf.PI) * 0.6f);
            tampaLateral.localRotation = Quaternion.Euler(0f, 0f, -7f * tt);
            // Parafusos na traseira: giram e saem; fora, somem (ficam no potinho)
            Vector3 s = tampaLateral.localScale;
            for (int i = 0; i < parafusosTampa.Length; i++)
            {
                var pz = parafusosTampa[i];
                float x = tampaPos.x + s.x * 0.5f;
                Vector3 baseP = new Vector3(x, tampaPos.y + (i == 0 ? 0.3f : -0.3f) * s.y, tampaPos.z + s.z / 2f + 0.004f);
                pz.localPosition = baseP + new Vector3(0f, 0f, 0.025f * parafTampa);
                pz.localRotation = Quaternion.Euler(90f, parafTampa * 1080f, 0f);
                bool mostra = parafTampa < 0.98f && aberturaGab < 0.5f;
                if (pz.gameObject.activeSelf != mostra) pz.gameObject.SetActive(mostra);
            }
        }

        // ---------- Poeira: encolhe enquanto o ar sopra ----------
        if (poeiraGab != null)
        {
            bool temPoeira = e != null && e.pcPoeira > 0.3f;
            bool limpando = AnimacaoPecas.Segurando(eq, "gab_limpar", out p);
            if (limpando) temPoeira = true;
            poeiraGab.localScale = new Vector3(1f, limpando ? Mathf.Lerp(1f, 0.03f, p) : 1f, 1f);
            if (poeiraGab.gameObject.activeSelf != temPoeira) poeiraGab.gameObject.SetActive(temPoeira);
        }

        // ---------- Memória: travas abrem, o pente sai e volta (o novo é mais verde) ----------
        if (pente1 != null)
        {
            bool troca = AnimacaoPecas.Segurando(eq, "gab_memoria_troca", out p);
            bool limpa = !troca && AnimacaoPecas.Segurando(eq, "gab_memoria", out p);
            float travas = 0f, solta = 0f, longe = 0f;
            if (troca || limpa)
            {
                travas = AnimacaoPecas.Trecho(p, 0f, 0.15f) * (1f - AnimacaoPecas.Trecho(p, 0.85f, 1f));
                // sai do slot (perpendicular à placa) e depois a mão leva para cima; volta ao contrário
                solta = AnimacaoPecas.Trecho(p, 0.15f, 0.28f) * (1f - AnimacaoPecas.Trecho(p, 0.72f, 0.85f));
                longe = AnimacaoPecas.Trecho(p, 0.28f, 0.42f) * (1f - AnimacaoPecas.Trecho(p, 0.58f, 0.72f));
                if (troca && p > 0.5f) penteNovo = true;
            }
            pente1.localPosition = pentePos + new Vector3(-0.035f * solta - 0.06f * longe, 0.1f * longe, 0f);
            pente1.localRotation = Quaternion.Euler(0f, 0f, -12f * longe);
            if (penteMat != null) Pintar(pente1.GetComponent<Renderer>(), penteNovo ? mPenteNovo : penteMat);
            if (travaCima != null)
            {
                travaCima.localRotation = Quaternion.Euler(0f, 0f, 55f * travas);
                travaBaixo.localRotation = Quaternion.Euler(0f, 0f, -55f * travas);
            }
        }

        // ---------- Fonte: desliza para fora pela lateral e a nova entra ----------
        if (fontePc != null)
        {
            float fora = 0f;
            if (AnimacaoPecas.Segurando(eq, "gab_fonte", out p))
            {
                fora = AnimacaoPecas.Trecho(p, 0.1f, 0.45f) * (1f - AnimacaoPecas.Trecho(p, 0.55f, 0.95f));
                if (p > 0.5f) fonteNova = true;
            }
            fontePc.localPosition = fontePos + new Vector3(-0.28f * fora, 0.02f * fora, 0f);
            if (fonteMat != null) Pintar(fontePc.GetComponent<Renderer>(), fonteNova ? mFonteNova : fonteMat);
        }

        // ---------- Cooler: 4 parafusos soltam em X, o cooler sai e mostra a pasta ----------
        if (coolerPc != null && dissipadorPc != null)
        {
            bool tirando = AnimacaoPecas.Segurando(eq, "gab_cooler_tirar", out float pt);
            bool pondo = !tirando && AnimacaoPecas.Segurando(eq, "gab_cooler_por", out pt);
            bool fora = e != null && e.pcCoolerFora;
            if (tirando) coolerFora = AnimacaoPecas.Trecho(pt, 0.8f, 1f) * 0.4f;
            else if (pondo) coolerFora = 1f - AnimacaoPecas.Trecho(pt, 0f, 0.55f);
            else coolerFora = Mathf.MoveTowards(coolerFora, fora ? 1f : 0f, 1.6f * Time.deltaTime);
            float c = Mathf.SmoothStep(0f, 1f, coolerFora);
            Vector3 desloc = new Vector3(-0.1f, -0.2f, 0.02f) * c;
            coolerPc.localPosition = coolerPos + desloc;
            dissipadorPc.localPosition = dissipadorPos + desloc;
            // ligado, a ventoinha gira (mais rápido quando está esquentando)
            if (e != null && e.pcLigado) coolerPc.Rotate(0f, (e.PcSuperaquecendo ? 1400f : 500f) * Time.deltaTime, 0f, Space.Self);

            // Parafusos: presos nos cantos do dissipador; soltando, cada um gira e sobe na sua vez (ordem em X)
            for (int k = 0; k < 4; k++)
            {
                int i = OrdemX[k];
                float solto = fora ? 1f : 0f;
                if (tirando) solto = AnimacaoPecas.Trecho(pt, k * 0.2f, k * 0.2f + 0.18f);
                else if (pondo) solto = 1f - AnimacaoPecas.Trecho(pt, 0.55f + k * 0.11f, 0.55f + k * 0.11f + 0.1f);
                var pz = parafusosCooler[i];
                Vector3 baseP = dissipadorPos + desloc + new Vector3(-0.019f, CantosCooler[i].x, CantosCooler[i].y);
                pz.localPosition = baseP + new Vector3(-0.012f * solto, 0f, 0f);
                pz.localRotation = Quaternion.Euler(solto * 720f, 0f, 90f);
            }
        }
        if (pastaPc != null)
        {
            bool limpando = AnimacaoPecas.Segurando(eq, "gab_pasta_limpar", out float pl);
            bool aplicando = !limpando && AnimacaoPecas.Segurando(eq, "gab_pasta_aplicar", out pl);
            bool mostra = e != null && !(e.pcCoolerFora && e.pcPastaLimpa && !e.pcPastaNova);
            if (limpando || aplicando) mostra = true;
            if (pastaPc.gameObject.activeSelf != mostra) pastaPc.gameObject.SetActive(mostra);
            if (e != null)
            {
                bool nova = e.pcPastaNova || !e.pcPastaSeca || aplicando;
                Pintar(pastaPc.GetComponent<Renderer>(), nova ? mPastaNova : mPastaSeca);
                // pasta nova recém-aplicada: só um grão no centro
                float lado = e.pcCoolerFora && e.pcPastaNova ? 0.012f : 0.034f;
                if (limpando) lado = Mathf.Lerp(0.034f, 0.002f, pl);
                if (aplicando) lado = Mathf.Lerp(0.001f, 0.012f, pl);
                pastaPc.localScale = new Vector3(pastaPc.localScale.x, lado, lado);
            }
        }

        // ---------- HD » SSD: o disco sai, vira SSD e entra ----------
        if (hdPc != null)
        {
            float fora = 0f;
            bool ssd = e != null && e.pcTrocasHd > 0;
            if (AnimacaoPecas.Segurando(eq, "gab_ssd", out p))
            {
                fora = AnimacaoPecas.Trecho(p, 0.05f, 0.4f) * (1f - AnimacaoPecas.Trecho(p, 0.6f, 0.95f));
                ssd = p > 0.5f;
            }
            hdPc.localPosition = hdPos + new Vector3(-0.26f * fora, 0.02f * fora, 0f);
            hdPc.localScale = ssd ? new Vector3(0.07f, 0.008f, 0.1f) : hdEscala;   // SSD de 2,5"
            Pintar(hdPc.GetComponent<Renderer>(), ssd ? mSsd : hdMat);
        }

        // Superaquecimento de verdade: ligado e quente por tempo demais, desliga sozinho
        if (e != null && e.pcLigado && e.PcSuperaquecendo)
        {
            e.pcSegundosQuente += Time.deltaTime;
            if (e.pcSegundosQuente >= EstadoEquipamentos.SegundosAteDesligarPorCalor)
            {
                e.pcLigado = false;
                e.pcSegundosQuente = 0f;
                SomManager.TocarEm("erro", transform.position, 0.6f, 0.7f);
                GameUI.Instance?.Toast("O computador <b>desligou sozinho</b>: proteção contra superaquecimento.", UIFactory.Alerta, 4f);
            }
        }
        else if (e != null) e.pcSegundosQuente = 0f;

        Pintar(ledGabinete, e == null || e.pcLigado ? mVerde : mApagado);
    }
}
