using System.Collections.Generic;
using UnityEngine;

// Etapa 9D (vida na rua): rotas dos carros e caminhos dos pedestres, calculados a partir da malha de ruas.
// Tudo aqui é puro (sem cena): os testes conferem que as rotas ficam no asfalto, na mão certa, e param antes dos
// cruzamentos e das faixas de pedestre.
//
// Etapa 10E-1: nada aqui conhece as ruas pelo nome. As pontas (fim de cada rua, na borda do mapa) vêm da MalhaRuas;
// os carros entram por uma ponta e saem por outra (nascem e somem fora da vista) pelo caminho mais curto no grafo das
// ruas, virando nos cruzamentos. Mão direita (Brasil): indo para o leste, o carro anda na metade sul da rua; indo para
// o norte, na metade leste. Cada cruzamento tem PARE (para quem não vem pela principal) ou semáforo.
public static class RotasTransito
{
    public const float DeslocFaixa = 1.6f;                 // do eixo da rua ao meio da faixa
    public const float RaioDireita = 3f, RaioEsquerda = 5f; // curvas nos cruzamentos (esquinas sem chanfro)
    public const float MeioComprimento = 1.95f, MeiaLargura = 0.85f;   // carro dos vizinhos (mesmo modelo do técnico)
    public const float VelCidade = 8f;                      // m/s (~29 km/h no bairro)
    public const float AcelLateral = 2.5f;                  // m/s² nas curvas
    public const float FolgaParada = 0.8f;                  // da frente do carro até a faixa/cruzamento quando para
    public const float MeiaFaixaPedestre = 1.5f;            // a zebra tem 3 m de largura

    public enum Manobra { Reto, Direita, Esquerda }
    public enum TipoParada { Cruzamento, Faixa }

    public static Vector3 CentroCruzamento(MalhaRuas m, int i) => m.cruzamentos[i].centro;

    public struct Parada
    {
        public TipoParada tipo;
        public int indice;          // cruzamento ou faixa
        public float s;             // distância do centro do carro no caminho quando a frente chega na linha de parada
        public float sSaida;        // cruzamento: a traseira saiu (libera a vez)
        public Vector3 chegada;     // direção de quem chega (para saber o eixo do semáforo e quem é preferencial)
        public Manobra manobra;
        public bool preferencial;   // vem pela rua principal do cruzamento (no PARE, quem vem pela outra para)
    }

    // Caminho de um carro: polilinha com distância acumulada e velocidade máxima em cada ponto
    public class Caminho
    {
        public int de, para;        // índices em MalhaRuas.pontas
        public readonly List<Vector3> pontos = new List<Vector3>();
        public readonly List<float> limite = new List<float>();
        public readonly List<float> acumulado = new List<float>();
        public readonly List<Parada> paradas = new List<Parada>();
        public float Comprimento => acumulado.Count == 0 ? 0f : acumulado[acumulado.Count - 1];

        int Segmento(float s)
        {
            int lo = 0, hi = acumulado.Count - 2;
            if (hi < 0) return 0;
            while (lo < hi)
            {
                int m = (lo + hi + 1) / 2;
                if (acumulado[m] <= s) lo = m; else hi = m - 1;
            }
            return lo;
        }

        public Vector3 Posicao(float s)
        {
            if (pontos.Count == 0) return Vector3.zero;
            if (s <= 0f) return pontos[0] + Direcao(0f) * s;
            if (s >= Comprimento) return pontos[pontos.Count - 1] + Direcao(Comprimento) * (s - Comprimento);
            int i = Segmento(s);
            float l = acumulado[i + 1] - acumulado[i];
            return Vector3.Lerp(pontos[i], pontos[i + 1], l < 1e-4f ? 0f : (s - acumulado[i]) / l);
        }

        public Vector3 Direcao(float s)
        {
            if (pontos.Count < 2) return Vector3.forward;
            int i = Segmento(Mathf.Clamp(s, 0f, Comprimento));
            var d = pontos[i + 1] - pontos[i];
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        public float Limite(float s)
        {
            if (limite.Count == 0) return VelCidade;
            int i = Segmento(Mathf.Clamp(s, 0f, Comprimento));
            return Mathf.Min(limite[i], limite[Mathf.Min(i + 1, limite.Count - 1)]);
        }

        // Distância no caminho do ponto da polilinha mais próximo de p (e a que distância ficou)
        public float Projetar(Vector3 p, out float erro)
        {
            erro = float.MaxValue;
            float melhor = 0f;
            for (int i = 0; i + 1 < pontos.Count; i++)
            {
                Vector3 a = pontos[i], b = pontos[i + 1], ab = b - a;
                float l2 = ab.sqrMagnitude;
                float t = l2 < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
                float d = (a + ab * t - p).magnitude;
                if (d < erro) { erro = d; melhor = acumulado[i] + t * Mathf.Sqrt(l2); }
            }
            return melhor;
        }

        internal void Add(Vector3 p, float lim)
        {
            if (pontos.Count > 0 && (p - pontos[pontos.Count - 1]).sqrMagnitude < 1e-6f) { limite[limite.Count - 1] = Mathf.Min(limite[limite.Count - 1], lim); return; }
            acumulado.Add(pontos.Count == 0 ? 0f : acumulado[acumulado.Count - 1] + (p - pontos[pontos.Count - 1]).magnitude);
            pontos.Add(p);
            limite.Add(lim);
        }
    }

    // ---------------- Pontas ----------------

    // Direção de quem entra pela ponta (para dentro do mapa)
    public static Vector3 Entrando(MalhaRuas m, int ponta)
    {
        var p = m.pontas[ponta];
        return m.ruas[p.rua].Dir * (p.lado < 0 ? 1f : -1f);
    }

    // Ponto do eixo da rua na ponta: o carro inteiro cabe antes da barreira do fim da rua
    public const float RecuoPonta = 4f;
    public static Vector3 EixoNaPonta(MalhaRuas m, int ponta)
    {
        var p = m.pontas[ponta];
        var r = m.ruas[p.rua];
        return r.Ponto(p.lado < 0 ? r.ini + RecuoPonta : r.fim - RecuoPonta);
    }

    // Lado direito de quem anda na direção d (mão de direção)
    public static Vector3 Direita(Vector3 d) => new Vector3(d.z, 0f, -d.x);

    // Meio da faixa de quem anda na direção d pela rua cujo eixo passa por 'eixo'
    public static Vector3 NaFaixa(Vector3 eixo, Vector3 d) => eixo + Direita(d) * DeslocFaixa;

    // Distância do centro do cruzamento até onde a frente do carro para (antes da zebra, se houver uma no caminho)
    public static float DistanciaParada(MalhaRuas m, int cruzamento, Vector3 chegada)
    {
        var cz = m.cruzamentos[cruzamento];
        bool lo = Mathf.Abs(chegada.x) > 0.5f;
        int iRua = cz.Rua(lo);
        var rua = m.ruas[iRua];
        float d = m.ruas[cz.Rua(!lo)].meiaPista + FolgaParada;   // antes do asfalto da rua cruzada
        float c = rua.Ao(cz.centro), sentido = Vector3.Dot(chegada, rua.Dir);
        foreach (var f in m.faixas)
        {
            if (f.rua != iRua) continue;
            float rel = f.pos - c;
            if (Mathf.Abs(rel) < 9f && Mathf.Sign(rel) == -Mathf.Sign(sentido))
                d = Mathf.Max(d, Mathf.Abs(rel) + MeiaFaixaPedestre + FolgaParada);
        }
        return d;
    }

    // O ponto p está em cima da zebra f? (margem > 0 aumenta a zebra, < 0 diminui)
    public static bool SobreFaixa(MalhaRuas m, int f, Vector3 p, float margem) => m.SobreFaixa(f, p, MeiaFaixaPedestre, margem);

    // ---------------- Rotas ----------------

    public static List<Caminho> TodasAsRotas() => TodasAsRotas(Bairro.Malha);

    public static List<Caminho> TodasAsRotas(MalhaRuas m)
    {
        var l = new List<Caminho>();
        for (int a = 0; a < m.pontas.Count; a++)
            for (int b = 0; b < m.pontas.Count; b++)
                if (a != b)
                {
                    var c = Rota(m, a, b);
                    if (c != null) l.Add(c);
                }
        return l;
    }

    struct Curva { public int cruzamento; public Vector3 d1, d2; }

    // Rota de uma ponta a outra pelo caminho mais curto no grafo das ruas; null se não há caminho
    public static Caminho Rota(MalhaRuas m, int de, int para)
    {
        var nos = m.CaminhoEntrePontas(de, para);
        if (nos == null || nos.Count < 2) return null;
        var c = new Caminho { de = de, para = para };
        Vector3 dIn = Entrando(m, de), dOut = -Entrando(m, para);
        var curvas = new List<Curva>();
        var retos = new List<(int cruzamento, Vector3 d)>();   // cruzamentos atravessados em linha reta (para as paradas)
        Vector3 Sentido(int i)   // as ruas são retas em x ou z: o sentido é exato (sem erro de arredondamento)
        {
            var v = m.PosNo(nos[i + 1]) - m.PosNo(nos[i]);
            return Mathf.Abs(v.x) > Mathf.Abs(v.z) ? new Vector3(Mathf.Sign(v.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(v.z));
        }
        for (int i = 1; i + 1 < nos.Count; i++)
        {
            Vector3 d1 = i == 1 ? dIn : Sentido(i - 1), d2 = i + 2 == nos.Count ? dOut : Sentido(i);
            if (Vector3.Dot(d1, d2) > 0.99f) retos.Add((nos[i], d1));
            else curvas.Add(new Curva { cruzamento = nos[i], d1 = d1, d2 = d2 });
        }

        // Polilinha: começo, curvas (arco tangente às duas faixas) e fim
        c.Add(NaFaixa(EixoNaPonta(m, de), dIn), VelCidade);
        var arcos = new List<(Curva cv, Manobra mb)>();
        foreach (var cv in curvas)
        {
            Vector3 I = CentroCruzamento(m, cv.cruzamento);
            Vector3 canto = I + Direita(cv.d1) * DeslocFaixa + Direita(cv.d2) * DeslocFaixa;
            var mb = Vector3.Dot(cv.d2, Direita(cv.d1)) > 0.5f ? Manobra.Direita : Manobra.Esquerda;
            float r = mb == Manobra.Direita ? RaioDireita : RaioEsquerda;
            Vector3 centro = canto - cv.d1 * r + cv.d2 * r;
            float lim = Mathf.Sqrt(AcelLateral * r);
            const int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n * Mathf.PI / 2f;
                c.Add(centro + r * (-cv.d2 * Mathf.Cos(t) + cv.d1 * Mathf.Sin(t)), lim);
            }
            arcos.Add((cv, mb));
        }
        c.Add(NaFaixa(EixoNaPonta(m, para), dOut), VelCidade);

        // Paradas nos cruzamentos (curvas e retos)
        foreach (var a in arcos) AddParadaCruzamento(m, c, a.cv.cruzamento, a.cv.d1, a.cv.d2, a.mb);
        foreach (var rt in retos) AddParadaCruzamento(m, c, rt.cruzamento, rt.d, rt.d, Manobra.Reto);

        // Paradas nas faixas de pedestre (nos trechos que passam por cima delas, no sentido da rua)
        for (int f = 0; f < m.faixas.Count; f++)
        {
            var rua = m.ruas[m.faixas[f].rua];
            foreach (var d in new[] { rua.Dir, -rua.Dir })
            {
                var meioZebra = NaFaixa(rua.Ponto(m.faixas[f].pos), d);
                float sz = c.Projetar(meioZebra, out float erro);
                if (erro > 0.05f || Vector3.Dot(c.Direcao(sz), d) < 0.99f) continue;
                c.paradas.Add(new Parada
                {
                    tipo = TipoParada.Faixa, indice = f, chegada = d,
                    s = sz - MeiaFaixaPedestre - FolgaParada - MeioComprimento,
                    sSaida = sz + MeiaFaixaPedestre + MeioComprimento,
                });
            }
        }
        c.paradas.Sort((x, y) => x.s.CompareTo(y.s));
        return c;
    }

    static void AddParadaCruzamento(MalhaRuas m, Caminho c, int k, Vector3 d1, Vector3 d2, Manobra mb)
    {
        Vector3 I = CentroCruzamento(m, k);
        float dist = DistanciaParada(m, k, d1);
        Vector3 linha = NaFaixa(I, d1) - d1 * dist;
        // sai do cruzamento depois de passar o asfalto da rua que fica atravessada à saída
        float meiaSaida = m.ruas[m.cruzamentos[k].Rua(Mathf.Abs(d2.x) <= 0.5f)].meiaPista;
        Vector3 saida = NaFaixa(I, d2) + d2 * (meiaSaida + 0.5f);
        float sLinha = c.Projetar(linha, out _);
        float sSaida = c.Projetar(saida, out _);
        c.paradas.Add(new Parada
        {
            tipo = TipoParada.Cruzamento, indice = k, chegada = d1, manobra = mb,
            s = sLinha - MeioComprimento,
            sSaida = sSaida + MeioComprimento,
            preferencial = m.cruzamentos[k].Principal(d1),
        });
    }

    // Duas manobras no mesmo cruzamento podem acontecer juntas? (mesma chegada, ou de frente sem ninguém virar à esquerda)
    public static bool Compativeis(Vector3 chegadaA, Manobra a, Vector3 chegadaB, Manobra b)
    {
        float dot = Vector3.Dot(chegadaA, chegadaB);
        if (dot > 0.9f) return true;
        if (dot < -0.9f) return a != Manobra.Esquerda && b != Manobra.Esquerda;
        return false;
    }

    // ---------------- Semáforo ----------------
    // O eixo principal do cruzamento tem o verde longo. Etapa 10E-2: cada cruzamento soma a sua defasagem ao relógio
    // (os semáforos não abrem todos ao mesmo tempo).
    public static Cor CorSemaforo(MalhaRuas m, int cruzamento, bool eixoPrincipal, float tempo) =>
        CorSemaforo(eixoPrincipal, tempo + m.cruzamentos[cruzamento].defasagem);

    public enum Cor { Verde, Amarelo, Vermelho }
    public const float VerdePrincipal = 16f, Amarelo = 3f, TodosVermelho = 1.5f, VerdeTransversal = 10f;
    public static float Ciclo => VerdePrincipal + Amarelo + TodosVermelho + VerdeTransversal + Amarelo + TodosVermelho;

    public static Cor CorSemaforo(bool eixoPrincipal, float tempo)
    {
        float t = Mathf.Repeat(tempo, Ciclo);
        float a0 = VerdePrincipal, a1 = a0 + Amarelo, a2 = a1 + TodosVermelho, b0 = a2 + VerdeTransversal, b1 = b0 + Amarelo;
        if (eixoPrincipal) return t < a0 ? Cor.Verde : t < a1 ? Cor.Amarelo : Cor.Vermelho;
        if (t < a2 || t >= b1 + TodosVermelho) return Cor.Vermelho;
        return t < b0 ? Cor.Verde : t < b1 ? Cor.Amarelo : Cor.Vermelho;
    }

    // ---------------- Movimento por hora ----------------
    // Quantos carros e pedestres circulando: pico de manhã e no fim da tarde, horário comercial cheio, noite vazia;
    // fim de semana com menos gente.
    public static (int carros, int pedestres) Lotacao(float hora, bool fimDeSemana)
    {
        int c, p;
        if (hora < 6f) { c = 1; p = 1; }
        else if (hora < 7f) { c = 3; p = 3; }
        else if (hora < 9.5f) { c = 9; p = 10; }
        else if (hora < 17f) { c = 7; p = 12; }
        else if (hora < 19.5f) { c = 10; p = 12; }
        else if (hora < 21.5f) { c = 5; p = 6; }
        else { c = 2; p = 2; }
        if (fimDeSemana) { c = Mathf.Max(1, Mathf.RoundToInt(c * 0.6f)); p = Mathf.Max(1, Mathf.RoundToInt(p * 0.6f)); }
        // Etapa 10E-2: a cidade ficou ~7x maior; mais gente circulando (quem está longe nem é desenhado)
        return (Mathf.RoundToInt(c * EscalaLotacao), Mathf.RoundToInt(p * EscalaLotacao));
    }

    public const float EscalaLotacao = 2.5f;

    public static bool FaroisAcesos(float hora) => hora >= 17.75f || hora < 6.25f;

    // ---------------- Calçadas (pedestres) ----------------
    // Linhas a 85 cm da fachada (longe dos postes, que ficam no meio-fio), dos dois lados de cada rua: a linha é cortada
    // onde sai uma transversal para aquele lado e termina na linha da transversal (a quina é um nó comum às duas) ou,
    // na borda do mapa, numa ponta (onde o pedestre nasce e some). Os dois lados de uma rua se ligam só pelas faixas.
    // Etapa 10E-2: lido rua a rua (não mais por quarteirão), para servir no "T" — o lado sem transversal passa reto.
    public struct NoCalcada { public Vector3 pos; public bool ponta; }
    public struct ArestaCalcada { public int a, b; public int faixa; /* -1 = calçada */ public Vector3 meioFio; /* lado da rua */ }

    public class RedeCalcadas
    {
        public readonly List<NoCalcada> nos = new List<NoCalcada>();
        public readonly List<ArestaCalcada> arestas = new List<ArestaCalcada>();
        List<int>[] vizinhas;
        public List<int> Vizinhas(int no)
        {
            if (vizinhas == null || vizinhas.Length != nos.Count)
            {
                vizinhas = new List<int>[nos.Count];
                for (int i = 0; i < nos.Count; i++) vizinhas[i] = new List<int>();
                for (int i = 0; i < arestas.Count; i++) { vizinhas[arestas[i].a].Add(i); vizinhas[arestas[i].b].Add(i); }
            }
            return new List<int>(vizinhas[no]);
        }
        public int Outro(int aresta, int no) => arestas[aresta].a == no ? arestas[aresta].b : arestas[aresta].a;
    }

    public const float AFachada = 0.85f;
    const float GrudarNaQuina = 1f;   // zebra a menos disso da quina sai da própria quina
    const float RecuoBorda = 1f;      // a ponta da calçada fica 1 m antes da borda do mapa

    public static RedeCalcadas Calcadas() => Calcadas(Bairro.Malha);

    public static RedeCalcadas Calcadas(MalhaRuas m)
    {
        var r = new RedeCalcadas();
        const float y = 0.12f;
        var porPos = new Dictionary<(int, int), int>();
        var noDaFaixa = new Dictionary<(int, int), int>();   // (faixa, lado da rua) → nó
        int No(Vector3 p, bool ponta)
        {
            var k = (Mathf.RoundToInt(p.x * 100f), Mathf.RoundToInt(p.z * 100f));
            if (porPos.TryGetValue(k, out int i)) { if (ponta) r.nos[i] = new NoCalcada { pos = r.nos[i].pos, ponta = true }; return i; }
            r.nos.Add(new NoCalcada { pos = new Vector3(p.x, y, p.z), ponta = ponta });
            porPos[k] = r.nos.Count - 1;
            return r.nos.Count - 1;
        }

        // Caminho na calçada do lado 'lado' da rua (+1 norte/leste, -1 sul/oeste), de a0 a a1 ao longo dela
        void Lado(int iRua, int lado, float a0, float a1, bool ponta0, bool ponta1)
        {
            if (a1 - a0 < 0.01f) return;
            var rua = m.ruas[iRua];
            float t = lado > 0 ? rua.FrentePos - AFachada : rua.FrenteNeg + AFachada;
            Vector3 meioFio = -lado * rua.Normal;
            var pontos = new List<(float a, int faixa)> { (a0, -1), (a1, -1) };
            for (int f = 0; f < m.faixas.Count; f++)
            {
                if (m.faixas[f].rua != iRua) continue;
                float a = m.faixas[f].pos;
                if (Mathf.Abs(a - a0) < GrudarNaQuina) a = a0;
                else if (Mathf.Abs(a - a1) < GrudarNaQuina) a = a1;
                if (a < a0 || a > a1) continue;
                pontos.Add((a, f));
            }
            pontos.Sort((x, z) => x.a.CompareTo(z.a));
            int anterior = -1;
            float aAnterior = 0f;
            foreach (var (a, f) in pontos)
            {
                Vector3 p = rua.lesteOeste ? new Vector3(a, 0f, t) : new Vector3(t, 0f, a);
                int no = No(p, (a == a0 && ponta0) || (a == a1 && ponta1));
                if (f >= 0) noDaFaixa[(f, lado)] = no;
                if (anterior >= 0 && a - aAnterior > 0.01f) r.arestas.Add(new ArestaCalcada { a = anterior, b = no, faixa = -1, meioFio = meioFio });
                anterior = no; aAnterior = a;
            }
        }

        for (int i = 0; i < m.ruas.Count; i++)
        {
            var rua = m.ruas[i];
            var cruz = m.CruzamentosDa(i);
            int kIni = m.CruzamentoNoFim(i, -1), kFim = m.CruzamentoNoFim(i, 1);
            foreach (int lado in new[] { -1, 1 })
            {
                // começo: na borda (ponta) ou na linha da calçada do lado de lá da rua onde esta nasce ("T")
                float a0 = kIni >= 0 ? m.Transversal(kIni, i).FrentePos - AFachada : rua.ini + RecuoBorda;
                float a1 = kFim >= 0 ? m.Transversal(kFim, i).FrenteNeg + AFachada : rua.fim - RecuoBorda;
                float cur = a0;
                bool pontaCur = kIni < 0;
                foreach (int k in cruz)
                {
                    if (k == kIni || k == kFim) continue;
                    if (!m.Braco(k, lado * rua.Normal)) continue;   // o lado sem transversal passa reto
                    var o = m.Transversal(k, i);
                    Lado(i, lado, cur, o.FrenteNeg + AFachada, pontaCur, false);
                    cur = o.FrentePos - AFachada;
                    pontaCur = false;
                }
                Lado(i, lado, cur, a1, pontaCur, kFim < 0);
            }
        }
        // Travessias: a zebra liga as duas calçadas da rua
        for (int f = 0; f < m.faixas.Count; f++)
            if (noDaFaixa.TryGetValue((f, 1), out int n1) && noDaFaixa.TryGetValue((f, -1), out int n2))
                r.arestas.Add(new ArestaCalcada { a = n1, b = n2, faixa = f, meioFio = Vector3.zero });
        return r;
    }
}
