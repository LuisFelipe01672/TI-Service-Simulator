using UnityEngine;

// Etapa 10C: animação das peças ENQUANTO o técnico segura E (pedido do dono: "tirar a memória, as peças, tudo tem
// que ter animação"). Cada ação demorada ganha um id de animação (pela tabela abaixo, pelo começo do rótulo); o
// visual do equipamento lê o progresso (0 a 1) da barra e põe as peças na pose daquele instante: parafusos girando
// e saindo, travas da memória abrindo, pente subindo, fonte e disco deslizando, cooler saindo, pasta sumindo.
// Soltou o E antes do fim: as peças voltam. Terminou: o estado novo já é a pose final.
// Na mão, a ferramenta certa aparece na frente da câmera e trabalha junto (chave girando, lata de ar soprando...).
public static class AnimacaoPecas
{
    public enum Ferramenta { Nenhuma, Chave, Ar, Pano, Seringa, Espatula }

    // Começo do rótulo » id da animação e a ferramenta na mão
    static readonly (string inicio, string id, Ferramenta f)[] Tabela =
    {
        // Computador (gabinete)
        ("Virar o gabinete e tirar a tampa", "gab_abrir", Ferramenta.Chave),
        ("Colocar a tampa e voltar o gabinete", "gab_fechar", Ferramenta.Chave),
        ("Limpar com pincel e ar comprimido", "gab_limpar", Ferramenta.Ar),
        ("Tirar os pentes de memória", "gab_memoria", Ferramenta.Nenhuma),
        ("Trocar o pente de memória", "gab_memoria_troca", Ferramenta.Nenhuma),
        ("Trocar a fonte", "gab_fonte", Ferramenta.Chave),
        ("Tirar o cooler do processador", "gab_cooler_tirar", Ferramenta.Chave),
        ("Recolocar o cooler", "gab_cooler_por", Ferramenta.Chave),
        ("Limpar a pasta velha", "gab_pasta_limpar", Ferramenta.Pano),
        ("Aplicar pasta térmica", "gab_pasta_aplicar", Ferramenta.Seringa),
        ("Trocar o HD por um SSD", "gab_ssd", Ferramenta.Chave),
        // Notebook
        ("Virar e tirar os 10 parafusos", "nb_abrir", Ferramenta.Chave),
        ("Encaixar a tampa de baixo e parafusar", "nb_fechar", Ferramenta.Chave),
        ("Desconectar e retirar a bateria", "nb_bateria_tirar", Ferramenta.Nenhuma),
        ("Recolocar a mesma bateria", "nb_bateria_por", Ferramenta.Nenhuma),
        ("Instalar a bateria nova", "nb_bateria_por", Ferramenta.Nenhuma),
        ("Limpar a ventoinha e o radiador", "nb_limpar", Ferramenta.Ar),
        ("Tirar a capinha da tecla", "nb_tecla_tirar", Ferramenta.Espatula),
        ("Soprar com ar comprimido e limpar com pincel", "nb_tecla_limpar", Ferramenta.Ar),
        ("Encaixar a capinha da tecla", "nb_tecla_por", Ferramenta.Nenhuma),
        ("Soltar a moldura da tela", "nb_moldura_tirar", Ferramenta.Espatula),
        ("Encaixar a moldura da tela", "nb_moldura_por", Ferramenta.Nenhuma),
        ("Trocar as dobradiças", "nb_dobradica", Ferramenta.Chave),
        ("Trocar pelo carregador universal", "nb_carregador", Ferramenta.Nenhuma),
        // Impressora
        ("Abrir a tampa traseira e puxar o papel atolado", "imp_papel", Ferramenta.Nenhuma),
        ("Trocar o cilindro", "imp_cilindro", Ferramenta.Nenhuma),
    };

    public static string Id(string rotulo)
    {
        if (string.IsNullOrEmpty(rotulo)) return null;
        foreach (var t in Tabela) if (rotulo.StartsWith(t.inicio)) return t.id;
        return null;
    }

    public static Ferramenta FerramentaDe(string id)
    {
        foreach (var t in Tabela) if (t.id == id) return t.f;
        return Ferramenta.Nenhuma;
    }

    // Todas as ações demoradas com animação (o teste confere que cada id existe em algum visual)
    public static System.Collections.Generic.IEnumerable<string> Ids()
    {
        foreach (var t in Tabela) yield return t.id;
    }

    // Esta animação está rodando neste equipamento? p = progresso da barra (0 a 1)
    public static bool Segurando(Equipamento eq, string id, out float p)
    {
        p = 0f;
        var a = GameUI.AcaoSegurada;
        if (a == null || a.animacao != id || GameUI.EquipamentoSegurado != eq) return false;
        p = Mathf.Clamp01(GameUI.ProgressoSegurado);
        return true;
    }

    // Qual animação (de qualquer equipamento) está rodando agora
    public static string IdSegurado => GameUI.AcaoSegurada?.animacao;

    // Trecho [a, b] do progresso virando 0..1 (suave)
    public static float Trecho(float p, float a, float b) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, p));

    // ---------- Ferramenta na mão ----------

    static Transform mao;
    static Ferramenta naMao = Ferramenta.Nenhuma;
    static Transform[] modelos;
    static ParticleSystem sopro;
    static Material mAmarelo, mPreto, mMetal, mBranco, mAzul, mCinza;

    // Chamado a cada quadro por EquipamentoPecas (idempotente)
    public static void AtualizarMao()
    {
        var cam = Camera.main;
        string id = IdSegurado;
        var f = id != null ? FerramentaDe(id) : Ferramenta.Nenhuma;
        if (cam == null) return;
        if (mao == null) Criar(cam);
        if (f != naMao)
        {
            naMao = f;
            for (int i = 0; i < modelos.Length; i++) if (modelos[i] != null) modelos[i].gameObject.SetActive(i == (int)f);
        }
        if (f == Ferramenta.Nenhuma) { if (sopro != null && sopro.isEmitting) sopro.Stop(); return; }

        float p = Mathf.Clamp01(GameUI.ProgressoSegurado), t = Time.time;
        var m = modelos[(int)f];
        Vector3 basePos = new Vector3(0.16f, -0.17f, 0.42f);
        switch (f)
        {
            case Ferramenta.Chave:   // gira no eixo e empurra de leve a cada volta
                m.localPosition = basePos + new Vector3(0f, 0f, 0.02f * Mathf.Abs(Mathf.Sin(t * 6f)));
                m.localRotation = Quaternion.Euler(0f, 0f, t * 540f);
                break;
            case Ferramenta.Ar:      // balança e sopra
                m.localPosition = basePos + new Vector3(0.02f * Mathf.Sin(t * 5f), 0f, 0f);
                m.localRotation = Quaternion.Euler(-10f, 10f * Mathf.Sin(t * 5f), 0f);
                if (sopro != null && !sopro.isEmitting) sopro.Play();
                break;
            case Ferramenta.Pano:    // esfrega em círculos
                m.localPosition = basePos + new Vector3(0.03f * Mathf.Cos(t * 9f), 0.02f * Mathf.Sin(t * 9f), 0.03f);
                m.localRotation = Quaternion.identity;
                break;
            case Ferramenta.Seringa: // o êmbolo desce com o progresso
                m.localPosition = basePos + new Vector3(0f, 0f, 0.03f);
                var embolo = m.Find("Embolo");
                if (embolo != null) embolo.localPosition = new Vector3(0f, 0f, -0.1f + 0.06f * p);
                m.localRotation = Quaternion.Euler(20f, 0f, 0f);
                break;
            case Ferramenta.Espatula: // entra na fresta e alavanca
                m.localPosition = basePos + new Vector3(0f, 0f, 0.03f * p);
                m.localRotation = Quaternion.Euler(-15f * Mathf.Sin(p * Mathf.PI * 3f), 0f, 0f);
                break;
        }
        if (f != Ferramenta.Ar && sopro != null && sopro.isEmitting) sopro.Stop();
    }

    static Material M(Color c, float metal = 0f)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Glossiness", 0.45f);
        m.SetFloat("_Metallic", metal);
        return m;
    }

    static Transform Peca(Transform pai, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat, Vector3? giro = null)
    {
        var g = GameObject.CreatePrimitive(tipo);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        if (giro != null) g.transform.localRotation = Quaternion.Euler(giro.Value);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    static void Criar(Camera cam)
    {
        mAmarelo = M(new Color(0.95f, 0.75f, 0.1f)); mPreto = M(new Color(0.06f, 0.06f, 0.07f)); mMetal = M(new Color(0.75f, 0.76f, 0.78f), 0.85f);
        mBranco = M(new Color(0.95f, 0.95f, 0.95f)); mAzul = M(new Color(0.2f, 0.4f, 0.85f)); mCinza = M(new Color(0.55f, 0.56f, 0.58f));
        mao = new GameObject("FerramentaNaMao").transform;
        mao.SetParent(cam.transform, false);
        modelos = new Transform[System.Enum.GetValues(typeof(Ferramenta)).Length];

        // Chave Phillips: cabo amarelo com faixa preta e haste de metal apontando para a frente
        var chave = Novo("Chave");
        Peca(chave, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.05f), new Vector3(0.028f, 0.045f, 0.028f), mAmarelo, new Vector3(90f, 0f, 0f));
        Peca(chave, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.03f), new Vector3(0.029f, 0.006f, 0.029f), mPreto, new Vector3(90f, 0f, 0f));
        Peca(chave, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.04f), new Vector3(0.006f, 0.05f, 0.006f), mMetal, new Vector3(90f, 0f, 0f));
        modelos[(int)Ferramenta.Chave] = chave;

        // Lata de ar comprimido com o canudinho vermelho
        var ar = Novo("ArComprimido");
        Peca(ar, PrimitiveType.Cylinder, new Vector3(0f, -0.03f, 0f), new Vector3(0.05f, 0.07f, 0.05f), mAzul);
        Peca(ar, PrimitiveType.Cube, new Vector3(0f, 0.045f, 0.01f), new Vector3(0.02f, 0.015f, 0.03f), mPreto);
        Peca(ar, PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0.07f), new Vector3(0.004f, 0.045f, 0.004f), M(new Color(0.85f, 0.15f, 0.12f)), new Vector3(90f, 0f, 0f));
        sopro = CriarSopro(ar, new Vector3(0f, 0.045f, 0.12f));
        modelos[(int)Ferramenta.Ar] = ar;

        // Pano de microfibra dobrado
        var pano = Novo("Pano");
        Peca(pano, PrimitiveType.Cube, Vector3.zero, new Vector3(0.07f, 0.012f, 0.06f), M(new Color(0.3f, 0.65f, 0.85f)), new Vector3(-60f, 0f, 0f));
        modelos[(int)Ferramenta.Pano] = pano;

        // Seringa de pasta térmica (corpo transparente-acinzentado, pasta branca, êmbolo)
        var ser = Novo("Seringa");
        Peca(ser, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.016f, 0.05f, 0.016f), mCinza, new Vector3(90f, 0f, 0f));
        Peca(ser, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.06f), new Vector3(0.005f, 0.012f, 0.005f), mBranco, new Vector3(90f, 0f, 0f));
        var emb = Peca(ser, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.1f), new Vector3(0.012f, 0.03f, 0.012f), mBranco, new Vector3(90f, 0f, 0f));
        emb.name = "Embolo";
        modelos[(int)Ferramenta.Seringa] = ser;

        // Espátula de plástico
        var esp = Novo("Espatula");
        Peca(esp, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.018f, 0.004f, 0.11f), M(new Color(0.15f, 0.5f, 0.3f)));
        modelos[(int)Ferramenta.Espatula] = esp;

        foreach (var m in modelos) if (m != null) m.gameObject.SetActive(false);
    }

    static Transform Novo(string nome)
    {
        var t = new GameObject(nome).transform;
        t.SetParent(mao, false);
        return t;
    }

    // Nuvenzinha de poeira saindo do canudo (partículas cinza, curtas)
    static ParticleSystem CriarSopro(Transform pai, Vector3 pos)
    {
        var go = new GameObject("Sopro");
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.6f;
        main.startSize = 0.025f;
        main.startColor = new Color(0.62f, 0.6f, 0.56f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.05f;
        main.playOnAwake = false;
        var em = ps.emission;
        em.rateOverTime = 40f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 25f;
        sh.radius = 0.005f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        return ps;
    }
}
