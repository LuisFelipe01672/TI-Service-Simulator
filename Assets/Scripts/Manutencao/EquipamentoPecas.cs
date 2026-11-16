using UnityEngine;
using UnityEngine.SceneManagement;

// Monta as peças 3D (PecaEquipamento) de cada equipamento do cliente ao carregar a cena
// e mostra o estado no modelo: tampa aberta, cartucho, luzes do roteador, LED da impressora.
// Usa os nomes do "Visual" montado pelo MontarCenario (Corpo, Tampa, Painel, LED0..3);
// não precisa remontar o cenário.
public partial class EquipamentoPecas : MonoBehaviour
{
    Equipamento eq;
    Transform visual;

    // Impressora
    Transform dobradica, cartucho;
    Renderer painel, botaoLiga;
    GameObject telaViva;   // computador: a tela do sistema (some com o PC desligado)
    float anguloTampa;
    float anguloAberta = 75f;   // a impressora realista levanta o scanner só 55°
    public static bool testeTampaAberta;   // atalho de teste: abre a tampa sem precisar de chamado

    // Roteador
    Renderer[] leds;
    Transform plugue;

    static Material mCartucho, mBotao, mPreto, mVerde, mApagado, mLaranja;

    // ---------- Criação automática ----------

    static bool registrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!registrado)
        {
            SceneManager.sceneLoaded += (_, __) => MontarTodos();
            registrado = true;
        }
        MontarTodos();
    }

    static void MontarTodos()
    {
        foreach (var e in FindObjectsByType<Equipamento>())
            if (e.GetComponent<EquipamentoPecas>() == null)
                e.gameObject.AddComponent<EquipamentoPecas>();
    }

    void Start()
    {
        eq = GetComponent<Equipamento>();
        visual = transform.Find("Visual");
        CriarMateriais();
        switch (eq.tipoEquipamento)
        {
            case TipoEquipamento.Impressora: MontarImpressora(); break;
            case TipoEquipamento.Roteador: MontarRoteador(); break;
            case TipoEquipamento.Computador: MontarComputador(); break;
            default: MontarGenerico(); break;   // equipamento sem visual próprio: peças da definição sobre o colisor
        }
    }

    static void CriarMateriais()
    {
        if (mCartucho != null) return;
        mCartucho = Mat(new Color(0.12f, 0.12f, 0.14f));
        mBotao = Mat(new Color(0.55f, 0.58f, 0.62f));
        mPreto = Mat(new Color(0.05f, 0.05f, 0.06f));
        mVerde = Mat(new Color(0.2f, 0.9f, 0.45f), new Color(0.2f, 0.9f, 0.45f) * 1.5f);
        mLaranja = Mat(new Color(1f, 0.6f, 0.15f), new Color(1f, 0.6f, 0.15f) * 1.5f);
        mApagado = Mat(new Color(0.1f, 0.12f, 0.1f));
    }

    static Material Mat(Color cor, Color? emissao = null)
    {
        var m = new Material(Shader.Find("Standard")) { color = cor };
        m.SetFloat("_Glossiness", 0.5f);
        if (emissao != null) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emissao.Value); }
        return m;
    }

    // O Visual da impressora e do roteador tem a frente em -z. Se ela estiver virada para a parede
    // (cenas montadas antes da correção no MontarCenario), gira o Visual para a frente ficar para a sala.
    void VirarParaASala()
    {
        var sala = GameObject.Find("Cenario/Cliente");
        if (visual == null || sala == null) return;
        Vector3 frente = visual.TransformDirection(Vector3.back);
        Vector3 paraSala = sala.transform.position - transform.position;
        paraSala.y = 0f;
        if (Vector3.Dot(frente, paraSala) < 0f) visual.localRotation *= Quaternion.Euler(0f, 180f, 0f);
    }

    // ---------- Impressora ----------

    void MontarImpressora()
    {
        if (visual == null) { MontarGenerico(); return; }
        VirarParaASala();
        if (visual.Find("Ancora_Dobradica") != null) { MontarImpressoraReal(); return; }

        // Tampa com dobradiça na borda de trás: abre para cima
        var tampa = visual.Find("Tampa");
        dobradica = new GameObject("Dobradica").transform;
        dobradica.SetParent(visual, false);
        dobradica.localPosition = new Vector3(0f, 0.32f, 0.21f);
        if (tampa != null) tampa.SetParent(dobradica, true);

        // Aparece saindo do corpo quando a tampa abre
        cartucho = Cubo(visual, "Cartucho", new Vector3(0f, 0.29f, 0.05f), new Vector3(0.32f, 0.08f, 0.12f), mCartucho).transform;
        botaoLiga = Cubo(visual, "BotaoLiga", new Vector3(-0.2f, 0.25f, -0.212f), new Vector3(0.035f, 0.035f, 0.01f), mVerde).GetComponent<Renderer>();
        Cubo(visual, "BotaoTeste", new Vector3(0.07f, 0.312f, -0.16f), new Vector3(0.04f, 0.012f, 0.04f), mBotao);
        var p = visual.Find("Painel");
        if (p != null) painel = p.GetComponent<Renderer>();

        PecaEquipamento.Criar(visual, eq, "visor", "Visor", new Vector3(0.18f, 0.315f, -0.14f), new Vector3(0.14f, 0.05f, 0.1f));
        PecaEquipamento.Criar(visual, eq, "botao", "Botão de teste", new Vector3(0.07f, 0.315f, -0.16f), new Vector3(0.07f, 0.05f, 0.07f));
        PecaEquipamento.Criar(visual, eq, "energia", "Botão liga/desliga", new Vector3(-0.2f, 0.25f, -0.225f), new Vector3(0.08f, 0.08f, 0.05f));
        // Fica encostada na parede: a peça de trás sobe acima da tampa para dar para mirar por cima
        PecaEquipamento.Criar(visual, eq, "traseira", "Parte de trás (cabo de rede)", new Vector3(0f, 0.24f, 0.25f), new Vector3(0.52f, 0.42f, 0.1f));
        PecaEquipamento.Criar(visual, eq, "cartucho", "Cartucho de toner", new Vector3(0f, 0.25f, 0.05f), new Vector3(0.34f, 0.1f, 0.2f));
        // A peça da tampa vai junto com a tampa (aberta, ela sai da frente do cartucho)
        PecaEquipamento.Criar(dobradica, eq, "tampa", "Tampa", new Vector3(0f, -0.01f, -0.16f), new Vector3(0.52f, 0.05f, 0.32f));
    }

    // Impressora realista (MontarCenario.VisualImpressoraReal): as peças seguem as âncoras do Visual
    void MontarImpressoraReal()
    {
        Transform A(string n) => visual.Find("Ancora_" + n);
        Vector3 P(string n) => A(n).localPosition;
        Vector3 S(string n) => A(n).localScale;

        // A parte de cima (scanner) gira na dobradiça da borda de trás
        var tampa = visual.Find("Tampa");
        dobradica = new GameObject("Dobradica").transform;
        dobradica.SetParent(visual, false);
        dobradica.localPosition = P("Dobradica");
        if (tampa != null) tampa.SetParent(dobradica, true);
        anguloAberta = 55f;

        cartucho = visual.Find("Cartucho");
        if (cartucho == null)
            cartucho = Cubo(visual, "Cartucho", P("Cartucho"), S("Cartucho"), mCartucho).transform;

        // Botão liga/desliga: LED pequeno na frente do corpo de baixo
        botaoLiga = Cubo(visual, "BotaoLiga", P("Energia") + new Vector3(0f, 0f, -0.003f), new Vector3(0.018f, 0.018f, 0.006f), mVerde).GetComponent<Renderer>();
        var p = visual.GetComponentsInChildren<Transform>(true);
        foreach (var t in p) if (t.name == "Painel") { painel = t.GetComponent<Renderer>(); break; }
        if (painel != null) CriarLcd(painel);

        PecaEquipamento.Criar(visual, eq, "visor", "Visor", P("Visor"), S("Visor"));
        PecaEquipamento.Criar(visual, eq, "botao", "Botão de teste", P("BotaoTeste"), S("BotaoTeste"));
        PecaEquipamento.Criar(visual, eq, "energia", "Botão liga/desliga", P("Energia"), S("Energia"));
        PecaEquipamento.Criar(visual, eq, "traseira", "Parte de trás (cabo de rede)", P("Traseira"), S("Traseira"));
        PecaEquipamento.Criar(visual, eq, "cartucho", "Cartucho de toner", P("Cartucho"), S("Cartucho"));
        if (A("Gaveta") != null) PecaEquipamento.Criar(visual, eq, "gaveta", "Gaveta de papel", P("Gaveta"), S("Gaveta"));
        MontarLocacao();   // antes do plugue e do cabo pendurado: a caixa mede só a impressora
        MontarDetalhesImpressora();
        // A peça da tampa acompanha a parte de cima quando ela levanta
        PecaEquipamento.Criar(dobradica, eq, "tampa", "Tampa (levantar o scanner)",
            P("Tampa") - P("Dobradica"), S("Tampa") + new Vector3(0.01f, 0.02f, 0.01f));
    }

    // ---------- Roteador ----------

    void MontarRoteador()
    {
        if (visual == null) { MontarGenerico(); return; }
        VirarParaASala();

        leds = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            var l = visual.Find("LED" + i);
            if (l != null) leds[i] = l.GetComponent<Renderer>();
        }
        // Roteador realista (MontarCenario.VisualRoteadorReal): plugue e peças pelas âncoras
        if (visual.Find("Ancora_Luzes") != null)
        {
            Transform A(string n) => visual.Find("Ancora_" + n);
            plugue = Cubo(visual, "Plugue", A("Plugue").localPosition, A("Plugue").localScale, mPreto).transform;
            Cubo(visual, "FuroReset", A("FuroReset").localPosition, A("FuroReset").localScale, mPreto);
            PecaEquipamento.Criar(visual, eq, "luzes", "Luzes", A("Luzes").localPosition, A("Luzes").localScale);
            PecaEquipamento.Criar(visual, eq, "etiqueta", "Roteador (etiqueta embaixo)", A("Corpo").localPosition, A("Corpo").localScale);
            PecaEquipamento.Criar(visual, eq, "fonte", "Fonte na tomada", A("Fonte").localPosition, A("Fonte").localScale);
            PecaEquipamento.Criar(visual, eq, "reset", "Botão de reset", A("Reset").localPosition, A("Reset").localScale);
            return;
        }
        plugue = Cubo(visual, "Plugue", new Vector3(0.1f, 0.035f, 0.12f), new Vector3(0.03f, 0.025f, 0.03f), mPreto).transform;
        Cubo(visual, "FuroReset", new Vector3(-0.1f, 0.035f, 0.1105f), new Vector3(0.008f, 0.008f, 0.002f), mPreto);

        PecaEquipamento.Criar(visual, eq, "luzes", "Luzes", new Vector3(0f, 0.045f, -0.125f), new Vector3(0.26f, 0.05f, 0.05f));
        PecaEquipamento.Criar(visual, eq, "etiqueta", "Roteador (etiqueta embaixo)", new Vector3(0f, 0.035f, 0f), new Vector3(0.34f, 0.08f, 0.17f));
        // Atrás, mais altas que o corpo: dá para mirar por cima mesmo com o roteador encostado na parede
        PecaEquipamento.Criar(visual, eq, "fonte", "Fonte na tomada", new Vector3(0.1f, 0.09f, 0.14f), new Vector3(0.1f, 0.14f, 0.06f));
        PecaEquipamento.Criar(visual, eq, "reset", "Botão de reset", new Vector3(-0.1f, 0.09f, 0.14f), new Vector3(0.1f, 0.14f, 0.06f));
    }

    // ---------- Computador ----------

    void MontarComputador()
    {
        // Computador realista (MontarCenario.VisualComputadorReal): peças pelas âncoras e tela viva
        if (visual != null && visual.Find("Ancora_Tela") != null)
        {
            Transform A(string n) => visual.Find("Ancora_" + n);
            PecaEquipamento.Criar(visual, eq, "tela", "Tela", A("Tela").localPosition, A("Tela").localScale);
            PecaEquipamento.Criar(visual, eq, "energia", "Botão liga/desliga", A("Energia").localPosition, A("Energia").localScale);
            PecaEquipamento.Criar(visual, eq, "traseira", "Parte de trás (cabo de rede)", A("Traseira").localPosition, A("Traseira").localScale);
            // gabinete: a torre ao lado do monitor (ou, em cenas antigas sem ela, a base do monitor)
            if (A("Gabinete") != null) PecaEquipamento.Criar(visual, eq, "gabinete", "Gabinete (por dentro)", A("Gabinete").localPosition, A("Gabinete").localScale);
            else PecaEquipamento.Criar(visual, eq, "gabinete", "Gabinete (por dentro)", A("Tela").localPosition + new Vector3(0f, -A("Tela").localScale.y * 0.75f, 0.05f), new Vector3(0.3f, 0.12f, 0.2f));
            MontarGabinete();
            botaoLiga = Cubo(visual, "LedLiga", A("Energia").localPosition + new Vector3(0f, 0.005f, -0.004f),
                new Vector3(0.008f, 0.008f, 0.004f), mVerde).GetComponent<Renderer>();
            foreach (var t in visual.GetComponentsInChildren<Transform>(true))
                if (t.name == "TelaViva") { telaViva = t.gameObject; break; }
            return;
        }
        MontarGenerico();
    }

    // Peças pelo colisor do equipamento: tela na frente, cabo atrás, botão embaixo à direita.
    // A frente é o lado (z+ ou z-) virado para o centro da sala do cliente.
    void MontarGenerico()
    {
        var bc = GetComponent<BoxCollider>();
        Vector3 c = bc != null ? bc.center : new Vector3(0f, 0.25f, 0f);
        Vector3 s = bc != null ? bc.size : new Vector3(0.5f, 0.5f, 0.3f);

        float frente = 1f;
        var sala = GameObject.Find("Cenario/Cliente");
        if (sala != null)
        {
            Vector3 paraSala = sala.transform.position - transform.position;
            paraSala.y = 0f;
            if (Vector3.Dot(transform.forward, paraSala) < 0f) frente = -1f;
        }
        float zFrente = c.z + frente * s.z / 2f;
        float zTras = c.z - frente * s.z / 2f;

        if (eq.tipoEquipamento == TipoEquipamento.Computador)
        {
            // O colisor inclui o pé do monitor, que avança além da tela: por isso não há LED visível aqui
            // (ficaria flutuando). A tela fica na parte de cima; o botão, logo abaixo dela, à direita.
            PecaEquipamento.Criar(transform, eq, "tela", "Tela", new Vector3(c.x, c.y + s.y * 0.2f, zFrente), new Vector3(s.x * 0.85f, s.y * 0.6f, 0.06f));
            PecaEquipamento.Criar(transform, eq, "energia", "Botão liga/desliga",
                new Vector3(c.x + s.x * 0.35f, c.y - s.y * 0.22f, c.z), new Vector3(0.16f, 0.12f, s.z));
            // Sobe 25 cm acima do monitor: mira-se por cima dele (a mesa fica encostada na parede)
            PecaEquipamento.Criar(transform, eq, "traseira", "Parte de trás (cabo de rede)",
                new Vector3(c.x, c.y + 0.125f, zTras), new Vector3(s.x, s.y + 0.25f, 0.1f));
        }
        else
        {
            // Sem o visual esperado: uma peça só, com todas as ações do equipamento numa lista
            foreach (var id in RegistroEquipamentos.De(eq.tipoEquipamento).Pecas)
                PecaEquipamento.Criar(transform, eq, id, eq.Nome, c + Vector3.up * 0.001f * id.Length, s * 1.02f);
        }
    }

    static GameObject Cubo(Transform pai, string nome, Vector3 pos, Vector3 tam, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = nome;
        Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localPosition = pos;
        c.transform.localScale = tam;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c;
    }

    // ---------- Estado no modelo ----------

    void Update()
    {
        var e = MissionManager.Instance != null ? MissionManager.Instance.Estado : null;
        AtualizarTestador(e);
        AtualizarFerramentas(e);

        if (eq.tipoEquipamento == TipoEquipamento.Impressora)
        {
            bool aberta = testeTampaAberta || (e != null && e.impTampaAberta);
            anguloTampa = Mathf.MoveTowards(anguloTampa, aberta ? anguloAberta : 0f, 220f * Time.deltaTime);
            if (dobradica != null) dobradica.localRotation = Quaternion.Euler(anguloTampa, 0f, 0f);
            if (cartucho != null) cartucho.gameObject.SetActive(anguloTampa > 20f && (e == null || e.impCartuchoPresente));
            bool ligada = e == null || e.impLigada;
            Pintar(botaoLiga, ligada ? mVerde : mApagado);
            Pintar(painel, ligada ? (e != null && e.impCartuchoPresente && e.impToner > 0.1f ? mVerde : mLaranja) : mApagado);
            AtualizarLcd(e, ligada);
            AtualizarDetalhesImpressora(e, ligada);
            AtualizarLocacao(e);
        }
        else if (eq.tipoEquipamento == TipoEquipamento.Roteador)
        {
            bool tomada = e == null || e.rotNaTomada;
            bool iniciando = e != null && e.rotLigaEm > Time.time;
            if (plugue != null) plugue.gameObject.SetActive(tomada);
            if (leds != null)
                for (int i = 0; i < leds.Length; i++)
                {
                    bool acesa = tomada && (!iniciando || Mathf.Repeat(Time.time * 4f + i, 2f) < 1f);
                    bool semInternet = e != null && i == 3 && e.rotTravado;
                    Pintar(leds[i], !acesa ? mApagado : semInternet ? mLaranja : mVerde);
                }
        }
        else if (eq.tipoEquipamento == TipoEquipamento.Computador)
        {
            bool pcLigado = e == null || e.pcLigado;
            Pintar(botaoLiga, pcLigado ? mVerde : mApagado);
            if (telaViva != null && telaViva.activeSelf != pcLigado) telaViva.SetActive(pcLigado);   // desligado: tela preta
            AtualizarGabinete(e);
            AtualizarTelaDiagnostico(e);
        }
    }

    static void Pintar(Renderer r, Material m)
    {
        if (r != null && r.sharedMaterial != m) r.sharedMaterial = m;
    }
}
