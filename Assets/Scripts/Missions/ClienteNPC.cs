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

    public override string TextoPrompt =>
        chamado != null ? $"[E] Conversar com {chamado.contato}" : "[E] Conversar";

    public override void Interagir()
    {
        if (GameUI.Instance != null) GameUI.Instance.AbrirConversa(this);
    }

    // ---------- Criação ----------

    public static ClienteNPC Criar(Chamado ch)
    {
        var mm = MissionManager.Instance;
        if (ch == null || mm == null || mm.locaisClientes == null || mm.locaisClientes.Length == 0 || mm.locaisClientes[0] == null)
            return null;

        // Remove o cliente que tenha sobrado de outro chamado
        if (atual != null) Destroy(atual.gameObject);

        // Fica um pouco à frente e à direita de onde o jogador chega
        Transform chegada = mm.locaisClientes[0];
        Vector3 pos = chegada.position + chegada.forward * 2.0f + chegada.right * 0.9f;
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
        npc.distanciaInteracao = 2.6f;
        npc.Montar();
        atual = npc;
        return npc;
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

        // Respira de leve
        fase += Time.deltaTime;
        if (corpo != null) corpo.localPosition = new Vector3(0f, Mathf.Sin(fase * 2f) * 0.008f, 0f);

        // Vira para o jogador quando ele está por perto
        if (jogadorT == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) jogadorT = p.transform;
        }
        if (jogadorT != null)
        {
            Vector3 dir = jogadorT.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.04f && dir.sqrMagnitude < 36f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);

            // Com o técnico por perto, explica o problema gesticulando; senão, fica esperando
            Tocar(dir.sqrMagnitude < 3.5f * 3.5f ? BancoPersonagens.ConversandoEmPe : BancoPersonagens.Parado);
        }
    }

    void OnDestroy()
    {
        if (atual == this) atual = null;
        foreach (var m in materiais)
            if (m != null) Destroy(m);
    }
}
