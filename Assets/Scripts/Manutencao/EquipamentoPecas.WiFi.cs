using UnityEngine;

// Etapa 10: visual do projeto de Wi-Fi, feito em código. O objeto do AP fica no centro do piso da sala
// (AccessPointCliente); os filhos são: a caixa do kit no chão ao lado do rack, o forro (peça para mirar), a marca de
// lápis, o suporte, o AP redondo de teto com a luz (laranja piscando iniciando, verde no ar), a sobra de cabo e a
// canaleta do rack até o AP (subindo pela parede e correndo rente ao teto, em L).
public partial class EquipamentoPecas
{
    Transform apCaixa, apMarca, apSuporteT, apCorpo, apSobra, apCanaleta;
    PecaEquipamento apPecaForro, apPecaCaixa;
    Renderer apLed;
    int apVersaoVista = -1, apSalaVista = -1;
    EstadoEquipamentos apEstadoVisto;
    static Material mApBranco, mApPapelao, mApCaboAzul;

    void MontarAccessPoint()
    {
        CriarMateriaisRack();
        if (mApBranco == null)
        {
            mApBranco = Mat(new Color(0.95f, 0.95f, 0.94f));
            mApBranco.SetFloat("_Glossiness", 0.45f);
            mApPapelao = Mat(new Color(0.62f, 0.47f, 0.3f));
            mApPapelao.SetFloat("_Glossiness", 0.1f);
            mApCaboAzul = Mat(new Color(0.15f, 0.35f, 0.8f));
        }
        var v = new GameObject("Visual").transform;
        v.SetParent(transform, false);
        visual = v;

        // Caixa do kit (fica no chão ao lado do rack; reposicionada a cada sala)
        apCaixa = new GameObject("CaixaKit").transform;
        apCaixa.SetParent(v, false);
        Cubo(apCaixa, "Caixa", new Vector3(0f, 0.06f, 0f), new Vector3(0.34f, 0.12f, 0.3f), mApPapelao);
        Cubo(apCaixa, "Etiqueta", new Vector3(0f, 0.121f, 0f), new Vector3(0.14f, 0.002f, 0.08f), mRkEtiqueta);
        apPecaCaixa = PecaEquipamento.Criar(apCaixa, eq, "caixa", "Kit do access point", new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.2f, 0.38f));

        // Forro: peça fina logo abaixo do teto (mirar + E escolhe o ponto)
        apPecaForro = PecaEquipamento.Criar(v, eq, "forro", "Forro (teto)", new Vector3(0f, 2.74f, 0f),
            new Vector3(AccessPointCliente.LarguraSala - 0.4f, 0.08f, AccessPointCliente.ProfundidadeSala - 0.4f));

        apMarca = Cilindro(v, "MarcaLapis", new Vector3(0.05f, 0.002f, 0.05f), mRkLapis);
        apSuporteT = Cilindro(v, "Suporte", new Vector3(0.16f, 0.006f, 0.16f), mApBranco);
        apCorpo = new GameObject("AP").transform;
        apCorpo.SetParent(v, false);
        Cilindro(apCorpo, "Corpo", new Vector3(0.22f, 0.018f, 0.22f), mApBranco).localPosition = new Vector3(0f, -0.02f, 0f);
        Cilindro(apCorpo, "Borda", new Vector3(0.23f, 0.004f, 0.23f), mRkMetal).localPosition = new Vector3(0f, -0.004f, 0f);
        apLed = Cilindro(apCorpo, "Led", new Vector3(0.02f, 0.002f, 0.02f), mApagado).GetComponent<Renderer>();
        apLed.transform.localPosition = new Vector3(0f, -0.039f, 0f);
        apSobra = Cilindro(v, "SobraCabo", new Vector3(0.007f, 0.15f, 0.007f), mApCaboAzul);
        apCanaleta = new GameObject("Canaleta").transform;
        apCanaleta.SetParent(v, false);
        apVersaoVista = -1;
    }

    static Transform Cilindro(Transform pai, string nome, Vector3 escala, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        c.name = nome;
        Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localScale = escala;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c.transform;
    }

    void AtualizarAccessPoint(EstadoEquipamentos e)
    {
        bool servico = e != null && e.wifiServico;
        if (visual == null) return;
        if (visual.gameObject.activeSelf != servico) visual.gameObject.SetActive(servico);
        if (!servico) { apEstadoVisto = null; return; }

        if (e != apEstadoVisto || e.rackVersao != apVersaoVista || AccessPointCliente.Versao != apSalaVista)
        {
            apEstadoVisto = e;
            apVersaoVista = e.rackVersao;
            apSalaVista = AccessPointCliente.Versao;
            RefazerAccessPoint(e);
        }

        // Luz do AP: laranja piscando iniciando, verde no ar
        if (e.apInstalado)
            Pintar(apLed, e.ApLigado ? mVerde : e.ApIniciando && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? mLaranja : mApagado);
    }

    void RefazerAccessPoint(EstadoEquipamentos e)
    {
        float h = AccessPointCliente.AlturaForro;
        apPecaForro.transform.localPosition = new Vector3(0f, h - 0.06f, 0f);
        apCaixa.position = AccessPointCliente.PosicaoCaixa();
        var rack = RackCliente.Transform;
        apCaixa.rotation = rack != null ? rack.rotation : transform.rotation;
        apCaixa.gameObject.SetActive(!e.apInstalado);

        Vector3 local = transform.InverseTransformPoint(e.apPos);
        local.y = h;
        apMarca.localPosition = local - new Vector3(0f, 0.002f, 0f);
        apSuporteT.localPosition = local - new Vector3(0f, 0.006f, 0f);
        apCorpo.localPosition = local;
        apSobra.localPosition = local - new Vector3(0f, 0.16f, 0f);
        apMarca.gameObject.SetActive(e.apMarcado && !e.apSuporte);
        apSuporteT.gameObject.SetActive(e.apSuporte);
        apCorpo.gameObject.SetActive(e.apInstalado);
        apSobra.gameObject.SetActive(e.apCabo && !e.apInstalado);

        for (int i = apCanaleta.childCount - 1; i >= 0; i--) Destroy(apCanaleta.GetChild(i).gameObject);
        if (e.apCabo && rack != null)
        {
            // No referencial do rack: sobe pela parede ao lado dele, corre rente ao teto ao longo da parede e entra na sala
            Vector3 ap = rack.InverseTransformPoint(e.apPos);
            float y = h - 0.02f, x0 = RackCliente.Largura / 2f - 0.06f, z0 = -0.02f;
            Vector3 a = new Vector3(x0, RackCliente.Base + RackCliente.Altura, z0);
            Vector3 b = new Vector3(x0, y, z0);
            Vector3 c = new Vector3(ap.x, y, z0);
            Vector3 d = new Vector3(ap.x, y, ap.z + 0.09f * Mathf.Sign(-ap.z + 0.0001f));
            SegmentoCanaleta(rack, a, b);
            if (Mathf.Abs(c.x - b.x) > 0.05f) SegmentoCanaleta(rack, b, c);
            if (Mathf.Abs(d.z - c.z) > 0.05f) SegmentoCanaleta(rack, c, d);
        }
    }

    void SegmentoCanaleta(Transform referencial, Vector3 aLocal, Vector3 bLocal)
    {
        Vector3 a = referencial.TransformPoint(aLocal), b = referencial.TransformPoint(bLocal);
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TrechoCanaleta";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(apCanaleta, false);
        go.transform.position = (a + b) / 2f;
        Vector3 cima = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.9f ? referencial.forward : Vector3.up;
        go.transform.rotation = Quaternion.LookRotation(dir, cima);
        go.transform.localScale = new Vector3(0.03f, 0.02f, len + 0.03f);
        go.GetComponent<Renderer>().sharedMaterial = mRkCanaleta;
    }
}
