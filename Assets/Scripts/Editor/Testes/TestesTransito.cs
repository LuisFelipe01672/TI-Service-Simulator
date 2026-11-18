using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Etapa 9D (vida na rua): rotas dos carros dos vizinhos (no asfalto, na mão certa, parando antes dos cruzamentos e
// das faixas), semáforo, vez no cruzamento, calçadas dos pedestres e o carro parando diante de obstáculo.
// Etapa 10E-1: os testes da malha rodam no bairro de hoje e numa grade 3 x 3 (MalhaRuas.Grade), para garantir que
// nada depende das 3 ruas do bairro.
public class TestesTransito
{
    public static IEnumerable<MalhaRuas> Malhas()
    {
        yield return Bairro.Malha;
        yield return MalhaRuas.Grade(3, 3, 40f);
        yield return MalhaRuas.GradeComT();
    }

    static List<Rect> Asfalto(MalhaRuas m) => m.Pedacos().Where(p => p.tipo == TipoPiso.Asfalto).Select(p => p.area).ToList();
    static bool NoAsfalto(List<Rect> a, Vector3 p) => a.Any(r => r.Contains(new Vector2(p.x, p.z)));
    static string Nome(MalhaRuas m, RotasTransito.Caminho r) =>
        $"[{m}] {m.ruas[m.pontas[r.de].rua]} ({m.pontas[r.de].lado})→{m.ruas[m.pontas[r.para].rua]} ({m.pontas[r.para].lado})";

    // Índice da ponta da rua 'nome' (lado -1: começo, a oeste/sul; +1: fim, a leste/norte)
    public static int Ponta(MalhaRuas m, string nome, int lado)
    {
        int r = m.Indice(nome);
        return m.pontas.FindIndex(p => p.rua == r && p.lado == lado);
    }

    [TestCaseSource(nameof(Malhas))]
    public void Rotas_TodasAsCombinacoes(MalhaRuas m)
    {
        var rotas = RotasTransito.TodasAsRotas(m);
        Assert.AreEqual(m.pontas.Count * (m.pontas.Count - 1), rotas.Count, "Falta rota entre duas pontas.");
        foreach (var r in rotas) Assert.Greater(r.Comprimento, 40f, $"Rota {Nome(m, r)} curta demais.");
    }

    // O carro inteiro (meio, laterais e cantos da frente) fica em cima do asfalto o caminho todo, inclusive nas curvas
    [TestCaseSource(nameof(Malhas))]
    public void Rotas_FicamNoAsfalto(MalhaRuas m)
    {
        var a = Asfalto(m);
        foreach (var r in RotasTransito.TodasAsRotas(m))
            for (float s = 0f; s <= r.Comprimento; s += 0.5f)
            {
                Vector3 p = r.Posicao(s), lado = RotasTransito.Direita(r.Direcao(s)) * RotasTransito.MeiaLargura;
                Vector3 frente = r.Direcao(s) * RotasTransito.MeioComprimento;
                foreach (var q in new[] { p, p + lado, p - lado, p + frente + lado * 0.8f, p + frente - lado * 0.8f })
                    Assert.IsTrue(NoAsfalto(a, q), $"Rota {Nome(m, r)} sai do asfalto em s={s:0.0} ({q}).");
            }
    }

    // Mão direita: fora dos cruzamentos, o carro anda na metade direita da rua (como no Brasil)
    [TestCaseSource(nameof(Malhas))]
    public void Rotas_NaMaoCerta(MalhaRuas m)
    {
        foreach (var r in RotasTransito.TodasAsRotas(m))
            for (float s = 0f; s <= r.Comprimento; s += 1f)
            {
                Vector3 p = r.Posicao(s), d = r.Direcao(s);
                bool perto = m.cruzamentos.Any(c => Mathf.Abs(p.z - c.centro.z) < 6f && Mathf.Abs(p.x - c.centro.x) < 6f);
                if (perto) continue;   // curva no cruzamento
                bool lo = Mathf.Abs(d.x) > 0.95f;
                if (!lo && Mathf.Abs(d.z) < 0.95f) { Assert.Fail($"Rota {Nome(m, r)} torta fora do cruzamento em s={s}."); continue; }
                // rua por onde anda: a do mesmo sentido com o eixo mais perto
                var rua = m.ruas.Where(x => x.lesteOeste == lo && x.Ao(p) >= x.ini && x.Ao(p) <= x.fim).OrderBy(x => Mathf.Abs(x.Lateral(p))).First();
                float lado = Vector3.Dot(p - rua.Ponto(rua.Ao(p)), RotasTransito.Direita(d));
                Assert.That(lado, Is.EqualTo(RotasTransito.DeslocFaixa).Within(0.05f), $"Rota {Nome(m, r)} na contramão em s={s} ({p}).");
            }
    }

    // Quem chega no cruzamento para com a frente fora dele e fora da zebra; atravessa cada cruzamento uma vez
    [TestCaseSource(nameof(Malhas))]
    public void Rotas_ParamAntesDoCruzamentoEDaFaixa(MalhaRuas m)
    {
        foreach (var r in RotasTransito.TodasAsRotas(m))
        {
            var cruz = r.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).ToList();
            Assert.AreEqual(cruz.Count, cruz.Select(p => p.indice).Distinct().Count(), $"Rota {Nome(m, r)} passa duas vezes no mesmo cruzamento.");
            if (m.pontas[r.de].rua != m.pontas[r.para].rua || m.CruzamentosDa(m.pontas[r.de].rua).Count > 0)
                Assert.Greater(cruz.Count, 0, $"Rota {Nome(m, r)} sem cruzamento.");
            foreach (var p in r.paradas)
            {
                Vector3 frente = r.Posicao(p.s) + r.Direcao(p.s) * RotasTransito.MeioComprimento;
                Assert.Less(p.s, p.sSaida);
                Assert.Greater(p.s, 0f, $"Rota {Nome(m, r)}: parada antes do começo.");
                if (p.tipo == RotasTransito.TipoParada.Cruzamento)
                    foreach (var c in m.cruzamentos)
                    {
                        Vector3 rel = frente - c.centro;
                        bool dentro = Mathf.Abs(rel.x) < m.ruas[c.ruaNS].meiaPista && Mathf.Abs(rel.z) < m.ruas[c.ruaLO].meiaPista;
                        Assert.IsFalse(dentro, $"Rota {Nome(m, r)} para dentro do cruzamento {c.nome} ({frente}).");
                    }
                for (int f = 0; f < m.faixas.Count; f++)
                    Assert.IsFalse(RotasTransito.SobreFaixa(m, f, frente, -0.1f), $"Rota {Nome(m, r)} para em cima da faixa {f} ({frente}).");
            }
        }
    }

    // As rotas seguem o caminho mais curto pelas ruas: o comprimento fica perto da distância "em L" entre as pontas
    [TestCaseSource(nameof(Malhas))]
    public void Rotas_PeloCaminhoMaisCurto(MalhaRuas m)
    {
        foreach (var r in RotasTransito.TodasAsRotas(m))
        {
            Vector3 a = RotasTransito.EixoNaPonta(m, r.de), b = RotasTransito.EixoNaPonta(m, r.para);
            var nos = m.CaminhoEntrePontas(r.de, r.para);
            float pelasRuas = 0f;
            for (int i = 1; i < nos.Count; i++) pelasRuas += (m.PosNo(nos[i]) - m.PosNo(nos[i - 1])).magnitude;
            float manhattan = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
            // a rota não é mais longa que o caminho pelas ruas, e o caminho não dá volta além de uma quadra para cada lado
            // (pontas de ruas paralelas na mesma borda obrigam a ir até o cruzamento e voltar)
            Assert.LessOrEqual(r.Comprimento, pelasRuas + 1f, $"Rota {Nome(m, r)} mais longa que o caminho pelas ruas.");
            if (m.pontas[r.de].rua != m.pontas[r.para].rua)
                Assert.That(pelasRuas - 2f * RotasTransito.RecuoPonta, Is.LessThanOrEqualTo(manhattan + 0.5f + 2f * MaiorQuadra(m)), $"Rota {Nome(m, r)} dá volta.");
        }
    }

    static float MaiorQuadra(MalhaRuas m)
    {
        float maior = 0f;
        foreach (var lo in new[] { true, false })
        {
            var e = m.ruas.Where(r => r.lesteOeste == lo).Select(r => r.eixo).OrderBy(x => x).ToList();
            for (int i = 1; i < e.Count; i++) maior = Mathf.Max(maior, e[i] - e[i - 1]);
        }
        return maior;
    }

    [Test]
    public void Rotas_ManobrasEPreferencial()
    {
        var m = Bairro.Malha;
        // Acácias de ponta a ponta: reto e preferencial nos 6 cruzamentos, passando nas 7 zebras e nos 2 semáforos
        var reta = RotasTransito.Rota(m, Ponta(m, Bairro.RuaAcacias, -1), Ponta(m, Bairro.RuaAcacias, 1));
        var cr = reta.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).ToList();
        Assert.AreEqual(6, cr.Count);
        Assert.AreEqual(7, reta.paradas.Count(p => p.tipo == RotasTransito.TipoParada.Faixa), "Pela Acácias inteira passa nas 7 faixas.");
        Assert.IsTrue(cr.All(p => p.preferencial && p.manobra == RotasTransito.Manobra.Reto));
        Assert.AreEqual(2, cr.Count(p => m.cruzamentos[p.indice].controle == ControleCruzamento.Semaforo));
        // Cedros norte → Acácias oeste: passa reto no "T" da Sibipirunas (preferencial), para no PARE da Paineiras e
        // vira à direita no PARE da Acácias
        var r = RotasTransito.Rota(m, Ponta(m, Bairro.RuaCedros, 1), Ponta(m, Bairro.RuaAcacias, -1));
        var c = r.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).OrderBy(p => p.s).ToList();
        Assert.AreEqual(3, c.Count);
        Assert.AreEqual(m.CruzamentoDe(Bairro.RuaSibipirunas, Bairro.RuaCedros), c[0].indice);
        Assert.IsTrue(c[0].preferencial && c[0].manobra == RotasTransito.Manobra.Reto, "No \"T\" quem passa reto é a principal.");
        Assert.IsFalse(c[1].preferencial, "Quem vem pela Cedros para no PARE da avenida.");
        Assert.AreEqual(m.CruzamentoDe(Bairro.RuaAcacias, Bairro.RuaCedros), c[2].indice);
        Assert.AreEqual(RotasTransito.Manobra.Direita, c[2].manobra);
        Assert.IsFalse(c[2].preferencial);
        // Acácias oeste → Cedros norte: vira à esquerda na Cedros, vindo pela preferencial
        var e = RotasTransito.Rota(m, Ponta(m, Bairro.RuaAcacias, -1), Ponta(m, Bairro.RuaCedros, 1));
        var ce = e.paradas.Where(p => p.tipo == RotasTransito.TipoParada.Cruzamento).OrderBy(p => p.s).ToList();
        Assert.AreEqual(RotasTransito.Manobra.Esquerda, ce[0].manobra);
        Assert.IsTrue(ce[0].preferencial);
    }

    // A cidade (Etapa 10E-2): as 3 ruas de antes nos mesmos lugares, mais 8 ruas novas numa grade com "T"
    [Test]
    public void Malha_BairroIgualAoH()
    {
        var m = Bairro.Malha;
        Assert.AreEqual(11, m.ruas.Count);
        Assert.AreEqual(30, m.cruzamentos.Count);
        Assert.AreEqual(12, m.pontas.Count, "Pontas: Acácias, Paineiras, Ipê, Jacarandás, Cedros e Oitis (as outras terminam em \"T\").");
        var ac = m.ruas[m.Indice(Bairro.RuaAcacias)];
        Assert.That(ac.AsfaltoPos, Is.EqualTo(Bairro.AcaciasAsfaltoZ0).Within(0.001f));
        Assert.That(ac.AsfaltoNeg, Is.EqualTo(Bairro.AcaciasAsfaltoZ1).Within(0.001f));
        Assert.That(ac.FrentePos, Is.EqualTo(Bairro.FrenteNorteAcacias).Within(0.001f));
        Assert.That(ac.FrenteNeg, Is.EqualTo(Bairro.FrenteSulAcacias).Within(0.001f));
        Assert.That(m.ruas[m.Indice(Bairro.RuaIpe)].eixo, Is.EqualTo(Bairro.IpeX));
        Assert.That(m.ruas[m.Indice(Bairro.RuaJacarandas)].eixo, Is.EqualTo(Bairro.JacarandasX));
        int ipe = m.CruzamentoDe(Bairro.RuaAcacias, Bairro.RuaIpe), jac = m.CruzamentoDe(Bairro.RuaAcacias, Bairro.RuaJacarandas);
        Assert.AreEqual(new Vector3(Bairro.IpeX, 0f, Bairro.AcaciasCentroZ), m.cruzamentos[ipe].centro);
        Assert.AreEqual(new Vector3(Bairro.JacarandasX, 0f, Bairro.AcaciasCentroZ), m.cruzamentos[jac].centro);
        Assert.AreEqual(ControleCruzamento.Pare, m.cruzamentos[ipe].controle);
        Assert.AreEqual(ControleCruzamento.Semaforo, m.cruzamentos[jac].controle);
        Assert.AreEqual(0f, m.cruzamentos[jac].defasagem, "O semáforo de sempre continua no relógio de antes.");
        // as 3 zebras de antes continuam na Acácias
        foreach (float x in Bairro.FaixasPedestreX) Assert.IsTrue(m.faixas.Any(f => f.rua == m.Indice(Bairro.RuaAcacias) && Mathf.Abs(f.pos - x) < 0.01f));
        // ~6x maior que o bairro de antes (124 x 89 m)
        Assert.That(m.limites.width * m.limites.height / (124f * 89f), Is.InRange(5.5f, 10f));
    }

    // Toda ponta fica na borda do mapa; todo fim de rua que não é ponta é um "T" (cai no eixo de outra rua)
    [TestCaseSource(nameof(Malhas))]
    public void Malha_PontasNaBordaETs(MalhaRuas m)
    {
        var lim = m.limites;
        foreach (var p in m.pontas)
        {
            var q = m.ruas[p.rua].Ponto(p.lado < 0 ? m.ruas[p.rua].ini : m.ruas[p.rua].fim);
            bool borda = Mathf.Abs(q.x - lim.xMin) < 0.01f || Mathf.Abs(q.x - lim.xMax) < 0.01f || Mathf.Abs(q.z - lim.yMin) < 0.01f || Mathf.Abs(q.z - lim.yMax) < 0.01f;
            Assert.IsTrue(borda, $"[{m}] Ponta da {m.ruas[p.rua]} fora da borda: {q}.");
        }
        for (int i = 0; i < m.ruas.Count; i++)
            foreach (int lado in new[] { -1, 1 })
            {
                bool ponta = m.pontas.Any(p => p.rua == i && p.lado == lado);
                int k = m.CruzamentoNoFim(i, lado);
                Assert.IsTrue(ponta ^ (k >= 0), $"[{m}] {m.ruas[i]}: o fim {lado} tem que ser ponta ou \"T\".");
                if (k < 0) continue;
                // no "T" falta só o braço desta rua para o lado de fora; a outra rua passa reto e é a principal
                var o = m.Transversal(k, i);
                int bracos = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }.Count(d => m.Braco(k, d));
                Assert.AreEqual(3, bracos, $"[{m}] {m.cruzamentos[k].nome}: \"T\" sem 3 braços.");
                if (m.cruzamentos[k].controle == ControleCruzamento.Pare)
                    Assert.AreEqual(o.lesteOeste, m.cruzamentos[k].principalLesteOeste, $"[{m}] {m.cruzamentos[k].nome}: no \"T\" a principal é a que passa reto.");
            }
    }

    [Test]
    public void Malha_Grade3x3()
    {
        var m = MalhaRuas.Grade(3, 3, 40f);
        Assert.AreEqual(6, m.ruas.Count);
        Assert.AreEqual(9, m.cruzamentos.Count);
        Assert.AreEqual(12, m.pontas.Count);
        Assert.AreEqual(1, m.cruzamentos.Count(c => c.controle == ControleCruzamento.Semaforo));
        Assert.AreEqual(Vector3.zero, m.cruzamentos[4].centro, "O cruzamento do meio fica na origem.");
        foreach (var r in m.ruas) Assert.AreEqual(3, m.CruzamentosDa(m.ruas.IndexOf(r)).Count);
    }

    [Test]
    public void Malha_GradeComT()
    {
        var m = MalhaRuas.GradeComT();
        Assert.AreEqual(6, m.ruas.Count);
        Assert.AreEqual(9, m.cruzamentos.Count);
        Assert.AreEqual(8, m.pontas.Count, "A, D e F nas duas bordas; C e E numa só; B termina em \"T\" nos dois lados.");
        Assert.AreEqual(4, Enumerable.Range(0, m.cruzamentos.Count).Count(k => new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }.Count(d => m.Braco(k, d)) == 3));
        Assert.Greater(m.faixas.Count, 10, "Uma zebra em cada trecho.");
    }

    // Semáforos com relógio por cruzamento: nunca verde nos dois eixos, e cruzamentos diferentes em tempos diferentes
    [Test]
    public void Semaforo_RelogioPorCruzamento()
    {
        var m = Bairro.Malha;
        var sem = Enumerable.Range(0, m.cruzamentos.Count).Where(k => m.cruzamentos[k].controle == ControleCruzamento.Semaforo).ToList();
        Assert.AreEqual(4, sem.Count);
        Assert.AreEqual(sem.Count, sem.Select(k => m.cruzamentos[k].defasagem).Distinct().Count(), "Dois semáforos no mesmo relógio.");
        bool diferentes = false;
        for (float t = 0f; t < RotasTransito.Ciclo; t += 0.5f)
        {
            foreach (int k in sem)
            {
                var a = RotasTransito.CorSemaforo(m, k, true, t);
                var b = RotasTransito.CorSemaforo(m, k, false, t);
                Assert.IsTrue(a == RotasTransito.Cor.Vermelho || b == RotasTransito.Cor.Vermelho, $"Os dois eixos abertos em {m.cruzamentos[k].nome}, t={t}.");
            }
            diferentes |= sem.Select(k => RotasTransito.CorSemaforo(m, k, true, t)).Distinct().Count() > 1;
        }
        Assert.IsTrue(diferentes, "Todos os semáforos mudam juntos.");
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

    // Guia rebaixada (posto): calçada que virou asfalto no nível da rua
    static bool NoRebaixo(MalhaRuas m, Vector3 p)
    {
        foreach (var rb in m.rebaixos)
        {
            var r = m.ruas[rb.rua];
            float t0 = rb.lado > 0 ? r.AsfaltoPos : r.FrenteNeg, t1 = rb.lado > 0 ? r.FrentePos : r.AsfaltoNeg;
            if (r.Area(rb.a0, rb.a1, t0, t1).Contains(new Vector2(p.x, p.z))) return true;
        }
        return false;
    }

    // Calçadas dos pedestres: nós e caminhos em cima da calçada; a única passagem pelo asfalto é a faixa de pedestre
    [TestCaseSource(nameof(Malhas))]
    public void Calcadas_NaCalcadaOuNaFaixa(MalhaRuas m)
    {
        var pedacos = m.Pedacos();
        bool Calcada(Vector3 p) => pedacos.Any(r => r.tipo == TipoPiso.Calcada && r.area.Contains(new Vector2(p.x, p.z)));
        var rede = RotasTransito.Calcadas(m);
        foreach (var n in rede.nos) Assert.IsTrue(Calcada(n.pos), $"[{m}] Nó da calçada fora da calçada: {n.pos}.");
        foreach (var a in rede.arestas)
        {
            Vector3 p0 = rede.nos[a.a].pos, p1 = rede.nos[a.b].pos;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                Vector3 p = Vector3.Lerp(p0, p1, t);
                if (a.faixa >= 0) { Assert.IsTrue(Calcada(p) || RotasTransito.SobreFaixa(m, a.faixa, p, 0f), $"[{m}] Travessia fora da zebra: {p}."); continue; }
                if (NoRebaixo(m, p)) continue;
                Assert.IsTrue(Calcada(p), $"[{m}] Caminho do pedestre fora da calçada: {p}.");
                foreach (float lat in new[] { -0.45f, 0.75f })
                {
                    var q = p + a.meioFio * lat;
                    Assert.IsTrue(NoRebaixo(m, q) || Calcada(q), $"[{m}] Desvio lateral sai da calçada em {p}.");
                }
            }
        }
        Assert.AreEqual(m.faixas.Count, rede.arestas.Count(a => a.faixa >= 0), $"[{m}] Faixa de pedestre sem as duas calçadas.");
        Assert.Greater(rede.nos.Count(n => n.ponta), 0, $"[{m}] Sem ponta de calçada (onde o pedestre nasce).");
        // toda ponta leva a algum lugar e todo nó tem saída
        for (int i = 0; i < rede.nos.Count; i++) Assert.Greater(rede.Vizinhas(i).Count, 0, $"[{m}] Nó solto: {rede.nos[i].pos}.");
    }

    // Dá para ir a pé de qualquer ponto da calçada a qualquer outro (as zebras ligam todos os quarteirões)
    [TestCaseSource(nameof(Malhas))]
    public void Calcadas_TudoLigadoAPe(MalhaRuas m)
    {
        var rede = RotasTransito.Calcadas(m);
        var visto = new bool[rede.nos.Count];
        var fila = new Queue<int>();
        visto[0] = true; fila.Enqueue(0);
        while (fila.Count > 0)
        {
            int n = fila.Dequeue();
            foreach (int a in rede.Vizinhas(n)) { int o = rede.Outro(a, n); if (!visto[o]) { visto[o] = true; fila.Enqueue(o); } }
        }
        for (int i = 0; i < rede.nos.Count; i++) Assert.IsTrue(visto[i], $"[{m}] Calçada sem caminho a pé até o resto: {rede.nos[i].pos}.");
    }

    // Carro dos vizinhos freia e para antes de um pedestre parado na faixa dele, e para antes da zebra ocupada
    [Test]
    public void CarroNPC_ParaDianteDeObstaculo()
    {
        var m = Bairro.Malha;
        var goRua = new GameObject("TesteRua");
        var rua = goRua.AddComponent<VidaNaRua>();
        var goCarro = new GameObject("TesteCarro");
        var carro = goCarro.AddComponent<CarroNPC>();
        var goPed = new GameObject("TestePedestre");
        var ped = goPed.AddComponent<Pedestre>();
        try
        {
            var rota = RotasTransito.Rota(m, Ponta(m, Bairro.RuaAcacias, 1), Ponta(m, Bairro.RuaAcacias, -1));
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
            int faixaEscritorio = m.faixas.FindIndex(f => Mathf.Abs(f.pos) < 0.01f);
            rua.pedestres.Clear();
            rua.OcuparFaixa(faixaEscritorio);
            var pz = rota.paradas.First(p => p.tipo == RotasTransito.TipoParada.Faixa && p.indice == faixaEscritorio);
            for (int i = 0; i < 1200; i++) { rua.AtualizarObstaculos(); carro.Passo(0.05f, rua); }
            Assert.Less(carro.v, 0.05f, $"Não parou na faixa (s={carro.s:0.0}, motivo '{carro.motivoParada}').");
            Assert.That(carro.s, Is.LessThanOrEqualTo(pz.s + 0.05f), "Passou da linha da faixa ocupada.");
            Assert.That(carro.s, Is.GreaterThan(pz.s - 3f));
            Assert.IsFalse(rua.NaFaixa(faixaEscritorio, carro.transform.position + carro.transform.forward * RotasTransito.MeioComprimento, 0f));
            rua.SoltarFaixa(faixaEscritorio);
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
        var m = Bairro.Malha;
        var goRua = new GameObject("TesteRua");
        var rua = goRua.AddComponent<VidaNaRua>();
        var a = new GameObject("A").AddComponent<CarroNPC>();
        var b = new GameObject("B").AddComponent<CarroNPC>();
        try
        {
            var rota = RotasTransito.Rota(m, Ponta(m, Bairro.RuaIpe, 1), Ponta(m, Bairro.RuaIpe, -1));
            a.Iniciar(rota, 40f, 1); a.v = 0f;   // no meio do quarteirão (depois do PARE da Sibipirunas)
            b.Iniciar(rota, 2f, 2);
            rua.carros.Add(a); rua.carros.Add(b);
            for (int i = 0; i < 800; i++) { rua.AtualizarObstaculos(); b.Passo(0.05f, rua); }
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

    // Grade 3 x 3: um carro atravessa a cidade virando e parando nos PAREs, e chega no fim da rota
    [Test]
    public void CarroNPC_AtravessaAGrade()
    {
        var m = MalhaRuas.Grade(3, 3, 40f);
        var goRua = new GameObject("TesteRua");
        var rua = goRua.AddComponent<VidaNaRua>();
        rua.UsarMalha(m);
        var carro = new GameObject("TesteCarro").AddComponent<CarroNPC>();
        try
        {
            // rota com curva e PARE (vindo pela transversal), sem semáforo (o relógio não anda no modo de edição)
            var rota = RotasTransito.TodasAsRotas(m)
                .Where(r => r.paradas.Any(p => p.tipo == RotasTransito.TipoParada.Cruzamento && !p.preferencial)
                         && r.paradas.Any(p => p.tipo == RotasTransito.TipoParada.Cruzamento && p.manobra != RotasTransito.Manobra.Reto)
                         && r.paradas.All(p => p.tipo != RotasTransito.TipoParada.Cruzamento || m.cruzamentos[p.indice].controle == ControleCruzamento.Pare))
                .OrderByDescending(r => r.Comprimento).First();
            carro.Iniciar(rota, 0f, 1);
            rua.carros.Add(carro);
            bool parouNoPare = false;
            for (int i = 0; i < 4000 && !carro.chegouNoFim; i++)
            {
                rua.AtualizarObstaculos();
                carro.Passo(0.05f, rua);
                parouNoPare |= carro.parouNoPare;
            }
            Assert.IsTrue(carro.chegouNoFim, $"O carro travou em s={carro.s:0.0} de {rota.Comprimento:0.0} (motivo '{carro.motivoParada}').");
            Assert.IsTrue(parouNoPare, "Passou pelo PARE sem parar.");
            Assert.AreEqual(0, Enumerable.Range(0, m.cruzamentos.Count).Sum(k => rua.Reservas(k)), "Ficou cruzamento reservado.");
        }
        finally
        {
            Object.DestroyImmediate(goRua); Object.DestroyImmediate(carro.gameObject);
        }
    }

    // ---------------- GPS: caminho mais curto pelas ruas ----------------

    [Test]
    public void GPS_CaminhoPeloBairro()
    {
        var m = Bairro.Malha;
        Vector3 jac = m.cruzamentos[m.CruzamentoDe(Bairro.RuaAcacias, Bairro.RuaJacarandas)].centro;
        Vector3 ipe = m.cruzamentos[m.CruzamentoDe(Bairro.RuaAcacias, Bairro.RuaIpe)].centro;
        // da clínica (oeste) ao fornecedor (Jacarandás, ao sul): pela Acácias, virando na Jacarandás
        var c = m.CaminhoMaisCurto(Bairro.De(TipoEndereco.Clinica).porta, Bairro.De(TipoEndereco.Fornecedor).porta);
        Assert.AreEqual(4, c.Count, "Clínica → fornecedor: Acácias (passando a Ipê) e vira na Jacarandás.");
        Assert.AreEqual(ipe, c[1]); Assert.AreEqual(jac, c[2]);
        Assert.That(c[c.Count - 1].x, Is.EqualTo(Bairro.JacarandasX).Within(0.01f), "Não termina na Jacarandás.");
        // na mesma rua, sem cruzamento no meio: direto
        var d = m.CaminhoMaisCurto(Bairro.De(TipoEndereco.Escritorio).porta, Bairro.De(TipoEndereco.Casa).porta);
        Assert.AreEqual(2, d.Count, "Escritório → casa é atravessar a rua.");
        // da coleta (Ipê, ao sul) ao posto (Jacarandás, ao norte): Ipê → Acácias → Jacarandás
        var e = m.CaminhoMaisCurto(Bairro.De(TipoEndereco.Coleta).porta, Bairro.De(TipoEndereco.Posto).porta);
        Assert.AreEqual(4, e.Count);
        Assert.AreEqual(ipe, e[1]); Assert.AreEqual(jac, e[2]);
    }

    [Test]
    public void GPS_CaminhoNaGrade()
    {
        var m = MalhaRuas.Grade(3, 3, 40f);
        // de um canto a outro: anda "em L" pelas ruas (nunca mais que a distância de Manhattan entre os eixos)
        Vector3 de = new Vector3(-40f, 0f, -60f), para = new Vector3(60f, 0f, 40f);
        var c = m.CaminhoMaisCurto(de, para);
        float l = 0f;
        for (int i = 1; i < c.Count; i++)
        {
            l += (c[i] - c[i - 1]).magnitude;
            var v = c[i] - c[i - 1];
            Assert.IsTrue(Mathf.Abs(v.x) < 0.01f || Mathf.Abs(v.z) < 0.01f, "Trecho do caminho fora das ruas (na diagonal).");
        }
        Assert.That(l, Is.EqualTo(Mathf.Abs(c[0].x - c[c.Count - 1].x) + Mathf.Abs(c[0].z - c[c.Count - 1].z)).Within(0.01f));
        // o GPS aponta para o próximo cruzamento, não para o destino em linha reta
        var prox = GPS.ProximoPonto(c, de, para, out float dist);
        Assert.AreEqual(c[1], prox);
        Assert.That(dist, Is.GreaterThanOrEqualTo(l - 0.01f));
        Assert.AreEqual("siga em frente", GPS.Direcao(Vector3.forward, de, prox));
        // em cima do cruzamento: já aponta para o seguinte
        var c2 = m.CaminhoMaisCurto(c[1], para);
        var prox2 = GPS.ProximoPonto(c2, c[1], para, out _);
        Assert.Greater((prox2 - c[1]).magnitude, GPS.PertoDoPonto);
    }
}
