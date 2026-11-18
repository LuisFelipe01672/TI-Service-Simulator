using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Etapa 9D (vida na rua): rotas dos carros dos vizinhos (no asfalto, na mão certa, parando antes dos cruzamentos e
// das faixas), semáforo, vez no cruzamento, calçadas dos pedestres e o carro parando diante de obstáculo.
public class TestesTransito
{
    static List<Rect> Asfalto() => Bairro.Pedacos().Where(p => p.tipo == TipoPiso.Asfalto).Select(p => p.area).ToList();
    static bool NoAsfalto(List<Rect> a, Vector3 p) => a.Any(r => r.Contains(new Vector2(p.x, p.z)));

    [Test]
    public void Rotas_TodasAsCombinacoes()
    {
        var rotas = RotasTransito.TodasAsRotas();
        Assert.AreEqual(30, rotas.Count);
        foreach (var r in rotas) Assert.Greater(r.Comprimento, 40f, $"Rota {r.de}→{r.para} curta demais.");
    }

    // O carro inteiro (meio, laterais e cantos da frente) fica em cima do asfalto o caminho todo, inclusive nas curvas
    [Test]
    public void Rotas_FicamNoAsfalto()
    {
        var a = Asfalto();
        foreach (var r in RotasTransito.TodasAsRotas())
            for (float s = 0f; s <= r.Comprimento; s += 0.5f)
            {
                Vector3 p = r.Posicao(s), lado = RotasTransito.Direita(r.Direcao(s)) * RotasTransito.MeiaLargura;
                Vector3 frente = r.Direcao(s) * RotasTransito.MeioComprimento;
                foreach (var q in new[] { p, p + lado, p - lado, p + frente + lado * 0.8f, p + frente - lado * 0.8f })
                    Assert.IsTrue(NoAsfalto(a, q), $"Rota {r.de}→{r.para} sai do asfalto em s={s:0.0} ({q}).");
            }
    }

    // Mão direita: fora dos cruzamentos, o carro anda na metade direita da rua (como no Brasil)
    [Test]
    public void Rotas_NaMaoCerta()
    {
        foreach (var r in RotasTransito.TodasAsRotas())
            for (float s = 0f; s <= r.Comprimento; s += 1f)
            {
                Vector3 p = r.Posicao(s), d = r.Direcao(s);
                bool perto = Mathf.Abs(p.z - Bairro.AcaciasCentroZ) < 6f && (Mathf.Abs(p.x - Bairro.IpeX) < 6f || Mathf.Abs(p.x - Bairro.JacarandasX) < 6f);
                if (perto) continue;   // curva no cruzamento
                Vector3 eixo;
                if (Mathf.Abs(d.x) > 0.95f) eixo = new Vector3(p.x, 0f, Bairro.AcaciasCentroZ);
                else if (Mathf.Abs(d.z) > 0.95f) eixo = new Vector3(Mathf.Abs(p.x - Bairro.IpeX) < Mathf.Abs(p.x - Bairro.JacarandasX) ? Bairro.IpeX : Bairro.JacarandasX, 0f, p.z);
                else { Assert.Fail($"Rota {r.de}→{r.para} torta fora do cruzamento em s={s}."); continue; }
                float lado = Vector3.Dot(p - eixo, RotasTransito.Direita(d));
                Assert.That(lado, Is.EqualTo(RotasTransito.DeslocFaixa).Within(0.05f), $"Rota {r.de}→{r.para} na contramão em s={s} ({p}).");
            }
    }

    // Quem chega no cruzamento para com a frente fora dele e fora da zebra; atravessa cada cruzamento uma vez
    [Test]
    public void Rotas_ParamAntesDoCruzamentoEDaFaixa()
    {
        foreach (var r in RotasTransito.TodasAsRotas())
        {
            var cruz = r.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).ToList();
            Assert.AreEqual(cruz.Count, cruz.Select(p => p.indice).Distinct().Count(), $"Rota {r.de}→{r.para} passa duas vezes no mesmo cruzamento.");
            Assert.Greater(cruz.Count, 0, $"Rota {r.de}→{r.para} sem cruzamento.");
            foreach (var p in r.paradas)
            {
                Vector3 frente = r.Posicao(p.s) + r.Direcao(p.s) * RotasTransito.MeioComprimento;
                Assert.Less(p.s, p.sSaida);
                Assert.Greater(p.s, 0f, $"Rota {r.de}→{r.para}: parada antes do começo.");
                if (p.tipo == RotasTransito.TipoParada.Cruzamento)
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 rel = frente - RotasTransito.CentroCruzamento(k);
                        bool dentro = Mathf.Abs(rel.x) < Bairro.MeiaPista && Mathf.Abs(rel.z) < Bairro.MeiaPista;
                        Assert.IsFalse(dentro, $"Rota {r.de}→{r.para} para dentro do cruzamento {k} ({frente}).");
                    }
                for (int f = 0; f < 3; f++)
                    Assert.IsFalse(VidaNaRua.NaFaixa(f, frente, -0.1f), $"Rota {r.de}→{r.para} para em cima da faixa {f} ({frente}).");
            }
        }
    }

    [Test]
    public void Rotas_ManobrasEPreferencial()
    {
        var reta = RotasTransito.Rota(RotasTransito.Ponta.AcaciasOeste, RotasTransito.Ponta.AcaciasLeste);
        Assert.AreEqual(2, reta.paradas.Count(p => p.tipo == RotasTransito.TipoParada.Cruzamento));
        Assert.AreEqual(3, reta.paradas.Count(p => p.tipo == RotasTransito.TipoParada.Faixa), "Pela Acácias inteira passa nas 3 faixas.");
        Assert.IsTrue(reta.paradas.All(p => p.tipo != RotasTransito.TipoParada.Cruzamento || (p.preferencial && p.manobra == RotasTransito.Manobra.Reto)));
        // Ipê sul → Jacarandás norte: na Ipê vira à direita (para o leste), na Jacarandás vira à esquerda (para o norte)
        var r = RotasTransito.Rota(RotasTransito.Ponta.IpeSul, RotasTransito.Ponta.JacarandasNorte);
        var c = r.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).OrderBy(p => p.s).ToList();
        Assert.AreEqual(RotasTransito.CruzamentoIpe, c[0].indice);
        Assert.AreEqual(RotasTransito.Manobra.Direita, c[0].manobra);
        Assert.IsFalse(c[0].preferencial, "Quem vem pela Ipê para no PARE.");
        Assert.AreEqual(RotasTransito.Manobra.Esquerda, c[1].manobra);
        Assert.IsTrue(c[1].preferencial);
    }

    [Test]
    public void Cruzamento_QuemPodeIrJunto()
    {
        var M = RotasTransito.Manobra.Reto;
        Assert.IsTrue(RotasTransito.Compativeis(Vector3.right, M, Vector3.right, RotasTransito.Manobra.Esquerda), "Mesma chegada vai em fila.");
        Assert.IsTrue(RotasTransito.Compativeis(Vector3.right, M, Vector3.left, RotasTransito.Manobra.Direita));
        Assert.IsFalse(RotasTransito.Compativeis(Vector3.right, RotasTransito.Manobra.Esquerda, Vector3.left, M), "Conversão à esquerda espera quem vem de frente.");
        Assert.IsFalse(RotasTransito.Compativeis(Vector3.right, M, Vector3.forward, M), "Transversal não cruza junto.");
    }

    [Test]
    public void Semaforo_NuncaVerdeNosDoisEixos()
    {
        bool viuVerdeA = false, viuVerdeB = false;
        for (float t = 0f; t < RotasTransito.Ciclo * 2f; t += 0.1f)
        {
            var a = RotasTransito.CorSemaforo(true, t);
            var b = RotasTransito.CorSemaforo(false, t);
            Assert.IsTrue(a == RotasTransito.Cor.Vermelho || b == RotasTransito.Cor.Vermelho, $"Os dois eixos abertos em t={t}.");
            viuVerdeA |= a == RotasTransito.Cor.Verde; viuVerdeB |= b == RotasTransito.Cor.Verde;
        }
        Assert.IsTrue(viuVerdeA && viuVerdeB);
    }

    [Test]
    public void Lotacao_PorHora()
    {
        var comercial = RotasTransito.Lotacao(11f, false);
        var pico = RotasTransito.Lotacao(18f, false);
        var noite = RotasTransito.Lotacao(22.5f, false);
        var sabado = RotasTransito.Lotacao(11f, true);
        Assert.Greater(comercial.carros, noite.carros);
        Assert.Greater(comercial.pedestres, noite.pedestres);
        Assert.GreaterOrEqual(pico.carros, comercial.carros);
        Assert.Less(sabado.carros, comercial.carros);
        Assert.IsTrue(RotasTransito.FaroisAcesos(19f) && !RotasTransito.FaroisAcesos(12f));
        Assert.AreEqual(0f, JanelasNoturnas.Acesas(12f));
        Assert.Greater(JanelasNoturnas.Acesas(20f), JanelasNoturnas.Acesas(23.8f));
    }

    // Calçadas dos pedestres: nós e caminhos em cima da calçada; a única passagem pelo asfalto é a faixa de pedestre
    [Test]
    public void Calcadas_NaCalcadaOuNaFaixa()
    {
        var pedacos = Bairro.Pedacos();
        bool Calcada(Vector3 p) => pedacos.Any(r => r.tipo == TipoPiso.Calcada && r.area.Contains(new Vector2(p.x, p.z)));
        var rede = RotasTransito.Calcadas();
        foreach (var n in rede.nos) Assert.IsTrue(Calcada(n.pos), $"Nó da calçada fora da calçada: {n.pos}.");
        foreach (var a in rede.arestas)
        {
            Vector3 p0 = rede.nos[a.a].pos, p1 = rede.nos[a.b].pos;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                Vector3 p = Vector3.Lerp(p0, p1, t);
                if (a.faixa >= 0) { Assert.IsTrue(Calcada(p) || VidaNaRua.NaFaixa(a.faixa, p, 0f), $"Travessia fora da zebra: {p}."); continue; }
                // posto: a calçada leste da Jacarandás tem a guia rebaixada (asfalto no nível da rua)
                bool rebaixo = p.x > Bairro.JacarandasX + Bairro.MeiaPista && p.z > Bairro.RebaixoZ0 && p.z < Bairro.RebaixoZ1;
                if (rebaixo) continue;
                Assert.IsTrue(Calcada(p), $"Caminho do pedestre fora da calçada: {p}.");
                foreach (float lat in new[] { -0.45f, 0.75f })
                {
                    var q = p + a.meioFio * lat;
                    bool qRebaixo = q.x > Bairro.JacarandasX + Bairro.MeiaPista && q.z > Bairro.RebaixoZ0 && q.z < Bairro.RebaixoZ1;
                    Assert.IsTrue(qRebaixo || Calcada(q), $"Desvio lateral sai da calçada em {p}.");
                }
            }
        }
        Assert.AreEqual(3, rede.arestas.Count(a => a.faixa >= 0));
    }

    // Carro dos vizinhos freia e para antes de um pedestre parado na faixa dele, e para antes da zebra ocupada
    [Test]
    public void CarroNPC_ParaDianteDeObstaculo()
    {
        var goRua = new GameObject("TesteRua");
        var rua = goRua.AddComponent<VidaNaRua>();
        var goCarro = new GameObject("TesteCarro");
        var carro = goCarro.AddComponent<CarroNPC>();
        var goPed = new GameObject("TestePedestre");
        var ped = goPed.AddComponent<Pedestre>();
        try
        {
            var rota = RotasTransito.Rota(RotasTransito.Ponta.AcaciasLeste, RotasTransito.Ponta.AcaciasOeste);
            carro.Iniciar(rota, 2f, 1);
            rua.carros.Add(carro);
            // pedestre parado no meio da faixa do carro, 15 m à frente (antes do primeiro cruzamento)
            float sPed = 2f + 15f;
            goPed.transform.position = rota.Posicao(sPed);
            rua.pedestres.Add(ped);
            for (int i = 0; i < 400; i++) { rua.AtualizarObstaculos(); carro.Passo(0.05f, rua); }
            float frente = carro.s + RotasTransito.MeioComprimento;
            Assert.Less(carro.v, 0.05f, "O carro não parou.");
            Assert.Less(frente, sPed - 0.6f, "O carro encostou no pedestre.");
            Assert.Greater(frente, sPed - 4f, "O carro parou longe demais.");
            Assert.AreEqual("Pedestre", carro.motivoParada);

            // sai o pedestre: o carro anda; zebra do escritório ocupada: para antes dela
            rua.pedestres.Clear();
            rua.OcuparFaixa(1);
            var pz = rota.paradas.First(p => p.tipo == RotasTransito.TipoParada.Faixa && p.indice == 1);
            for (int i = 0; i < 1200; i++) { rua.AtualizarObstaculos(); carro.Passo(0.05f, rua); }
            Assert.Less(carro.v, 0.05f, $"Não parou na faixa (s={carro.s:0.0}, motivo '{carro.motivoParada}').");
            Assert.That(carro.s, Is.LessThanOrEqualTo(pz.s + 0.05f), "Passou da linha da faixa ocupada.");
            Assert.That(carro.s, Is.GreaterThan(pz.s - 3f));
            Assert.IsFalse(VidaNaRua.NaFaixa(1, carro.transform.position + carro.transform.forward * RotasTransito.MeioComprimento, 0f));
            rua.SoltarFaixa(1);
            for (int i = 0; i < 100; i++) { rua.AtualizarObstaculos(); carro.Passo(0.05f, rua); }
            Assert.Greater(carro.s, pz.s + 1f, "Faixa livre e o carro não seguiu.");
        }
        finally
        {
            Object.DestroyImmediate(goRua);
            Object.DestroyImmediate(goCarro);
            Object.DestroyImmediate(goPed);
        }
    }

    // Fila: o carro de trás para atrás do da frente, sem encostar
    [Test]
    public void CarroNPC_ParaAtrasDeOutroCarro()
    {
        var goRua = new GameObject("TesteRua");
        var rua = goRua.AddComponent<VidaNaRua>();
        var a = new GameObject("A").AddComponent<CarroNPC>();
        var b = new GameObject("B").AddComponent<CarroNPC>();
        try
        {
            var rota = RotasTransito.Rota(RotasTransito.Ponta.IpeNorte, RotasTransito.Ponta.IpeSul);
            a.Iniciar(rota, 20f, 1); a.v = 0f;
            b.Iniciar(rota, 2f, 2);
            rua.carros.Add(a); rua.carros.Add(b);
            for (int i = 0; i < 400; i++) { rua.AtualizarObstaculos(); b.Passo(0.05f, rua); }
            float vao = (a.s - RotasTransito.MeioComprimento) - (b.s + RotasTransito.MeioComprimento);
            Assert.Less(b.v, 0.05f);
            Assert.That(vao, Is.InRange(0.5f, 4f), $"Vão entre os carros: {vao:0.00} m.");
            Assert.AreEqual("Carro", b.motivoParada);
        }
        finally
        {
            Object.DestroyImmediate(rua.gameObject); Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject);
        }
    }
}
