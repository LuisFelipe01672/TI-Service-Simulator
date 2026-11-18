using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Detalhes animados da impressora realista (Fase 3), todos acompanhando o estado do chamado:
// plugue RJ45 entrando/saindo da porta de trás, LED da porta, folha saindo com o resultado da impressão
// e o cartucho na mão do técnico (sai da impressora, balança ao chacoalhar, fita do lacre sai ao puxar).
public partial class EquipamentoPecas
{
    Transform plugueRede, ledRede, saida, cartuchoNaMao;
    Vector3 plugueConectado, plugueSolto;
    readonly List<GameObject> folhas = new List<GameObject>();
    int paginasVistas;
    EstadoEquipamentos estadoVisto;
    bool tinhaCartuchoNaMao, estavaChacoalhado, tinhaLacre;
    static Material mCaboAzul, mPlugue, mFolha;
    static readonly Dictionary<ResultadoPagina, Texture2D> texturasFolha = new Dictionary<ResultadoPagina, Texture2D>();

    void MontarDetalhesImpressora()
    {
        mCaboAzul ??= Mat(new Color(0.2f, 0.42f, 0.85f));
        mPlugue ??= Mat(new Color(0.85f, 0.88f, 0.9f));
        saida = visual.Find("Ancora_Saida");
        var porta = visual.Find("Ancora_PortaRede");
        if (porta == null) return;

        // Plugue RJ45 (corpo transparente-acinzentado) com o cabo azul descendo para a tomada
        plugueRede = new GameObject("PlugueRJ45").transform;
        plugueRede.SetParent(visual, false);
        Cubo(plugueRede, "Conector", Vector3.zero, new Vector3(0.014f, 0.012f, 0.024f), mPlugue);
        Cubo(plugueRede, "Cabo", new Vector3(0f, -0.004f, 0.06f), new Vector3(0.006f, 0.006f, 0.1f), mCaboAzul);
        Cubo(plugueRede, "CaboDescendo", new Vector3(0f, -0.1f, 0.11f), new Vector3(0.006f, 0.2f, 0.006f), mCaboAzul);
        plugueConectado = porta.localPosition + new Vector3(0f, 0f, 0.008f);
        plugueSolto = porta.localPosition + new Vector3(0.02f, -0.05f, 0.09f);
        plugueRede.localPosition = plugueConectado;

        ledRede = Cubo(visual, "LedRede", porta.localPosition + new Vector3(-0.016f, 0.01f, 0.001f), new Vector3(0.004f, 0.004f, 0.002f), mApagado).transform;
    }

    void AtualizarDetalhesImpressora(EstadoEquipamentos e, bool ligada)
    {
        // Chamado novo: recomeça (sem folhas antigas, sem cartucho na mão)
        if (e != estadoVisto)
        {
            estadoVisto = e;
            paginasVistas = e != null ? e.paginasImpressas : 0;
            foreach (var f in folhas) if (f != null) Destroy(f);
            folhas.Clear();
            if (cartuchoNaMao != null) Destroy(cartuchoNaMao.gameObject);
            tinhaCartuchoNaMao = estavaChacoalhado = tinhaLacre = false;
        }

        // Plugue: encaixado ou pendurado para fora
        if (plugueRede != null)
        {
            bool conectado = e == null || e.impCabo.conectado;
            Vector3 alvo = conectado ? plugueConectado : plugueSolto;
            plugueRede.localPosition = Vector3.MoveTowards(plugueRede.localPosition, alvo, 0.25f * Time.deltaTime);
            plugueRede.localRotation = Quaternion.RotateTowards(plugueRede.localRotation,
                conectado ? Quaternion.identity : Quaternion.Euler(35f, 0f, 10f), 240f * Time.deltaTime);
        }
        // LED da porta de rede: verde piscando com link, apagado sem
        if (ledRede != null)
        {
            bool link = ligada && (e == null || e.impCabo.Funciona);
            Pintar(ledRede.GetComponent<Renderer>(), link && Mathf.Repeat(Time.time * 3f, 1f) < 0.6f ? mVerde : mApagado);
        }

        if (e == null) return;

        // Folha nova saindo pela frente
        if (e.paginasImpressas > paginasVistas)
        {
            paginasVistas = e.paginasImpressas;
            SoltarFolha(e.ultimaPagina);
        }

        AtualizarCartuchoNaMao(e);
    }

    // ---------- Folha impressa ----------

    void SoltarFolha(ResultadoPagina r)
    {
        if (saida == null) return;
        mFolha ??= new Material(Shader.Find("Standard"));
        var folha = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(folha.GetComponent<Collider>());
        folha.name = "Folha_" + r;
        folha.transform.SetParent(visual, false);
        // deitada (A4 = 21 x 29,7 cm), com o topo para fora da impressora
        folha.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        folha.transform.localScale = new Vector3(0.21f, 0.297f, 1f);
        Vector3 dentro = saida.localPosition + new Vector3(0f, folhas.Count * 0.0015f, 0.16f);
        Vector3 fora = saida.localPosition + new Vector3(0f, folhas.Count * 0.0015f - 0.004f, -0.07f);
        folha.transform.localPosition = dentro;
        var rend = folha.GetComponent<Renderer>();
        rend.material = new Material(mFolha) { mainTexture = TexturaFolha(r) };
        rend.material.SetFloat("_Glossiness", 0.05f);
        Tween.LocalPosition(folha.transform, fora, 1.4f, Ease.OutQuad);
        SomManager.TocarEm("impressora", transform.position, 0.8f);
        folhas.Add(folha);
        if (folhas.Count > 4) { Destroy(folhas[0]); folhas.RemoveAt(0); }
    }

    // A pista está na folha: perfeita, em branco, clarinha com falhas ou com listra vertical (cilindro)
    public static Texture2D TexturaFolha(ResultadoPagina r)
    {
        if (texturasFolha.TryGetValue(r, out var t) && t != null) return t;
        const int w = 128, h = 181;
        t = new Texture2D(w, h, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0.97f, 0.97f, 0.95f);
        var rnd = new System.Random(42);
        if (r == ResultadoPagina.Lixo)
        {
            // driver errado: símbolos sem sentido espalhados pela folha inteira, sem margem nem linhas
            for (int y = 2; y < h - 6; y += 5)
                for (int x = 2; x < w - 4; x += 4)
                {
                    if (rnd.Next(100) < 30) continue;
                    int lw = rnd.Next(1, 4), lh = rnd.Next(1, 5);
                    for (int yy = y; yy < y + lh; yy++) for (int xx = x; xx < x + lw; xx++) px[yy * w + xx] = new Color(0.12f, 0.12f, 0.12f);
                }
        }
        else if (r == ResultadoPagina.Relatorio)
        {
            // relatório de configuração: título e duas colunas (nome do campo e valor)
            for (int y = h - 20; y < h - 13; y++) for (int x = 24; x < 104; x++) px[y * w + x] = new Color(0.15f, 0.15f, 0.15f);
            for (int linha = 0; linha < 16; linha++)
            {
                int y0 = h - 36 - linha * 8;
                int a = 14 + rnd.Next(24, 40), b = 64 + rnd.Next(20, 46);
                for (int y = y0; y < y0 + 2; y++)
                {
                    for (int x = 14; x < a; x++) px[y * w + x] = new Color(0.15f, 0.15f, 0.15f);
                    for (int x = 64; x < b; x++) px[y * w + x] = new Color(0.3f, 0.3f, 0.3f);
                }
            }
        }
        else if (r != ResultadoPagina.Branca)
        {
            float tinta = r == ResultadoPagina.Clara ? 0.72f : 0.15f;
            // título e linhas de texto
            for (int y = h - 22; y < h - 14; y++) for (int x = 14; x < 80; x++) px[y * w + x] = new Color(tinta, tinta, tinta);
            for (int linha = 0; linha < 22; linha++)
            {
                int y0 = h - 34 - linha * 6;
                int fim = 14 + rnd.Next(60, 100);
                for (int y = y0; y < y0 + 2; y++)
                    for (int x = 14; x < fim; x++)
                    {
                        float c = tinta;
                        if (r == ResultadoPagina.Clara && (x / 9) % 3 == 0) c = 0.95f;   // faixas brancas (toner no fim)
                        px[y * w + x] = new Color(c, c, c);
                    }
            }
            // Listra preta vertical sempre no mesmo lugar (cilindro riscado)
            if (r == ResultadoPagina.Listra)
                for (int y = 0; y < h; y++) for (int x = 61; x < 64; x++) px[y * w + x] = new Color(0.08f, 0.08f, 0.08f);
        }
        t.SetPixels(px);
        t.Apply();
        texturasFolha[r] = t;
        return t;
    }

    // ---------- Cartucho na mão do técnico ----------

    void AtualizarCartuchoNaMao(EstadoEquipamentos e)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 naMao = new Vector3(0.16f, -0.2f, 0.45f);

        if (e.cartuchoNaMao && !tinhaCartuchoNaMao)
        {
            // sai da impressora (ou da mochila) e vem para a frente da câmera
            var modelo = cartucho != null ? cartucho : null;
            var go = modelo != null ? Instantiate(modelo.gameObject) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CartuchoNaMao";
            go.SetActive(true);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            cartuchoNaMao = go.transform;
            Vector3 escala = modelo != null ? modelo.lossyScale : new Vector3(0.3f, 0.08f, 0.1f);
            Vector3 de = modelo != null && e.cartuchoNaMaoUsado ? modelo.position : cam.transform.position + cam.transform.forward * 0.3f - cam.transform.up * 0.5f;
            cartuchoNaMao.SetParent(cam.transform, true);
            cartuchoNaMao.position = de;
            cartuchoNaMao.localScale = new Vector3(escala.x / cam.transform.lossyScale.x, escala.y / cam.transform.lossyScale.y, escala.z / cam.transform.lossyScale.z);
            Sequence.Create()
                .Group(Tween.LocalPosition(cartuchoNaMao, naMao, 0.6f, Ease.OutCubic))
                .Group(Tween.LocalRotation(cartuchoNaMao, Quaternion.Euler(15f, -20f, 5f), 0.6f, Ease.OutCubic));
            estavaChacoalhado = e.cartuchoNaMaoChacoalhado;
            tinhaLacre = e.cartuchoNaMaoComLacre;
            SomManager.TocarEm("mexer", transform.position, 0.6f, 1.1f);
        }
        else if (!e.cartuchoNaMao && tinhaCartuchoNaMao && cartuchoNaMao != null)
        {
            // volta para dentro da impressora (ou para a mochila) e some
            var t = cartuchoNaMao;
            cartuchoNaMao = null;
            Vector3 destino = cartucho != null && e.impCartuchoPresente ? cartucho.position : t.position - Camera.main.transform.up * 0.6f;
            t.SetParent(null, true);
            Tween.Position(t, destino, 0.45f, Ease.InCubic).OnComplete(t, x => Destroy(x.gameObject));
            SomManager.TocarEm("mexer", transform.position, 0.6f, 0.9f);
        }
        tinhaCartuchoNaMao = e.cartuchoNaMao;
        if (cartuchoNaMao == null) return;

        // Chacoalhar: balança de lado a lado
        if (e.cartuchoNaMaoChacoalhado && !estavaChacoalhado)
            Tween.ShakeLocalPosition(cartuchoNaMao, new Vector3(0.06f, 0.01f, 0f), 1.3f, 7f);
        estavaChacoalhado = e.cartuchoNaMaoChacoalhado;

        // Puxar o lacre: uma fita laranja sai do lado do cartucho e é jogada fora
        if (tinhaLacre && !e.cartuchoNaMaoComLacre)
        {
            var fita = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(fita.GetComponent<Collider>());
            fita.GetComponent<Renderer>().material = Mat(new Color(1f, 0.55f, 0.1f));
            fita.transform.SetParent(cam.transform, false);
            fita.transform.localPosition = naMao + new Vector3(0.12f, 0f, 0f);
            fita.transform.localScale = new Vector3(0.01f, 0.002f, 0.02f);
            Sequence.Create()
                .Group(Tween.LocalPosition(fita.transform, naMao + new Vector3(0.45f, 0.05f, 0.05f), 1.6f, Ease.OutQuad))
                .Group(Tween.Scale(fita.transform, new Vector3(0.7f, 0.002f, 0.02f), 1.6f, Ease.OutQuad))
                .ChainCallback(() => { if (fita != null) Destroy(fita); });
        }
        tinhaLacre = e.cartuchoNaMaoComLacre;
    }

    // ---------- Etapa 10C: papel atolado e cilindro, andando com a barra do E (AnimacaoPecas) ----------
    Transform impFolhaAtolada, impCilindro;
    static Material mCilindroVelho, mCilindroNovo;

    void AnimarPecasImpressora()
    {
        var traseira = visual != null ? visual.Find("Peca_traseira") : null;
        var cart = visual != null ? (visual.Find("Peca_cartucho") ?? (dobradica != null ? dobradica.Find("Peca_cartucho") : null)) : null;

        // Folha amassada sai pela tampa de trás, devagar
        bool puxando = AnimacaoPecas.Segurando(eq, "imp_papel", out float p);
        if (puxando && impFolhaAtolada == null && traseira != null)
        {
            mFolha ??= Mat(new Color(0.96f, 0.96f, 0.94f));
            impFolhaAtolada = Cubo(visual, "FolhaAtolada", Vector3.zero, new Vector3(0.2f, 0.004f, 0.18f), mFolha).transform;
        }
        if (impFolhaAtolada != null)
        {
            impFolhaAtolada.gameObject.SetActive(puxando);
            if (puxando)
            {
                Vector3 o = traseira.localPosition + new Vector3(0f, -0.08f, -0.12f);
                impFolhaAtolada.localPosition = o + new Vector3(0f, 0.02f * p, 0.26f * AnimacaoPecas.Trecho(p, 0.15f, 1f));
                impFolhaAtolada.localRotation = Quaternion.Euler(12f + 8f * Mathf.Sin(p * 25f), 6f * Mathf.Sin(p * 13f), 4f);
            }
        }

        // Cilindro (unidade de imagem): o velho sobe, o novo (verde mais vivo) desce no lugar
        bool trocando = AnimacaoPecas.Segurando(eq, "imp_cilindro", out p);
        if (trocando && impCilindro == null && cart != null)
        {
            mCilindroVelho ??= Mat(new Color(0.12f, 0.3f, 0.16f));
            mCilindroNovo ??= Mat(new Color(0.1f, 0.55f, 0.25f));
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "CilindroNaMao";
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(cart.parent, false);
            c.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            c.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
            impCilindro = c.transform;
        }
        if (impCilindro != null)
        {
            impCilindro.gameObject.SetActive(trocando);
            if (trocando)
            {
                float fora = AnimacaoPecas.Trecho(p, 0f, 0.45f) * (1f - AnimacaoPecas.Trecho(p, 0.55f, 1f));
                impCilindro.localPosition = cart.localPosition + new Vector3(0f, 0.02f + 0.22f * fora, -0.05f * fora);
                Pintar(impCilindro.GetComponent<Renderer>(), p > 0.5f ? mCilindroNovo : mCilindroVelho);
            }
        }
    }
}
