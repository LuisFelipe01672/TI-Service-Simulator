using UnityEngine;

// Etapa 7: notebook do cliente feito em primitivas (base, teclado, touchpad, tampa com a tela TI OS, carregador).
// Frente (lado do teclado) em -z; a tampa gira na dobradiça da borda de trás.
// O estado aparece no modelo: tela tombando (dobradiça), base empenada balançando (bateria estufada), notebook virado
// de barriga para cima com a tampa de baixo fora (bateria e ventoinha à mostra), capinha da tecla fora, luz do carregador.
public partial class EquipamentoPecas
{
    Transform nbVisual, nbPivoTampa, nbPlacaTeclado, nbTouchpad, nbTeclaA, nbTampaFundo, nbBateria, nbPoeiraVent, nbMoldura, nbMolduraSolta,
              nbDobradicaEsq, nbFita;
    Renderer nbLedLiga, nbLedCarregador;
    float nbAnguloTampa = 15f, nbVirado;
    float nbTopo = 0.019f;          // altura do tampo do teclado (o modelo 3D é mais fino que as primitivas)
    bool nbModelo;                  // usando o modelo do Sketchfab (ModelosUsados.notebook)
    int nbLimpezasVistas, nbTrocasVistas;
    static Material mNbCarcaca, mNbTeclado, mNbBateria, mNbPoeira, mNbMetal, mNbFita;

    void MontarNotebook()
    {
        mNbCarcaca = mNbCarcaca != null ? mNbCarcaca : Mat(new Color(0.32f, 0.33f, 0.36f));
        mNbTeclado = mNbTeclado != null ? mNbTeclado : Mat(new Color(0.08f, 0.08f, 0.09f));
        mNbBateria = mNbBateria != null ? mNbBateria : Mat(new Color(0.15f, 0.17f, 0.2f));
        mNbPoeira = mNbPoeira != null ? mNbPoeira : Mat(new Color(0.55f, 0.53f, 0.5f));
        mNbMetal = mNbMetal != null ? mNbMetal : Mat(new Color(0.7f, 0.7f, 0.72f));
        mNbFita = mNbFita != null ? mNbFita : Mat(new Color(0.02f, 0.02f, 0.02f));

        // Tudo que vira junto com o notebook fica no Visual (o carregador fica de fora, na mesa)
        nbVisual = new GameObject("Visual").transform;
        nbVisual.SetParent(transform, false);
        visual = nbVisual;
        Cubo(nbVisual, "Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.34f, 0.018f, 0.235f), mNbCarcaca);
        nbPlacaTeclado = Cubo(nbVisual, "Teclado", new Vector3(0f, 0.0195f, 0.025f), new Vector3(0.29f, 0.002f, 0.105f), mNbTeclado).transform;
        // Fileiras de teclas (só relevo) e a tecla A destacada
        for (int f = 0; f < 4; f++)
            Cubo(nbPlacaTeclado, "Fileira" + f, new Vector3(0f, 1.2f, -0.35f + f * 0.23f), new Vector3(0.96f, 1f, 0.17f), mBotao);
        nbTeclaA = Cubo(nbVisual, "TeclaA", new Vector3(-0.105f, 0.0215f, 0.003f), new Vector3(0.016f, 0.003f, 0.016f), mBotao).transform;
        nbTouchpad = Cubo(nbVisual, "Touchpad", new Vector3(0f, 0.0192f, -0.075f), new Vector3(0.1f, 0.0015f, 0.06f), mPreto).transform;
        nbLedLiga = Cubo(nbVisual, "LedLiga", new Vector3(0.14f, 0.0196f, 0.1f), new Vector3(0.008f, 0.002f, 0.008f), mVerde).GetComponent<Renderer>();
        // Saída de ar na lateral esquerda e a poeira que aparece pela grade
        Cubo(nbVisual, "Grade", new Vector3(-0.1705f, 0.01f, 0.05f), new Vector3(0.002f, 0.01f, 0.08f), mPreto);
        nbPoeiraVent = Cubo(nbVisual, "PoeiraGrade", new Vector3(-0.166f, 0.01f, 0.05f), new Vector3(0.006f, 0.008f, 0.075f), mNbPoeira).transform;

        // Por baixo (aparece com o notebook virado e a tampa fora): bateria, ventoinha e a tampa de baixo
        nbBateria = Cubo(nbVisual, "Bateria", new Vector3(0f, -0.003f, -0.05f), new Vector3(0.24f, 0.006f, 0.07f), mNbBateria).transform;
        var vent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vent.name = "Ventoinha";
        Destroy(vent.GetComponent<Collider>());
        vent.transform.SetParent(nbVisual, false);
        vent.transform.localPosition = new Vector3(-0.11f, -0.003f, 0.06f);
        vent.transform.localScale = new Vector3(0.06f, 0.003f, 0.06f);
        vent.GetComponent<Renderer>().sharedMaterial = mPreto;
        Cubo(vent.transform, "PoeiraVentoinha", new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 0.6f, 0.9f), mNbPoeira);
        nbTampaFundo = Cubo(nbVisual, "TampaFundo", new Vector3(0f, -0.0005f, 0f), new Vector3(0.336f, 0.008f, 0.232f), mNbCarcaca).transform;

        // Tampa com a tela: gira na borda de trás
        nbPivoTampa = new GameObject("DobradicaNb").transform;
        nbPivoTampa.SetParent(nbVisual, false);
        nbPivoTampa.localPosition = new Vector3(0f, 0.019f, 0.113f);
        Cubo(nbPivoTampa, "Tampa", new Vector3(0f, 0.112f, 0.002f), new Vector3(0.34f, 0.224f, 0.007f), mNbCarcaca);
        nbMoldura = Cubo(nbPivoTampa, "Moldura", new Vector3(0f, 0.112f, -0.0025f), new Vector3(0.336f, 0.22f, 0.002f), mPreto).transform;
        nbMolduraSolta = Cubo(transform, "MolduraSolta", new Vector3(0.05f, 0.002f, -0.24f), new Vector3(0.336f, 0.002f, 0.22f), mPreto).transform;
        nbMolduraSolta.gameObject.SetActive(false);
        nbDobradicaEsq = Cubo(nbPivoTampa, "DobradicaEsq", new Vector3(-0.13f, 0.004f, 0f), new Vector3(0.03f, 0.012f, 0.012f), mNbMetal).transform;
        Cubo(nbPivoTampa, "DobradicaDir", new Vector3(0.13f, 0.004f, 0f), new Vector3(0.03f, 0.012f, 0.012f), mNbMetal);
        var tela = new GameObject("TelaViva");
        tela.transform.SetParent(nbPivoTampa, false);
        tela.transform.localPosition = new Vector3(0f, 0.118f, -0.0037f);
        tela.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // a tela olha para a frente (-z)
        var tc = tela.AddComponent<TelaComputador>();
        tc.tamanho = new Vector2(0.3f, 0.185f);
        tc.usuario = "Cliente";
        telaViva = tela;

        // Carregador na mesa, à direita, com o cabo até a lateral do notebook
        Cubo(transform, "Carregador", new Vector3(0.3f, 0.015f, 0.04f), new Vector3(0.11f, 0.03f, 0.05f), mNbTeclado);
        nbLedCarregador = Cubo(transform, "LedCarregador", new Vector3(0.3f, 0.0305f, 0.025f), new Vector3(0.006f, 0.002f, 0.006f), mVerde).GetComponent<Renderer>();
        Cubo(transform, "CaboCarregador", new Vector3(0.21f, 0.004f, 0.04f), new Vector3(0.07f, 0.005f, 0.005f), mNbTeclado);
        nbFita = Cubo(transform, "FitaIsolante", new Vector3(0.188f, 0.004f, 0.04f), new Vector3(0.02f, 0.009f, 0.009f), mNbFita).transform;
        Cubo(transform, "CaboTomada", new Vector3(0.36f, 0.004f, 0.16f), new Vector3(0.005f, 0.005f, 0.24f), mNbTeclado);

        // Peças (mirar + E)
        PecaEquipamento.Criar(nbPivoTampa, eq, "tela", "Tela", new Vector3(0f, 0.115f, -0.006f), new Vector3(0.33f, 0.2f, 0.02f));
        PecaEquipamento.Criar(nbPivoTampa, eq, "dobradica", "Dobradiça da tela", new Vector3(0f, 0.012f, 0f), new Vector3(0.36f, 0.035f, 0.04f));
        PecaEquipamento.Criar(nbVisual, eq, "teclado", "Teclado e touchpad", new Vector3(-0.02f, 0.022f, -0.02f), new Vector3(0.26f, 0.012f, 0.16f));
        PecaEquipamento.Criar(nbVisual, eq, "energia", "Botão liga/desliga", new Vector3(0.14f, 0.022f, 0.09f), new Vector3(0.05f, 0.012f, 0.035f));
        PecaEquipamento.Criar(nbVisual, eq, "fundo", "Base (tampa de baixo e bateria)", new Vector3(0f, 0.01f, -0.108f), new Vector3(0.35f, 0.03f, 0.03f));
        PecaEquipamento.Criar(nbVisual, eq, "saida_ar", "Saída de ar", new Vector3(-0.175f, 0.01f, 0.05f), new Vector3(0.03f, 0.03f, 0.1f));
        PecaEquipamento.Criar(transform, eq, "carregador", "Carregador", new Vector3(0.3f, 0.02f, 0.04f), new Vector3(0.13f, 0.05f, 0.08f));
        TrocarPeloModelo();
    }

    // Modelo "Laptop" (Aullwen, CC-BY; ver CREDITOS.md): a base no lugar da base de primitivas e a tela dentro da
    // dobradiça que gira. As primitivas das pistas (touchpad estufado, buraco da tecla, bateria e ventoinha por baixo)
    // ficam por cima. Sem o modelo, fica tudo em primitivas.
    void TrocarPeloModelo()
    {
        var banco = ModelosUsados.Carregar();
        var prefab = banco != null ? banco.notebook : null;
        if (prefab == null) return;
        var inst = Instantiate(prefab, nbVisual, false);
        inst.name = "Modelo";
        Transform baseNb = null, tela = null;
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("Frame")) baseNb = r.transform;
            else if (r.name.Contains("Screen")) tela = r.transform;
        }
        if (baseNb == null || tela == null) { Destroy(inst); Debug.LogWarning("[Notebook] Modelo sem Frame/Screen: fica em primitivas."); return; }

        // Caixa da base no espaço do Visual
        Bounds B()
        {
            var b = baseNb.GetComponent<Renderer>().bounds;
            var c = nbVisual.InverseTransformPoint(b.center);
            var t = nbVisual.InverseTransformVector(b.size);
            return new Bounds(c, new Vector3(Mathf.Abs(t.x), Mathf.Abs(t.y), Mathf.Abs(t.z)));
        }
        // Largura no x, 34 cm como as primitivas; base no tampo, centrada
        var b0 = B();
        if (b0.size.z > b0.size.x) inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * inst.transform.localRotation;
        b0 = B();
        inst.transform.localScale *= 0.34f / b0.size.x;
        b0 = B();
        inst.transform.localPosition -= new Vector3(b0.center.x, b0.min.y, b0.center.z);
        // O pé da tela (dobradiça) fica atrás (+z); se ficou na frente, gira meia volta
        if (nbVisual.InverseTransformPoint(tela.position).z < 0f)
        {
            inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * inst.transform.localRotation;
            inst.transform.localPosition = Quaternion.Euler(0f, 180f, 0f) * inst.transform.localPosition;
        }
        b0 = B();
        nbTopo = b0.max.y;

        // Tela na dobradiça: o pé da tela vira o pivô e a inclinação do modelo é desfeita (a tampa gira por código)
        nbPivoTampa.localPosition = nbVisual.InverseTransformPoint(tela.position);
        tela.SetParent(nbPivoTampa, true);
        Vector3 cima = nbVisual.InverseTransformDirection(tela.up);
        float inclinacao = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(cima, Vector3.right), Vector3.right);
        tela.RotateAround(nbPivoTampa.position, nbVisual.right, -inclinacao);

        // Some com as primitivas que o modelo substitui
        foreach (var n in new[] { "Base", "Teclado", "TeclaA" }) { var x = nbVisual.Find(n); if (x != null) x.gameObject.SetActive(false); }
        foreach (var n in new[] { "Tampa", "Moldura", "DobradicaEsq", "DobradicaDir" }) { var x = nbPivoTampa.Find(n); if (x != null) x.gameObject.SetActive(false); }
        nbModelo = true;
        // Buraco da tecla A (aparece sem a capinha) e o LED rentes ao tampo do modelo
        nbTeclaA = Cubo(nbVisual, "BuracoTeclaA", new Vector3(-0.1f, nbTopo + 0.0004f, 0.015f), new Vector3(0.016f, 0.001f, 0.016f), mPreto).transform;
        nbLedLiga.transform.localPosition = new Vector3(0.14f, nbTopo + 0.0004f, 0.1f);
        // A tela viva vai para a frente da tela do modelo
        var tb = tela.GetComponent<Renderer>().bounds;
        float frente = nbPivoTampa.InverseTransformPoint(tb.center).z - Mathf.Abs(nbPivoTampa.InverseTransformVector(tb.size).z) / 2f;
        float alto = Mathf.Abs(nbPivoTampa.InverseTransformVector(tb.size).y);
        telaViva.transform.localPosition = new Vector3(0.003f, alto * 0.53f - 0.004f, frente - 0.0007f);
        telaViva.GetComponent<TelaComputador>().tamanho = new Vector2(0.322f, 0.194f);   // cobre a área branca da tela do modelo
        Debug.Log($"[Notebook] Modelo do Sketchfab ligado: tampo a {nbTopo * 100f:0.0} cm, tela de {alto * 100f:0} cm, inclinada {inclinacao:0} graus no original.");
    }

    void AtualizarNotebook(EstadoEquipamentos e)
    {
        if (nbVisual == null) return;
        bool nbCena = e != null && MissionManager.Instance != null && MissionManager.Instance.chamadoAtivo != null &&
                      MissionManager.Instance.chamadoAtivo.equipamento == TipoEquipamento.Notebook;
        if (!nbCena) e = null;   // o notebook só "vive" no chamado dele; fora disso fica normal, ligado

        bool ligado = e == null || e.nbLigado;
        bool virado = e != null && e.nbTampaFundoAberta;

        // Tampa: aberta 15° para trás; tombando com a dobradiça quebrada; fechada para virar o notebook
        float alvo = virado ? -90f : e != null && e.nbDobradicaQuebrada ? 52f + Mathf.Sin(Time.time * 0.7f) * 3f : 15f;
        nbAnguloTampa = Mathf.MoveTowards(nbAnguloTampa, alvo, 120f * Time.deltaTime);
        nbPivoTampa.localRotation = Quaternion.Euler(nbAnguloTampa, 0f, 0f);
        if (nbDobradicaEsq != null) nbDobradicaEsq.localRotation = Quaternion.Euler(e != null && e.nbDobradicaQuebrada ? 25f : 0f, 0f, 0f);

        // Virar de barriga para cima (e desvirar) com a tampa de baixo
        nbVirado = Mathf.MoveTowards(nbVirado, virado && nbAnguloTampa < -80f ? 180f : 0f, 240f * Time.deltaTime);
        nbVisual.localPosition = new Vector3(0f, nbVirado > 1f ? nbTopo + 0.008f : 0f, 0f);
        float balanco = e != null && e.nbBateriaEstufada && !virado ? Mathf.Sin(Time.time * 2.2f) * 1.2f : 0f;
        nbVisual.localRotation = Quaternion.Euler(balanco, 0f, nbVirado);
        nbTampaFundo.gameObject.SetActive(!(virado && nbVirado > 170f));

        // Bateria estufada: teclado e touchpad levantados; a bateria (por baixo) abaulada
        bool estufada = e != null && e.nbBateriaEstufada;
        nbTouchpad.localPosition = new Vector3(0f, nbTopo + (estufada ? 0.0035f : 0.0002f), -0.075f);
        nbTouchpad.gameObject.SetActive(!nbModelo || estufada);   // no modelo, o touchpad de primitiva só aparece quando levanta
        nbTouchpad.localRotation = Quaternion.Euler(estufada ? -4f : 0f, 0f, 0f);
        if (!nbModelo) nbPlacaTeclado.localPosition = new Vector3(0f, estufada ? 0.0215f : 0.0195f, 0.025f);
        nbBateria.gameObject.SetActive(e == null || !e.nbBateriaFora);
        nbBateria.localScale = new Vector3(0.24f, estufada ? 0.016f : 0.006f, 0.07f);

        // Tecla A sem a capinha; poeira na grade e na ventoinha
        bool semCapa = e != null && e.nbTeclaSemCapa;
        nbTeclaA.gameObject.SetActive(nbModelo ? semCapa : !semCapa);   // modelo: aparece o buraco; primitivas: some a tecla
        bool poeira = e != null && e.nbPoeira > 0.7f;
        nbPoeiraVent.gameObject.SetActive(poeira);
        var pv = nbVisual.Find("Ventoinha/PoeiraVentoinha");
        if (pv != null) pv.gameObject.SetActive(e != null && e.nbPoeira > 0.3f);

        // Moldura solta em cima da mesa
        bool moldura = e != null && e.nbMolduraAberta;
        if (!nbModelo) nbMoldura.gameObject.SetActive(!moldura);
        nbMolduraSolta.gameObject.SetActive(moldura);

        // Carregador: fita isolante no cabo ruim, luz verde só carregando
        nbFita.gameObject.SetActive(e != null && e.nbCarregadorRuim);
        Pintar(nbLedCarregador, e == null || (e.nbNaTomada && !e.nbCarregadorRuim) ? mVerde : mApagado);
        Pintar(nbLedLiga, ligado ? mVerde : mApagado);
        if (telaViva != null && telaViva.activeSelf != (ligado && !virado)) telaViva.SetActive(ligado && !virado);

        if (e == null) return;
        // Sons das trocas e limpezas
        if (e.nbLimpezas != nbLimpezasVistas) { nbLimpezasVistas = e.nbLimpezas; SomManager.TocarEm("mexer", transform.position, 0.7f, 1.3f); }
        int trocas = e.nbTrocasBateria + e.nbTrocasDobradica;
        if (trocas != nbTrocasVistas) { nbTrocasVistas = trocas; SomManager.TocarEm("clique", transform.position, 0.7f, 0.8f); }

        // A bateria carrega na tomada e descarrega fora dela
        if (e.nbLigado && !e.nbBateriaFora)
            e.nbCarga = Mathf.Clamp01(e.nbCarga + (e.NbCarregando ? 0.004f : -0.002f) * Time.deltaTime);
        // Superaquecendo: desliga sozinho depois de um tempo ligado
        if (e.nbLigado && e.NbSuperaquecendo)
        {
            e.nbSegundosQuente += Time.deltaTime;
            if (e.nbSegundosQuente > EstadoEquipamentos.SegundosNotebookDesligarPorCalor)
            {
                e.nbSegundosQuente = 0f;
                e.nbLigado = false;
                SomManager.TocarEm("erro", transform.position, 0.4f, 0.8f);
                GameUI.Instance?.Toast("O notebook <b>desligou sozinho</b> de tão quente.", UIFactory.Alerta, 3f);
            }
        }
    }
}
