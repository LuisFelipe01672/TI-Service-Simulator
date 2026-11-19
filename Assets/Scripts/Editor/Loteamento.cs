using System.Collections.Generic;
using UnityEngine;

// Etapa 10E-2: lotes da cidade. As casas postas à mão no bairro de antes (Imóveis fixos) e os lotes gerados nas ruas
// novas: casas e sobrados do Sketchfab (CC-BY) com cor de parede, muro, portão, recuo e lado espelhado sorteados,
// comércios de fachada (sobrado de porta de enrolar com placa), terrenos baldios e a praça. Tudo puro (sem cena): o
// MontarCenario monta a partir daqui e os testes da planta conferem que nada se sobrepõe e todo lote dá na calçada.
public static class Loteamento
{
    // Planta de cada modelo virado para a rua: largura (ao longo da rua) e profundidade, medidas com o Imovel
    // (menu antigo "Medir imóveis", 18/11/2026). Frente = para onde a fachada aponta no arquivo; Lado = maior lado.
    public struct Modelo { public string nome; public float lado, frente, largura, profundidade; }

    public static Modelo CasaComum => new Modelo { nome = "Casa_Comum_Brasileira", lado = 12.01f, frente = 270f, largura = 12.01f, profundidade = 7.89f };
    public static Modelo CasaTerrea => new Modelo { nome = "Casa_Terrea", lado = 7.08f, frente = 90f, largura = 6.51f, profundidade = 7.08f };
    public static Modelo SobradoTijolo => new Modelo { nome = "Sobrado_Tijolo", lado = 9.0f, frente = 180f, largura = 4.99f, profundidade = 6.75f };
    public static Modelo SobradoEnrolar => new Modelo { nome = "Sobrado_Porta_Enrolar", lado = 7.78f, frente = 180f, largura = 5.78f, profundidade = 7.78f };
    public static Modelo PredioGrande => new Modelo { nome = "Predio_Hospital", lado = 32f, frente = 270f, largura = 32f, profundidade = 28.21f };

    public class Lote
    {
        public Modelo modelo;
        public bool baldio;            // terreno vazio com muro
        public bool fixo;              // casa posta à mão no bairro de antes (sem muro nem variação)
        public string rua;
        public float viradoPara;       // graus (0 = norte): para onde a fachada olha (a rua)
        public Vector2 alinhamento;    // meio da frente do terreno, em cima da linha onde a calçada termina
        public float largura;          // frente do terreno
        public float recuo;            // da linha da calçada até a fachada
        public float fundo;            // do alinhamento até o fundo do terreno
        public bool muro;              // muro na frente (e nas laterais até a casa)
        public int portao;             // 0 só social, 1 social + garagem
        public int corParede, corMuro;
        public bool espelhado;
        public bool comercio;
        public string placa, subtitulo;
        public string marcador;        // nome para achar o lote (coleta, fornecedor)

        public Vector3 Dir => new Vector3(Mathf.Sin(viradoPara * Mathf.Deg2Rad), 0f, Mathf.Cos(viradoPara * Mathf.Deg2Rad));
        Vector2 D => new Vector2(Mathf.Round(Dir.x), Mathf.Round(Dir.z));
        Vector2 Lat => new Vector2(D.y, -D.x);
        // Meio da fachada da casa (onde o Imovel encosta a frente)
        public Vector2 FrenteCasa => alinhamento - D * recuo;
        public Rect Terreno => Retangulo(alinhamento, largura, fundo);
        public Rect Planta => baldio ? new Rect() : Retangulo(FrenteCasa, modelo.largura, modelo.profundidade);
        Rect Retangulo(Vector2 frente, float larg, float prof)
        {
            Vector2 a = frente + Lat * (larg / 2f), b = frente - Lat * (larg / 2f) - D * prof;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
        // Um ponto da calçada logo na frente do terreno
        public Vector2 NaCalcada => alinhamento + D * 0.4f;
    }

    // ---------------- Imóveis postos à mão no bairro de antes (Etapa 9B; não mudam de lugar) ----------------

    static Lote Fixo(Modelo m, float viradoPara, Vector2 frente, string marcador = null) =>
        new Lote { modelo = m, fixo = true, viradoPara = viradoPara, alinhamento = frente, largura = m.largura, fundo = m.profundidade, marcador = marcador };

    public static List<Lote> Fixos()
    {
        float fN = Bairro.FrenteNorteAcacias, fS = Bairro.FrenteSulAcacias;
        float ipeO = Bairro.FrenteOesteIpe, ipeL = Bairro.FrenteLesteIpe, jacO = Bairro.FrenteOesteJacarandas, jacL = Bairro.FrenteLesteJacarandas;
        return new List<Lote>
        {
            // Acácias, lado norte (fachada para o sul)
            Fixo(CasaComum, 180, new Vector2(-10.5f, fN)),
            Fixo(CasaTerrea, 180, new Vector2(9f, fN)),
            Fixo(SobradoTijolo, 180, new Vector2(15.5f, fN)),
            Fixo(CasaTerrea, 180, new Vector2(-54f, fN)),
            Fixo(SobradoEnrolar, 180, new Vector2(53f, fN)),
            // Acácias, lado sul (fachada para o norte)
            Fixo(CasaComum, 0, new Vector2(-11f, fS)),
            Fixo(CasaTerrea, 0, new Vector2(10f, fS)),
            Fixo(SobradoTijolo, 0, new Vector2(16.5f, fS)),
            Fixo(CasaTerrea, 0, new Vector2(-42f, fS)),
            Fixo(CasaComum, 0, new Vector2(-53f, fS)),
            Fixo(CasaComum, 0, new Vector2(54f, fS)),
            // Ipê Amarelo: lado oeste (fachada para o leste) e lado leste (fachada para o oeste)
            Fixo(SobradoTijolo, 90, new Vector2(ipeO, Bairro.PosColetaZ), "coleta"),
            Fixo(CasaTerrea, 90, new Vector2(ipeO, -40f)),
            Fixo(PredioGrande, 90, new Vector2(ipeO, 22f), "hospital"),
            Fixo(CasaComum, 270, new Vector2(ipeL, 14f)),
            Fixo(CasaTerrea, 270, new Vector2(ipeL, 28f)),
            Fixo(CasaTerrea, 270, new Vector2(ipeL, -28f)),
            Fixo(SobradoEnrolar, 270, new Vector2(ipeL, -38f)),
            // Jacarandás
            Fixo(CasaComum, 90, new Vector2(jacO, 14f)),
            Fixo(CasaTerrea, 90, new Vector2(jacO, 27f)),
            Fixo(CasaComum, 90, new Vector2(jacO, -28f)),
            Fixo(SobradoEnrolar, 270, new Vector2(jacL, Bairro.PosFornecedorZ), "fornecedor"),
            Fixo(CasaTerrea, 270, new Vector2(jacL, -40f)),
            Fixo(CasaComum, 270, new Vector2(jacL, 14f)),
            Fixo(SobradoTijolo, 270, new Vector2(jacL, 26f)),
        };
    }

    // ---------------- Praça (quarteirão inteiro entre Jacarandás, Quaresmeiras, Paineiras e Sibipirunas) ----------------
    public static Rect Praca => Bairro.Praca;

    // ---------------- Comércios de fachada (sem marca real) ----------------
    static readonly (string titulo, string sub)[] Comercios =
    {
        ("PADARIA", "Pão quente · Café · Lanches"), ("MERCEARIA", "Hortifrúti · Frios · Bebidas"),
        ("FARMÁCIA", "Medicamentos · Perfumaria"), ("AÇOUGUE", "Carnes · Frango · Linguiça"),
        ("LANCHONETE", "Salgados · Sucos · Açaí"), ("SALÃO DE BELEZA", "Cabelo · Unhas · Sobrancelha"),
        ("PET SHOP", "Banho e tosa · Ração"), ("MATERIAIS DE CONSTRUÇÃO", "Cimento · Areia · Ferragens"),
        ("ÓTICA", "Óculos · Lentes · Exame de vista"), ("BAZAR", "Utilidades · Presentes"),
        ("BARBEARIA", "Corte · Barba"), ("CHAVEIRO", "Cópias · Fechaduras"),
        ("PIZZARIA", "Forno a lenha · Entrega"), ("LAVANDERIA", "Lavar · Passar"),
        ("OFICINA MECÂNICA", "Freios · Suspensão · Óleo"), ("SORVETERIA", "Sorvete · Picolé · Milk-shake"),
    };

    // ---------------- Geração ----------------
    public const int Semente = 1022;
    public const int CoresParede = 8, CoresMuro = 6;

    static List<Lote> cache;
    public static List<Lote> Todos() => cache ??= Gerar();
    public static void Esquecer() => cache = null;

    static Rect Crescer(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
    static Rect Encolher(Rect r, float m) => Crescer(r, -m);

    // Áreas que os lotes novos não podem tocar: prédios com interior (com o muro e o banheiro da casa), posto e praça
    public static List<(string nome, Rect area)> Reservados()
    {
        var l = new List<(string, Rect)>();
        float t = MontarCenario.T;
        void Sala(string nome, Vector3 c, float w, float d, float folga) =>
            l.Add((nome, Rect.MinMaxRect(c.x - w / 2f - t - folga, c.z - d / 2f - t - folga, c.x + w / 2f + t + folga, c.z + d / 2f + t + folga)));
        Sala("Escritório", Bairro.CentroEscritorio, 6f, 6f, 0.5f);
        // prédios de cliente com interior (Etapa 3 e 10E-3), com 1 m de folga (as paredes do lado ficam livres)
        foreach (var p in Bairro.Predios) Sala(p.nome, p.centro, PredioCliente.Largura, PredioCliente.Profundidade, 1f);
        Sala("Banheiro", MontarCenario.CentroBanheiro, MontarCenario.LarguraBanheiro, MontarCenario.ProfBanheiro, 0.5f);
        // casa do jogador: do muro na calçada até o fundo
        var c = Bairro.CentroCasa;
        l.Add(("Casa", Rect.MinMaxRect(c.x - 4f - t - 0.5f, c.z - 3f - t - 0.5f, c.x + 4f + t + 0.5f, Bairro.FrenteSulAcacias)));
        l.Add(("Posto", Bairro.TerrenoPosto));
        l.Add(("Praça", Praca));
        return l;
    }

    static List<Lote> Gerar()
    {
        var m = Bairro.Malha;
        var rnd = new System.Random(Semente);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var lotes = Fixos();
        var ocupado = new List<Rect>();
        foreach (var f in lotes) ocupado.Add(Crescer(f.Planta, 0.4f));
        foreach (var r in Reservados()) ocupado.Add(r.area);
        var ruas = new List<Rect>();
        foreach (var p in m.Pedacos()) ruas.Add(p.area);
        var lim = Encolher(m.limites, 1f);
        int nComercio = 0;

        bool Livre(Rect r)
        {
            var e = Encolher(r, 0.02f);
            if (r.xMin < lim.xMin || r.xMax > lim.xMax || r.yMin < lim.yMin || r.yMax > lim.yMax) return false;
            foreach (var o in ocupado) if (e.Overlaps(o)) return false;
            foreach (var o in ruas) if (e.Overlaps(o)) return false;
            return true;
        }

        // Ruas comerciais: a avenida e a Acácias (fora do bairro de antes)
        bool Comercial(string rua) => rua == Bairro.AvPaineiras || rua == Bairro.RuaAcacias;

        for (int i = 0; i < m.ruas.Count; i++)
        {
            var rua = m.ruas[i];
            int kIni = m.CruzamentoNoFim(i, -1), kFim = m.CruzamentoNoFim(i, 1);
            foreach (int lado in new[] { 1, -1 })
            {
                // trechos da frente: entre as transversais que saem para este lado
                var trechos = new List<Vector2>();
                float cur = kIni >= 0 ? m.Transversal(kIni, i).FrentePos + 0.5f : rua.ini + 1.5f;
                foreach (int k in m.CruzamentosDa(i))
                {
                    if (k == kIni || k == kFim || !m.Braco(k, lado * rua.Normal)) continue;
                    var o = m.Transversal(k, i);
                    trechos.Add(new Vector2(cur, o.FrenteNeg - 0.5f));
                    cur = o.FrentePos + 0.5f;
                }
                trechos.Add(new Vector2(cur, kFim >= 0 ? m.Transversal(kFim, i).FrenteNeg - 0.5f : rua.fim - 1.5f));

                float frente = rua.Frente(lado);
                // fachada para a rua: lado + (norte/leste) olha para -Normal
                Vector3 olha = -lado * rua.Normal;
                float virado = Mathf.Repeat(Mathf.Atan2(olha.x, olha.z) * Mathf.Rad2Deg, 360f);
                foreach (var tr in trechos)
                {
                    float a = tr.x;
                    bool primeiro = true;
                    while (a < tr.y - 4f)
                    {
                        Vector2 Ponto(float ao) => rua.lesteOeste ? new Vector2(ao, frente) : new Vector2(frente, ao);
                        var lote = new Lote { rua = rua.nome, viradoPara = virado, espelhado = rnd.NextDouble() < 0.5, corParede = rnd.Next(CoresParede), corMuro = rnd.Next(CoresMuro), portao = rnd.Next(2) };
                        double sorte = rnd.NextDouble();
                        bool fora = !Bairro.BairroAntigo.Contains(Ponto(a));
                        bool comercio = (Comercial(rua.nome) && fora && sorte < 0.45) || (primeiro && sorte < 0.2);
                        if (comercio)
                        {
                            lote.modelo = SobradoEnrolar; lote.comercio = true; lote.recuo = 0f;
                            var c = Comercios[nComercio % Comercios.Length];
                            lote.placa = c.titulo; lote.subtitulo = c.sub;
                        }
                        else if (sorte < 0.55) { lote.modelo = CasaTerrea; lote.recuo = R(2.5f, 4.5f); lote.muro = true; }
                        else if (sorte < 0.75) { lote.modelo = SobradoTijolo; lote.recuo = rnd.NextDouble() < 0.5 ? 0f : R(2f, 3f); lote.muro = lote.recuo > 0f; }
                        else if (sorte < 0.92) { lote.modelo = CasaComum; lote.recuo = 0f; }
                        else { lote.baldio = true; lote.muro = true; }

                        float folga = lote.modelo.nome == CasaComum.nome ? R(0.6f, 1.4f) : R(1.6f, 3.4f);
                        lote.largura = lote.baldio ? R(8f, 12f) : lote.modelo.largura + folga;
                        lote.fundo = lote.baldio ? R(9f, 12f) : lote.recuo + lote.modelo.profundidade + R(1f, 2.5f);
                        if (a + lote.largura > tr.y) { a += 1f; continue; }
                        lote.alinhamento = Ponto(a + lote.largura / 2f);
                        if (!Livre(lote.Terreno)) { a += 1f; continue; }
                        if (lote.comercio) nComercio++;
                        lotes.Add(lote);
                        ocupado.Add(lote.Terreno);
                        a += lote.largura;
                        primeiro = false;
                        // de vez em quando um quintal de lado ou um vão com mato
                        if (rnd.NextDouble() < 0.3) a += R(2f, 7f);
                    }
                }
            }
        }
        return lotes;
    }
}
