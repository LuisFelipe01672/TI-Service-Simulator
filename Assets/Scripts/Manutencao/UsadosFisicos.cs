using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

// Etapa 6: os equipamentos usados como objetos no escritório. O lote chega em caixas encostadas na parede sul;
// E abre a caixa (estilete), E pega o equipamento (vai para as mãos, um por vez), E na bancada põe em cima.
// Na bancada, mirar o equipamento mostra as ações (AcoesBancada). Com a sucata na mão, E no lixo eletrônico descarta.
// Sem nada mirado, E larga o que está na mão no chão. A posição volta do save (caixa, chão ou bancada).
public class UsadosFisicos : MonoBehaviour
{
    public static UsadosFisicos Instance;
    public static EquipUsado NaMao { get; private set; }

    readonly Dictionary<EquipUsado, UsadoFisico> objetos = new Dictionary<EquipUsado, UsadoFisico>();
    Transform sala;
    Bancada bancada;
    TelaBancada monitor;
    float proximaSincronia;
    EventBinding<DiaIniciadoEvent> bDia;

    // Vagas no chão do escritório (coordenadas da sala): encostadas na parede sul, à direita da porta
    static readonly Vector3[] Vagas =
    {
        new Vector3(0.75f, 0, -2.55f), new Vector3(1.4f, 0, -2.55f), new Vector3(2.05f, 0, -2.55f),
        new Vector3(0.75f, 0, -1.9f), new Vector3(1.4f, 0, -1.9f), new Vector3(1.85f, 0, -0.35f),
        new Vector3(0.75f, 0, -1.25f), new Vector3(1.4f, 0, -1.25f),
    };
    // Lugar do equipamento na bancada e do monitor (coordenadas da bancada; quem usa fica do lado +Z)
    public static readonly Vector3 PosNaBancada = new Vector3(0.08f, 0.885f, 0.04f);
    static readonly Vector3 PosMonitor = new Vector3(0.45f, 0.885f, -0.17f);

    // ---------- Criação automática ----------

    static bool registrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!registrado)
        {
            SceneManager.sceneLoaded += (_, __) => CriarSePrecisar();
            registrado = true;
        }
        CriarSePrecisar();
    }

    static void CriarSePrecisar()
    {
        if (Instance != null) return;
        if (GameObject.Find("Cenario/Escritorio") == null) return;   // menu principal
        Instance = new GameObject("UsadosFisicos").AddComponent<UsadosFisicos>();
    }

    void Awake()
    {
        Instance = this;
        NaMao = null;
        EquipamentosUsados.AoMudar += AoMudar;
        bDia = new EventBinding<DiaIniciadoEvent>(_ => NovoDia());
        EventBus<DiaIniciadoEvent>.Register(bDia);
    }

    void OnDestroy()
    {
        EquipamentosUsados.AoMudar -= AoMudar;
        EventBus<DiaIniciadoEvent>.Deregister(bDia);
        if (Instance == this) { Instance = null; NaMao = null; }
    }

    // ---------- Sincronia com o estoque ----------

    void Update()
    {
        EquipamentoPecas.AnimarFerramentaNaMao();
        if (Time.time >= proximaSincronia) { proximaSincronia = Time.time + 0.5f; Sincronizar(); }
        TratarLargar();
    }

    bool AcharCenario()
    {
        if (sala == null) { var s = GameObject.Find("Cenario/Escritorio"); if (s != null) sala = s.transform; }
        if (bancada == null) bancada = Bancada.DeEquipamentos();
        if (bancada != null && monitor == null)
            monitor = TelaBancada.Criar(bancada.transform, PosMonitor, Quaternion.Euler(0f, 180f, 0f));
        return sala != null;
    }

    public void Sincronizar()
    {
        if (!AcharCenario()) return;
        var estoque = EquipamentosUsados.Estoque;
        // o que saiu do estoque (vendido, frota, descarte) some
        var sairam = new List<EquipUsado>();
        foreach (var kv in objetos) if (!estoque.Contains(kv.Key)) sairam.Add(kv.Key);
        foreach (var e in sairam)
        {
            var o = objetos[e];
            objetos.Remove(e);
            if (NaMao == e) NaMao = null;
            if (o != null) Tween.Scale(o.transform, Vector3.zero, 0.35f, Ease.InBack).OnComplete(o, x => Destroy(x.gameObject));
        }
        // só um na bancada e só um na mão
        bool bancadaOcupada = false;
        foreach (var e in estoque)
        {
            if (e.local == LocalUsado.Bancada && bancada != null) { if (bancadaOcupada) e.local = LocalUsado.Chao; else bancadaOcupada = true; }
            if (e.local == LocalUsado.Mao && NaMao != null && NaMao != e) e.local = LocalUsado.Chao;
        }
        foreach (var e in estoque)
        {
            if (objetos.TryGetValue(e, out var o) && o != null) continue;
            if (e.local == LocalUsado.Bancada && bancada == null) continue;   // a bancada ainda não foi criada
            if ((e.local == LocalUsado.Caixa || e.local == LocalUsado.Chao) && (e.vaga < 0 || VagaOcupada(e.vaga, e))) e.vaga = VagaLivre(e);
            o = UsadoFisico.Criar(e, this);
            objetos[e] = o;
            Posicionar(e, false);
        }
    }

    bool VagaOcupada(int vaga, EquipUsado quem)
    {
        foreach (var e in EquipamentosUsados.Estoque)
            if (e != quem && e.vaga == vaga && (e.local == LocalUsado.Caixa || e.local == LocalUsado.Chao)) return true;
        return false;
    }

    int VagaLivre(EquipUsado quem)
    {
        for (int i = 0; i < Vagas.Length; i++) if (!VagaOcupada(i, quem)) return i;
        return Vagas.Length;   // além das vagas: segunda fila, mais para dentro
    }

    Vector3 PosicaoVaga(int vaga)
    {
        Vector3 local = vaga < Vagas.Length ? Vagas[Mathf.Max(0, vaga)] : new Vector3(0.75f + (vaga - Vagas.Length) % 3 * 0.6f, 0f, -0.6f);
        return sala.TransformPoint(local);
    }

    // Leva o objeto para onde o estado diz (caixa/chão na vaga, bancada, mão)
    public void Posicionar(EquipUsado e, bool animar)
    {
        if (!objetos.TryGetValue(e, out var o) || o == null) return;
        var t = o.transform;
        o.AoMudarLocal();
        switch (e.local)
        {
            case LocalUsado.Mao:
                NaMao = e;
                var cam = Camera.main;
                if (cam == null) break;
                t.SetParent(cam.transform, true);
                Vector3 alvo = new Vector3(0f, -0.38f - o.Altura * 0.35f, 0.62f + o.Profundidade * 0.5f);
                Quaternion giro = Quaternion.Euler(8f, 0f, 0f);
                if (animar) { Tween.LocalPosition(t, alvo, 0.45f, Ease.OutCubic); Tween.LocalRotation(t, giro, 0.45f, Ease.OutCubic); }
                else { t.localPosition = alvo; t.localRotation = giro; }
                break;
            case LocalUsado.Carro:
                if (NaMao == e) NaMao = null;
                var pm = PortaMalas.Instance;
                if (pm == null) { e.local = e.naCaixa ? LocalUsado.Caixa : LocalUsado.Chao; e.naCaixa = false; e.vaga = -1; Posicionar(e, animar); return; }
                var vagaCarro = pm.VagaDoUsado(e);
                t.SetParent(vagaCarro, true);
                Quaternion deitado = Quaternion.Euler(0f, 90f, 0f);
                if (animar) { Tween.LocalPosition(t, Vector3.zero, 0.45f, Ease.OutCubic); Tween.LocalRotation(t, deitado, 0.45f, Ease.OutCubic); }
                else { t.localPosition = Vector3.zero; t.localRotation = deitado; }
                break;
            case LocalUsado.Bancada:
                if (NaMao == e) NaMao = null;
                if (bancada == null) { e.local = LocalUsado.Chao; Posicionar(e, animar); return; }
                t.SetParent(bancada.transform, true);
                Vector3 pos = PosNaBancada;
                Quaternion rot = Quaternion.Euler(0f, 180f, 0f);   // frente (-Z do equipamento) para quem usa a bancada
                if (animar) { Tween.LocalPosition(t, pos, 0.45f, Ease.OutCubic); Tween.LocalRotation(t, rot, 0.45f, Ease.OutCubic); SomManager.TocarEm("mexer", bancada.transform.position, 0.6f, 0.8f); }
                else { t.localPosition = pos; t.localRotation = rot; }
                break;
            default:
                if (NaMao == e) NaMao = null;
                t.SetParent(null, true);
                if (e.vaga < 0 || VagaOcupada(e.vaga, e)) e.vaga = VagaLivre(e);
                Vector3 chao = PosicaoVaga(e.vaga);
                Quaternion virado = sala.rotation * Quaternion.Euler(0f, 180f + (e.id * 37 % 17 - 8), 0f);   // frente para dentro da sala
                if (animar) { Tween.Position(t, chao, 0.45f, Ease.OutCubic); Tween.Rotation(t, virado, 0.45f, Ease.OutCubic); }
                else { t.position = chao; t.rotation = virado; }
                break;
        }
    }

    // ---------- Avisos da regra ----------

    void AoMudar(EquipUsado e, string o)
    {
        if (o == "lote" || e == null) { Sincronizar(); return; }
        if (o == "saiu" || o == "frota") { Sincronizar(); return; }
        if (o == "pegar") { Pegar(e); return; }
        if (objetos.TryGetValue(e, out var obj) && obj != null) obj.visual.Animar(o, monitor);
    }

    int quadroMudou = -1;

    // Etapa 9C: [G] pega a caixa do lote ainda fechada (para levar no porta-malas)
    public bool PegarCaixa(EquipUsado e)
    {
        if (e == null || e.local != LocalUsado.Caixa) return false;
        if (CargaFisica.NaMao != null) { GameUI.Instance?.Toast("Suas mãos estão ocupadas.", UIFactory.Alerta, 2.5f); return false; }
        e.naCaixa = true;
        return Pegar(e);
    }

    // Saiu da mão para o porta-malas
    public void SoltarDaMao(EquipUsado e)
    {
        if (NaMao == e) NaMao = null;
        quadroMudou = Time.frameCount;
    }

    public bool Pegar(EquipUsado e)
    {
        if (NaMao != null && NaMao != e) { GameUI.Instance?.Toast("Suas mãos estão ocupadas: largue o que está segurando (E no chão).", UIFactory.Alerta, 3f); return false; }
        bool daBancada = e.local == LocalUsado.Bancada;
        e.local = LocalUsado.Mao;
        quadroMudou = Time.frameCount;
        if (daBancada) monitor?.Mostrar(TelaBancada.Modo.Desligada);
        SomManager.TocarEm("mexer", Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f, 0.85f);
        Posicionar(e, true);
        return true;
    }

    public bool BancadaLivre
    {
        get { foreach (var e in EquipamentosUsados.Estoque) if (e.local == LocalUsado.Bancada) return false; return true; }
    }

    public string ColocarNaBancada()
    {
        var e = NaMao;
        if (e == null) return null;
        if (e.naCaixa) return "A caixa do lote ainda está fechada: largue no chão e abra com o estilete.";
        if (!BancadaLivre) return "Já tem um equipamento na bancada. Tire ele antes.";
        e.local = LocalUsado.Bancada;
        NaMao = null;
        Posicionar(e, true);
        monitor?.Mostrar(TelaBancada.Modo.Desligada);
        return $"{e.nome} na bancada. Mire nele para testar e trabalhar.";
    }

    public void Largar()
    {
        var e = NaMao;
        if (e == null) return;
        e.local = e.naCaixa ? LocalUsado.Caixa : LocalUsado.Chao;
        e.naCaixa = false;
        e.vaga = -1;
        NaMao = null;
        Posicionar(e, true);
        SomManager.TocarEm("mexer", PosicaoVaga(e.vaga), 0.6f, 0.75f);
    }

    // Com algo na mão e nada para interagir: E larga no lugar dele no chão
    void TratarLargar()
    {
        if (NaMao == null || !Input.GetKeyDown(KeyCode.E) || Time.frameCount == quadroMudou) return;
        if (GameUI.Instance != null && GameUI.Instance.PainelAberto) return;
        if (Interactable.MaisProximoEmAlcance() != null || PecaEquipamento.Mirada() != null) return;
        // Longe do escritório não larga (o lugar dos usados é o chão do escritório): guarde no porta-malas
        var cam = Camera.main;
        if (sala != null && cam != null && Vector3.Distance(cam.transform.position, sala.position) > 12f)
        {
            GameUI.Instance?.Toast("Aqui não: leve de volta ao escritório ou guarde no porta-malas do carro.", UIFactory.Alerta, 3f);
            return;
        }
        Largar();
        GameUI.Instance?.Toast("Equipamento deixado no chão, junto do lote.", UIFactory.CorSuave, 2f);
    }

    // Lixo eletrônico com a sucata na mão
    public string DescartarNaMao()
    {
        var e = NaMao;
        if (e == null) return null;
        if (!e.testado) return "Teste antes de jogar fora: pode estar funcionando.";
        string r = EquipamentosUsados.Descartar(e);
        if (!EquipamentosUsados.Estoque.Contains(e)) NaMao = null;
        return r;
    }

    void NovoDia()
    {
        foreach (var m in EquipamentosUsados.VenderAnunciados())
            GameUI.Instance?.Toast("Comprador do anúncio passou: " + m, UIFactory.Sucesso, 5f);
    }

    public UsadoFisico Objeto(EquipUsado e) => objetos.TryGetValue(e, out var o) ? o : null;
    public TelaBancada Monitor => monitor;
    public Bancada BancadaAtual => bancada;
}

// Um equipamento usado na cena: caixa fechada, no chão, na mão ou na bancada
public class UsadoFisico : Interactable
{
    public EquipUsado e;
    public UsadoVisual visual;
    UsadosFisicos dono;
    Transform caixa;
    BoxCollider corpo;
    PecaEquipamento peca;

    public float Altura => visual != null ? visual.limites.size.y : 0.3f;
    public float Profundidade => visual != null ? visual.limites.size.z : 0.3f;

    public static UsadoFisico Criar(EquipUsado e, UsadosFisicos dono)
    {
        var go = new GameObject($"Usado_{e.id}_{e.tipo}");
        var u = go.AddComponent<UsadoFisico>();
        u.e = e;
        u.dono = dono;
        u.distanciaInteracao = 1.7f;
        u.visual = UsadoVisual.Montar(go.transform, e);
        var b = u.visual.limites;
        u.corpo = go.AddComponent<BoxCollider>();
        u.corpo.center = b.center;
        u.corpo.size = b.size;
        u.peca = PecaEquipamento.Criar(go.transform, null, AcoesBancada.Peca, "Na bancada", b.center, b.size + Vector3.one * 0.02f);
        u.peca.titulo = e.nome;
        u.peca.acoesProprias = () => AcoesBancada.Para(u.e);
        if (e.local == LocalUsado.Caixa || e.naCaixa) u.Encaixotar();
        u.AoMudarLocal();
        return u;
    }

    // Caixa de papelão do lote (o equipamento fica escondido dentro)
    void Encaixotar()
    {
        var b = visual.limites;
        Vector3 tam = new Vector3(b.size.x + 0.06f, b.size.y + 0.05f, b.size.z + 0.06f);
        caixa = new GameObject("CaixaLote").transform;
        caixa.SetParent(transform, false);
        caixa.localPosition = new Vector3(b.center.x, 0f, b.center.z);
        EncomendaFisica.CriarCaixa(caixa, tam);
        var bc = caixa.GetComponent<BoxCollider>();
        if (bc != null) Destroy(bc);   // o colisor é o do equipamento (ajustado abaixo)
        EncomendaFisica.Etiqueta(caixa, tam, "LOTE USADO\n" + Rotulo(e.tipo) + "\nno estado", false);
        visual.transform.Find("Visual")?.gameObject.SetActive(false);
        corpo.center = new Vector3(b.center.x, tam.y / 2f, b.center.z);
        corpo.size = tam;
    }

    static string Rotulo(TipoUsado t) => t == TipoUsado.Computador ? "CPU" : t == TipoUsado.Notebook ? "NOTEBOOK" : t == TipoUsado.Monitor ? "MONITOR"
                                       : t == TipoUsado.Impressora ? "IMPRESSORA" : t == TipoUsado.Roteador ? "ROTEADOR" : "SWITCH";

    // Liga/desliga colisores e o E conforme o lugar
    public void AoMudarLocal()
    {
        bool naMao = e.local == LocalUsado.Mao, naBancada = e.local == LocalUsado.Bancada, noCarro = e.local == LocalUsado.Carro;
        enabled = !naMao && !naBancada && (!noCarro || PortaMalasAberto);   // no chão/caixa/porta-malas aberto: o E é deste objeto
        if (corpo != null) { corpo.enabled = !naMao && !naBancada; corpo.isTrigger = noCarro; }
        if (peca != null) peca.gameObject.SetActive(naBancada);   // na bancada: mira nas ações
        // na mão não projeta sombra na cara do técnico nem atravessa a parede de longe
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = naMao ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
    }

    static bool PortaMalasAberto => CarroTecnico.Instance != null && CarroTecnico.Instance.TampaAberta;

    public override string TextoPrompt
    {
        get
        {
            if (e == null) return "";
            bool maos = CargaFisica.MaosOcupadas;
            if (e.local == LocalUsado.Carro) return maos ? $"{e.nome} (mãos ocupadas)" : $"[E] Tirar do porta-malas: {(e.naCaixa ? "caixa do lote (" + Rotulo(e.tipo).ToLowerInvariant() + ")" : e.nome)}";
            if (e.local == LocalUsado.Caixa) return maos ? "Caixa do lote (mãos ocupadas)" : $"[E] Abrir a caixa do lote ({Rotulo(e.tipo).ToLowerInvariant()}) com o estilete" +
                                                     (CarroTecnico.Instance != null ? "   [G] Pegar a caixa" : "");
            if (UsadosFisicos.NaMao != null) return $"{e.nome} (mãos ocupadas)";
            return $"[E] Pegar: {e.nome}  <size=80%><color=#9AA6B8>{Resumo(e)}</color></size>";
        }
    }

    public static string Resumo(EquipUsado e)
    {
        if (!e.testado) return "não testado";
        string s = e.condicao == CondicaoUsado.Bom ? "funcionando" : e.condicao == CondicaoUsado.Sucata ? "sucata" : "com defeito";
        if (e.PrecisaApagar && e.condicao != CondicaoUsado.Sucata) s += " · com dados";
        if (e.anunciado) s += " · anunciado";
        return s;
    }

    public override void Interagir()
    {
        if (e == null) return;
        if (e.local == LocalUsado.Caixa)
        {
            if (CargaFisica.MaosOcupadas) return;
            AbrirCaixa();
            return;
        }
        if (e.local == LocalUsado.Carro && !CargaFisica.MaosOcupadas)
        {
            PortaMalas.Instance?.UsadoSaiu(e);
            dono.Pegar(e);
            Debug.Log($"[Carga] Tirou do porta-malas o usado: {e.nome}.");
            return;
        }
        if (e.local == LocalUsado.Chao && !CargaFisica.MaosOcupadas) dono.Pegar(e);
    }

    // O estilete corta a fita, as abas abrem, o equipamento sobe e a caixa desmonta
    void AbrirCaixa()
    {
        e.local = LocalUsado.Chao;
        SomManager.TocarEm("mexer", transform.position, 0.8f, 1.2f);
        var vis = visual.transform.Find("Visual");
        if (vis != null)
        {
            vis.gameObject.SetActive(true);
            var final = vis.localPosition;
            vis.localPosition = final - Vector3.up * 0.15f;
            Tween.LocalPosition(vis, final, 0.6f, Ease.OutCubic);
        }
        if (caixa != null)
        {
            var c = caixa;
            caixa = null;
            Sequence.Create()
                .Chain(Tween.Scale(c, new Vector3(1.15f, 0.08f, 1.15f), 0.5f, Ease.InCubic))
                .ChainDelay(0.4f)
                .Chain(Tween.Scale(c, Vector3.zero, 0.3f))
                .OnComplete(c, x => Destroy(x.gameObject));
        }
        var b = visual.limites;
        corpo.center = b.center;
        corpo.size = b.size;
        GameUI.Instance?.Toast($"Caixa aberta: {e.nome}. Leve à bancada para testar.", UIFactory.Destaque, 3f);
    }
}
