using System.Collections.Generic;
using UnityEngine;

// Planta do bairro (Etapa 9B): ruas, calçadas, lotes e endereços. É a fonte única das posições — o
// MontarCenario constrói a partir daqui, o GPS do celular e os testes da planta leem daqui.
// Tudo é constante ou propriedade (=>): nada de static readonly que dependa de outro arquivo (a ordem de
// inicialização entre classes parciais já colocou o banheiro no meio da rua uma vez).
//
// Coordenadas do mundo, em metros: x de oeste (-) a leste (+), z de sul (-) a norte (+).
// O escritório do jogador fica na origem, com a porta para o sul, na Rua das Acácias (leste-oeste).
// Duas ruas cortam a das Acácias: a Rua Ipê Amarelo (a oeste) e a Rua dos Jacarandás (a leste).
//
//            Ipê Amarelo          Jacarandás
//     (clínica) |   (escritório)       |  (contabilidade)
//   ============+======================+============  Rua das Acácias
//     (coleta)  |   (casa)             |  (loja / fornecedor)
public enum TipoEndereco { Casa, Escritorio, Fornecedor, Coleta, Contabilidade, Clinica, Loja, Posto }

public struct Endereco
{
    public TipoEndereco tipo;
    public string nome;     // como aparece no GPS ("Sua casa", "Distribuidora Ponto Info"...)
    public string rua;
    public int numero;
    public Vector3 porta;   // na calçada, na frente da porta (onde o GPS diz "você chegou")

    public string Linha => $"{rua}, {numero}";
}

public enum TipoPiso { Asfalto, Calcada }

public struct PedacoRua
{
    public TipoPiso tipo;
    public Rect area;       // no plano x-z (Rect.y = z)
    public string rua;
}

public static class Bairro
{
    public const string RuaAcacias = "Rua das Acácias";
    public const string RuaIpe = "Rua Ipê Amarelo";
    public const string RuaJacarandas = "Rua dos Jacarandás";

    // Limites do bairro
    public const float LimiteX = 62f;
    public const float LimiteSul = -47f, LimiteNorte = 42f;

    // Rua das Acácias (leste-oeste): calçada norte, asfalto e calçada sul
    public const float AcaciasCalcadaNorteZ0 = -3.15f, AcaciasAsfaltoZ0 = -5.5f, AcaciasAsfaltoZ1 = -12.5f, AcaciasCalcadaSulZ1 = -15f;
    public static float AcaciasCentroZ => (AcaciasAsfaltoZ0 + AcaciasAsfaltoZ1) / 2f;

    // Ruas norte-sul: centro em x, 7 m de asfalto e 2,5 m de calçada de cada lado
    public const float IpeX = -26f, JacarandasX = 26f;
    public const float MeiaPista = 3.5f, Calcada = 2.5f;

    // Alinhamento das fachadas (a calçada termina aqui)
    public const float FrenteNorteAcacias = AcaciasCalcadaNorteZ0;   // prédios ao norte: fachada em z = -3,15
    public const float FrenteSulAcacias = AcaciasCalcadaSulZ1;       // prédios ao sul: fachada em z = -15
    public static float FrenteOesteIpe => IpeX - MeiaPista - Calcada;          // -32
    public static float FrenteLesteIpe => IpeX + MeiaPista + Calcada;          // -20
    public static float FrenteOesteJacarandas => JacarandasX - MeiaPista - Calcada;   // 20
    public static float FrenteLesteJacarandas => JacarandasX + MeiaPista + Calcada;   // 32

    // ---------------- Prédios montados por código (têm interior) ----------------
    // Salas de cliente de 10 x 8 m com a parede da frente alinhada à calçada (parede de 15 cm)
    const float T = 0.15f;
    public static Vector3 CentroEscritorio => Vector3.zero;
    public static Vector3 CentroCasa => new Vector3(0f, 0f, -19.5f);
    public static Vector3 CentroContabilidade => new Vector3(42f, 0f, FrenteNorteAcacias + T + 4f);
    public static Vector3 CentroClinica => new Vector3(-42f, 0f, FrenteNorteAcacias + T + 4f);
    // A loja fica do lado sul, girada 180° (porta para o norte, para a rua)
    public static Vector3 CentroLoja => new Vector3(42f, 0f, FrenteSulAcacias - T - 4f);
    public const float GiroLoja = 180f;

    // ---------------- Endereços ----------------
    public static List<Endereco> Enderecos => new List<Endereco>
    {
        new Endereco { tipo = TipoEndereco.Casa, nome = "Sua casa", rua = RuaAcacias, numero = 128,
                       porta = new Vector3(1.5f, 1f, FrenteSulAcacias + 0.9f) },
        new Endereco { tipo = TipoEndereco.Escritorio, nome = "Seu escritório", rua = RuaAcacias, numero = 131,
                       porta = new Vector3(0f, 1f, FrenteNorteAcacias - 0.9f) },
        new Endereco { tipo = TipoEndereco.Contabilidade, nome = "Escritório de contabilidade", rua = RuaAcacias, numero = 187,
                       porta = new Vector3(CentroContabilidade.x, 1f, FrenteNorteAcacias - 0.9f) },
        new Endereco { tipo = TipoEndereco.Clinica, nome = "Clínica", rua = RuaAcacias, numero = 75,
                       porta = new Vector3(CentroClinica.x, 1f, FrenteNorteAcacias - 0.9f) },
        new Endereco { tipo = TipoEndereco.Loja, nome = "Loja (papelaria)", rua = RuaAcacias, numero = 190,
                       porta = new Vector3(CentroLoja.x, 1f, FrenteSulAcacias + 0.9f) },
        new Endereco { tipo = TipoEndereco.Fornecedor, nome = "Distribuidora de peças", rua = RuaJacarandas, numero = 95,
                       porta = new Vector3(FrenteLesteJacarandas - 0.9f, 1f, PosFornecedorZ) },
        new Endereco { tipo = TipoEndereco.Coleta, nome = "Ponto de coleta de lixo eletrônico", rua = RuaIpe, numero = 210,
                       porta = new Vector3(FrenteOesteIpe + 0.9f, 1f, PosColetaZ) },
        new Endereco { tipo = TipoEndereco.Posto, nome = "Posto de combustível", rua = RuaJacarandas, numero = 340,
                       porta = new Vector3(BombaPosto.x - 1.6f, 1f, BombaPosto.z) },
    };

    public const float PosFornecedorZ = -30f, PosColetaZ = -30f;

    public static Endereco De(TipoEndereco t)
    {
        foreach (var e in Enderecos) if (e.tipo == t) return e;
        return Enderecos[0];
    }

    public static TipoEndereco DoTipoSala(TipoSala s) =>
        s == TipoSala.Clinica ? TipoEndereco.Clinica : s == TipoSala.Loja ? TipoEndereco.Loja : TipoEndereco.Contabilidade;

    // ---------------- Vagas na rua (Etapa 9C) ----------------
    // Estaciona-se junto ao meio-fio, no sentido da mão (no Brasil, mão direita): do lado norte das Acácias o carro
    // fica virado para o oeste, do lado sul para o leste; na Jacarandás (lado leste) para o norte; na Ipê (lado oeste)
    // para o sul. 'centro' é o meio do carro no asfalto; 'giro' é para onde a frente aponta (graus, 0 = norte).
    public const float DistMeioFio = 1.1f;   // do meio-fio ao meio do carro (1,7 m de largura + uns 25 cm)

    public struct Vaga { public TipoEndereco tipo; public Vector3 centro; public float giro; }

    public static Vaga VagaDe(TipoEndereco t)
    {
        float zN = AcaciasAsfaltoZ0 - DistMeioFio, zS = AcaciasAsfaltoZ1 + DistMeioFio;
        switch (t)
        {
            // escritório e casa: um pouco a leste da faixa de pedestre (não se para em cima da faixa)
            case TipoEndereco.Escritorio: return new Vaga { tipo = t, centro = new Vector3(4.6f, 0f, zN), giro = 270f };
            case TipoEndereco.Casa: return new Vaga { tipo = t, centro = new Vector3(5.6f, 0f, zS), giro = 90f };
            case TipoEndereco.Contabilidade: return new Vaga { tipo = t, centro = new Vector3(CentroContabilidade.x, 0f, zN), giro = 270f };
            case TipoEndereco.Clinica: return new Vaga { tipo = t, centro = new Vector3(CentroClinica.x, 0f, zN), giro = 270f };
            case TipoEndereco.Loja: return new Vaga { tipo = t, centro = new Vector3(CentroLoja.x, 0f, zS), giro = 90f };
            case TipoEndereco.Fornecedor: return new Vaga { tipo = t, centro = new Vector3(JacarandasX + MeiaPista - DistMeioFio, 0f, PosFornecedorZ), giro = 0f };
            case TipoEndereco.Coleta: return new Vaga { tipo = t, centro = new Vector3(IpeX - MeiaPista + DistMeioFio, 0f, PosColetaZ), giro = 180f };
            default: return new Vaga { tipo = t, centro = VagaPosto, giro = 90f };
        }
    }

    // ---------------- Posto de combustível (Etapa 9C) ----------------
    // No fim norte da Rua dos Jacarandás, lado leste. A calçada na frente é rebaixada (guia rebaixada) para o carro
    // entrar; a ilha das bombas fica paralela à rua, debaixo da cobertura, e a lojinha no fundo do terreno.
    public static float PostoZ0 => 31.5f;
    public static float PostoZ1 => 41.5f;
    public static float PostoX0 => FrenteLesteJacarandas;          // 32: onde a calçada termina
    public static float PostoX1 => PostoX0 + 16f;                  // 48
    public static Rect TerrenoPosto => Rect.MinMaxRect(PostoX0, PostoZ0, PostoX1, PostoZ1);
    public static Vector3 BombaPosto => new Vector3(PostoX0 + 6.5f, 0f, (PostoZ0 + PostoZ1) / 2f + 1.6f);
    public static Vector3 VagaPosto => new Vector3(BombaPosto.x, 0f, BombaPosto.z - 1.9f);   // carro ao lado da bomba, virado para o leste
    // Trecho da calçada leste dos Jacarandás rebaixado (entrada e saída do posto)
    public static float RebaixoZ0 => PostoZ0 + 1f;
    public static float RebaixoZ1 => PostoZ1 - 1f;

    // ---------------- Ruas e calçadas, em pedaços que não se sobrepõem ----------------
    // Nos cruzamentos o asfalto passa reto e as esquinas ficam com a calçada da Rua das Acácias.
    public static List<PedacoRua> Pedacos()
    {
        var lista = new List<PedacoRua>();
        void Add(TipoPiso t, string rua, float x0, float z0, float x1, float z1)
        {
            if (x1 - x0 > 0.01f && z1 - z0 > 0.01f)
                lista.Add(new PedacoRua { tipo = t, rua = rua, area = Rect.MinMaxRect(x0, z0, x1, z1) });
        }

        float[] ruasNS = { IpeX, JacarandasX };
        string[] nomesNS = { RuaIpe, RuaJacarandas };

        // Acácias: asfalto inteiro; calçadas cortadas onde passa o asfalto das ruas norte-sul
        Add(TipoPiso.Asfalto, RuaAcacias, -LimiteX, AcaciasAsfaltoZ1, LimiteX, AcaciasAsfaltoZ0);
        float x = -LimiteX;
        foreach (float c in ruasNS)
        {
            Add(TipoPiso.Calcada, RuaAcacias, x, AcaciasAsfaltoZ0, c - MeiaPista, AcaciasCalcadaNorteZ0);
            Add(TipoPiso.Calcada, RuaAcacias, x, AcaciasCalcadaSulZ1, c - MeiaPista, AcaciasAsfaltoZ1);
            x = c + MeiaPista;
        }
        Add(TipoPiso.Calcada, RuaAcacias, x, AcaciasAsfaltoZ0, LimiteX, AcaciasCalcadaNorteZ0);
        Add(TipoPiso.Calcada, RuaAcacias, x, AcaciasCalcadaSulZ1, LimiteX, AcaciasAsfaltoZ1);

        // Norte-sul: asfalto sem o trecho do asfalto das Acácias; calçadas sem o trecho da Acácias inteira
        for (int i = 0; i < ruasNS.Length; i++)
        {
            float c = ruasNS[i];
            string n = nomesNS[i];
            Add(TipoPiso.Asfalto, n, c - MeiaPista, LimiteSul, c + MeiaPista, AcaciasAsfaltoZ1);
            Add(TipoPiso.Asfalto, n, c - MeiaPista, AcaciasAsfaltoZ0, c + MeiaPista, LimiteNorte);
            foreach (float lado in new[] { -1f, 1f })
            {
                float a = lado < 0 ? c - MeiaPista - Calcada : c + MeiaPista;
                float b = a + Calcada;
                Add(TipoPiso.Calcada, n, a, LimiteSul, b, AcaciasCalcadaSulZ1);
                if (c == JacarandasX && lado > 0)
                {
                    // guia rebaixada na frente do posto: vira asfalto no nível da rua
                    Add(TipoPiso.Calcada, n, a, AcaciasCalcadaNorteZ0, b, RebaixoZ0);
                    Add(TipoPiso.Asfalto, n, a, RebaixoZ0, b, RebaixoZ1);
                    Add(TipoPiso.Calcada, n, a, RebaixoZ1, b, LimiteNorte);
                }
                else Add(TipoPiso.Calcada, n, a, AcaciasCalcadaNorteZ0, b, LimiteNorte);
            }
        }
        return lista;
    }

    // Ponto está em cima de rua ou calçada?
    public static bool NaRua(Vector2 xz)
    {
        foreach (var p in Pedacos()) if (p.area.Contains(xz)) return true;
        return false;
    }

    // Nome da rua mais próxima do ponto (para o GPS dizer onde o jogador está)
    public static string RuaMaisProxima(Vector3 p)
    {
        string melhor = RuaAcacias;
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
}
