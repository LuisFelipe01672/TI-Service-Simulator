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
// Etapa 10E-1: as ruas viraram uma MalhaRuas (Bairro.Malha); as constantes abaixo ficam como a planta dos lotes.
// Etapa 10E-2: a cidade cresceu ~7x (320 x 244 m) numa grade de quarteirões em volta do bairro de antes, que não saiu
// do lugar. Ruas novas (nomes de árvores, como as de antes), algumas terminando em "T" no meio da cidade:
//
//   z=96  Sibipirunas    ·····+·······+·······+·······+·······+·······+·····   (de Cedros a Oitis)
//   z=50  Av. Paineiras ======+=======+=======+=======+=======+=======+=====   (de borda a borda)
//   z=-9  Acácias       ======+=======+=======+=======+=======+=======+=====   (de borda a borda)
//   z=-68 Palmeiras          +-------+-------+-------+-------+-------+         (de Cedros a Oitis)
//   z=-108 Flamboyants       +-------+-------+-------+-------+-------+         (de Cedros a Oitis)
//                         Cedros Mangueiras Ipê  Jacarandás Quaresmeiras Oitis
//                         x=-130   x=-78   x=-26   x=26     x=78      x=130
//   Cedros, Ipê, Jacarandás e Oitis vão de borda a borda; Mangueiras e Quaresmeiras vão de Flamboyants a Sibipirunas.
//
//            Ipê Amarelo          Jacarandás
//     (clínica) |   (escritório)       |  (contabilidade)
//   ============+======================+============  Rua das Acácias
//     (coleta)  |   (casa)             |  (loja / fornecedor)
// Etapa 10E-3: sete clientes novos com interior (os valores novos vão no fim: a ordem antiga não muda)
public enum TipoEndereco { Casa, Escritorio, Fornecedor, Coleta, Contabilidade, Clinica, Loja, Posto,
                           Advocacia, Imobiliaria, Escola, Laboratorio, Veterinaria, Farmacia, Otica }

// Prédio de cliente com interior (sala de 10 x 8 m montada pelo MontarCenario). Cada empresa cai sempre no mesmo
// prédio (SalaCliente.EnderecoDoCliente). 'giro' 0 = porta ao sul (prédio do lado norte da rua); 180 = porta ao norte.
public struct PredioCliente
{
    public TipoEndereco tipo;
    public TipoSala sala;            // layout de base (escritório, clínica ou loja): fala do contato e testes antigos
    public string objeto;            // nome da sala na cena ("Cliente", "Cliente_Clinica"...)
    public string nome, rotulo;      // como aparece no GPS e no mapa do celular
    public string placa, subtitulo;  // placa luminosa sobre a porta
    public string rua;
    public int numero;
    public Vector3 centro;
    public float giro;
    public Color corFachada;
    public bool doisAndares;
    public string[] palavras;        // ramo no nome da empresa que leva a este prédio

    public const float Largura = 10f, Profundidade = 8f;
    Vector3 Local(float x, float z) => centro + Quaternion.Euler(0f, giro, 0f) * new Vector3(x, 0f, z);
    // Meio da porta, na linha da fachada (onde a calçada termina)
    public Vector3 Porta => Local(0f, -(Profundidade / 2f + 0.15f));
    // Ponto do GPS: na calçada, 90 cm na frente da porta
    public Vector3 PontoGPS { get { var p = Local(0f, -(Profundidade / 2f + 0.15f + 0.9f)); p.y = 1f; return p; } }
    // Planta por fora das paredes (giro 0 ou 180: mesmo retângulo)
    public Rect Planta => new Rect(centro.x - Largura / 2f - 0.15f, centro.z - Profundidade / 2f - 0.15f, Largura + 0.3f, Profundidade + 0.3f);
}

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
    // Etapa 10E-2
    public const string RuaCedros = "Rua dos Cedros", RuaMangueiras = "Rua das Mangueiras", RuaQuaresmeiras = "Rua das Quaresmeiras",
                        RuaOitis = "Rua dos Oitis", AvPaineiras = "Avenida das Paineiras", RuaSibipirunas = "Rua das Sibipirunas",
                        RuaPalmeiras = "Rua das Palmeiras", RuaFlamboyants = "Rua dos Flamboyants";
    public const float CedrosX = -130f, MangueirasX = -78f, QuaresmeirasX = 78f, OitisX = 130f;
    public const float SibipirunasZ = 96f, PaineirasZ = 50f, PalmeirasZ = -68f, FlamboyantsZ = -108f;

    // Limites da cidade (Etapa 10E-2; o bairro de antes ia de x = ±62 e z = -47 a 42)
    public const float LimiteX = 160f;
    public const float LimiteSul = -128f, LimiteNorte = 116f;
    // O bairro de antes (as casas postas à mão ficam dentro dele)
    public static Rect BairroAntigo => Rect.MinMaxRect(-62f, -47f, 62f, 42f);

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

    // ---------------- Prédios de cliente (Etapa 3 e 10E-3) ----------------
    // Os três de antes (Rua das Acácias, no bairro de antes) e sete novos nas ruas leste-oeste da cidade nova, todos
    // com a fachada no alinhamento da calçada. Criada na primeira vez que alguém pede (lê a Malha).
    static List<PredioCliente> predios;
    public static List<PredioCliente> Predios => predios ??= CriarPredios();

    // Prédio de 10 x 8 m numa rua leste-oeste: lado +1 = lado norte (porta ao sul), -1 = lado sul (porta ao norte)
    static PredioCliente NaRua(string rua, float x, int lado)
    {
        var r = Malha.ruas[Malha.Indice(rua)];
        float z = lado > 0 ? r.FrentePos + T + PredioCliente.Profundidade / 2f : r.FrenteNeg - T - PredioCliente.Profundidade / 2f;
        return new PredioCliente { rua = rua, centro = new Vector3(x, 0f, z), giro = lado > 0 ? 0f : 180f };
    }

    static List<PredioCliente> CriarPredios()
    {
        var l = new List<PredioCliente>();
        void Add(PredioCliente p, TipoEndereco t, TipoSala s, string objeto, string nome, string rotulo, int numero,
                 string placa, string sub, Color cor, bool doisAndares, params string[] palavras)
        {
            p.tipo = t; p.sala = s; p.objeto = objeto; p.nome = nome; p.rotulo = rotulo; p.numero = numero;
            p.placa = placa; p.subtitulo = sub; p.corFachada = cor; p.doisAndares = doisAndares; p.palavras = palavras;
            l.Add(p);
        }
        // Ordem = ordem de procura das palavras: os ramos mais específicos antes ("Clínica Veterinária" é veterinária)
        Add(NaRua(RuaSibipirunas, -100f, -1), TipoEndereco.Veterinaria, TipoSala.Clinica, "Cliente_Veterinaria", "Clínica veterinária", "veterinária", 154,
            "VETERINÁRIA & PET SHOP", "Consultas · Vacinas · Banho e tosa", new Color(0.78f, 0.89f, 0.84f), false, "veterin", "pet shop");
        Add(NaRua(RuaPalmeiras, 0f, 1), TipoEndereco.Laboratorio, TipoSala.Clinica, "Cliente_Laboratorio", "Laboratório de análises", "laboratório", 801,
            "LABORATÓRIO", "Análises clínicas · Odontologia", new Color(0.9f, 0.95f, 0.97f), true, "laborat", "odonto", "consult", "dra.", "dr.");
        Add(NaRua(RuaAcacias, CentroClinica.x, 1), TipoEndereco.Clinica, TipoSala.Clinica, "Cliente_Clinica", "Clínica", "clínica", 75,
            "CLÍNICA", "Clínica médica · Exames", new Color(0.95f, 0.97f, 0.96f), true, "clínica", "clinica", "hospital", "saúde", "saude");
        Add(NaRua(RuaAcacias, -100f, 1), TipoEndereco.Farmacia, TipoSala.Loja, "Cliente_Farmacia", "Farmácia", "farmácia", 33,
            "FARMÁCIA", "Medicamentos · Perfumaria · Aferição de pressão", new Color(0.93f, 0.97f, 0.93f), false, "farmácia", "farmacia", "drogaria");
        Add(NaRua(RuaAcacias, 100f, -1), TipoEndereco.Otica, TipoSala.Loja, "Cliente_Otica", "Ótica", "ótica", 348,
            "ÓTICA", "Óculos de grau e de sol · Exame de vista", new Color(0.34f, 0.37f, 0.42f), false, "ótica", "otica");
        Add(NaRua(RuaAcacias, CentroLoja.x, -1), TipoEndereco.Loja, TipoSala.Loja, "Cliente_Loja", "Loja (papelaria)", "loja", 190,
            "PAPELARIA & VARIEDADES", "Material escolar · Presentes", new Color(0.97f, 0.9f, 0.74f), false,
            "loja", "autopeças", "autopecas", "gráfica", "grafica", "papelaria", "mercado", "padaria", "materiais", "magazine");
        Add(NaRua(AvPaineiras, -50f, 1), TipoEndereco.Advocacia, TipoSala.Contabilidade, "Cliente_Advocacia", "Escritório de advocacia", "advocacia", 411,
            "ADVOCACIA & CONSULTORIA", "Cível · Trabalhista · Despachante", new Color(0.86f, 0.82f, 0.74f), true, "advoca", "despachante");
        Add(NaRua(AvPaineiras, 100f, -1), TipoEndereco.Imobiliaria, TipoSala.Contabilidade, "Cliente_Imobiliaria", "Imobiliária", "imobiliária", 1250,
            "IMOBILIÁRIA", "Venda · Aluguel · Lançamentos", new Color(0.8f, 0.87f, 0.93f), false, "imobili", "construtora");
        Add(NaRua(RuaFlamboyants, 50f, 1), TipoEndereco.Escola, TipoSala.Contabilidade, "Cliente_Escola", "Escola de cursos", "escola", 523,
            "ESCOLA & CURSOS", "Informática · Idiomas · Reforço escolar", new Color(0.98f, 0.87f, 0.55f), true,
            "escola", "colégio", "colegio", "agência", "agencia", "curso");
        Add(NaRua(RuaAcacias, CentroContabilidade.x, 1), TipoEndereco.Contabilidade, TipoSala.Contabilidade, "Cliente", "Escritório de contabilidade", "contábil", 187,
            "ESCRITÓRIO CONTÁBIL", "Contabilidade · Departamento pessoal", new Color(0.88f, 0.91f, 0.94f), false,
            "contabil", "escritório", "escritorio", "banco", "multinacional", "indústria", "industria", "transportadora", "hotel");
        return l;
    }

    public static bool EhCliente(TipoEndereco t) => Predios.Exists(p => p.tipo == t);

    public static PredioCliente Predio(TipoEndereco t)
    {
        foreach (var p in Predios) if (p.tipo == t) return p;
        throw new System.ArgumentException($"{t} não é prédio de cliente");
    }

    // ---------------- Endereços ----------------
    public static List<Endereco> Enderecos
    {
        get
        {
            var l = new List<Endereco>
            {
                new Endereco { tipo = TipoEndereco.Casa, nome = "Sua casa", rua = RuaAcacias, numero = 128,
                               porta = new Vector3(1.5f, 1f, FrenteSulAcacias + 0.9f) },
                new Endereco { tipo = TipoEndereco.Escritorio, nome = "Seu escritório", rua = RuaAcacias, numero = 131,
                               porta = new Vector3(0f, 1f, FrenteNorteAcacias - 0.9f) },
            };
            foreach (var p in Predios)
                l.Add(new Endereco { tipo = p.tipo, nome = p.nome, rua = p.rua, numero = p.numero, porta = p.PontoGPS });
            l.Add(new Endereco { tipo = TipoEndereco.Fornecedor, nome = "Distribuidora de peças", rua = RuaJacarandas, numero = 95,
                                 porta = new Vector3(FrenteLesteJacarandas - 0.9f, 1f, PosFornecedorZ) });
            l.Add(new Endereco { tipo = TipoEndereco.Coleta, nome = "Ponto de coleta de lixo eletrônico", rua = RuaIpe, numero = 210,
                                 porta = new Vector3(FrenteOesteIpe + 0.9f, 1f, PosColetaZ) });
            l.Add(new Endereco { tipo = TipoEndereco.Posto, nome = "Posto de combustível", rua = RuaJacarandas, numero = 340,
                                 porta = new Vector3(BombaPosto.x - 1.6f, 1f, BombaPosto.z) });
            return l;
        }
    }

    // Rótulo curto no mapa do celular
    public static string Rotulo(TipoEndereco t)
    {
        switch (t)
        {
            case TipoEndereco.Casa: return "casa";
            case TipoEndereco.Escritorio: return "escritório";
            case TipoEndereco.Fornecedor: return "peças";
            case TipoEndereco.Coleta: return "coleta";
            case TipoEndereco.Posto: return "posto";
            default: return EhCliente(t) ? Predio(t).rotulo : t.ToString().ToLowerInvariant();
        }
    }

    public const float PosFornecedorZ = -30f, PosColetaZ = -30f;

    public static Endereco De(TipoEndereco t)
    {
        foreach (var e in Enderecos) if (e.tipo == t) return e;
        return Enderecos[0];
    }

    // Endereço do prédio de base de cada tipo de sala (os três de antes)
    public static TipoEndereco DoTipoSala(TipoSala s) =>
        s == TipoSala.Clinica ? TipoEndereco.Clinica : s == TipoSala.Loja ? TipoEndereco.Loja : TipoEndereco.Contabilidade;

    // ---------------- Vagas na rua (Etapa 9C) ----------------
    // Estaciona-se junto ao meio-fio, no sentido da mão (no Brasil, mão direita): do lado norte das Acácias o carro
    // fica virado para o oeste, do lado sul para o leste; na Jacarandás (lado leste) para o norte; na Ipê (lado oeste)
    // para o sul. 'centro' é o meio do carro no asfalto; 'giro' é para onde a frente aponta (graus, 0 = norte).
    public const float DistMeioFio = 1.1f;

    // Faixas de pedestre atravessando a Rua das Acácias (centro em x): perto da Ipê, em frente ao escritório e perto
    // da Jacarandás. O cenário pinta, os carros param antes e os pedestres atravessam por elas (Etapa 9D).
    public static float[] FaixasPedestreX => new[] { IpeX - MeiaPista - 2f, 0f, JacarandasX + MeiaPista + 2f };   // do meio-fio ao meio do carro (1,7 m de largura + uns 25 cm)

    public struct Vaga { public TipoEndereco tipo; public Vector3 centro; public float giro; }

    public static Vaga VagaDe(TipoEndereco t)
    {
        float zN = AcaciasAsfaltoZ0 - DistMeioFio, zS = AcaciasAsfaltoZ1 + DistMeioFio;
        if (EhCliente(t))
        {
            // Na frente da porta, junto ao meio-fio do lado do prédio: lado norte virado para o oeste, sul para o leste
            var p = Predio(t);
            var r = Malha.ruas[Malha.Indice(p.rua)];
            bool norte = p.giro < 90f;
            return new Vaga { tipo = t, centro = new Vector3(p.centro.x, 0f, norte ? r.AsfaltoPos - DistMeioFio : r.AsfaltoNeg + DistMeioFio), giro = norte ? 270f : 90f };
        }
        switch (t)
        {
            // escritório e casa: um pouco a leste da faixa de pedestre (não se para em cima da faixa)
            case TipoEndereco.Escritorio: return new Vaga { tipo = t, centro = new Vector3(4.6f, 0f, zN), giro = 270f };
            case TipoEndereco.Casa: return new Vaga { tipo = t, centro = new Vector3(5.6f, 0f, zS), giro = 90f };
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

    // ---------------- Malha de ruas (Etapa 10E-1) ----------------
    // As 3 ruas de hoje numa MalhaRuas genérica (mesmas posições e larguras de antes). Trânsito, GPS, calçadas dos
    // pedestres e a montagem do cenário leem daqui. Criada na primeira vez que alguém pede (nada de static readonly).
    static MalhaRuas malha;
    public static MalhaRuas Malha => malha ??= CriarMalha();

    // Postes da rede elétrica (posição ao longo da rua; Acácias nas duas calçadas, Ipê do lado leste, Jacarandás do oeste)
    static float[] PostesAcacias => new[] { -58f, -47f, -35f, -18f, -8f, 8f, 18f, 35f, 49f, 58f };
    static float[] PostesTransversais => new[] { -44f, -32f, -21f, 3f, 15f, 27f, 39f };

    static MalhaRuas CriarMalha()
    {
        var m = new MalhaRuas { nome = "bairro", limites = Rect.MinMaxRect(-LimiteX, LimiteSul, LimiteX, LimiteNorte) };
        Rua NS(string nome, float x, float ini, float fim) =>
            new Rua { nome = nome, lesteOeste = false, eixo = x, ini = ini, fim = fim, meiaPista = MeiaPista, calcadaNeg = Calcada, calcadaPos = Calcada, ladosPostes = new[] { 1 } };
        Rua LO(string nome, float z, float ini, float fim) =>
            new Rua { nome = nome, lesteOeste = true, eixo = z, ini = ini, fim = fim, meiaPista = MeiaPista, calcadaNeg = Calcada, calcadaPos = Calcada, ladosPostes = new[] { -1 } };

        int acacias = m.AddRua(new Rua
        {
            nome = RuaAcacias, lesteOeste = true, eixo = AcaciasCentroZ, ini = -LimiteX, fim = LimiteX,
            meiaPista = (AcaciasAsfaltoZ0 - AcaciasAsfaltoZ1) / 2f,
            calcadaPos = AcaciasCalcadaNorteZ0 - AcaciasAsfaltoZ0, calcadaNeg = AcaciasAsfaltoZ1 - AcaciasCalcadaSulZ1,
            ladosPostes = new[] { 1, -1 },
        });
        int ipe = m.AddRua(NS(RuaIpe, IpeX, LimiteSul, LimiteNorte));
        m.ruas[ipe].ladosPostes = new[] { 1 };
        int jacarandas = m.AddRua(NS(RuaJacarandas, JacarandasX, LimiteSul, LimiteNorte));
        m.ruas[jacarandas].ladosPostes = new[] { -1 };
        // ruas novas: as de borda a borda e as que terminam em "T"
        m.AddRua(NS(RuaCedros, CedrosX, LimiteSul, LimiteNorte));
        m.AddRua(NS(RuaMangueiras, MangueirasX, FlamboyantsZ, SibipirunasZ));
        m.AddRua(NS(RuaQuaresmeiras, QuaresmeirasX, FlamboyantsZ, SibipirunasZ));
        m.AddRua(NS(RuaOitis, OitisX, LimiteSul, LimiteNorte));
        m.AddRua(LO(AvPaineiras, PaineirasZ, -LimiteX, LimiteX));
        m.AddRua(LO(RuaSibipirunas, SibipirunasZ, CedrosX, OitisX));
        m.AddRua(LO(RuaPalmeiras, PalmeirasZ, CedrosX, OitisX));
        m.AddRua(LO(RuaFlamboyants, FlamboyantsZ, CedrosX, OitisX));
        m.Fechar();

        // Controle: PARE em geral (quem passa reto no "T" é a principal); semáforos nos cruzamentos das ruas de
        // passagem, cada um no seu tempo
        foreach (var c in m.cruzamentos)
        {
            var lo = m.ruas[c.ruaLO];
            if (Mathf.Abs(c.centro.x - lo.ini) < 0.01f || Mathf.Abs(c.centro.x - lo.fim) < 0.01f) c.principalLesteOeste = false;
        }
        void Semaforo(string a, string b, float defasagem)
        {
            var c = m.cruzamentos[m.CruzamentoDe(a, b)];
            c.controle = ControleCruzamento.Semaforo;
            c.defasagem = defasagem;
        }
        Semaforo(RuaAcacias, RuaJacarandas, 0f);          // o de sempre (relógio de antes)
        Semaforo(AvPaineiras, RuaIpe, 9f);
        Semaforo(AvPaineiras, RuaJacarandas, 18f);
        Semaforo(RuaAcacias, RuaQuaresmeiras, 5f);       // quase junto com o da Jacarandás: onda verde na Acácias

        foreach (float x in FaixasPedestreX) m.faixas.Add(new FaixaPedestre { rua = acacias, pos = x });
        m.FaixasEmCadaTrecho();
        // guia rebaixada na frente do posto (calçada leste da Jacarandás)
        m.rebaixos.Add(new Rebaixo { rua = jacarandas, lado = 1, a0 = RebaixoZ0, a1 = RebaixoZ1 });

        // Postes: os de antes (à mão) dentro do bairro antigo e um a cada 12 m no resto
        m.ruas[acacias].postes = Juntar(PostesAcacias, m.PostesPadrao(acacias), -62f, 62f);
        m.ruas[ipe].postes = Juntar(PostesTransversais, m.PostesPadrao(ipe), -47f, 42f);
        m.ruas[jacarandas].postes = Juntar(PostesTransversais, m.PostesPadrao(jacarandas), -47f, 42f);
        return m;
    }

    // Postes à mão (dentro de [de, ate]) mais os padrão de fora, sem nenhum a menos de 6 m de outro
    static float[] Juntar(float[] mao, float[] padrao, float de, float ate)
    {
        var l = new List<float>(mao);
        foreach (float a in padrao)
        {
            if (a >= de && a <= ate) continue;
            bool perto = false;
            foreach (float b in l) if (Mathf.Abs(a - b) < 6f) perto = true;
            if (!perto) l.Add(a);
        }
        l.Sort();
        return l.ToArray();
    }

    // Praça (Etapa 10E-2): o quarteirão inteiro entre Jacarandás, Quaresmeiras, Paineiras e Sibipirunas
    public static Rect Praca
    {
        get
        {
            var m = Malha;
            var jac = m.ruas[m.Indice(RuaJacarandas)];
            var qua = m.ruas[m.Indice(RuaQuaresmeiras)];
            var pai = m.ruas[m.Indice(AvPaineiras)];
            var sib = m.ruas[m.Indice(RuaSibipirunas)];
            return Rect.MinMaxRect(jac.FrentePos, pai.FrentePos, qua.FrenteNeg, sib.FrenteNeg);
        }
    }

    // Orelhão na calçada perto do escritório, junto ao meio-fio (o cenário põe, os pedestres desviam)
    public static Vector3 Orelhao => new Vector3(-5f, 0.12f, AcaciasAsfaltoZ0 + 0.7f);

    // ---------------- Ruas e calçadas, em pedaços que não se sobrepõem ----------------
    public static List<PedacoRua> Pedacos() => new List<PedacoRua>(Malha.Pedacos());

    // Ponto está em cima de rua ou calçada?
    public static bool NaRua(Vector2 xz) => Malha.NaRua(xz);

    // Nome da rua mais próxima do ponto (para o GPS dizer onde o jogador está)
    public static string RuaMaisProxima(Vector3 p) => Malha.RuaMaisProxima(p);
}
