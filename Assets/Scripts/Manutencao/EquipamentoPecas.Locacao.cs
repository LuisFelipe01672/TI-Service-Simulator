using PrimeTween;
using UnityEngine;

// Etapa 5 — Locação física, no modelo da impressora:
// a impressora chega lacrada numa caixa de papelão (a impressora some e a caixa fica no móvel); ao abrir,
// ela sobe de dentro da caixa e a caixa desmonta. Fitas laranja de transporte na tampa e na gaveta.
// Na visita: a caixa do toner reserva aparece ao lado e a unidade de imagem nova entra na impressora.
public partial class EquipamentoPecas
{
    Transform caixaLocacao, fitas, tonerReserva;
    Vector3 visualPosOriginal;
    Vector3 tamCaixa;
    EstadoEquipamentos estadoLocacao;
    int caixasVistas, reservasVistas, cilindrosVistos;
    bool naCaixaVisto;
    static Material mFita, mUnidadeImagem;

    void MontarLocacao()
    {
        if (visual == null) return;
        visualPosOriginal = visual.localPosition;
        // Tamanho da impressora no espaço do Visual (para a caixa caber nela com folga)
        var rends = visual.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;
        Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
        foreach (var r in rends)
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var canto = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var p = visual.InverseTransformPoint(canto);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
        }
        Vector3 tam = max - min;
        tamCaixa = new Vector3(tam.x + 0.08f, tam.y + 0.07f, tam.z + 0.08f);
        Vector3 baseCentro = new Vector3((min.x + max.x) / 2f, min.y, (min.z + max.z) / 2f);
        baseCaixaLocal = baseCentro;

        caixaLocacao = new GameObject("CaixaLocacao").transform;
        caixaLocacao.SetParent(transform, false);
        caixaLocacao.SetPositionAndRotation(visual.TransformPoint(baseCentro), visual.rotation);
        EncomendaFisica.CriarCaixa(caixaLocacao, tamCaixa);
        var bc = caixaLocacao.GetComponent<BoxCollider>();
        if (bc != null) Destroy(bc);   // fica no móvel: não precisa travar o jogador
        EncomendaFisica.Etiqueta(caixaLocacao, tamCaixa, "PATRIMÔNIO\nImpressora multifuncional\nLOCAÇÃO", false);
        PecaEquipamento.Criar(caixaLocacao, eq, "caixa", "Caixa da impressora", Vector3.up * tamCaixa.y / 2f, tamCaixa * 1.02f);
        caixaLocacao.gameObject.SetActive(false);
    }

    void AtualizarLocacao(EstadoEquipamentos e)
    {
        if (caixaLocacao == null || visual == null) return;

        // Chamado novo: recomeça do jeito que o estado manda (sem animação)
        if (e != estadoLocacao)
        {
            estadoLocacao = e;
            caixasVistas = e != null ? e.caixasAbertas : 0;
            reservasVistas = e != null ? e.reservasDeixadas : 0;
            cilindrosVistos = e != null ? e.trocasCilindro : 0;
            bool naCaixa = e != null && e.impNaCaixa;
            Tween.StopAll(caixaLocacao);
            Tween.StopAll(visual);
            caixaLocacao.gameObject.SetActive(naCaixa);
            caixaLocacao.localScale = Vector3.one;
            visual.gameObject.SetActive(!naCaixa);
            visual.localPosition = visualPosOriginal;
            naCaixaVisto = naCaixa;
            if (tonerReserva != null) { Destroy(tonerReserva.gameObject); tonerReserva = null; }
        }

        if (e == null) { MostrarFitas(false); return; }

        // Abriu a caixa: a impressora sobe de dentro e a caixa desmonta para o chão
        if (naCaixaVisto && !e.impNaCaixa)
        {
            naCaixaVisto = false;
            AbrirCaixaAnimado();
        }
        else if (!naCaixaVisto && e.impNaCaixa)
        {
            naCaixaVisto = true;
            caixaLocacao.gameObject.SetActive(true);
            visual.gameObject.SetActive(false);
        }

        // Etapa 9C: a caixa só aparece na sala depois que o técnico traz do escritório
        if (naCaixaVisto && caixaLocacao.gameObject.activeSelf == e.impNoEscritorio) caixaLocacao.gameObject.SetActive(!e.impNoEscritorio);
        MostrarFitas(e.impComFitas && !e.impNaCaixa);

        if (e.reservasDeixadas > reservasVistas) { reservasVistas = e.reservasDeixadas; DeixarTonerReserva(); }
        if (e.trocasCilindro > cilindrosVistos) { cilindrosVistos = e.trocasCilindro; TrocarUnidadeImagemAnimado(); }
    }

    void AbrirCaixaAnimado()
    {
        SomManager.TocarEm("mexer", transform.position, 0.9f, 0.8f);
        visual.gameObject.SetActive(true);
        visual.localPosition = visualPosOriginal + Vector3.down * (tamCaixa.y * 0.85f);
        var caixa = caixaLocacao;
        Vector3 lado = caixa.right * (tamCaixa.x * 0.9f);
        Sequence.Create()
            .Chain(Tween.LocalPosition(visual, visualPosOriginal + Vector3.up * 0.06f, 1.1f, Ease.OutCubic))
            .Chain(Tween.LocalPosition(visual, visualPosOriginal, 0.25f, Ease.InQuad))
            .Group(Tween.Position(caixa, caixa.position + lado + Vector3.down * 0.1f, 0.5f, Ease.InCubic))
            .Group(Tween.Scale(caixa, new Vector3(1f, 0.05f, 1f), 0.5f, Ease.InCubic))
            .ChainCallback(() =>
            {
                if (caixaLocacao == null) return;
                caixaLocacao.gameObject.SetActive(false);
                caixaLocacao.localScale = Vector3.one;
                RecolocarCaixa();
            });
    }

    // Volta a caixa para o lugar da impressora (para a próxima instalação)
    void RecolocarCaixa()
    {
        if (caixaLocacao == null || visual == null) return;
        caixaLocacao.SetPositionAndRotation(visual.TransformPoint(baseCaixaLocal), visual.rotation);
    }

    Vector3 baseCaixaLocal;

    // Fitas laranja de transporte: duas na tampa (por cima e descendo pela frente) e uma na gaveta
    void MostrarFitas(bool mostrar)
    {
        if (!mostrar)
        {
            if (fitas == null) return;
            var f = fitas;
            fitas = null;
            var cam = Camera.main;
            Vector3 destino = cam != null ? cam.transform.position - cam.transform.up * 0.6f : f.position + Vector3.down;
            Sequence.Create()
                .Group(Tween.Position(f, destino, 0.6f, Ease.InCubic))
                .Group(Tween.Scale(f, Vector3.one * 0.2f, 0.6f, Ease.InCubic))
                .ChainCallback(() => { if (f != null) Destroy(f.gameObject); });
            return;
        }
        if (fitas != null) return;
        mFita ??= Mat(new Color(1f, 0.5f, 0.08f));
        fitas = new GameObject("FitasTransporte").transform;
        fitas.SetParent(visual, false);
        var tampa = visual.Find("Ancora_Tampa");
        var gaveta = visual.Find("Ancora_Gaveta");
        if (tampa != null)
        {
            Vector3 p = tampa.localPosition, s = tampa.localScale;
            foreach (float lado in new[] { -0.3f, 0.3f })
            {
                Cubo(fitas, "FitaTopo", p + new Vector3(s.x * lado, s.y / 2f + 0.002f, -s.z * 0.2f), new Vector3(0.035f, 0.002f, s.z * 0.6f), mFita);
                Cubo(fitas, "FitaFrente", p + new Vector3(s.x * lado, -0.02f, -s.z / 2f - 0.003f), new Vector3(0.035f, s.y + 0.06f, 0.002f), mFita);
            }
        }
        if (gaveta != null)
        {
            Vector3 p = gaveta.localPosition, s = gaveta.localScale;
            Cubo(fitas, "FitaGaveta", p + new Vector3(0f, 0f, -s.z / 2f - 0.003f), new Vector3(s.x * 0.6f, 0.03f, 0.002f), mFita);
        }
    }

    // A caixa do toner reserva vem da mão do técnico e fica ao lado da impressora
    void DeixarTonerReserva()
    {
        if (tonerReserva != null) Destroy(tonerReserva.gameObject);
        tonerReserva = new GameObject("TonerReserva").transform;
        tonerReserva.SetParent(transform, false);
        Vector3 tam = new Vector3(0.36f, 0.11f, 0.12f);
        EncomendaFisica.CriarCaixa(tonerReserva, tam);
        var bc = tonerReserva.GetComponent<BoxCollider>();
        if (bc != null) Destroy(bc);
        EncomendaFisica.Etiqueta(tonerReserva, tam, "TONER\nreserva", false);
        Vector3 destino = visual.TransformPoint(baseCaixaLocal) - visual.right * (tamCaixa.x / 2f + 0.22f);   // à esquerda de quem olha a frente
        var cam = Camera.main;
        tonerReserva.rotation = visual.rotation;
        tonerReserva.position = cam != null ? cam.transform.position + cam.transform.forward * 0.4f - cam.transform.up * 0.3f : destino + Vector3.up * 0.3f;
        Tween.Position(tonerReserva, destino, 0.7f, Ease.OutCubic);
        SomManager.TocarEm("mexer", transform.position, 0.6f, 1.2f);
    }

    // Unidade de imagem: a velha sai da impressora para a mão, a nova vem da mão e entra
    void TrocarUnidadeImagemAnimado()
    {
        var cam = Camera.main;
        if (cam == null) return;
        mUnidadeImagem ??= Mat(new Color(0.12f, 0.45f, 0.25f));
        Vector3 dentro = cartucho != null ? cartucho.position : visual.position + Vector3.up * 0.2f;
        Vector3 mao = cam.transform.position + cam.transform.forward * 0.45f - cam.transform.up * 0.25f;
        GameObject Unidade(string nome, Vector3 de)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.name = nome;
            go.GetComponent<Renderer>().sharedMaterial = mUnidadeImagem;
            go.transform.position = de;
            go.transform.rotation = visual.rotation * Quaternion.Euler(0f, 0f, 90f);
            go.transform.localScale = new Vector3(0.05f, 0.16f, 0.05f);
            return go;
        }
        var velha = Unidade("UnidadeImagemVelha", dentro);
        Tween.Position(velha.transform, mao - cam.transform.up * 0.4f, 0.8f, Ease.InOutCubic).OnComplete(() =>
        {
            Destroy(velha);
            var nova = Unidade("UnidadeImagemNova", mao);
            Tween.Position(nova.transform, dentro, 0.8f, Ease.InOutCubic).OnComplete(() => Destroy(nova));
        });
        SomManager.TocarEm("mexer", transform.position, 0.7f, 1f);
    }
}
