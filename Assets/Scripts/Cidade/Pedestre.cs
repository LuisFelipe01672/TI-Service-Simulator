using System.Collections.Generic;
using UnityEngine;

// Etapa 9D: pedestre andando pelas calçadas do bairro (rede de RotasTransito.Calcadas). Anda pela direita, desvia do
// técnico, de outros pedestres e do orelhão; atravessa a Acácias só na faixa, esperando os carros pararem.
// Usa os personagens do Mixamo (BancoPersonagens, fora do Git) com a animação "Andando"; sem eles, um boneco simples
// de cápsulas balançando as pernas.
public class Pedestre : MonoBehaviour
{
    public int aresta, deNo, paraNo;
    public float t;                 // metros andados na aresta
    public float vel, velBase;
    public float lateral;           // na direção 'Lv' da aresta (lado do meio-fio, ou à direita na faixa)
    public bool atravessando, esperandoFaixa;
    public float tempoParado;

    Animator anim;
    string estadoAnim;
    Transform pernaE, pernaD, bracoE, bracoD;
    float fase, espera, y;
    int faixaReservada = -1;
    static readonly List<(Vector3 c, float r, bool jogador)> viz = new List<(Vector3, float, bool)>();
    static Material[] roupas, peles;
    static Material calca, sapato;

    public void Montar(int semente)
    {
        var col = gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f); col.height = 1.8f; col.radius = 0.25f;
        var rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        anim = BancoPersonagens.Instanciar(transform, semente);
        if (anim != null) return;
        MontarSimples(semente);
    }

    // Boneco de formas simples (quando não há os personagens do Mixamo)
    void MontarSimples(int semente)
    {
        if (roupas == null)
        {
            Material M(Color c, float g) { var m = new Material(Shader.Find("Standard")); m.color = c; m.SetFloat("_Glossiness", g); return m; }
            roupas = new[] { M(new Color(0.75f, 0.22f, 0.2f), 0.15f), M(new Color(0.3f, 0.55f, 0.8f), 0.15f), M(new Color(0.95f, 0.95f, 0.92f), 0.15f),
                             M(new Color(0.2f, 0.5f, 0.3f), 0.15f), M(new Color(0.9f, 0.7f, 0.2f), 0.15f), M(new Color(0.15f, 0.15f, 0.18f), 0.15f) };
            peles = new[] { M(new Color(0.96f, 0.8f, 0.69f), 0.3f), M(new Color(0.82f, 0.62f, 0.47f), 0.3f), M(new Color(0.55f, 0.38f, 0.26f), 0.3f), M(new Color(0.36f, 0.24f, 0.16f), 0.3f) };
            calca = M(new Color(0.16f, 0.2f, 0.32f), 0.2f);
            sapato = M(new Color(0.08f, 0.07f, 0.07f), 0.4f);
        }
        var rnd = new System.Random(semente * 7919);
        var roupa = roupas[rnd.Next(roupas.Length)];
        var pele = peles[rnd.Next(peles.Length)];
        Transform Peca(PrimitiveType tipo, Transform pai, Vector3 pos, Vector3 esc, Material m)
        {
            var g = GameObject.CreatePrimitive(tipo);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(pai, false);
            g.transform.localPosition = pos;
            g.transform.localScale = esc;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }
        Transform Pivo(string nome, Vector3 pos) { var p = new GameObject(nome).transform; p.SetParent(transform, false); p.localPosition = pos; return p; }
        pernaE = Pivo("PernaE", new Vector3(-0.1f, 0.85f, 0f));
        pernaD = Pivo("PernaD", new Vector3(0.1f, 0.85f, 0f));
        foreach (var pe in new[] { pernaE, pernaD })
        {
            Peca(PrimitiveType.Capsule, pe, new Vector3(0f, -0.42f, 0f), new Vector3(0.15f, 0.42f, 0.15f), calca);
            Peca(PrimitiveType.Cube, pe, new Vector3(0f, -0.81f, 0.04f), new Vector3(0.12f, 0.08f, 0.24f), sapato);
        }
        Peca(PrimitiveType.Capsule, transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.35f, 0.25f), roupa);
        bracoE = Pivo("BracoE", new Vector3(-0.27f, 1.38f, 0f));
        bracoD = Pivo("BracoD", new Vector3(0.27f, 1.38f, 0f));
        Peca(PrimitiveType.Capsule, bracoE, new Vector3(0f, -0.3f, 0f), new Vector3(0.11f, 0.3f, 0.11f), roupa);
        Peca(PrimitiveType.Capsule, bracoD, new Vector3(0f, -0.3f, 0f), new Vector3(0.11f, 0.3f, 0.11f), roupa);
        Peca(PrimitiveType.Sphere, transform, new Vector3(0f, 1.63f, 0f), new Vector3(0.23f, 0.26f, 0.24f), pele);
    }

    public void Iniciar(VidaNaRua rua, int novaAresta, bool sentidoAB, Vector3 pos)
    {
        var ar = rua.calcadas.arestas[novaAresta];
        aresta = novaAresta;
        deNo = sentidoAB ? ar.a : ar.b;
        paraNo = sentidoAB ? ar.b : ar.a;
        Vector3 a = rua.calcadas.nos[deNo].pos;
        t = Mathf.Clamp(Vector3.Dot(pos - a, Dir(rua)), 0f, Comprimento(rua));
        velBase = Random.Range(1.15f, 1.5f);
        vel = velBase;
        lateral = LateralPadrao(rua);
        atravessando = ar.faixa >= 0;
        esperandoFaixa = false;
        tempoParado = 0f;
        y = rua.AlturaChao(pos);
        Aplicar(rua);
        if (anim != null) { anim.Play(BancoPersonagens.Andando, 0, Random.value); estadoAnim = BancoPersonagens.Andando; }
    }

    Vector3 A(VidaNaRua r) => r.calcadas.nos[deNo].pos;
    Vector3 B(VidaNaRua r) => r.calcadas.nos[paraNo].pos;
    Vector3 Dir(VidaNaRua r) { var d = B(r) - A(r); d.y = 0f; return d.normalized; }
    float Comprimento(VidaNaRua r) { var d = B(r) - A(r); d.y = 0f; return d.magnitude; }

    // Eixo lateral: para o meio-fio nas calçadas; à direita de quem anda, na faixa
    Vector3 Lv(VidaNaRua r)
    {
        var m = r.calcadas.arestas[aresta].meioFio;
        return m == Vector3.zero ? RotasTransito.Direita(Dir(r)) : m;
    }

    (float min, float max) Limites(VidaNaRua r) => r.calcadas.arestas[aresta].faixa >= 0 ? (-1f, 1f) : (-0.45f, 0.75f);

    float LateralPadrao(VidaNaRua r) => 0.3f * Mathf.Sign(Vector3.Dot(RotasTransito.Direita(Dir(r)), Lv(r)));

    public void Passo(float dt, VidaNaRua rua)
    {
        if (esperandoFaixa)
        {
            vel = 0f;
            espera += dt;
            int f = rua.calcadas.arestas[aresta].faixa;
            if (rua.PodeAtravessar(f)) { esperandoFaixa = false; atravessando = true; faixaReservada = f; rua.OcuparFaixa(f); }
            else if (espera > 25f) { esperandoFaixa = false; Desistir(rua); }
            Aplicar(rua);
            Animar(dt);
            return;
        }

        Vector3 dir = Dir(rua), lv = Lv(rua);
        Vector3 pos = A(rua) + dir * t + lv * lateral;
        var (lmin, lmax) = Limites(rua);

        // Desvio: quem está à frente no meu lado vira um "buraco" lateral; escolho o lado livre mais perto
        float alvoLat = LateralPadrao(rua), alvoVel = velBase;
        rua.Vizinhos(this, viz);
        foreach (var o in viz)
        {
            Vector3 rel = o.c - pos; rel.y = 0f;
            float frente = Vector3.Dot(rel, dir);
            if (frente < -0.2f || frente > 2.2f) continue;
            float olat = Vector3.Dot(o.c - A(rua), lv);
            float precisa = o.r + 0.35f;
            if (Mathf.Abs(olat - alvoLat) >= precisa) continue;
            float l1 = olat - precisa, l2 = olat + precisa;
            bool ok1 = l1 >= lmin, ok2 = l2 <= lmax;
            if (ok1 && (!ok2 || Mathf.Abs(l1 - lateral) <= Mathf.Abs(l2 - lateral))) alvoLat = l1;
            else if (ok2) alvoLat = l2;
            else if (frente < 1.1f) alvoVel = 0f;               // não dá para passar: espera
            else alvoVel = Mathf.Min(alvoVel, 0.5f);
            if (o.jogador && frente < 1.4f) alvoVel = Mathf.Min(alvoVel, 0.6f);
        }
        if (atravessando && rua.CarroPerto(pos, 0.6f)) alvoVel = 0f;
        vel = Mathf.MoveTowards(vel, alvoVel, 3f * dt);
        lateral = Mathf.MoveTowards(lateral, Mathf.Clamp(alvoLat, lmin, lmax), 1.1f * dt);
        t += vel * dt;
        tempoParado = vel < 0.05f ? tempoParado + dt : 0f;
        if (tempoParado > 6f && !atravessando) { tempoParado = 0f; Virar(rua, deNo); }

        if (t >= Comprimento(rua)) Chegou(rua);
        Aplicar(rua);
        Animar(dt);
    }

    void Chegou(VidaNaRua rua)
    {
        int no = paraNo;
        SoltarFaixa(rua);
        atravessando = false;
        var nodo = rua.calcadas.nos[no];
        if (nodo.ponta)
        {
            if (rua.TrocarDePonta(this, no)) return;
            Virar(rua, no);
            return;
        }
        var opcoes = rua.calcadas.Vizinhas(no);
        opcoes.Remove(aresta);
        if (opcoes.Count == 0) { Virar(rua, no); return; }
        // faixa com menos chance (a pessoa nem sempre atravessa)
        int escolhida = opcoes[Random.Range(0, opcoes.Count)];
        if (rua.calcadas.arestas[escolhida].faixa >= 0 && opcoes.Count > 1 && Random.value < 0.4f)
        {
            opcoes.Remove(escolhida);
            escolhida = opcoes[Random.Range(0, opcoes.Count)];
        }
        Seguir(rua, escolhida, no);
    }

    void Seguir(VidaNaRua rua, int novaAresta, int doNo)
    {
        Vector3 aqui = transform.position;   // continua do mesmo lugar (sem pulo na esquina)
        aresta = novaAresta;
        deNo = doNo;
        paraNo = rua.calcadas.Outro(novaAresta, doNo);
        Vector3 rel = aqui - A(rua);
        t = Mathf.Clamp(Vector3.Dot(rel, Dir(rua)), 0f, Comprimento(rua));
        var (lmin, lmax) = Limites(rua);
        lateral = Mathf.Clamp(Vector3.Dot(rel, Lv(rua)), lmin, lmax);
        if (rua.calcadas.arestas[novaAresta].faixa >= 0) { esperandoFaixa = true; espera = 0f; }
    }

    // Desistiu de atravessar: segue por outra calçada a partir da esquina
    void Desistir(VidaNaRua rua)
    {
        var opcoes = rua.calcadas.Vizinhas(deNo);
        opcoes.Remove(aresta);
        if (opcoes.Count == 0) return;
        Seguir(rua, opcoes[Random.Range(0, opcoes.Count)], deNo);
    }

    // Meia-volta: no fim da aresta ('no' = paraNo) ou no meio dela (travado: inverte o sentido no mesmo lugar)
    void Virar(VidaNaRua rua, int no)
    {
        if (no == paraNo) { deNo = paraNo; paraNo = rua.calcadas.Outro(aresta, deNo); t = 0f; }
        else { int velho = paraNo; paraNo = deNo; deNo = velho; t = Mathf.Max(0f, Comprimento(rua) - t); }
        lateral = LateralPadrao(rua);
    }

    Renderer[] desenhos;
    bool mostrando = true;
    public void Mostrar(bool sim)
    {
        if (sim == mostrando) return;
        mostrando = sim;
        if (desenhos == null) desenhos = GetComponentsInChildren<Renderer>(true);
        foreach (var r in desenhos) if (r != null) r.enabled = sim;
        if (anim != null) anim.cullingMode = sim ? AnimatorCullingMode.CullUpdateTransforms : AnimatorCullingMode.CullCompletely;
    }

    public void SoltarFaixa(VidaNaRua rua)
    {
        if (faixaReservada >= 0) { rua.SoltarFaixa(faixaReservada); faixaReservada = -1; }
    }

    void Aplicar(VidaNaRua rua)
    {
        Vector3 dir = Dir(rua);
        Vector3 p = A(rua) + dir * t + Lv(rua) * lateral;
        float chao = rua.AlturaChao(p);
        y = Mathf.MoveTowards(y, chao, 1.5f * Time.deltaTime);
        p.y = y;
        transform.position = p;
        if (vel > 0.05f || esperandoFaixa)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 300f * Time.deltaTime);
        }
    }

    void Animar(float dt)
    {
        if (anim != null)
        {
            string e = vel > 0.15f ? BancoPersonagens.Andando : BancoPersonagens.Parado;
            if (e != estadoAnim) { estadoAnim = e; anim.CrossFadeInFixedTime(e, 0.25f); }
            anim.speed = e == BancoPersonagens.Andando ? Mathf.Clamp(vel / 1.35f, 0.5f, 1.4f) : 1f;
            return;
        }
        if (pernaE == null) return;
        fase += vel * dt * 5.2f;
        float a = Mathf.Sin(fase) * 28f * Mathf.Clamp01(vel / 1.2f);
        pernaE.localRotation = Quaternion.Euler(a, 0f, 0f);
        pernaD.localRotation = Quaternion.Euler(-a, 0f, 0f);
        bracoE.localRotation = Quaternion.Euler(-a * 0.7f, 0f, 0f);
        bracoD.localRotation = Quaternion.Euler(a * 0.7f, 0f, 0f);
    }
}
