using System.Collections.Generic;
using UnityEngine;

// Etapa 9D (vida na rua): rotas dos carros e caminhos dos pedestres, calculados a partir da planta do Bairro.
// Tudo aqui é puro (sem cena): os testes conferem que as rotas ficam no asfalto, na mão certa, e param antes dos
// cruzamentos e das faixas de pedestre.
//
// O bairro é um "H deitado": a Rua das Acácias (leste-oeste) cortada pela Ipê Amarelo e pela Jacarandás. As 6 pontas
// das ruas são a borda do mapa; os carros entram por uma ponta e saem por outra (nascem e somem fora da vista).
// Mão direita (Brasil): indo para o leste, o carro anda na metade sul das Acácias; indo para o norte, na metade leste.
public static class RotasTransito
{
    public const float DeslocFaixa = 1.6f;                 // do eixo da rua ao meio da faixa
    public const float RaioDireita = 3f, RaioEsquerda = 5f; // curvas nos cruzamentos (esquinas sem chanfro)
    public const float MeioComprimento = 1.95f, MeiaLargura = 0.85f;   // carro dos vizinhos (mesmo modelo do técnico)
    public const float VelCidade = 8f;                      // m/s (~29 km/h no bairro)
    public const float AcelLateral = 2.5f;                  // m/s² nas curvas
    public const float FolgaParada = 0.8f;                  // da frente do carro até a faixa/cruzamento quando para
    public const float MeiaFaixaPedestre = 1.5f;            // a zebra tem 3 m de largura

    public enum Ponta { AcaciasOeste, AcaciasLeste, IpeSul, IpeNorte, JacarandasSul, JacarandasNorte }
    public enum Manobra { Reto, Direita, Esquerda }
    public enum TipoParada { Cruzamento, Faixa }

    public const int CruzamentoIpe = 0, CruzamentoJacarandas = 1;   // Ipê: PARE na transversal; Jacarandás: semáforo
    public static Vector3 CentroCruzamento(int i) => new Vector3(i == CruzamentoIpe ? Bairro.IpeX : Bairro.JacarandasX, 0f, Bairro.AcaciasCentroZ);

    // Faixas de pedestre (todas atravessam a Rua das Acácias): mesma lista que o MontarCenario pinta
    public static float[] FaixasX => Bairro.FaixasPedestreX;

    public struct Parada
    {
        public TipoParada tipo;
        public int indice;          // cruzamento ou faixa
        public float s;             // distância do centro do carro no caminho quando a frente chega na linha de parada
        public float sSaida;        // cruzamento: a traseira saiu (libera a vez)
        public Vector3 chegada;     // direção de quem chega (para saber o eixo do semáforo e quem é preferencial)
        public Manobra manobra;
        public bool preferencial;   // vem pela Acácias (na Ipê, quem vem pela transversal para no PARE)
    }

    // Caminho de um carro: polilinha com distância acumulada e velocidade máxima em cada ponto
    public class Caminho
    {
        public Ponta de, para;
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

    // ---------------- Pontas e faixas ----------------

    static int Rua(Ponta p) => p <= Ponta.AcaciasLeste ? 0 : p <= Ponta.IpeNorte ? 1 : 2;   // 0 Acácias, 1 Ipê, 2 Jacarandás
    static float EixoX(int rua) => rua == 1 ? Bairro.IpeX : Bairro.JacarandasX;

    // Direção de quem entra pela ponta (para dentro do bairro)
    public static Vector3 Entrando(Ponta p)
    {
        switch (p)
        {
            case Ponta.AcaciasOeste: return Vector3.right;
            case Ponta.AcaciasLeste: return Vector3.left;
            case Ponta.IpeSul: case Ponta.JacarandasSul: return Vector3.forward;
            default: return Vector3.back;
        }
    }

    // Ponto do eixo da rua na ponta: o carro inteiro cabe antes da barreira do fim da rua
    public const float RecuoPonta = 4f;
    public static Vector3 EixoNaPonta(Ponta p)
    {
        switch (p)
        {
            case Ponta.AcaciasOeste: return new Vector3(-Bairro.LimiteX + RecuoPonta, 0f, Bairro.AcaciasCentroZ);
            case Ponta.AcaciasLeste: return new Vector3(Bairro.LimiteX - RecuoPonta, 0f, Bairro.AcaciasCentroZ);
            case Ponta.IpeSul: return new Vector3(Bairro.IpeX, 0f, Bairro.LimiteSul + RecuoPonta);
            case Ponta.IpeNorte: return new Vector3(Bairro.IpeX, 0f, Bairro.LimiteNorte - RecuoPonta);
            case Ponta.JacarandasSul: return new Vector3(Bairro.JacarandasX, 0f, Bairro.LimiteSul + RecuoPonta);
            default: return new Vector3(Bairro.JacarandasX, 0f, Bairro.LimiteNorte - RecuoPonta);
        }
    }

    // Lado direito de quem anda na direção d (mão de direção)
    public static Vector3 Direita(Vector3 d) => new Vector3(d.z, 0f, -d.x);

    // Meio da faixa de quem anda na direção d pela rua cujo eixo passa por 'eixo'
    public static Vector3 NaFaixa(Vector3 eixo, Vector3 d) => eixo + Direita(d) * DeslocFaixa;

    // Distância do centro do cruzamento até onde a frente do carro para (antes da zebra, se houver uma no caminho)
    public static float DistanciaParada(int cruzamento, Vector3 chegada)
    {
        float d = Bairro.MeiaPista + FolgaParada;
        if (Mathf.Abs(chegada.x) > 0.5f)
        {
            float cx = CentroCruzamento(cruzamento).x;
            foreach (float xf in FaixasX)
            {
                float rel = xf - cx;
                if (Mathf.Abs(rel) < 9f && Mathf.Sign(rel) == -Mathf.Sign(chegada.x))
                    d = Mathf.Max(d, Mathf.Abs(rel) + MeiaFaixaPedestre + FolgaParada);
            }
        }
        return d;
    }

    // ---------------- Rotas ----------------

    public static readonly Ponta[] Pontas = (Ponta[])System.Enum.GetValues(typeof(Ponta));

    public static List<Caminho> TodasAsRotas()
    {
        var l = new List<Caminho>();
        foreach (var a in Pontas)
            foreach (var b in Pontas)
                if (a != b) l.Add(Rota(a, b));
        return l;
    }

    struct Curva { public int cruzamento; public Vector3 d1, d2; }

    public static Caminho Rota(Ponta de, Ponta para)
    {
        var c = new Caminho { de = de, para = para };
        int r1 = Rua(de), r2 = Rua(para);
        Vector3 dIn = Entrando(de), dOut = -Entrando(para);
        var curvas = new List<Curva>();
        // cruzamentos atravessados em linha reta (para as paradas)
        var retos = new List<(int cruzamento, Vector3 d)>();

        int Cruz(int rua) => rua == 1 ? CruzamentoIpe : CruzamentoJacarandas;

        if (r1 == 0 && r2 == 0)
        {
            foreach (int k in OrdemAoLongo(dIn)) retos.Add((k, dIn));
        }
        else if (r1 == 0)
        {
            int k = Cruz(r2);
            foreach (int kk in OrdemAoLongo(dIn)) { if (kk == k) break; retos.Add((kk, dIn)); }
            curvas.Add(new Curva { cruzamento = k, d1 = dIn, d2 = dOut });
        }
        else if (r2 == 0)
        {
            curvas.Add(new Curva { cruzamento = Cruz(r1), d1 = dIn, d2 = dOut });
            int k = Cruz(r1);
            bool depois = false;
            foreach (int kk in OrdemAoLongo(dOut)) { if (depois) retos.Add((kk, dOut)); if (kk == k) depois = true; }
        }
        else if (r1 == r2)
        {
            retos.Add((Cruz(r1), dIn));
        }
        else
        {
            var meio = EixoX(r2) > EixoX(r1) ? Vector3.right : Vector3.left;
            curvas.Add(new Curva { cruzamento = Cruz(r1), d1 = dIn, d2 = meio });
            curvas.Add(new Curva { cruzamento = Cruz(r2), d1 = meio, d2 = dOut });
        }

        // Polilinha: começo, curvas (arco tangente às duas faixas) e fim
        c.Add(NaFaixa(EixoNaPonta(de), dIn), VelCidade);
        var arcos = new List<(Curva cv, Vector3 inicio, Vector3 fim, Manobra m)>();
        foreach (var cv in curvas)
        {
            Vector3 I = CentroCruzamento(cv.cruzamento);
            Vector3 canto = I + Direita(cv.d1) * DeslocFaixa + Direita(cv.d2) * DeslocFaixa;
            var m = Vector3.Dot(cv.d2, Direita(cv.d1)) > 0.5f ? Manobra.Direita : Manobra.Esquerda;
            float r = m == Manobra.Direita ? RaioDireita : RaioEsquerda;
            Vector3 centro = canto - cv.d1 * r + cv.d2 * r;
            float lim = Mathf.Sqrt(AcelLateral * r);
            const int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n * Mathf.PI / 2f;
                c.Add(centro + r * (-cv.d2 * Mathf.Cos(t) + cv.d1 * Mathf.Sin(t)), lim);
            }
            arcos.Add((cv, canto - cv.d1 * r, canto + cv.d2 * r, m));
        }
        c.Add(NaFaixa(EixoNaPonta(para), dOut), VelCidade);

        // Paradas nos cruzamentos (curvas e retos)
        foreach (var a in arcos) AddParadaCruzamento(c, a.cv.cruzamento, a.cv.d1, a.cv.d2, a.m);
        foreach (var rt in retos) AddParadaCruzamento(c, rt.cruzamento, rt.d, rt.d, Manobra.Reto);

        // Paradas nas faixas de pedestre (só nos trechos pela Acácias)
        for (int f = 0; f < FaixasX.Length; f++)
            foreach (var d in new[] { Vector3.right, Vector3.left })
            {
                var eixo = new Vector3(FaixasX[f], 0f, Bairro.AcaciasCentroZ);
                var meioZebra = NaFaixa(eixo, d);
                float sz = c.Projetar(meioZebra, out float erro);
                if (erro > 0.05f || Vector3.Dot(c.Direcao(sz), d) < 0.99f) continue;
                c.paradas.Add(new Parada
                {
                    tipo = TipoParada.Faixa, indice = f, chegada = d,
                    s = sz - MeiaFaixaPedestre - FolgaParada - MeioComprimento,
                    sSaida = sz + MeiaFaixaPedestre + MeioComprimento,
                });
            }
        c.paradas.Sort((x, y) => x.s.CompareTo(y.s));
        return c;
    }

    static void AddParadaCruzamento(Caminho c, int k, Vector3 d1, Vector3 d2, Manobra m)
    {
        Vector3 I = CentroCruzamento(k);
        float dist = DistanciaParada(k, d1);
        Vector3 linha = NaFaixa(I, d1) - d1 * dist;
        Vector3 saida = NaFaixa(I, d2) + d2 * (Bairro.MeiaPista + 0.5f);
        float sLinha = c.Projetar(linha, out _);
        float sSaida = c.Projetar(saida, out _);
        c.paradas.Add(new Parada
        {
            tipo = TipoParada.Cruzamento, indice = k, chegada = d1, manobra = m,
            s = sLinha - MeioComprimento,
            sSaida = sSaida + MeioComprimento,
            preferencial = Mathf.Abs(d1.x) > 0.5f,
        });
    }

    // Cruzamentos na ordem em que aparecem andando pela Acácias na direção d
    static int[] OrdemAoLongo(Vector3 d) => d.x > 0 ? new[] { CruzamentoIpe, CruzamentoJacarandas } : new[] { CruzamentoJacarandas, CruzamentoIpe };

    // Duas manobras no mesmo cruzamento podem acontecer juntas? (mesma chegada, ou de frente sem ninguém virar à esquerda)
    public static bool Compativeis(Vector3 chegadaA, Manobra a, Vector3 chegadaB, Manobra b)
    {
        float dot = Vector3.Dot(chegadaA, chegadaB);
        if (dot > 0.9f) return true;
        if (dot < -0.9f) return a != Manobra.Esquerda && b != Manobra.Esquerda;
        return false;
    }

    // ---------------- Semáforo da Jacarandás ----------------
    public enum Cor { Verde, Amarelo, Vermelho }
    public const float VerdeAcacias = 16f, Amarelo = 3f, TodosVermelho = 1.5f, VerdeTransversal = 10f;
    public static float Ciclo => VerdeAcacias + Amarelo + TodosVermelho + VerdeTransversal + Amarelo + TodosVermelho;

    public static Cor CorSemaforo(bool eixoAcacias, float tempo)
    {
        float t = Mathf.Repeat(tempo, Ciclo);
        float a0 = VerdeAcacias, a1 = a0 + Amarelo, a2 = a1 + TodosVermelho, b0 = a2 + VerdeTransversal, b1 = b0 + Amarelo;
        if (eixoAcacias) return t < a0 ? Cor.Verde : t < a1 ? Cor.Amarelo : Cor.Vermelho;
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
        return (c, p);
    }

    public static bool FaroisAcesos(float hora) => hora >= 17.75f || hora < 6.25f;

    // ---------------- Calçadas (pedestres) ----------------
    // Linhas a 85 cm da fachada (longe dos postes, que ficam no meio-fio). Três pedaços de bairro ligados pelas
    // faixas de pedestre: a oeste da Ipê, entre as duas transversais e a leste da Jacarandás.
    public struct NoCalcada { public Vector3 pos; public bool ponta; }
    public struct ArestaCalcada { public int a, b; public int faixa; /* -1 = calçada */ public Vector3 meioFio; /* lado da rua */ }

    public class RedeCalcadas
    {
        public readonly List<NoCalcada> nos = new List<NoCalcada>();
        public readonly List<ArestaCalcada> arestas = new List<ArestaCalcada>();
        public List<int> Vizinhas(int no)
        {
            var l = new List<int>();
            for (int i = 0; i < arestas.Count; i++) if (arestas[i].a == no || arestas[i].b == no) l.Add(i);
            return l;
        }
        public int Outro(int aresta, int no) => arestas[aresta].a == no ? arestas[aresta].b : arestas[aresta].a;
    }

    public const float AFachada = 0.85f;

    public static RedeCalcadas Calcadas()
    {
        var r = new RedeCalcadas();
        float zN = Bairro.FrenteNorteAcacias - AFachada, zS = Bairro.FrenteSulAcacias + AFachada;
        float xIO = Bairro.FrenteOesteIpe + AFachada, xIL = Bairro.FrenteLesteIpe - AFachada;
        float xJO = Bairro.FrenteOesteJacarandas + AFachada, xJL = Bairro.FrenteLesteJacarandas - AFachada;
        float xFim = Bairro.LimiteX - 1f, zNorte = Bairro.LimiteNorte - 1f, zSul = Bairro.LimiteSul + 1f;
        float y = 0.12f;
        int No(float x, float z, bool ponta = false) { r.nos.Add(new NoCalcada { pos = new Vector3(x, y, z), ponta = ponta }); return r.nos.Count - 1; }
        void Aresta(int a, int b, Vector3 meioFio, int faixa = -1) => r.arestas.Add(new ArestaCalcada { a = a, b = b, faixa = faixa, meioFio = meioFio });

        // Oeste da Ipê
        int A = No(-xFim, zN, true), B = No(xIO, zN), a = No(-xFim, zS, true), b = No(xIO, zS);
        int ipeON = No(xIO, zNorte, true), ipeOS = No(xIO, zSul, true);
        Aresta(A, B, Vector3.back); Aresta(a, b, Vector3.forward);
        Aresta(B, ipeON, Vector3.right); Aresta(b, ipeOS, Vector3.right);
        // Meio (entre a Ipê e a Jacarandás), com a faixa do escritório (x = 0)
        int C = No(xIL, zN), D = No(xJO, zN), c = No(xIL, zS), d = No(xJO, zS);
        int Fn = No(Bairro.FaixasPedestreX[1], zN), Fs = No(Bairro.FaixasPedestreX[1], zS);
        int ipeLN = No(xIL, zNorte, true), ipeLS = No(xIL, zSul, true), jacON = No(xJO, zNorte, true), jacOS = No(xJO, zSul, true);
        Aresta(C, Fn, Vector3.back); Aresta(Fn, D, Vector3.back); Aresta(c, Fs, Vector3.forward); Aresta(Fs, d, Vector3.forward);
        Aresta(C, ipeLN, Vector3.left); Aresta(c, ipeLS, Vector3.left); Aresta(D, jacON, Vector3.right); Aresta(d, jacOS, Vector3.right);
        // Leste da Jacarandás
        int E = No(xJL, zN), F = No(xFim, zN, true), e = No(xJL, zS), f = No(xFim, zS, true);
        int jacLN = No(xJL, zNorte, true), jacLS = No(xJL, zSul, true);
        Aresta(E, F, Vector3.back); Aresta(e, f, Vector3.forward);
        Aresta(E, jacLN, Vector3.left); Aresta(e, jacLS, Vector3.left);
        // Faixas de pedestre (atravessam a Acácias): perto da Ipê, do escritório e perto da Jacarandás
        Aresta(B, b, Vector3.zero, 0); Aresta(Fn, Fs, Vector3.zero, 1); Aresta(E, e, Vector3.zero, 2);
        return r;
    }
}
