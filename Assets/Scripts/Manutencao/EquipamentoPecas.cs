using UnityEngine;
using UnityEngine.SceneManagement;

// Monta as peças 3D (PecaEquipamento) de cada equipamento do cliente ao carregar a cena
// e mostra o estado no modelo: tampa aberta, cartucho, luzes do roteador, LED da impressora.
// Usa os nomes do "Visual" montado pelo MontarCenario (Corpo, Tampa, Painel, LED0..3);
// não precisa remontar o cenário.
public class EquipamentoPecas : MonoBehaviour
{
    Equipamento eq;
    Transform visual;

    // Impressora
    Transform dobradica, cartucho;
    Renderer painel, botaoLiga;
    float anguloTampa;

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
        if (eq.tipoEquipamento == TipoEquipamento.Impressora) MontarImpressora();
        else if (eq.tipoEquipamento == TipoEquipamento.Roteador) MontarRoteador();
        else MontarComputador();
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
            foreach (var id in eq.tipoEquipamento == TipoEquipamento.Impressora
                         ? new[] { "visor", "traseira", "tampa", "cartucho", "botao", "energia" }
                         : new[] { "luzes", "etiqueta", "fonte", "reset" })
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

        if (eq.tipoEquipamento == TipoEquipamento.Impressora)
        {
            bool aberta = e != null && e.impTampaAberta;
            anguloTampa = Mathf.MoveTowards(anguloTampa, aberta ? 75f : 0f, 300f * Time.deltaTime);
            if (dobradica != null) dobradica.localRotation = Quaternion.Euler(anguloTampa, 0f, 0f);
            if (cartucho != null) cartucho.gameObject.SetActive(anguloTampa > 20f && e != null && e.impCartuchoPresente);
            bool ligada = e == null || e.impLigada;
            Pintar(botaoLiga, ligada ? mVerde : mApagado);
            Pintar(painel, ligada ? (e != null && e.impCartuchoPresente && e.impToner > 0.1f ? mVerde : mLaranja) : mApagado);
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
        else
        {
            Pintar(botaoLiga, e == null || e.pcLigado ? mVerde : mApagado);
        }
    }

    static void Pintar(Renderer r, Material m)
    {
        if (r != null && r.sharedMaterial != m) r.sharedMaterial = m;
    }
}
