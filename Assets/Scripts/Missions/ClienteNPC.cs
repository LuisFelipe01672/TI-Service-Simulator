using System.Collections.Generic;
using UnityEngine;

// O cliente em pessoa, esperando o técnico no escritório dele.
// É criado quando um chamado é aceito e some sozinho quando o chamado acaba.
// Por enquanto é montado com formas simples (cápsulas e esferas); dá para trocar por um modelo depois.
public class ClienteNPC : Interactable
{
    public Chamado chamado;
    static ClienteNPC atual;

    Transform corpo;
    Transform jogadorT;
    readonly List<Material> materiais = new List<Material>();
    float fase;

    bool assinando;

    public override string TextoPrompt =>
        assinando ? null :
        chamado != null && chamado.aguardandoAssinatura ? $"[E] Mostrar a OS para {chamado.contato} conferir e assinar" :
        chamado != null ? $"[E] Conversar com {chamado.contato}" : "[E] Conversar";

    public override void Interagir()
    {
        if (assinando || GameUI.Instance == null) return;
        if (chamado != null && chamado.aguardandoAssinatura) GameUI.Instance.AbrirOS(this);
        else GameUI.Instance.AbrirConversa(this);
    }

    // ---------- Assinatura da OS (prancheta física) ----------

    // O técnico entrega a prancheta, o cliente assina com a caneta (o traço aparece no papel) e devolve.
    public void Assinar(System.Action aoTerminar)
    {
        if (assinando) return;
        StartCoroutine(RotinaAssinar(aoTerminar));
    }

    System.Collections.IEnumerator RotinaAssinar(System.Action aoTerminar)
    {
        assinando = true;
        var cam = Camera.main;
        Tocar(BancoPersonagens.ConversandoEmPe);

        // Prancheta: base de madeira, folha da OS, prendedor de metal e a caneta
        var prancheta = new GameObject("Prancheta_OS").transform;
        Peca(PrimitiveType.Cube, prancheta, Vector3.zero, new Vector3(0.23f, 0.012f, 0.32f), Mat(new Color(0.55f, 0.38f, 0.22f), 0.25f));
        Peca(PrimitiveType.Cube, prancheta, new Vector3(0f, 0.0075f, -0.01f), new Vector3(0.2f, 0.002f, 0.27f), Mat(new Color(0.95f, 0.95f, 0.93f), 0.1f));
        Peca(PrimitiveType.Cube, prancheta, new Vector3(0f, 0.014f, 0.145f), new Vector3(0.09f, 0.014f, 0.03f), Mat(new Color(0.7f, 0.72f, 0.75f), 0.8f));
        // Linhas impressas da OS e a linha da assinatura
        var mTinta = Mat(new Color(0.35f, 0.38f, 0.42f), 0.1f);
        for (int i = 0; i < 6; i++)
            Peca(PrimitiveType.Cube, prancheta, new Vector3(-0.015f, 0.0088f, 0.1f - i * 0.025f), new Vector3(i % 2 == 0 ? 0.15f : 0.12f, 0.0005f, 0.004f), mTinta);
        Peca(PrimitiveType.Cube, prancheta, new Vector3(0f, 0.0088f, -0.09f), new Vector3(0.12f, 0.0005f, 0.002f), mTinta);
        var caneta = Peca(PrimitiveType.Cylinder, prancheta, new Vector3(0.06f, 0.06f, -0.08f), new Vector3(0.012f, 0.06f, 0.012f), Mat(new Color(0.1f, 0.2f, 0.6f), 0.5f));
        caneta.localRotation = Quaternion.Euler(25f, 0f, -20f);

        // O traço da assinatura (no espaço da prancheta)
        var assinatura = new GameObject("Assinatura").transform;   // escala 1: os pontos ficam em metros
        assinatura.SetParent(prancheta, false);
        var traco = assinatura.gameObject.AddComponent<LineRenderer>();
        traco.useWorldSpace = false;
        traco.widthMultiplier = 0.0022f;
        traco.numCapVertices = 2;
        var mTraco = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.08f, 0.12f, 0.45f) };
        materiais.Add(mTraco);
        traco.sharedMaterial = mTraco;
        traco.positionCount = 0;

        // Posições: na mão do técnico (frente da câmera) e na frente do peito do cliente, inclinada para ele ler
        Vector3 maoTecnico = cam != null ? cam.transform.position + cam.transform.forward * 0.45f - cam.transform.up * 0.25f : transform.position + transform.forward;
        Vector3 maoCliente = transform.position + Vector3.up * 1.05f + transform.forward * 0.32f;
        Quaternion rotCliente = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(-35f, 180f, 0f);
        Quaternion rotTecnico = cam != null ? Quaternion.LookRotation(-cam.transform.forward, Vector3.up) * Quaternion.Euler(-50f, 180f, 0f) : rotCliente;

        // 1) Entrega a prancheta
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            prancheta.position = Vector3.Lerp(maoTecnico, maoCliente, k);
            prancheta.rotation = Quaternion.Slerp(rotTecnico, rotCliente, k);
            yield return null;
        }
        prancheta.position = maoCliente;
        prancheta.rotation = rotCliente;
        yield return new WaitForSeconds(0.6f);   // lê a OS

        // 2) Assina: a ponta da caneta desenha a rubrica sobre a linha
        SomManager.TocarEm("caneta", prancheta.position, 0.8f);
        var pontos = new List<Vector3>();
        float dur = 1.4f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
        {
            float x = Mathf.Lerp(-0.055f, 0.055f, t);
            float z = -0.082f + Mathf.Sin(t * Mathf.PI * 7f) * 0.009f * (1f - t * 0.4f) + Mathf.Sin(t * Mathf.PI * 2f) * 0.004f;
            var ponta = new Vector3(x, 0.0095f, z);
            pontos.Add(ponta);
            traco.positionCount = pontos.Count;
            traco.SetPositions(pontos.ToArray());
            caneta.localPosition = ponta + new Vector3(0.012f, 0.055f, 0.02f);
            yield return null;
        }
        yield return new WaitForSeconds(0.35f);

        // 3) Devolve a prancheta assinada e o técnico guarda
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            prancheta.position = Vector3.Lerp(maoCliente, maoTecnico, k);
            prancheta.rotation = Quaternion.Slerp(rotCliente, rotTecnico, k);
            prancheta.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((t - 0.6f) / 0.4f));
            yield return null;
        }
        Destroy(prancheta.gameObject);
        assinando = false;
        aoTerminar?.Invoke();
    }

    // ---------- Criação ----------

    public static ClienteNPC Criar(Chamado ch)
    {
        var mm = MissionManager.Instance;
        if (ch == null || mm == null) return null;
        var sala = SalaCliente.Para(ch.nomeCliente);
        Transform chegada = sala != null && sala.chegada != null ? sala.chegada
            : mm.locaisClientes != null && mm.locaisClientes.Length > 0 ? mm.locaisClientes[0] : null;
        if (chegada == null) return null;

        // Remove o cliente que tenha sobrado de outro chamado
        if (atual != null) Destroy(atual.gameObject);

        // Espera na recepção da sala (ou um pouco à frente e à direita de onde o jogador chega)
        Vector3 pos = sala != null && sala.recepcao != null ? sala.recepcao.position
            : chegada.position + chegada.forward * 2.0f + chegada.right * 0.9f;
        float chao = chegada.position.y - 1f;
        if (Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            chao = hit.point.y;
        pos.y = chao;

        var go = new GameObject("Cliente_" + ch.contato);
        go.transform.position = pos;
        Vector3 olhar = chegada.position - pos;
        olhar.y = 0f;
        if (olhar.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(olhar);

        var npc = go.AddComponent<ClienteNPC>();
        npc.chamado = ch;
        npc.sala = sala;
        npc.distanciaInteracao = 2.6f;
        npc.Montar();
        atual = npc;
        return npc;
    }

    // ---------- Levar o técnico até o equipamento ----------

    SalaCliente sala;
    Transform[] caminho;
    int passo = -1;          // -1: ainda não saiu; caminho.Length: já chegou
    const float Velocidade = 1.15f;

    bool Andando => caminho != null && passo >= 0 && passo < caminho.Length;

    // O contato mostra onde está o problema: assim que sabe qual é o equipamento (o confuso só depois da conversa)
    // e o técnico chega perto, ele diz "vem comigo" e vai andando até lá; espera se o técnico ficar para trás.
    void ConduzirAteEquipamento(float distJogador)
    {
        if (sala == null || chamado == null) return;
        if (passo < 0)
        {
            if (!chamado.EquipamentoConhecido || distJogador > 3.2f || GameUI.Instance == null || GameUI.Instance.PainelAberto) return;
            caminho = sala.Caminho(chamado.equipamento);
            if (caminho == null || caminho.Length == 0) { passo = int.MaxValue; return; }
            passo = 0;
            string nome = chamado.EquipamentoTexto().ToLowerInvariant();
            string artigo = chamado.equipamento == TipoEquipamento.Impressora ? "a" : "o";
            GameUI.Instance.Toast($"<b>{chamado.contato}</b>: \"Vem comigo, eu te mostro {artigo} {nome}.\"", UIFactory.Destaque, 3.5f);
            return;
        }
        if (!Andando) return;

        // Técnico ficou para trás: espera olhando para ele
        if (distJogador > 4.5f)
        {
            Tocar(BancoPersonagens.Parado);
            VirarPara(jogadorT.position, 4f);
            return;
        }

        Vector3 alvo = caminho[passo].position;
        alvo.y = transform.position.y;
        Vector3 d = alvo - transform.position;
        if (d.magnitude < 0.08f)
        {
            passo++;
            if (!Andando)
            {
                var eq = Equipamento();
                if (eq != null) VirarPara(eq.transform.position, 100f);
                string onde = sala.tipo == TipoSala.Loja ? "Aqui atende o caixa todo dia." :
                              sala.tipo == TipoSala.Clinica ? "Os pacientes já estão esperando..." : "É aqui que a gente usa.";
                GameUI.Instance?.Toast($"<b>{chamado.contato}</b>: \"É esse aqui. {onde}\"", UIFactory.Destaque, 3f);
            }
            return;
        }
        Tocar(BancoPersonagens.Andando);
        transform.position = Vector3.MoveTowards(transform.position, alvo, Velocidade * Time.deltaTime);
        VirarPara(alvo, 8f);
        if (corpo != null) corpo.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.025f, 0f);
    }

    Equipamento Equipamento()
    {
        foreach (var e in FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == chamado.equipamento) return e;
        return null;
    }

    void VirarPara(Vector3 ponto, float rapidez)
    {
        Vector3 dir = ponto - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0004f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(Time.deltaTime * rapidez));
    }

    Animator animador;
    string estadoAtual;

    void Montar()
    {
        // Personagem 3D do Mixamo, quando configurado (o VIP usa o de roupa formal, o primeiro da lista)
        int indice = chamado.personalidade == Personalidade.VIP ? 0 : 1 + Random.Range(0, 2);
        animador = BancoPersonagens.Instanciar(transform, indice);
        if (animador != null)
        {
            Tocar(BancoPersonagens.Parado);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.3f;
            return;
        }

        MontarSimples();
    }

    void Tocar(string estado)
    {
        if (animador == null || estadoAtual == estado) return;
        estadoAtual = estado;
        animador.CrossFadeInFixedTime(estado, 0.35f);
    }

    // Boneco de formas simples (quando não há modelos do Mixamo no projeto)
    void MontarSimples()
    {
        // Cores variam com o tipo de cliente, para dar para reconhecer de longe
        Color camisa, calca = new Color(0.16f, 0.18f, 0.24f);
        switch (chamado.personalidade)
        {
            case Personalidade.VIP:       camisa = new Color(0.1f, 0.12f, 0.2f); calca = camisa; break;
            case Personalidade.Apressado: camisa = new Color(0.75f, 0.22f, 0.2f); break;
            case Personalidade.Confuso:   camisa = new Color(0.55f, 0.45f, 0.8f); break;
            case Personalidade.Mentiroso: camisa = new Color(0.85f, 0.55f, 0.25f); break;
            default:                      camisa = new Color(0.3f, 0.55f, 0.8f); break;
        }
        Color[] peles = { new Color(0.96f, 0.8f, 0.69f), new Color(0.82f, 0.62f, 0.47f), new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.24f, 0.16f) };
        Color[] cabelos = { new Color(0.1f, 0.08f, 0.06f), new Color(0.35f, 0.22f, 0.12f), new Color(0.75f, 0.6f, 0.35f), new Color(0.6f, 0.6f, 0.62f) };
        Color pele = peles[Random.Range(0, peles.Length)];
        Color cabelo = cabelos[Random.Range(0, cabelos.Length)];

        var mCamisa = Mat(camisa, 0.15f);
        var mCalca = Mat(calca, 0.2f);
        var mPele = Mat(pele, 0.3f);
        var mCabelo = Mat(cabelo, 0.25f);
        var mOlho = Mat(new Color(0.05f, 0.05f, 0.06f), 0.8f);
        var mSapato = Mat(new Color(0.08f, 0.07f, 0.07f), 0.5f);

        // Pernas e sapatos ficam paradas; o resto (corpo) balança de leve
        Peca(PrimitiveType.Capsule, transform, new Vector3(-0.1f, 0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f), mCalca);
        Peca(PrimitiveType.Capsule, transform, new Vector3(0.1f, 0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f), mCalca);
        Peca(PrimitiveType.Cube, transform, new Vector3(-0.1f, 0.04f, 0.04f), new Vector3(0.13f, 0.08f, 0.24f), mSapato);
        Peca(PrimitiveType.Cube, transform, new Vector3(0.1f, 0.04f, 0.04f), new Vector3(0.13f, 0.08f, 0.24f), mSapato);

        corpo = new GameObject("Corpo").transform;
        corpo.SetParent(transform, false);

        Peca(PrimitiveType.Capsule, corpo, new Vector3(0f, 1.15f, 0f), new Vector3(0.44f, 0.36f, 0.26f), mCamisa);
        var bracoE = Peca(PrimitiveType.Capsule, corpo, new Vector3(-0.28f, 1.1f, 0f), new Vector3(0.12f, 0.33f, 0.12f), mCamisa);
        var bracoD = Peca(PrimitiveType.Capsule, corpo, new Vector3(0.28f, 1.1f, 0f), new Vector3(0.12f, 0.33f, 0.12f), mCamisa);
        bracoE.localRotation = Quaternion.Euler(0, 0, -8f);
        bracoD.localRotation = Quaternion.Euler(0, 0, 8f);
        Peca(PrimitiveType.Sphere, corpo, new Vector3(-0.31f, 0.8f, 0f), Vector3.one * 0.1f, mPele);
        Peca(PrimitiveType.Sphere, corpo, new Vector3(0.31f, 0.8f, 0f), Vector3.one * 0.1f, mPele);
        Peca(PrimitiveType.Cylinder, corpo, new Vector3(0f, 1.5f, 0f), new Vector3(0.1f, 0.05f, 0.1f), mPele);   // pescoço
        Peca(PrimitiveType.Sphere, corpo, new Vector3(0f, 1.65f, 0f), new Vector3(0.24f, 0.27f, 0.25f), mPele); // cabeça
        Peca(PrimitiveType.Sphere, corpo, new Vector3(0f, 1.73f, -0.015f), new Vector3(0.26f, 0.16f, 0.27f), mCabelo);
        Peca(PrimitiveType.Sphere, corpo, new Vector3(-0.05f, 1.67f, 0.115f), Vector3.one * 0.03f, mOlho);
        Peca(PrimitiveType.Sphere, corpo, new Vector3(0.05f, 1.67f, 0.115f), Vector3.one * 0.03f, mOlho);

        if (chamado.personalidade == Personalidade.VIP)
        {
            Peca(PrimitiveType.Cube, corpo, new Vector3(0f, 1.25f, 0.125f), new Vector3(0.1f, 0.2f, 0.02f), Mat(Color.white, 0.2f)); // camisa
            Peca(PrimitiveType.Cube, corpo, new Vector3(0f, 1.22f, 0.137f), new Vector3(0.04f, 0.24f, 0.01f), Mat(new Color(0.7f, 0.1f, 0.12f), 0.4f)); // gravata
        }

        // Um colisor só, para o jogador não atravessar o cliente
        var cc = gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.3f;

        fase = Random.value * 10f;
    }

    Transform Peca(PrimitiveType forma, Transform pai, Vector3 pos, Vector3 escala, Material mat)
    {
        var p = GameObject.CreatePrimitive(forma);
        p.name = forma.ToString();
        var col = p.GetComponent<Collider>();
        if (col != null) Destroy(col);
        p.transform.SetParent(pai, false);
        p.transform.localPosition = pos;
        p.transform.localScale = escala;
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p.transform;
    }

    Material Mat(Color cor, float brilho)
    {
        var m = new Material(Shader.Find("Standard")) { color = cor };
        m.SetFloat("_Glossiness", brilho);
        materiais.Add(m);
        return m;
    }

    // ---------- Comportamento ----------

    protected override void Update()
    {
        var mm = MissionManager.Instance;
        if (mm == null || mm.chamadoAtivo != chamado)
        {
            Destroy(gameObject);   // chamado resolvido ou abandonado: o cliente vai embora
            return;
        }

        if (jogadorT == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) jogadorT = p.transform;
        }
        if (jogadorT == null) return;
        Vector3 dir = jogadorT.position - transform.position;
        dir.y = 0f;

        if (!assinando)
        {
            ConduzirAteEquipamento(dir.magnitude);
            if (Andando) return;
        }

        // Respira de leve
        fase += Time.deltaTime;
        if (corpo != null) corpo.localPosition = new Vector3(0f, Mathf.Sin(fase * 2f) * 0.008f, 0f);

        // Vira para o jogador quando ele está por perto
        if (dir.sqrMagnitude > 0.04f && dir.sqrMagnitude < 36f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);

        // Com o técnico por perto, explica o problema gesticulando; senão, fica esperando
        Tocar(dir.sqrMagnitude < 3.5f * 3.5f ? BancoPersonagens.ConversandoEmPe : BancoPersonagens.Parado);
    }

    void OnDestroy()
    {
        if (atual == this) atual = null;
        foreach (var m in materiais)
            if (m != null) Destroy(m);
    }
}
