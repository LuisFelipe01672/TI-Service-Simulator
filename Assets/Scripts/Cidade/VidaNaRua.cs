using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Etapa 9D: vida na rua — carros dos vizinhos (CarroNPC) e pedestres (Pedestre) circulando pelo bairro.
// Este gerente se cria sozinho quando a cena tem o molde do carro (montado pelo MontarCenario); cuida de:
//   • quantos carros/pedestres circulam (pela hora do DiaManager: pico de manhã e à tarde, noite vazia);
//   • nascer e sumir só fora da vista do jogador (nas pontas das ruas, que são a borda do mapa);
//   • tudo lido da malha de ruas (Bairro.Malha; Etapa 10E-1): rotas, cruzamentos, faixas e calçadas;
//   • obstáculos (outros carros, pedestres, o técnico a pé, o carro do técnico), a vez nos cruzamentos,
//     o ciclo do semáforo e as faixas de pedestre ocupadas;
//   • faróis de verdade só nos carros mais perto da câmera (o resto acende só a lâmpada).
public class VidaNaRua : MonoBehaviour
{
    public static VidaNaRua Instance { get; private set; }
    public static bool Desligada;            // testes de FPS/atalhos podem desligar
    public static int? CarrosFixos, PedestresFixos;   // atalhos de teste fixam a lotação

    public const float DistanciaVisivel = 75f, DistanciaPedestre = 55f;   // além disso não desenha

    public enum TipoObstaculo { Nenhum, Carro, CarroJogador, Pedestre, Jogador }

    struct Obstaculo { public Vector3 c; public float r; public TipoObstaculo tipo; public Object dono; }

    public readonly List<CarroNPC> carros = new List<CarroNPC>();
    public readonly List<Pedestre> pedestres = new List<Pedestre>();
    readonly List<CarroNPC> carrosLivres = new List<CarroNPC>();
    readonly List<Pedestre> pedestresLivres = new List<Pedestre>();
    readonly List<Obstaculo> obst = new List<Obstaculo>();
    // Vez nos cruzamentos e faixas ocupadas: um por cruzamento/faixa da malha (Etapa 10E-1)
    List<(CarroNPC carro, Vector3 chegada, RotasTransito.Manobra m)>[] reservas;
    int[] faixaOcupada;
    MalhaRuas malha;

    // Malha de ruas por onde este gerente anda (o bairro; os testes podem trocar)
    public MalhaRuas Malha { get { if (malha == null) UsarMalha(Bairro.Malha); return malha; } }

    public void UsarMalha(MalhaRuas m)
    {
        malha = m;
        reservas = new List<(CarroNPC, Vector3, RotasTransito.Manobra)>[m.cruzamentos.Count];
        for (int i = 0; i < reservas.Length; i++) reservas[i] = new List<(CarroNPC, Vector3, RotasTransito.Manobra)>();
        faixaOcupada = new int[m.faixas.Count];
    }

    List<RotasTransito.Caminho> rotas;
    public RotasTransito.RedeCalcadas calcadas;
    List<Rect> pisoCalcada;
    CarroNPC molde;
    Transform raizCarros, raizPedestres;
    PlayerController jogador;
    float proximoNascer, proximoFarol, proximaBusca;
    int proximoId;
    public static float TempoSemaforo => Time.time + 6f;   // começa no meio do verde da Acácias
    public bool Noite { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
        CriarSePrecisar();
    }

    static void AoCarregar(Scene s, LoadSceneMode m) => CriarSePrecisar();

    static void CriarSePrecisar()
    {
        if (Instance != null) return;
        var molde = Object.FindFirstObjectByType<CarroNPC>(FindObjectsInactive.Include);
        if (molde == null) return;
        var go = new GameObject("VidaNaRua");
        var v = go.AddComponent<VidaNaRua>();
        v.molde = molde;
    }

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        rotas = RotasTransito.TodasAsRotas(Malha);
        calcadas = RotasTransito.Calcadas(Malha);
        pisoCalcada = new List<Rect>();
        foreach (var p in Malha.Pedacos()) if (p.tipo == TipoPiso.Calcada) pisoCalcada.Add(p.area);
        raizCarros = new GameObject("Carros_Vizinhos").transform;
        raizCarros.SetParent(transform, false);
        raizPedestres = new GameObject("Pedestres").transform;
        raizPedestres.SetParent(transform, false);
        AtualizarNoite();
        Povoar();
    }

    // ---------------- Lotação ----------------

    public (int carros, int pedestres) Alvo()
    {
        var d = DiaManager.Instance;
        var l = d == null ? RotasTransito.Lotacao(12f, false) : RotasTransito.Lotacao(d.Hora, d.FimDeSemana);
        if (CarrosFixos.HasValue) l.carros = CarrosFixos.Value;
        if (PedestresFixos.HasValue) l.pedestres = PedestresFixos.Value;
        return l;
    }

    void AtualizarNoite()
    {
        var d = DiaManager.Instance;
        Noite = d != null && RotasTransito.FaroisAcesos(d.Hora);
    }

    // Começo do jogo/carregar: já põe gente no meio das ruas (longe da vista)
    public void Povoar()
    {
        var alvo = Alvo();
        for (int tent = 0; tent < 60 && carros.Count < alvo.carros; tent++)
        {
            var c = rotas[Random.Range(0, rotas.Count)];
            float s = Random.Range(8f, c.Comprimento - 8f);
            if (DentroDeCruzamento(c, s) || !LugarLivre(c.Posicao(s), 9f) || Visivel(c.Posicao(s))) continue;
            NovoCarro(c, s);
        }
        for (int tent = 0; tent < 60 && pedestres.Count < alvo.pedestres; tent++)
        {
            int a = Random.Range(0, calcadas.arestas.Count);
            var ar = calcadas.arestas[a];
            if (ar.faixa >= 0) continue;
            var p = Vector3.Lerp(calcadas.nos[ar.a].pos, calcadas.nos[ar.b].pos, Random.value);
            if (Visivel(p)) continue;
            NovoPedestre(a, Random.value < 0.5f, p);
        }
    }

    // Tira todo mundo (atalhos de teste) e põe de novo
    public void Repovoar()
    {
        foreach (var c in carros.ToArray()) Recolher(c);
        foreach (var p in pedestres.ToArray()) Recolher(p);
        Povoar();
    }

    static bool DentroDeCruzamento(RotasTransito.Caminho c, float s)
    {
        foreach (var p in c.paradas)
            if (p.tipo == RotasTransito.TipoParada.Cruzamento && s > p.s - 3f && s < p.sSaida + 1f) return true;
        return false;
    }

    bool LugarLivre(Vector3 p, float raio)
    {
        foreach (var c in carros) if ((c.transform.position - p).sqrMagnitude < raio * raio) return false;
        var ct = CarroTecnico.Instance;
        if (ct != null && (ct.transform.position - p).sqrMagnitude < raio * raio) return false;
        if (jogador != null && (jogador.transform.position - p).sqrMagnitude < 36f) return false;
        return true;
    }

    // O jogador está vendo este ponto? (dentro da câmera, a menos de 45 m e sem parede no meio)
    public bool Visivel(Vector3 p)
    {
        var cam = Camera.main;
        if (cam == null) return false;
        Vector3 alto = p + Vector3.up * 1.2f;
        float dist = (alto - cam.transform.position).magnitude;
        if (dist > 45f) return false;
        if (dist < 8f) return true;
        var vp = cam.WorldToViewportPoint(alto);
        if (vp.z <= 0f || vp.x < -0.15f || vp.x > 1.15f || vp.y < -0.2f || vp.y > 1.2f) return false;
        return !Physics.Linecast(cam.transform.position, alto, ~0, QueryTriggerInteraction.Ignore);
    }

    CarroNPC NovoCarro(RotasTransito.Caminho c, float s)
    {
        CarroNPC n;
        if (carrosLivres.Count > 0) { n = carrosLivres[carrosLivres.Count - 1]; carrosLivres.RemoveAt(carrosLivres.Count - 1); }
        else
        {
            n = Instantiate(molde, raizCarros);
            n.name = "Carro_Vizinho";
        }
        n.Iniciar(c, s, ++proximoId);
        n.gameObject.SetActive(true);
        n.enabled = false;   // quem move é o gerente (ordem previsível)
        carros.Add(n);
        return n;
    }

    Pedestre NovoPedestre(int aresta, bool sentidoAB, Vector3 pos)
    {
        Pedestre p;
        if (pedestresLivres.Count > 0) { p = pedestresLivres[pedestresLivres.Count - 1]; pedestresLivres.RemoveAt(pedestresLivres.Count - 1); }
        else
        {
            var go = new GameObject("Pedestre");
            go.transform.SetParent(raizPedestres, false);
            p = go.AddComponent<Pedestre>();
            p.Montar(proximoId++);
        }
        p.gameObject.SetActive(true);
        p.Iniciar(this, aresta, sentidoAB, pos);
        pedestres.Add(p);
        return p;
    }

    public void Recolher(CarroNPC c)
    {
        Liberar(c);
        carros.Remove(c);
        c.gameObject.SetActive(false);
        carrosLivres.Add(c);
    }

    public void Recolher(Pedestre p)
    {
        p.SoltarFaixa(this);
        pedestres.Remove(p);
        p.gameObject.SetActive(false);
        pedestresLivres.Add(p);
    }

    // ---------------- Laço ----------------

    void Update()
    {
        if (Desligada)
        {
            if (carros.Count > 0 || pedestres.Count > 0) { foreach (var c in carros.ToArray()) Recolher(c); foreach (var p in pedestres.ToArray()) Recolher(p); }
            return;
        }
        if (Time.time > proximaBusca || jogador == null) { jogador = FindFirstObjectByType<PlayerController>(); proximaBusca = Time.time + 3f; AtualizarNoite(); }

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        AtualizarObstaculos();
        foreach (var c in carros) c.Passo(dt, this);
        for (int i = pedestres.Count - 1; i >= 0; i--) pedestres[i].Passo(dt, this);   // pode sair da lista (trocou de ponta)

        // Fim da rota: some fora da vista (ou depois de esperar um pouco)
        for (int i = carros.Count - 1; i >= 0; i--)
        {
            var c = carros[i];
            if (c.chegouNoFim && (!Visivel(c.transform.position) || c.tempoNoFim > 8f)) Recolher(c);
        }

        if (Time.time > proximoNascer)
        {
            proximoNascer = Time.time + 0.8f;
            NascerSePrecisar();
        }
        if (Time.time > proximoFarol)
        {
            proximoFarol = Time.time + 0.25f;
            FaroisPerto();
        }
    }

    void NascerSePrecisar()
    {
        var alvo = Alvo();
        if (carros.Count < alvo.carros)
        {
            int de = Random.Range(0, Malha.pontas.Count);
            // mais carro seguindo reto pela mesma rua do que virando
            var opcoes = rotas.FindAll(r => r.de == de);
            if (opcoes.Count == 0) return;
            var c = opcoes[Random.Range(0, opcoes.Count)];
            if (Random.value < 0.35f) { var reta = opcoes.Find(r => Malha.pontas[r.para].rua == Malha.pontas[de].rua); if (reta != null) c = reta; }
            var p0 = c.Posicao(0f);
            if (LugarLivre(p0, 10f) && !Visivel(p0)) NovoCarro(c, 0f);
        }
        if (pedestres.Count < alvo.pedestres)
        {
            // nasce numa ponta de calçada (borda do mapa) fora da vista
            var pontas = new List<int>();
            for (int i = 0; i < calcadas.nos.Count; i++) if (calcadas.nos[i].ponta && !Visivel(calcadas.nos[i].pos)) pontas.Add(i);
            if (pontas.Count > 0)
            {
                int no = pontas[Random.Range(0, pontas.Count)];
                int a = calcadas.Vizinhas(no)[0];
                NovoPedestre(a, calcadas.arestas[a].a == no, calcadas.nos[no].pos);
            }
        }
    }

    // Pedestre chegou na ponta da calçada: se ninguém está vendo, vai para outra ponta (ou some, se sobra gente)
    public bool TrocarDePonta(Pedestre p, int no)
    {
        if (Visivel(calcadas.nos[no].pos)) return false;
        if (pedestres.Count > Alvo().pedestres) { Recolher(p); return true; }
        var pontas = new List<int>();
        for (int i = 0; i < calcadas.nos.Count; i++) if (i != no && calcadas.nos[i].ponta && !Visivel(calcadas.nos[i].pos)) pontas.Add(i);
        if (pontas.Count == 0) return false;
        int n = pontas[Random.Range(0, pontas.Count)];
        int a = calcadas.Vizinhas(n)[0];
        p.Iniciar(this, a, calcadas.arestas[a].a == n, calcadas.nos[n].pos);
        return true;
    }

    void FaroisPerto()
    {
        var cam = Camera.main;
        Vector3 o = cam != null ? cam.transform.position : Vector3.zero;
        carros.Sort((a, b) => (a.transform.position - o).sqrMagnitude.CompareTo((b.transform.position - o).sqrMagnitude));
        for (int i = 0; i < carros.Count; i++)
        {
            float d2 = (carros[i].transform.position - o).sqrMagnitude;
            carros[i].FarolDeVerdade(Noite && i < 3 && d2 < 40f * 40f);
            carros[i].Mostrar(d2 < DistanciaVisivel * DistanciaVisivel);
        }
        foreach (var p in pedestres) p.Mostrar((p.transform.position - o).sqrMagnitude < DistanciaPedestre * DistanciaPedestre);
    }

    // ---------------- Obstáculos ----------------

    public void AtualizarObstaculos()
    {
        obst.Clear();
        foreach (var c in carros)
        {
            var t = c.transform; Vector3 f = t.forward;
            for (int k = -1; k <= 1; k++) obst.Add(new Obstaculo { c = t.position + f * (1.25f * k), r = 0.9f, tipo = TipoObstaculo.Carro, dono = c });
        }
        var ct = CarroTecnico.Instance;
        if (ct != null && ct.gameObject.activeInHierarchy)
        {
            var t = ct.transform; Vector3 f = t.forward;
            for (int k = -1; k <= 1; k++) obst.Add(new Obstaculo { c = t.position + f * (1.25f * k), r = 0.95f, tipo = TipoObstaculo.CarroJogador, dono = ct });
        }
        foreach (var p in pedestres) obst.Add(new Obstaculo { c = p.transform.position, r = 0.3f, tipo = TipoObstaculo.Pedestre, dono = p });
        if (jogador != null && !CarroTecnico.NoCarro) obst.Add(new Obstaculo { c = jogador.transform.position, r = 0.35f, tipo = TipoObstaculo.Jogador, dono = jogador });
    }

    CarroNPC ultimoBloqueador;

    // Distância livre à frente do carro (a partir do para-choque, seguindo a rota), até 'max'
    public float LivreAte(CarroNPC eu, float max, bool ignorarCarros, out TipoObstaculo quem)
    {
        quem = TipoObstaculo.Nenhum;
        ultimoBloqueador = null;
        eu.bloqueador = null;
        var cam = eu.caminho;
        float meia = RotasTransito.MeiaLargura + 0.15f;
        for (float k = 0f; k <= max; k += 1f)
        {
            float sk = eu.s + RotasTransito.MeioComprimento + k;
            Vector3 d = cam.Direcao(sk);
            Vector3 p = cam.Posicao(sk) + RotasTransito.Direita(d) * eu.lateral;
            foreach (var o in obst)
            {
                if (o.dono == eu) continue;
                if (ignorarCarros && o.tipo == TipoObstaculo.Carro) continue;
                if (Mathf.Abs(o.c.y - p.y) > 2.5f) continue;
                float dx = o.c.x - p.x, dz = o.c.z - p.z, lim = meia + o.r;
                // carro na contramão: as laterais não se tocam se os eixos estão a mais de 1,8 m (passam rente)
                if (o.tipo == TipoObstaculo.Carro && Vector3.Dot(((CarroNPC)o.dono).transform.forward, d) < -0.7f) lim = 2f * RotasTransito.MeiaLargura + 0.1f;
                if (dx * dx + dz * dz < lim * lim)
                {
                    quem = o.tipo;
                    ultimoBloqueador = o.dono as CarroNPC;
                    eu.bloqueador = ultimoBloqueador;
                    return k;
                }
            }
        }
        return max + 5f;
    }

    // Dois carros um esperando o outro (raro, numa curva fechada): o de id menor passa por cima da regra
    public bool Travados(CarroNPC eu)
    {
        var b = eu.bloqueador;
        return b != null && b.bloqueador == eu && b.v < 0.3f && eu.id < b.id;   // um esperando o outro
    }

    public bool PodeDesviar(CarroNPC eu)
    {
        Vector3 p = eu.transform.position, f = eu.transform.forward;
        // só passa por carro parado e encostado no meio-fio (não ultrapassa quem está andando ou parado no meio)
        var ct = CarroTecnico.Instance;
        if (ct == null || Mathf.Abs(ct.Velocidade) > 0.2f || Vector3.Dot(ct.transform.position - p, RotasTransito.Direita(f)) < 0.4f) return false;
        foreach (var cz in Malha.cruzamentos)
            if ((p - cz.centro).sqrMagnitude < 13f * 13f) return false;
        foreach (var c in carros)
        {
            if (c == eu) continue;
            Vector3 rel = c.transform.position - p;
            float frente = Vector3.Dot(rel, f), lado = Mathf.Abs(Vector3.Dot(rel, RotasTransito.Direita(f)));
            if (Vector3.Dot(c.transform.forward, f) < -0.7f && c.v > 0.5f && frente > -5f && frente < 28f && lado < 5f) return false;   // vindo na contramão (parado, dá para passar rente)
        }
        return true;
    }

    // ---------------- Cruzamentos ----------------

    public void Reservar(CarroNPC c, RotasTransito.Parada p)
    {
        _ = Malha;
        Liberar(c);
        reservas[p.indice].Add((c, p.chegada, p.manobra));
    }

    public void Liberar(CarroNPC c)
    {
        if (reservas == null) return;
        foreach (var r in reservas) r.RemoveAll(x => x.carro == c);
    }

    public bool CruzamentoLivrePara(CarroNPC c, RotasTransito.Parada p)
    {
        _ = Malha;
        foreach (var r in reservas[p.indice])
            if (r.carro != c && !RotasTransito.Compativeis(p.chegada, p.manobra, r.chegada, r.m)) return false;
        return true;
    }

    public int Reservas(int cruzamento) { _ = Malha; return reservas[cruzamento].Count; }

    // Algum carro da preferencial (rua principal do cruzamento) chegando neste cruzamento a menos de 'dist'?
    public bool PreferencialChegando(int cruzamento, float dist)
    {
        foreach (var c in carros)
            foreach (var p in c.caminho.paradas)
            {
                if (p.tipo != RotasTransito.TipoParada.Cruzamento || p.indice != cruzamento || !p.preferencial) continue;
                float d = p.s - c.s;
                if (d > -0.5f && d < dist && (c.v > 1f || d < 4f)) return true;
            }
        var ct = CarroTecnico.Instance;
        if (ct != null && Mathf.Abs(ct.Velocidade) > 1f)
        {
            var cz = Malha.cruzamentos[cruzamento];
            var principal = Malha.ruas[cz.Rua(cz.principalLesteOeste)];
            Vector3 rel = ct.transform.position - cz.centro;
            float ao = Vector3.Dot(rel, principal.Dir), lado = Vector3.Dot(rel, principal.Normal);
            if (Mathf.Abs(lado) < 6f && Mathf.Abs(ao) < dist && Vector3.Dot(ct.transform.forward * Mathf.Sign(ct.Velocidade), -rel) > 0f) return true;
        }
        return false;
    }

    // ---------------- Faixas de pedestre ----------------

    public bool FaixaOcupada(int f)
    {
        _ = Malha;
        if (faixaOcupada[f] > 0) return true;
        if (jogador != null && !CarroTecnico.NoCarro && NaFaixa(f, jogador.transform.position, 0.3f)) return true;
        return false;
    }

    public bool NaFaixa(int f, Vector3 p, float margem) => RotasTransito.SobreFaixa(Malha, f, p, margem);

    // Pedestre pode começar a atravessar? (nenhum carro em cima da faixa nem chegando perto demais para parar)
    public bool PodeAtravessar(int f)
    {
        foreach (var c in carros)
            foreach (var p in c.caminho.paradas)
            {
                if (p.tipo != RotasTransito.TipoParada.Faixa || p.indice != f) continue;
                float d = p.s - c.s;
                if (d < 0f && c.s < p.sSaida) return false;                       // em cima da faixa
                if (d >= 0f && d < c.v * c.v / (2f * 4f) + 3f && c.v > 0.3f) return false;   // não dá para parar
                if (d >= 0f && d < 1.5f && c.v > 0.05f) return false;
            }
        var ct = CarroTecnico.Instance;
        if (ct != null && Mathf.Abs(ct.Velocidade) > 1f)
        {
            var fx = Malha.faixas[f];
            var r = Malha.ruas[fx.rua];
            Vector3 pos = ct.transform.position;
            if (Mathf.Abs(r.Lateral(pos)) < 5f && Mathf.Abs(r.Ao(pos) - fx.pos) < 16f) return false;
        }
        return true;
    }

    public void OcuparFaixa(int f) { _ = Malha; faixaOcupada[f]++; }
    public void SoltarFaixa(int f) { _ = Malha; faixaOcupada[f] = Mathf.Max(0, faixaOcupada[f] - 1); }

    // ---------------- Pedestres ----------------

    // Altura do chão: calçada 12 cm acima do asfalto
    public float AlturaChao(Vector3 p)
    {
        var xz = new Vector2(p.x, p.z);
        foreach (var r in pisoCalcada) if (r.Contains(xz)) return 0.12f;
        return 0f;
    }

    // Outros pedestres e o jogador perto de 'p' (para desviar)
    public void Vizinhos(Pedestre eu, List<(Vector3 c, float r, bool jogador)> saida)
    {
        saida.Clear();
        Vector3 p = eu.transform.position;
        foreach (var o in pedestres)
            if (o != eu && (o.transform.position - p).sqrMagnitude < 9f) saida.Add((o.transform.position, 0.3f, false));
        if (jogador != null && !CarroTecnico.NoCarro && (jogador.transform.position - p).sqrMagnitude < 9f)
            saida.Add((jogador.transform.position, 0.4f, true));
        // orelhão na calçada perto do escritório
        var orelhao = Bairro.Orelhao;
        if ((orelhao - p).sqrMagnitude < 9f) saida.Add((orelhao, 0.6f, false));
    }

    // Carro (NPC ou do técnico) a menos de 'dist' do pedestre? (quem atravessa também não anda em cima de carro)
    public bool CarroPerto(Vector3 p, float dist)
    {
        foreach (var o in obst)
            if ((o.tipo == TipoObstaculo.Carro || o.tipo == TipoObstaculo.CarroJogador) && (o.c - p).sqrMagnitude < (dist + o.r) * (dist + o.r)) return true;
        return false;
    }
}
