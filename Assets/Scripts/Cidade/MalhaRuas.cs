using System.Collections.Generic;
using UnityEngine;

// Etapa 10E-1: malha de ruas genérica. Uma lista de ruas retas (leste-oeste ou norte-sul) com a largura do asfalto e
// das calçadas; os cruzamentos (onde uma rua leste-oeste cruza uma norte-sul), as pontas (fim de cada rua, na borda
// do mapa), as faixas de pedestre e as guias rebaixadas. Tudo o que depende das ruas lê daqui: os pedaços de asfalto e
// calçada (Bairro.Pedacos), as rotas dos carros e as calçadas dos pedestres (RotasTransito), o GPS (caminho mais curto
// no grafo das ruas) e a montagem do cenário (faixas pintadas, postes, semáforos, PAREs e barreiras).
// O bairro de hoje é Bairro.Malha; os testes também montam uma grade 3 x 3 (MalhaRuas.Grade) e uma grade com "T".
//
// Etapa 10E-2: uma rua pode começar ou terminar no eixo de outra (cruzamento em "T", no meio do mapa); só o fim que
// fica solto (na borda do mapa) vira ponta. Cada semáforo tem o seu relógio (Cruzamento.defasagem).
//
// Regras do desenho (iguais às do "H" que existia antes):
//  • a rua leste-oeste passa reta no cruzamento (o asfalto dela é inteiro); a norte-sul é cortada no asfalto dela;
//  • as calçadas da leste-oeste são cortadas só onde passa o asfalto da norte-sul (as esquinas ficam com ela);
//  • as calçadas da norte-sul são cortadas na faixa inteira da leste-oeste (asfalto + calçadas);
//  • no "T", a calçada do lado sem rua passa reta (não é cortada) e a rua que termina tem o asfalto até a borda
//    do asfalto da outra (a leste-oeste que termina numa norte-sul cobre o quadrado do cruzamento).
public enum ControleCruzamento { Pare, Semaforo }

public class Rua
{
    public string nome;
    public bool lesteOeste;              // true: corre ao longo de x; false: ao longo de z
    public float eixo;                   // z do eixo (leste-oeste) ou x do eixo (norte-sul)
    public float ini, fim;               // de onde a onde vai, ao longo da rua (x ou z)
    public float meiaPista = 3.5f;
    public float calcadaNeg = 2.5f;      // calçada do lado sul (leste-oeste) ou oeste (norte-sul)
    public float calcadaPos = 2.5f;      // calçada do lado norte ou leste
    public float[] postes;               // posições dos postes ao longo da rua (null = um a cada 12 m)
    public int[] ladosPostes = { 1, -1 };   // em que calçadas ficam (+1 norte/leste, -1 sul/oeste), nesta ordem

    public Vector3 Dir => lesteOeste ? Vector3.right : Vector3.forward;      // sentido crescente da rua
    public Vector3 Normal => lesteOeste ? Vector3.forward : Vector3.right;   // para o lado +
    public float AsfaltoNeg => eixo - meiaPista;
    public float AsfaltoPos => eixo + meiaPista;
    public float FrenteNeg => AsfaltoNeg - calcadaNeg;   // onde a calçada termina (alinhamento das fachadas)
    public float FrentePos => AsfaltoPos + calcadaPos;
    public float Asfalto(int lado) => lado < 0 ? AsfaltoNeg : AsfaltoPos;
    public float Frente(int lado) => lado < 0 ? FrenteNeg : FrentePos;

    // Ponto do mundo a 'ao' metros ao longo da rua e 'lateral' metros do eixo (lado + positivo)
    public Vector3 Ponto(float ao, float lateral = 0f) => lesteOeste ? new Vector3(ao, 0f, eixo + lateral) : new Vector3(eixo + lateral, 0f, ao);
    public float Ao(Vector3 p) => lesteOeste ? p.x : p.z;
    public float Lateral(Vector3 p) => (lesteOeste ? p.z : p.x) - eixo;
    // Retângulo de 'a0' a 'a1' ao longo da rua e de 't0' a 't1' na coordenada transversal (z ou x, absolutas)
    public Rect Area(float a0, float a1, float t0, float t1) => lesteOeste ? Rect.MinMaxRect(a0, t0, a1, t1) : Rect.MinMaxRect(t0, a0, t1, a1);

    public override string ToString() => nome;
}

public class Cruzamento
{
    public int ruaLO, ruaNS;             // índices em MalhaRuas.ruas
    public Vector3 centro;
    public ControleCruzamento controle = ControleCruzamento.Pare;
    public bool principalLesteOeste = true;   // PARE: quem vem pela outra para; semáforo: este eixo tem o verde longo
    public string nome;
    public float defasagem;              // semáforo: segundos somados ao relógio (cada cruzamento no seu tempo)
    public int Rua(bool lesteOeste) => lesteOeste ? ruaLO : ruaNS;
    // A chegada (direção de quem vem) é pela rua principal?
    public bool Principal(Vector3 chegada) => principalLesteOeste ? Mathf.Abs(chegada.x) > 0.5f : Mathf.Abs(chegada.z) > 0.5f;
}

public struct FaixaPedestre { public int rua; public float pos; }   // zebra atravessando a rua em 'pos' (ao longo dela)
public struct Ponta { public int rua; public int lado; }            // lado -1: começo da rua (ini), +1: fim
public struct Rebaixo { public int rua; public int lado; public float a0, a1; }   // calçada rebaixada (vira asfalto)

public class MalhaRuas
{
    public string nome = "malha";
    public Rect limites;                 // borda do mapa (x, z)
    public readonly List<Rua> ruas = new List<Rua>();
    public readonly List<Cruzamento> cruzamentos = new List<Cruzamento>();
    public readonly List<FaixaPedestre> faixas = new List<FaixaPedestre>();
    public readonly List<Ponta> pontas = new List<Ponta>();
    public readonly List<Rebaixo> rebaixos = new List<Rebaixo>();
    List<PedacoRua> pedacos;

    public override string ToString() => nome;

    public int AddRua(Rua r) { ruas.Add(r); pedacos = null; return ruas.Count - 1; }

    public int Indice(string nomeRua)
    {
        for (int i = 0; i < ruas.Count; i++) if (ruas[i].nome == nomeRua) return i;
        return -1;
    }

    // Calcula os cruzamentos e as pontas (chame depois de pôr as ruas). Cruzamentos na ordem: rua leste-oeste, depois
    // as norte-sul de oeste para leste.
    public MalhaRuas Fechar()
    {
        cruzamentos.Clear(); pontas.Clear(); pedacos = null;
        for (int i = 0; i < ruas.Count; i++)
        {
            if (!ruas[i].lesteOeste) continue;
            var lo = ruas[i];
            var ns = new List<int>();
            for (int j = 0; j < ruas.Count; j++)
                if (!ruas[j].lesteOeste && ruas[j].eixo >= lo.ini && ruas[j].eixo <= lo.fim && lo.eixo >= ruas[j].ini && lo.eixo <= ruas[j].fim) ns.Add(j);
            ns.Sort((a, b) => ruas[a].eixo.CompareTo(ruas[b].eixo));
            foreach (int j in ns)
                cruzamentos.Add(new Cruzamento { ruaLO = i, ruaNS = j, centro = new Vector3(ruas[j].eixo, 0f, lo.eixo), nome = $"{lo.nome} × {ruas[j].nome}" });
        }
        // Pontas: só o fim de rua que não está num cruzamento ("T")
        for (int i = 0; i < ruas.Count; i++)
        {
            if (CruzamentoNoFim(i, -1) < 0) pontas.Add(new Ponta { rua = i, lado = -1 });
            if (CruzamentoNoFim(i, 1) < 0) pontas.Add(new Ponta { rua = i, lado = 1 });
        }
        return this;
    }

    // Cruzamento em cima do começo (lado -1) ou do fim (+1) da rua: a rua termina num "T" (-1 se termina solta)
    public int CruzamentoNoFim(int rua, int lado)
    {
        var r = ruas[rua];
        float a = lado < 0 ? r.ini : r.fim;
        for (int k = 0; k < cruzamentos.Count; k++)
            if ((cruzamentos[k].ruaLO == rua || cruzamentos[k].ruaNS == rua) && Mathf.Abs(r.Ao(cruzamentos[k].centro) - a) < 0.01f) return k;
        return -1;
    }

    // A rua do cruzamento k continua na direção 'dir' (eixo x ou z)? No "T" falta um dos quatro braços.
    public bool Braco(int k, Vector3 dir)
    {
        var cz = cruzamentos[k];
        var r = ruas[cz.Rua(Mathf.Abs(dir.x) > 0.5f)];
        float c = r.Ao(cz.centro), s = Vector3.Dot(dir, r.Dir);
        return s > 0f ? r.fim > c + 0.5f : r.ini < c - 0.5f;
    }

    // Chegando ao cruzamento na direção d, vem-se por um braço que existe?
    public bool Chegada(int k, Vector3 d) => Braco(k, -d);

    public int CruzamentoDe(string ruaA, string ruaB)
    {
        int a = Indice(ruaA), b = Indice(ruaB);
        for (int k = 0; k < cruzamentos.Count; k++)
        {
            var c = cruzamentos[k];
            if ((c.ruaLO == a && c.ruaNS == b) || (c.ruaLO == b && c.ruaNS == a)) return k;
        }
        return -1;
    }

    // Cruzamentos que a rua atravessa, em ordem crescente ao longo dela
    public List<int> CruzamentosDa(int rua)
    {
        var l = new List<int>();
        for (int k = 0; k < cruzamentos.Count; k++) if (cruzamentos[k].ruaLO == rua || cruzamentos[k].ruaNS == rua) l.Add(k);
        var r = ruas[rua];
        l.Sort((a, b) => r.Ao(cruzamentos[a].centro).CompareTo(r.Ao(cruzamentos[b].centro)));
        return l;
    }

    // A outra rua do cruzamento
    public Rua Transversal(int cruzamento, int rua) => ruas[cruzamentos[cruzamento].ruaLO == rua ? cruzamentos[cruzamento].ruaNS : cruzamentos[cruzamento].ruaLO];

    // Rua por onde anda quem vai na direção d passando pelo cruzamento
    public Rua RuaNaDirecao(int cruzamento, Vector3 d) => ruas[cruzamentos[cruzamento].Rua(Mathf.Abs(d.x) > 0.5f)];

    // ---------------- Pedaços de asfalto e calçada (não se sobrepõem) ----------------

    public List<PedacoRua> Pedacos()
    {
        if (pedacos != null) return pedacos;
        var l = new List<PedacoRua>();
        for (int i = 0; i < ruas.Count; i++)
        {
            var r = ruas[i];
            var cortesAsfalto = new List<Vector2>();
            var cortesPos = new List<Vector2>();   // calçada do lado + (norte/leste)
            var cortesNeg = new List<Vector2>();
            float ini = r.ini, fim = r.fim;        // o asfalto da leste-oeste que termina num "T" cobre o cruzamento
            foreach (int k in CruzamentosDa(i))
            {
                var outra = Transversal(k, i);
                float c = r.Ao(cruzamentos[k].centro);
                // a outra rua sai para o lado + / - desta? (no "T", a calçada do lado sem rua passa reta)
                bool paraPos = Braco(k, r.Normal), paraNeg = Braco(k, -r.Normal);
                Vector2 corte;
                if (r.lesteOeste)
                {
                    corte = new Vector2(c - outra.meiaPista, c + outra.meiaPista);   // só o asfalto da transversal
                    if (Mathf.Abs(c - r.ini) < 0.01f) ini = c - outra.meiaPista;
                    if (Mathf.Abs(c - r.fim) < 0.01f) fim = c + outra.meiaPista;
                }
                else
                {
                    cortesAsfalto.Add(new Vector2(outra.AsfaltoNeg, outra.AsfaltoPos));
                    corte = new Vector2(outra.FrenteNeg, outra.FrentePos);          // a faixa inteira dela
                }
                if (paraPos) cortesPos.Add(corte);
                if (paraNeg) cortesNeg.Add(corte);
            }
            Trechos(l, i, TipoPiso.Asfalto, 0, r.AsfaltoNeg, r.AsfaltoPos, cortesAsfalto, ini, fim);
            Trechos(l, i, TipoPiso.Calcada, 1, r.AsfaltoPos, r.FrentePos, cortesPos, r.ini, r.fim);
            Trechos(l, i, TipoPiso.Calcada, -1, r.FrenteNeg, r.AsfaltoNeg, cortesNeg, r.ini, r.fim);
        }
        pedacos = l;
        return l;
    }

    void Trechos(List<PedacoRua> l, int rua, TipoPiso tipo, int lado, float t0, float t1, List<Vector2> cortes, float ini, float fim)
    {
        var r = ruas[rua];
        void Add(TipoPiso t, float a0, float a1)
        {
            if (a1 - a0 > 0.01f && t1 - t0 > 0.01f) l.Add(new PedacoRua { tipo = t, rua = r.nome, area = r.Area(a0, a1, t0, t1) });
        }
        void Trecho(float a0, float a1)
        {
            if (tipo != TipoPiso.Calcada) { Add(tipo, a0, a1); return; }
            // guia rebaixada: o pedaço dela vira asfalto no nível da rua
            var rb = rebaixos.FindAll(x => x.rua == rua && x.lado == lado && x.a1 > a0 && x.a0 < a1);
            rb.Sort((x, y) => x.a0.CompareTo(y.a0));
            float cur = a0;
            foreach (var x in rb)
            {
                Add(TipoPiso.Calcada, cur, Mathf.Max(cur, x.a0));
                Add(TipoPiso.Asfalto, Mathf.Max(cur, x.a0), Mathf.Min(a1, x.a1));
                cur = Mathf.Min(a1, x.a1);
            }
            Add(TipoPiso.Calcada, cur, a1);
        }
        cortes.Sort((x, y) => x.x.CompareTo(y.x));
        float a = ini;
        foreach (var c in cortes) { Trecho(a, Mathf.Min(c.x, fim)); a = Mathf.Max(a, c.y); }
        Trecho(a, fim);
    }

    public bool NaRua(Vector2 xz)
    {
        foreach (var p in Pedacos()) if (p.area.Contains(xz)) return true;
        return false;
    }

    public string RuaMaisProxima(Vector3 p)
    {
        string melhor = ruas.Count > 0 ? ruas[0].nome : "";
        float d = float.MaxValue;
        foreach (var r in Pedacos())
        {
            if (r.tipo != TipoPiso.Asfalto) continue;
            float dx = Mathf.Max(r.area.xMin - p.x, 0f, p.x - r.area.xMax);
            float dz = Mathf.Max(r.area.yMin - p.z, 0f, p.z - r.area.yMax);
            float dd = dx * dx + dz * dz;
            if (dd < d) { d = dd; melhor = r.rua; }
        }
        return melhor;
    }

    // Índice da rua cujo asfalto fica mais perto do ponto
    public int IndiceRuaMaisProxima(Vector3 p)
    {
        int melhor = 0;
        float d = float.MaxValue;
        for (int i = 0; i < ruas.Count; i++)
        {
            var r = ruas[i];
            float a = r.Ao(p), t = r.Lateral(p);
            float da = Mathf.Max(r.ini - a, 0f, a - r.fim), dt = Mathf.Max(Mathf.Abs(t) - r.meiaPista, 0f);
            float dd = da * da + dt * dt;
            if (dd < d) { d = dd; melhor = i; }
        }
        return melhor;
    }

    // ---------------- Faixas de pedestre ----------------

    // O ponto está em cima da zebra f? (margem > 0 aumenta, < 0 diminui)
    public bool SobreFaixa(int f, Vector3 p, float meiaFaixa, float margem)
    {
        var fx = faixas[f];
        var r = ruas[fx.rua];
        return Mathf.Abs(r.Ao(p) - fx.pos) < meiaFaixa + margem && Mathf.Abs(r.Lateral(p)) < r.meiaPista + margem;
    }

    // ---------------- Grafo das ruas (rotas e GPS) ----------------
    // Nós: os cruzamentos (0..C-1) e as pontas (C..C+P-1), no eixo das ruas; arestas entre nós vizinhos da mesma rua.

    public int NoDaPonta(int ponta) => cruzamentos.Count + ponta;

    public Vector3 PosNo(int no)
    {
        if (no < cruzamentos.Count) return cruzamentos[no].centro;
        var p = pontas[no - cruzamentos.Count];
        var r = ruas[p.rua];
        return r.Ponto(p.lado < 0 ? r.ini : r.fim);
    }

    // Nós ao longo de cada rua (ao, nó), em ordem; 'extras' são pontos soltos (rua, ao, nó) a enfiar no meio
    List<List<(float ao, int no)>> NosPorRua(List<(int rua, float ao, int no)> extras)
    {
        var l = new List<List<(float, int)>>();
        for (int i = 0; i < ruas.Count; i++)
        {
            var r = ruas[i];
            var nos = new List<(float ao, int no)>();
            for (int k = 0; k < cruzamentos.Count; k++)
                if (cruzamentos[k].ruaLO == i || cruzamentos[k].ruaNS == i) nos.Add((r.Ao(cruzamentos[k].centro), k));
            for (int p = 0; p < pontas.Count; p++)
                if (pontas[p].rua == i) nos.Add((pontas[p].lado < 0 ? r.ini : r.fim, NoDaPonta(p)));
            if (extras != null) foreach (var e in extras) if (e.rua == i) nos.Add((e.ao, e.no));
            nos.Sort((a, b) => a.ao != b.ao ? a.ao.CompareTo(b.ao) : a.no.CompareTo(b.no));
            l.Add(nos);
        }
        return l;
    }

    // Caminho mais curto (Dijkstra) entre dois nós; 'pos' dá a posição de cada nó
    static List<int> Dijkstra(int total, List<List<(float ao, int no)>> porRua, int de, int para)
    {
        var viz = new List<(int no, float custo)>[total];
        for (int i = 0; i < total; i++) viz[i] = new List<(int, float)>();
        foreach (var nos in porRua)
            for (int i = 0; i + 1 < nos.Count; i++)
            {
                float c = nos[i + 1].ao - nos[i].ao;
                viz[nos[i].no].Add((nos[i + 1].no, c));
                viz[nos[i + 1].no].Add((nos[i].no, c));
            }
        var dist = new float[total];
        var antes = new int[total];
        var feito = new bool[total];
        for (int i = 0; i < total; i++) { dist[i] = float.MaxValue; antes[i] = -1; }
        dist[de] = 0f;
        for (int passo = 0; passo < total; passo++)
        {
            int u = -1;
            for (int i = 0; i < total; i++) if (!feito[i] && dist[i] < float.MaxValue && (u < 0 || dist[i] < dist[u])) u = i;
            if (u < 0 || u == para) break;
            feito[u] = true;
            foreach (var (v, c) in viz[u])
                if (dist[u] + c < dist[v] - 1e-4f) { dist[v] = dist[u] + c; antes[v] = u; }
        }
        if (de != para && antes[para] < 0) return null;
        var l = new List<int>();
        for (int n = para; n >= 0; n = n == de ? -1 : antes[n]) l.Add(n);
        l.Reverse();
        return l;
    }

    // Nós do caminho mais curto de uma ponta a outra (para as rotas dos carros)
    public List<int> CaminhoEntrePontas(int de, int para)
    {
        int total = cruzamentos.Count + pontas.Count;
        return Dijkstra(total, NosPorRua(null), NoDaPonta(de), NoDaPonta(para));
    }

    // GPS: caminho mais curto pelas ruas de 'de' até 'para' (pontos no eixo das ruas: o primeiro é 'de' levado ao
    // eixo da rua mais perto, o último é 'para' levado ao eixo da rua dele; no meio, os cruzamentos onde virar ou passar)
    public List<Vector3> CaminhoMaisCurto(Vector3 de, Vector3 para)
    {
        int total = cruzamentos.Count + pontas.Count;
        int rA = IndiceRuaMaisProxima(de), rB = IndiceRuaMaisProxima(para);
        float aA = Mathf.Clamp(ruas[rA].Ao(de), ruas[rA].ini, ruas[rA].fim);
        float aB = Mathf.Clamp(ruas[rB].Ao(para), ruas[rB].ini, ruas[rB].fim);
        var extras = new List<(int, float, int)> { (rA, aA, total), (rB, aB, total + 1) };
        var nos = Dijkstra(total + 2, NosPorRua(extras), total, total + 1);
        var l = new List<Vector3>();
        if (nos == null) { l.Add(ruas[rA].Ponto(aA)); l.Add(ruas[rB].Ponto(aB)); return l; }
        foreach (int n in nos) l.Add(n == total ? ruas[rA].Ponto(aA) : n == total + 1 ? ruas[rB].Ponto(aB) : PosNo(n));
        return l;
    }

    // Postes a cada 12 m, longe dos cruzamentos e das zebras (ruas sem a lista feita à mão)
    public float[] PostesPadrao(int iRua)
    {
        var r = ruas[iRua];
        var l = new List<float>();
        for (float a = r.ini + 4f; a <= r.fim - 4f; a += 12f)
        {
            bool livre = true;
            foreach (int k in CruzamentosDa(iRua))
            {
                var t = Transversal(k, iRua);
                if (Mathf.Abs(a - r.Ao(cruzamentos[k].centro)) < t.meiaPista + Mathf.Max(t.calcadaNeg, t.calcadaPos) + 1f) livre = false;
            }
            foreach (var fx in faixas) if (fx.rua == iRua && Mathf.Abs(a - fx.pos) < 2.5f) livre = false;
            if (livre) l.Add(a);
        }
        return l.ToArray();
    }

    // ---------------- Faixas de pedestre em cada trecho (Etapa 10E-2) ----------------
    // Cada trecho de rua entre dois nós (cruzamento ou ponta) que ainda não tem zebra ganha uma, junto ao cruzamento de
    // baixo (oeste/sul) — ou ao de cima, se o trecho vem da borda. Assim todo quarteirão se liga aos vizinhos a pé.
    public void FaixasEmCadaTrecho()
    {
        for (int i = 0; i < ruas.Count; i++)
        {
            var r = ruas[i];
            var nos = new List<(float a, int k)>();
            if (CruzamentoNoFim(i, -1) < 0) nos.Add((r.ini, -1));
            foreach (int k in CruzamentosDa(i)) nos.Add((r.Ao(cruzamentos[k].centro), k));
            if (CruzamentoNoFim(i, 1) < 0) nos.Add((r.fim, -1));
            for (int j = 0; j + 1 < nos.Count; j++)
            {
                float a0 = nos[j].a, a1 = nos[j + 1].a;
                if (faixas.Exists(x => x.rua == i && x.pos > a0 && x.pos < a1)) continue;
                float pos = nos[j].k >= 0 ? a0 + Transversal(nos[j].k, i).meiaPista + 2f
                                          : a1 - Transversal(nos[j + 1].k, i).meiaPista - 2f;
                if (pos - a0 < 4f || a1 - pos < 4f) continue;   // trecho curto demais
                faixas.Add(new FaixaPedestre { rua = i, pos = pos });
            }
        }
    }

    // ---------------- Grade de teste ----------------

    // nx ruas norte-sul por nz ruas leste-oeste, 'quadra' metros entre os eixos, centrada na origem, com uma faixa de
    // pedestre no meio de cada quarteirão e semáforo no cruzamento do meio
    public static MalhaRuas Grade(int nx, int nz, float quadra)
    {
        float meiaX = (nx + 1) * quadra / 2f, meiaZ = (nz + 1) * quadra / 2f;
        var m = new MalhaRuas { nome = $"grade {nx}x{nz}", limites = Rect.MinMaxRect(-meiaX, -meiaZ, meiaX, meiaZ) };
        for (int j = 0; j < nz; j++)
            m.AddRua(new Rua { nome = $"Rua L{j + 1}", lesteOeste = true, eixo = -meiaZ + quadra * (j + 1), ini = -meiaX, fim = meiaX });
        for (int i = 0; i < nx; i++)
            m.AddRua(new Rua { nome = $"Rua N{i + 1}", lesteOeste = false, eixo = -meiaX + quadra * (i + 1), ini = -meiaZ, fim = meiaZ });
        m.Fechar();
        for (int r = 0; r < m.ruas.Count; r++)
        {
            var cr = m.CruzamentosDa(r);
            for (int k = 0; k + 1 < cr.Count; k++)
            {
                float a = m.ruas[r].Ao(m.cruzamentos[cr[k]].centro), b = m.ruas[r].Ao(m.cruzamentos[cr[k + 1]].centro);
                m.faixas.Add(new FaixaPedestre { rua = r, pos = (a + b) / 2f });
            }
        }
        m.cruzamentos[m.cruzamentos.Count / 2].controle = ControleCruzamento.Semaforo;
        m.FaixasEmCadaTrecho();   // e nos trechos que vão até a borda (todo quarteirão ligado a pé)
        return m;
    }

    // Grade com "T" (Etapa 10E-2): ruas que terminam no meio do mapa, no eixo de outra. 3 leste-oeste e 3 norte-sul,
    // 40 m entre os eixos, borda a 80 m da origem:
    //   A (z=-40) de borda a borda; B (z=0) de D a F; C (z=40) da borda oeste até F;
    //   D (x=-40) e F (x=40) de borda a borda; E (x=0) da borda sul até C.
    public static MalhaRuas GradeComT()
    {
        var m = new MalhaRuas { nome = "grade com T", limites = Rect.MinMaxRect(-80f, -80f, 80f, 80f) };
        m.AddRua(new Rua { nome = "Rua A", lesteOeste = true, eixo = -40f, ini = -80f, fim = 80f });
        m.AddRua(new Rua { nome = "Rua B", lesteOeste = true, eixo = 0f, ini = -40f, fim = 40f });
        m.AddRua(new Rua { nome = "Rua C", lesteOeste = true, eixo = 40f, ini = -80f, fim = 40f });
        m.AddRua(new Rua { nome = "Rua D", lesteOeste = false, eixo = -40f, ini = -80f, fim = 80f });
        m.AddRua(new Rua { nome = "Rua E", lesteOeste = false, eixo = 0f, ini = -80f, fim = 40f });
        m.AddRua(new Rua { nome = "Rua F", lesteOeste = false, eixo = 40f, ini = -80f, fim = 80f });
        m.Fechar();
        // no "T", quem passa reto é a principal
        foreach (var c in m.cruzamentos)
        {
            bool loTermina = Mathf.Abs(c.centro.x - m.ruas[c.ruaLO].ini) < 0.01f || Mathf.Abs(c.centro.x - m.ruas[c.ruaLO].fim) < 0.01f;
            if (loTermina) c.principalLesteOeste = false;
        }
        var bxe = m.cruzamentos[m.CruzamentoDe("Rua B", "Rua E")];
        bxe.controle = ControleCruzamento.Semaforo;
        bxe.defasagem = 7f;
        m.FaixasEmCadaTrecho();
        return m;
    }
}
