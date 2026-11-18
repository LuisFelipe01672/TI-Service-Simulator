using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Etapa 8: rede física — organizar o rack bagunçado, pontos do projeto de cabeamento e o andamento do projeto.
public class TestesRede
{
    static readonly Problema[] Servicos = { CatalogoProblemas.OrganizacaoRack, CatalogoProblemas.CabeamentoPontos };

    static EstadoEquipamentos Rack()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(CatalogoProblemas.OrganizacaoRack, e);
        return e;
    }

    static AcaoFisica Acao(EstadoEquipamentos e, string inicio) =>
        AcoesFisicas.Para(TipoEquipamento.Rack, e).FirstOrDefault(a => a.rotulo.StartsWith(inicio));

    [Test]
    public void Rack_BaguncadoTemPistaENaoEstaResolvido()
    {
        var e = Rack();
        Assert.AreEqual(EstadoEquipamentos.PortasRack, e.rackCordoes.Count);
        Assert.IsTrue(e.rackCordoes.All(c => !c.Curto), "cordões do rack bagunçado são os de 2,5 m");
        Assert.IsTrue(e.rackCordoes.Any(c => c.portaPatch != c.portaSwitch), "cordões cruzados (patch 1 em outra porta do switch)");
        Assert.IsTrue(e.rackPortaAberta, "a porta nem fecha com o bolo de cabo");
        StringAssert.Contains("embolados", e.OlharRack());
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.OrganizacaoRack, e));
        Assert.Contains("foto do antes", e.PendenciasRack());
        Assert.IsFalse(RegistroEquipamentos.De(TipoEquipamento.Rack).Fechado(e));
    }

    [Test]
    public void Rack_PortaNaoFechaComCaboPendurado()
    {
        var e = Rack();
        var fechar = Acao(e, "Fechar a porta");
        Assert.IsNotNull(fechar);
        StringAssert.Contains("não fecha", fechar.executar());
        Assert.IsTrue(e.rackPortaAberta);
    }

    [Test]
    public void Rack_OrganizarInteiroResolve()
    {
        var e = Rack();
        Acao(e, "Anotar o mapa").executar();
        Assert.IsTrue(e.rackMapaAnotado);
        e.DesconectarCordoes(out bool semMapa);
        Assert.IsFalse(semMapa);
        Assert.AreEqual(0, e.rackCordoes.Count);
        Assert.IsFalse(e.RackFuncionando, "sem cordão a rede do escritório cai");
        for (int i = 0; i < EstadoEquipamentos.PortasRack; i++) Acao(e, "Ligar cordão de 0,3 m").executar();
        Assert.IsTrue(e.RackCordoesCurtos);
        Assert.IsTrue(e.rackCordoes.All(c => c.portaPatch == c.portaSwitch), "reorganizado: P01 » SW01, P02 » SW02...");
        Acao(e, "Passar os cordões pelo organizador").executar();
        Acao(e, "Prender o feixe").executar();
        for (int i = 0; i < EstadoEquipamentos.PortasRack; i++) Acao(e, "Etiquetar a porta").executar();
        Assert.IsTrue(e.RackEtiquetado);
        Acao(e, "Olhar as luzes de link").executar();
        Acao(e, "Fechar a porta").executar();
        Assert.IsFalse(e.rackPortaAberta);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.OrganizacaoRack, e), "falta a foto do depois");
        Acao(e, "Tirar a foto do depois").executar();
        Assert.IsTrue(e.rackFotoDepois);
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.OrganizacaoRack, e));
        Assert.IsEmpty(e.PendenciasRack());
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.Rack).Fechado(e));
    }

    [Test]
    public void Rack_CordaoVelhoNaoPassaNoOrganizador()
    {
        var e = Rack();
        e.DesconectarCordoes(out bool semMapa);
        Assert.IsTrue(semMapa, "desconectar sem anotar o mapa é erro");
        Assert.IsNull(Acao(e, "Tirar a foto do antes"), "depois de mexer não dá mais a foto do antes");
        for (int i = 0; i < EstadoEquipamentos.PortasRack; i++) Acao(e, "Reaproveitar um cordão velho").executar();
        Assert.IsTrue(e.RackTodosConectados);
        Assert.IsFalse(e.RackCordoesCurtos);
        StringAssert.Contains("não cabem", e.PassarNoOrganizador());
        Assert.IsFalse(e.rackNoOrganizador);
        StringAssert.Contains("Ainda não dá", Acao(e, "Tirar a foto do depois").executar());
    }

    [Test]
    public void Rack_EtiquetaPrecisaDaEtiquetadora()
    {
        var e = Rack();
        e.DesconectarCordoes(out _);
        Acao(e, "Ligar cordão de 0,3 m").executar();
        Assert.AreEqual(Item.Etiquetadora, Acao(e, "Etiquetar a porta 1").requer);
    }

    [Test]
    public void Rack_ForaDoServicoFicaArrumado()
    {
        var e = new EstadoEquipamentos();
        Assert.IsTrue(e.RackFuncionando);
        Assert.IsEmpty(e.PendenciasRack());
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.Rack).Fechado(e));
        StringAssert.Contains("Tudo em ordem", e.OlharRack());
    }

    [Test]
    public void Pontos_NumeradosNoPatchDepoisDasPortasEmUso()
    {
        var e = new EstadoEquipamentos();
        e.PrepararPontos(5, 3);
        Assert.AreEqual(new[] { "P-05", "P-06", "P-07" }, e.pontos.Select(p => p.Nome).ToArray());
        Assert.AreEqual(EstadoEquipamentos.PortasRack + 1, e.pontos[0].portaPatch);
        Assert.AreEqual(3, e.PendenciasPontos().Count);
        Assert.IsFalse(e.PontosConcluidos);
    }

    [Test]
    public void Pontos_SequenciaETesteQueFalha()
    {
        var e = new EstadoEquipamentos();
        e.PrepararPontos(1, 1);
        var p = e.pontos[0];
        var testar = new System.Func<AcaoFisica>(() => Acao(e, "Testar o P-01"));
        Assert.IsNotNull(Acao(e, "Fixar a caixa do P-01"));
        Assert.IsNull(Acao(e, "Passar o cabo"), "sem canaleta não dá para passar o cabo");
        p.canaleta = true;
        Acao(e, "Passar o cabo Cat6 do P-01").executar();
        Assert.IsTrue(p.caboPassado);
        var crimpar = Acao(e, "Crimpar a ponta do P-01");
        Assert.IsNotNull(crimpar);
        Assert.IsTrue(crimpar.abreCrimpagem);
        Assert.IsNotNull(crimpar.aoCrimpar);
        crimpar.aoCrimpar(false);   // errou a ordem dos fios
        StringAssert.Contains("patch panel", testar().executar());   // ainda sem o punch-down
        Assert.IsFalse(p.testado);
        p.noPatch = true;
        StringAssert.Contains("3 e 6", testar().executar());
        Assert.AreEqual(DefeitoCabo.Conector, e.ultimoTesteCabo);
        Assert.IsFalse(p.Ok);
        Assert.IsNull(Acao(e, "Etiquetar o P-01"), "ponto reprovado não se etiqueta");
        var refazer = Acao(e, "Refazer a ponta do P-01");
        Assert.IsNotNull(refazer);
        refazer.aoCrimpar(true);
        Assert.IsFalse(p.testado, "ponta refeita precisa de teste de novo");
        testar().executar();
        Assert.IsTrue(p.Ok);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.CabeamentoPontos, e), "falta a etiqueta");
        Acao(e, "Etiquetar o P-01").executar();
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.CabeamentoPontos, e));
    }

    [Test]
    public void Servicos_FerramentasEPecasCertas()
    {
        var e = new EstadoEquipamentos();
        e.PrepararPontos(1, 1);
        var p = e.pontos[0];
        p.canaleta = p.caboPassado = p.crimpado = true;
        Assert.AreEqual(Item.PunchDown, Acao(e, "Inserir o ponto P-01").requer);
        Assert.AreEqual(Item.TestadorCabo, Acao(e, "Testar o P-01").requer);
        foreach (var it in new[] { Item.PunchDown, Item.Etiquetadora })
            Assert.IsTrue(Mochila.Catalogo.Any(i => i.item == it && i.ferramenta), $"{it} na loja como ferramenta");

        var def = RegistroEquipamentos.De(TipoEquipamento.Rack);
        foreach (var prob in Servicos)
        {
            var est = new EstadoEquipamentos();
            CatalogoProblemas.Quebrar(prob, est);
            foreach (var a in AcoesFisicas.Para(TipoEquipamento.Rack, est))
                Assert.Contains(a.peca, def.Pecas.ToList(), $"\"{a.rotulo}\" numa peça que o rack não tem");
            Assert.AreSame(prob, CatalogoProblemas.Buscar(TipoEquipamento.Rack, prob.solucao));
            Assert.IsFalse(MissionManager.SolucaoRemota(prob), "serviço de rede física é presencial");
            Assert.IsTrue(CatalogoProblemas.EhServicoProjeto(prob));
        }
        foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
            Assert.IsFalse(lista.Any(CatalogoProblemas.EhServicoProjeto), "serviço de projeto não entra no sorteio");
    }

    [Test]
    public void Reset_AnimaComOClipe()
    {
        var e = new EstadoEquipamentos();
        var reset = AcoesFisicas.Para(TipoEquipamento.Roteador, e).First(a => a.peca == "reset");
        Assert.AreEqual("clipe", reset.animacao);
        Assert.AreEqual(Item.Clipe, reset.requer);
        Assert.GreaterOrEqual(reset.segurar, 10f);
    }

    // ---------- Projeto ----------

    static Projeto Cabeamento(int tamanho, int feitos) => new Projeto
    {
        id = 77, tipo = TipoProjeto.Cabeamento, empresa = "Escritório Teste", tamanhoReal = tamanho, tamanhoOrcado = tamanho,
        etapa = 4, unidadesFeitas = feitos, horasTotais = ProjetosManager.Horas(TipoProjeto.Cabeamento, tamanho)
    };

    [Test]
    public void Projeto_AndamentoPorPontos()
    {
        Assert.IsTrue(ProjetosManager.Fisico(TipoProjeto.Cabeamento));
        Assert.IsTrue(ProjetosManager.Fisico(TipoProjeto.Rack));
        Assert.IsTrue(ProjetosManager.Fisico(TipoProjeto.WiFi), "Etapa 10: o Wi-Fi também é feito com as mãos");
        var p = Cabeamento(10, 8);
        Assert.AreEqual(2, ProjetosManager.PontosNaVisita(p));
        Assert.AreEqual(0.8f, ProjetosManager.Progresso(p), 0.001f);
        Assert.AreEqual(ProjetosManager.MaxPontosPorVisita, ProjetosManager.PontosNaVisita(Cabeamento(20, 0)));
        StringAssert.Contains("8 de 10", ProjetosManager.Andamento(p));
    }

    [Test]
    public void Projeto_VisitaContaSoPontosProntos()
    {
        var go = new GameObject("PM_Teste");
        try
        {
            var pm = go.AddComponent<ProjetosManager>();
            var p = Cabeamento(6, 0);
            pm.projetos.Add(p);
            var ch = new Chamado(p.empresa, "", TipoChamado.Rede, DificuldadeChamado.Medio, 0f, 600f)
            { projetoId = p.id, estado = new EstadoEquipamentos(), problema = CatalogoProblemas.CabeamentoPontos };
            pm.PrepararEstado(ch);
            Assert.AreEqual(4, ch.estado.pontos.Count);
            for (int i = 0; i < 3; i++)
            {
                var pt = ch.estado.pontos[i];
                pt.canaleta = pt.caboPassado = pt.crimpado = pt.pontaBoa = pt.noPatch = pt.testado = pt.etiquetado = true;
            }
            string r = pm.VisitaConcluida(ch);
            Assert.AreEqual(3, p.unidadesFeitas);
            Assert.AreEqual(4, p.etapa, "ainda faltam pontos: o projeto continua");
            StringAssert.Contains("3 de 6", r);
            // A próxima visita começa no P-04
            var ch2 = new Chamado(p.empresa, "", TipoChamado.Rede, DificuldadeChamado.Medio, 0f, 600f) { projetoId = p.id, estado = new EstadoEquipamentos() };
            pm.PrepararEstado(ch2);
            Assert.AreEqual("P-04", ch2.estado.pontos[0].Nome);
            Assert.AreEqual(3, ch2.estado.pontos.Count);
            StringAssert.Contains("cliente", pm.Trabalhar(p), "o tablet não faz mais o trabalho de cabeamento");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void Projeto_SaveAntigoViraUnidades()
    {
        var go = new GameObject("PM_Teste");
        try
        {
            var pm = go.AddComponent<ProjetosManager>();
            var p = Cabeamento(6, 0);
            p.horasFeitas = 2.0f;   // 0,75 h por ponto: 2 pontos
            var d = new ProjetosManager.Dados();
            d.projetos.Add(p);
            pm.Carregar(d);
            Assert.AreEqual(2, p.unidadesFeitas);
            Assert.AreEqual(0f, p.horasFeitas);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
