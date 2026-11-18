using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Planta do cenário (sem abrir a cena): os cômodos montados pelo MontarCenario não podem se sobrepor nem cair na rua,
// e o bairro (Cidade/Bairro.cs) precisa levar a pé de qualquer endereço a qualquer outro.
// Nasceu de um erro real: o banheiro foi calculado antes da posição da casa e apareceu na frente da porta do escritório.
// Etapa 10E-1: os testes das ruas (sobreposição, ligação, postes nas esquinas) rodam no bairro e na grade 3 x 3.
public class TestesPlantaCenario
{
    struct Comodo
    {
        public string nome; public Vector3 centro; public float largura, profundidade;
        // Planta por fora das paredes (15 cm de cada lado)
        public Rect Planta => new Rect(centro.x - largura / 2f - MontarCenario.T, centro.z - profundidade / 2f - MontarCenario.T,
                                       largura + 2f * MontarCenario.T, profundidade + 2f * MontarCenario.T);
    }

    static List<Comodo> Comodos() => new List<Comodo>
    {
        new Comodo { nome = "Escritório", centro = Bairro.CentroEscritorio, largura = 6f, profundidade = 6f },
        new Comodo { nome = "Casa", centro = MontarCenario.CentroCasa, largura = 8f, profundidade = 6f },
        new Comodo { nome = "Banheiro", centro = MontarCenario.CentroBanheiro, largura = MontarCenario.LarguraBanheiro, profundidade = MontarCenario.ProfBanheiro },
        new Comodo { nome = "Cliente (contabilidade)", centro = MontarCenario.CentroContabilidade, largura = 10f, profundidade = 8f },
        new Comodo { nome = "Clínica", centro = MontarCenario.CentroClinica, largura = 10f, profundidade = 8f },
        new Comodo { nome = "Loja", centro = MontarCenario.CentroLoja, largura = 10f, profundidade = 8f },
    };

    static Rect Encolher(Rect r, float m) => Rect.MinMaxRect(r.xMin + m, r.yMin + m, r.xMax - m, r.yMax - m);

    [Test]
    public void Comodos_NaoSeSobrepoem()
    {
        var c = Comodos();
        for (int i = 0; i < c.Count; i++)
            for (int j = i + 1; j < c.Count; j++)
                Assert.IsFalse(Encolher(c[i].Planta, 0.01f).Overlaps(c[j].Planta), $"{c[i].nome} está em cima de {c[j].nome}.");
    }

    // A rua (calçadas e asfalto) entre o escritório e a casa fica livre: dá para sair do escritório e chegar em casa
    [Test]
    public void Rua_LivreEntreEscritorioECasa()
    {
        var rua = Rect.MinMaxRect(-5f, Bairro.AcaciasCalcadaSulZ1, 5f, Bairro.AcaciasCalcadaNorteZ0);
        foreach (var c in Comodos())
            Assert.IsFalse(Encolher(c.Planta, 0.01f).Overlaps(rua), $"{c.nome} está no meio da rua, na frente do escritório.");
    }

    // Nenhum cômodo invade asfalto ou calçada de nenhuma rua do bairro (pode encostar no alinhamento)
    [Test]
    public void Comodos_ForaDasRuas()
    {
        foreach (var c in Comodos())
            foreach (var p in Bairro.Pedacos())
                Assert.IsFalse(Encolher(c.Planta, 0.01f).Overlaps(p.area),
                    $"{c.nome} invade {(p.tipo == TipoPiso.Asfalto ? "o asfalto" : "a calçada")} da {p.rua} em {p.area}.");
    }

    // O banheiro fica encostado na parede sul da casa (atrás dela), não solto no mapa
    [Test]
    public void Banheiro_EncostadoNaCasa()
    {
        var casa = Comodos()[1].Planta;
        var banheiro = Comodos()[2].Planta;
        Assert.That(banheiro.yMax, Is.EqualTo(casa.yMin).Within(0.05f), "O banheiro não encosta na parede sul da casa.");
        Assert.That(banheiro.center.x, Is.InRange(casa.xMin, casa.xMax), "O banheiro está fora da largura da casa.");
    }

    // Prédios com porta para a rua: a parede da porta fica no alinhamento da calçada (a porta abre direto nela)
    [Test]
    public void Clientes_ComPortaNaCalcada()
    {
        foreach (var c in Comodos().Where(c => c.nome.StartsWith("Cliente") || c.nome == "Clínica" || c.nome == "Escritório"))
            Assert.That(c.Planta.yMin, Is.EqualTo(Bairro.FrenteNorteAcacias).Within(0.05f), $"{c.nome}: a frente não encosta na calçada norte.");
        var loja = Comodos().First(c => c.nome == "Loja");
        Assert.That(loja.Planta.yMax, Is.EqualTo(Bairro.FrenteSulAcacias).Within(0.05f), "Loja: a frente não encosta na calçada sul.");
    }

    static IEnumerable<MalhaRuas> Malhas() => TestesTransito.Malhas();

    // Pedaços de rua e calçada não se sobrepõem (piso em cima de piso pisca na tela)
    [TestCaseSource(nameof(Malhas))]
    public void Ruas_SemSobreposicao(MalhaRuas m)
    {
        var p = m.Pedacos();
        for (int i = 0; i < p.Count; i++)
            for (int j = i + 1; j < p.Count; j++)
                Assert.IsFalse(Encolher(p[i].area, 0.01f).Overlaps(p[j].area), $"{p[i].tipo} da {p[i].rua} {p[i].area} sobre {p[j].tipo} da {p[j].rua} {p[j].area}.");
    }

    // Dá para ir a pé de qualquer pedaço de rua a qualquer outro (sem ilha de calçada solta)
    [TestCaseSource(nameof(Malhas))]
    public void Ruas_TodasLigadas(MalhaRuas m)
    {
        var p = m.Pedacos();
        var visto = new bool[p.Count];
        var fila = new Queue<int>();
        visto[0] = true; fila.Enqueue(0);
        while (fila.Count > 0)
        {
            int i = fila.Dequeue();
            var a = Rect.MinMaxRect(p[i].area.xMin - 0.05f, p[i].area.yMin - 0.05f, p[i].area.xMax + 0.05f, p[i].area.yMax + 0.05f);
            for (int j = 0; j < p.Count; j++)
                if (!visto[j] && a.Overlaps(p[j].area)) { visto[j] = true; fila.Enqueue(j); }
        }
        for (int j = 0; j < p.Count; j++)
            Assert.IsTrue(visto[j], $"[{m}] {p[j].tipo} da {p[j].rua} em {p[j].area} não se liga ao resto das ruas.");
    }

    // Os pedaços cobrem cada rua inteira: asfalto de ponta a ponta e calçada dos dois lados fora dos cruzamentos
    [TestCaseSource(nameof(Malhas))]
    public void Ruas_CobertasDePontaAPonta(MalhaRuas m)
    {
        var p = m.Pedacos();
        bool Tem(TipoPiso t, Vector3 q) => p.Any(x => x.tipo == t && x.area.Contains(new Vector2(q.x, q.z)));
        for (int i = 0; i < m.ruas.Count; i++)
        {
            var r = m.ruas[i];
            for (float a = r.ini + 0.2f; a < r.fim - 0.2f; a += 0.5f)
            {
                Assert.IsTrue(Tem(TipoPiso.Asfalto, r.Ponto(a)), $"[{m}] {r} sem asfalto em {r.Ponto(a)}.");
                bool cruzamento = m.CruzamentosDa(i).Any(k => Mathf.Abs(a - r.Ao(m.cruzamentos[k].centro)) < m.Transversal(k, i).FrentePos - m.Transversal(k, i).eixo + 0.3f);
                if (cruzamento) continue;
                foreach (int lado in new[] { -1, 1 })
                {
                    var q = r.Ponto(a, (lado > 0 ? (r.AsfaltoPos + r.FrentePos) / 2f : (r.AsfaltoNeg + r.FrenteNeg) / 2f) - r.eixo);
                    Assert.IsTrue(Tem(TipoPiso.Calcada, q) || m.rebaixos.Any(x => x.rua == i && x.lado == lado && a > x.a0 && a < x.a1), $"[{m}] {r} sem calçada em {q}.");
                }
            }
        }
    }

    // Todo endereço do GPS existe uma vez só e fica em cima de uma calçada
    [Test]
    public void Enderecos_NaCalcada()
    {
        var e = Bairro.Enderecos;
        foreach (TipoEndereco t in System.Enum.GetValues(typeof(TipoEndereco)))
            Assert.AreEqual(1, e.Count(x => x.tipo == t), $"Endereço de {t} faltando ou repetido.");
        foreach (var x in e)
        {
            var xz = new Vector2(x.porta.x, x.porta.z);
            if (x.tipo == TipoEndereco.Posto)
            {
                Assert.IsTrue(Bairro.TerrenoPosto.Contains(xz), $"O ponto do GPS do posto está fora do terreno: {x.porta}.");
                continue;
            }
            Assert.IsTrue(Bairro.Pedacos().Any(p => p.tipo == TipoPiso.Calcada && p.area.Contains(xz)),
                $"A porta de {x.nome} ({x.Linha}) não está numa calçada: {x.porta}.");
        }
    }

    // A porta de cada prédio com interior fica a menos de 1,5 m do ponto do GPS
    [Test]
    public void Enderecos_NaFrenteDaPorta()
    {
        void Conferir(TipoEndereco t, Vector3 porta)
        {
            var e = Bairro.De(t);
            Assert.That(Vector2.Distance(new Vector2(e.porta.x, e.porta.z), new Vector2(porta.x, porta.z)), Is.LessThan(1.5f), $"GPS de {t} longe da porta.");
        }
        Conferir(TipoEndereco.Escritorio, new Vector3(0f, 0f, -3f - MontarCenario.T));
        Conferir(TipoEndereco.Contabilidade, MontarCenario.CentroContabilidade + new Vector3(0f, 0f, -4f - MontarCenario.T));
        Conferir(TipoEndereco.Clinica, MontarCenario.CentroClinica + new Vector3(0f, 0f, -4f - MontarCenario.T));
        Conferir(TipoEndereco.Loja, MontarCenario.CentroLoja + new Vector3(0f, 0f, 4f + MontarCenario.T));   // girada: porta ao norte
        Conferir(TipoEndereco.Casa, MontarCenario.CentroCasa + new Vector3(1.5f, 0f, 3f + MontarCenario.T + 1.3f));   // portão no muro
    }

    [Test]
    public void GPS_DirecaoPeloOlhar()
    {
        var de = Vector3.zero;
        Assert.AreEqual("siga em frente", GPS.Direcao(Vector3.forward, de, new Vector3(0, 0, 10)));
        Assert.AreEqual("vire à direita", GPS.Direcao(Vector3.forward, de, new Vector3(10, 0, 0)));
        Assert.AreEqual("vire à esquerda", GPS.Direcao(Vector3.forward, de, new Vector3(-10, 0, 0)));
        Assert.AreEqual("dê meia-volta", GPS.Direcao(Vector3.forward, de, new Vector3(0, 0, -10)));
        Assert.AreEqual("siga à direita", GPS.Direcao(Vector3.forward, de, new Vector3(5, 0, 10)));
    }

    // Cada sala de cliente tem um endereço no GPS
    [Test]
    public void Salas_TemEndereco()
    {
        Assert.AreEqual(TipoEndereco.Contabilidade, Bairro.DoTipoSala(TipoSala.Contabilidade));
        Assert.AreEqual(TipoEndereco.Clinica, Bairro.DoTipoSala(TipoSala.Clinica));
        Assert.AreEqual(TipoEndereco.Loja, Bairro.DoTipoSala(TipoSala.Loja));
    }

    // ---------------- Etapa 9C: vagas e posto ----------------

    // Retângulo que o carro ocupa estacionado (3,9 x 1,75 m), no plano x-z
    static Rect AreaDoCarro(Bairro.Vaga v)
    {
        bool aoLongoDeZ = Mathf.Abs(Mathf.Cos(v.giro * Mathf.Deg2Rad)) > 0.5f;
        float mx = aoLongoDeZ ? 0.875f : 1.95f, mz = aoLongoDeZ ? 1.95f : 0.875f;
        return Rect.MinMaxRect(v.centro.x - mx, v.centro.z - mz, v.centro.x + mx, v.centro.z + mz);
    }

    static bool CantosNoAsfalto(Rect r)
    {
        var asfalto = Bairro.Pedacos().Where(p => p.tipo == TipoPiso.Asfalto).Select(p => p.area).ToList();
        foreach (var c in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax) })
            if (!asfalto.Any(a => a.Contains(c))) return false;
        return true;
    }

    // Toda vaga na rua fica inteira no asfalto, encostada no meio-fio e no sentido da mão
    [Test]
    public void Vagas_NoAsfaltoJuntoAoMeioFio()
    {
        foreach (TipoEndereco t in System.Enum.GetValues(typeof(TipoEndereco)))
        {
            if (t == TipoEndereco.Posto) continue;
            var v = Bairro.VagaDe(t);
            var r = AreaDoCarro(v);
            Assert.IsTrue(CantosNoAsfalto(r), $"A vaga de {t} ({r}) sai do asfalto.");
            // Mão direita: o meio-fio fica à direita de quem está estacionado; o carro fica encostado nele
            var m = Bairro.Malha;
            var rua = m.ruas[m.IndiceRuaMaisProxima(v.centro)];
            float lat = rua.Lateral(v.centro);
            var frente = new Vector3(Mathf.Sin(v.giro * Mathf.Deg2Rad), 0f, Mathf.Cos(v.giro * Mathf.Deg2Rad));
            Assert.Greater(Vector3.Dot(RotasTransito.Direita(frente), rua.Normal * Mathf.Sign(lat)), 0.99f, $"Vaga de {t} na contramão.");
            Assert.That(rua.meiaPista - Mathf.Abs(lat), Is.EqualTo(Bairro.DistMeioFio).Within(0.05f), $"Vaga de {t} longe do meio-fio.");
        }
    }

    // Ninguém estaciona em cima da faixa de pedestre nem no cruzamento; a vaga fica perto da porta
    [Test]
    public void Vagas_ForaDaFaixaEPertoDaPorta()
    {
        var m = Bairro.Malha;
        var faixas = new List<Rect>();
        foreach (var fx in m.faixas)
        {
            var r = m.ruas[fx.rua];
            faixas.Add(r.Area(fx.pos - 1.5f, fx.pos + 1.5f, r.AsfaltoNeg, r.AsfaltoPos));
        }
        foreach (var c in m.cruzamentos)   // cruzamento
        {
            float mx = m.ruas[c.ruaNS].meiaPista, mz = m.ruas[c.ruaLO].meiaPista;
            faixas.Add(Rect.MinMaxRect(c.centro.x - mx, c.centro.z - mz, c.centro.x + mx, c.centro.z + mz));
        }
        foreach (var e in Bairro.Enderecos)
        {
            var v = Bairro.VagaDe(e.tipo);
            var r = AreaDoCarro(v);
            foreach (var f in faixas) Assert.IsFalse(r.Overlaps(f), $"A vaga de {e.tipo} invade a faixa de pedestre ou o cruzamento ({f}).");
            float d = Vector2.Distance(new Vector2(v.centro.x, v.centro.z), new Vector2(e.porta.x, e.porta.z));
            Assert.That(d, Is.LessThan(7f), $"A vaga de {e.tipo} fica a {d:0.0} m da porta.");
        }
    }

    // O posto fica num terreno livre: sem rua, calçada ou cômodo dentro; bomba e vaga no terreno; entrada rebaixada
    [Test]
    public void Posto_NoTerrenoLivreComEntrada()
    {
        var t = Bairro.TerrenoPosto;
        foreach (var p in Bairro.Pedacos())
            Assert.IsFalse(Encolher(t, 0.01f).Overlaps(p.area), $"O posto invade {p.tipo} da {p.rua} ({p.area}).");
        foreach (var c in Comodos())
            Assert.IsFalse(Encolher(t, 0.01f).Overlaps(c.Planta), $"O posto invade {c.nome}.");
        Assert.IsTrue(t.Contains(new Vector2(Bairro.BombaPosto.x, Bairro.BombaPosto.z)), "Bomba fora do terreno.");
        var vaga = Bairro.VagaDe(TipoEndereco.Posto);
        var r = AreaDoCarro(vaga);
        Assert.IsTrue(t.Contains(r.min) && t.Contains(r.max), "A vaga da bomba sai do terreno do posto.");
        Assert.That(Vector3.Distance(vaga.centro, Bairro.BombaPosto), Is.LessThan(PostoCombustivel.DistanciaBomba), "A vaga fica longe da bomba.");
        // guia rebaixada: dá para ir do asfalto da Jacarandás ao terreno sem subir calçada
        var meio = new Vector2(Bairro.FrenteLesteJacarandas - 0.5f, (Bairro.PostoZ0 + Bairro.PostoZ1) / 2f);
        Assert.IsTrue(Bairro.Pedacos().Any(p => p.tipo == TipoPiso.Asfalto && p.area.Contains(meio)), "Falta a guia rebaixada na frente do posto.");
    }

    // Etapa 9D: semáforos e placas de PARE ficam na calçada da esquina (fora do asfalto e dos cômodos), à direita de
    // quem chega — em todo cruzamento da malha
    [TestCaseSource(nameof(Malhas))]
    public void Transito_PostesNasEsquinas(MalhaRuas m)
    {
        var comodos = m == Bairro.Malha ? Comodos() : new List<Comodo>();
        var rede = RotasTransito.Calcadas(m);
        for (int k = 0; k < m.cruzamentos.Count; k++)
            foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                Vector3 p = MontarCenario.EsquinaDireita(m, k, d);
                var xz = new Vector2(p.x, p.z);
                bool calcada = m.Pedacos().Any(r => r.tipo == TipoPiso.Calcada && r.area.Contains(xz));
                Assert.IsTrue(calcada, $"[{m}] Poste do cruzamento {m.cruzamentos[k].nome} (chegando por {d}) fora da calçada: {p}.");
                foreach (var c in comodos) Assert.IsFalse(c.Planta.Contains(xz), $"Poste do cruzamento {k} dentro de {c.nome}.");
                // e não fica no caminho do pedestre de quem anda junto à fachada (a 85 cm dela)
                foreach (var n in rede.nos) Assert.Greater(Vector2.Distance(xz, new Vector2(n.pos.x, n.pos.z)), 0.4f, $"[{m}] Poste em cima do caminho do pedestre em {p}.");
            }
    }

    // A frente dos imóveis do bairro olha para a rua mais perto (janelas que acendem à noite)
    [Test]
    public void Fachadas_ViradasParaARua()
    {
        Assert.AreEqual(Vector3.back, MontarCenario.FachadaDe(new Vector3(-10f, 0f, 2f)), "Lote ao norte da Acácias.");
        Assert.AreEqual(Vector3.forward, MontarCenario.FachadaDe(new Vector3(-11f, 0f, -20f)), "Lote ao sul da Acácias.");
        Assert.AreEqual(Vector3.right, MontarCenario.FachadaDe(new Vector3(-36f, 0f, 22f)), "Hospital, a oeste da Ipê.");
        Assert.AreEqual(Vector3.left, MontarCenario.FachadaDe(new Vector3(-16f, 0f, 28f)), "Lote a leste da Ipê.");
        Assert.AreEqual(Vector3.right, MontarCenario.FachadaDe(new Vector3(16f, 0f, 27f)), "Lote a oeste da Jacarandás.");
        Assert.AreEqual(Vector3.left, MontarCenario.FachadaDe(new Vector3(36f, 0f, -40f)), "Lote a leste da Jacarandás.");
        Assert.AreEqual(Vector3.zero, MontarCenario.FachadaDe(new Vector3(100f, 0f, 100f)), "Fora do mapa não é lote.");
    }
}
