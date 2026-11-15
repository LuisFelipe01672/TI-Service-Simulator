using PrimeTween;
using UnityEngine;

// Gabinete do computador (Fase 4), acompanhando o estado do chamado: o técnico vira o gabinete na mesa e
// deita a tampa lateral ao lado; por dentro, a poeira aparece conforme o estado e some ao limpar; o pente
// de memória e a fonte saem e voltam quando são trocados; o LED da frente acende com o PC ligado.
public partial class EquipamentoPecas
{
    Transform gabinete, tampaLateral, poeiraGab, pente1, fontePc, coolerPc, dissipadorPc, hdPc, pastaPc;
    Vector3 coolerPos, dissipadorPos, hdPos;
    float coolerFora;
    int hdVistas;
    static Material mPastaSeca, mPastaNova, mSsd;
    Renderer ledGabinete;
    Vector3 gabPos, tampaPos, pentePos, fontePos;
    float aberturaGab;
    int memVistas, fonteVistas, limpezasVistas;
    EstadoEquipamentos estadoPcVisto;
    bool poeiraSumindo;

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
        if (pente1 != null) pentePos = pente1.localPosition;
        if (fontePc != null) fontePos = fontePc.localPosition;
        coolerPc = gabinete.Find("Cooler");
        dissipadorPc = gabinete.Find("Dissipador");
        hdPc = gabinete.Find("HD");
        pastaPc = gabinete.Find("Pasta");
        if (coolerPc != null) coolerPos = coolerPc.localPosition;
        if (dissipadorPc != null) dissipadorPos = dissipadorPc.localPosition;
        if (hdPc != null) hdPos = hdPc.localPosition;
        mPastaSeca ??= Mat(new Color(0.5f, 0.47f, 0.4f));
        mPastaNova ??= Mat(new Color(0.93f, 0.93f, 0.95f));
        mSsd ??= Mat(new Color(0.1f, 0.1f, 0.12f));
    }

    void AtualizarGabinete(EstadoEquipamentos e)
    {
        if (gabinete == null) return;

        // Chamado novo: zera os contadores de animação
        if (e != estadoPcVisto)
        {
            estadoPcVisto = e;
            memVistas = e != null ? e.pcTrocasMemoria : 0;
            fonteVistas = e != null ? e.pcTrocasFonte : 0;
            limpezasVistas = e != null ? e.pcLimpezas : 0;
            hdVistas = e != null ? e.pcTrocasHd : 0;
            poeiraSumindo = false;
            if (poeiraGab != null) poeiraGab.localScale = Vector3.one;
        }

        // Abrir: o gabinete vem um pouco para a frente e gira para a lateral ficar de frente para o técnico;
        // a tampa sai e fica deitada na mesa, ao lado
        bool aberto = e != null && e.pcGabineteAberto;
        aberturaGab = Mathf.MoveTowards(aberturaGab, aberto ? 1f : 0f, 1.3f * Time.deltaTime);
        float t = Mathf.SmoothStep(0f, 1f, aberturaGab);
        gabinete.localPosition = gabPos + new Vector3(0.1f, 0f, -0.14f) * t;
        gabinete.localRotation = Quaternion.Euler(0f, -90f * t, 0f);
        if (tampaLateral != null)
        {
            float tt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, aberturaGab));
            // a tampa fica de pé, encostada do outro lado do gabinete (não cai da mesa)
            tampaLateral.localPosition = Vector3.Lerp(tampaPos, new Vector3(0.135f, tampaPos.y, 0f), tt) + new Vector3(0f, 0f, -0.5f * Mathf.Sin(tt * Mathf.PI) * 0.6f);
            tampaLateral.localRotation = Quaternion.Euler(0f, 0f, -7f * tt);
        }

        // Poeira: visível quando há acúmulo; ao limpar, encolhe e some
        if (poeiraGab != null)
        {
            bool temPoeira = e != null ? e.pcPoeira > 0.3f : false;
            if (e != null && e.pcLimpezas > limpezasVistas)
            {
                limpezasVistas = e.pcLimpezas;
                poeiraSumindo = true;
                Tween.Scale(poeiraGab, new Vector3(1f, 0.02f, 1f), 1.2f, Ease.InQuad)
                    .OnComplete(this, x => { x.poeiraSumindo = false; if (x.poeiraGab != null) x.poeiraGab.localScale = Vector3.one; });
                SomManager.TocarEm("mexer", transform.position, 0.7f, 1.3f);
            }
            if (!poeiraSumindo && poeiraGab.gameObject.activeSelf != temPoeira) poeiraGab.gameObject.SetActive(temPoeira);
        }

        // Troca/limpeza do pente de memória: ele sai das travas e volta
        if (e != null && e.pcTrocasMemoria > memVistas && pente1 != null)
        {
            memVistas = e.pcTrocasMemoria;
            Tween.StopAll(pente1);
            pente1.localPosition = pentePos;
            Sequence.Create()
                .Chain(Tween.LocalPosition(pente1, pentePos + new Vector3(-0.16f, 0.03f, 0f), 0.5f, Ease.OutCubic))
                .ChainDelay(0.5f)
                .Chain(Tween.LocalPosition(pente1, pentePos, 0.45f, Ease.InCubic));
            SomManager.TocarEm("clique", transform.position, 0.7f, 0.8f);
        }
        // Troca da fonte: sai pela lateral e a nova entra
        if (e != null && e.pcTrocasFonte > fonteVistas && fontePc != null)
        {
            fonteVistas = e.pcTrocasFonte;
            Tween.StopAll(fontePc);
            fontePc.localPosition = fontePos;
            Sequence.Create()
                .Chain(Tween.LocalPosition(fontePc, fontePos + new Vector3(-0.28f, 0f, 0f), 0.7f, Ease.OutCubic))
                .ChainDelay(0.6f)
                .Chain(Tween.LocalPosition(fontePc, fontePos, 0.6f, Ease.InCubic));
            SomManager.TocarEm("mexer", transform.position, 0.8f, 0.7f);
        }

        // Cooler do processador: sai para o lado (fica apoiado na base do gabinete) e mostra a pasta térmica
        if (coolerPc != null && dissipadorPc != null)
        {
            coolerFora = Mathf.MoveTowards(coolerFora, e != null && e.pcCoolerFora ? 1f : 0f, 1.6f * Time.deltaTime);
            float c = Mathf.SmoothStep(0f, 1f, coolerFora);
            Vector3 desloc = new Vector3(-0.1f, -0.2f, 0.02f) * c;
            coolerPc.localPosition = coolerPos + desloc;
            dissipadorPc.localPosition = dissipadorPos + desloc;
            // ligado, a ventoinha gira (mais rápido quando está esquentando)
            if (e != null && e.pcLigado) coolerPc.Rotate(0f, (e.PcSuperaquecendo ? 1400f : 500f) * Time.deltaTime, 0f, Space.Self);
        }
        if (pastaPc != null)
        {
            bool mostra = e != null && !(e.pcCoolerFora && e.pcPastaLimpa && !e.pcPastaNova);
            if (pastaPc.gameObject.activeSelf != mostra) pastaPc.gameObject.SetActive(mostra);
            if (e != null)
            {
                bool nova = e.pcPastaNova || !e.pcPastaSeca;
                Pintar(pastaPc.GetComponent<Renderer>(), nova ? mPastaNova : mPastaSeca);
                // pasta nova recém-aplicada: só um grão no centro
                float lado = e.pcCoolerFora && e.pcPastaNova ? 0.012f : 0.034f;
                pastaPc.localScale = new Vector3(pastaPc.localScale.x, lado, lado);
            }
        }

        // Troca do HD por SSD: o disco velho sai e entra um SSD (menor e preto)
        if (e != null && e.pcTrocasHd > hdVistas && hdPc != null)
        {
            hdVistas = e.pcTrocasHd;
            Tween.StopAll(hdPc);
            hdPc.localPosition = hdPos;
            var hd = hdPc;
            Sequence.Create()
                .Chain(Tween.LocalPosition(hdPc, hdPos + new Vector3(-0.26f, 0.02f, 0f), 0.6f, Ease.OutCubic))
                .ChainCallback(() =>
                {
                    if (hd == null) return;
                    hd.localScale = new Vector3(0.07f, 0.008f, 0.1f);   // SSD de 2,5"
                    hd.GetComponent<Renderer>().sharedMaterial = mSsd;
                })
                .ChainDelay(0.4f)
                .Chain(Tween.LocalPosition(hdPc, hdPos, 0.55f, Ease.InCubic));
            SomManager.TocarEm("mexer", transform.position, 0.8f, 0.9f);
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
