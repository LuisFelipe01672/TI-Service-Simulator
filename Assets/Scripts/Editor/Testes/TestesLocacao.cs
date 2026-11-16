using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// Etapa 5: locação física — instalação da impressora alugada (caixa, toner, papel, rede, IP fixo, computador)
// e visita mensal no cliente (contador, toner, troca preventiva, reserva).
public class TestesLocacao
{
    static AcaoFisica Acao(TipoEquipamento t, EstadoEquipamentos e, string inicio)
    {
        var a = AcoesFisicas.Para(t, e).FirstOrDefault(x => x.rotulo.StartsWith(inicio));
        Assert.IsNotNull(a, $"Ação \"{inicio}...\" não apareceu. Disponíveis: {string.Join(" | ", AcoesFisicas.Para(t, e).Select(x => x.rotulo))}");
        return a;
    }

    static string Fazer(TipoEquipamento t, EstadoEquipamentos e, string inicio) => Acao(t, e, inicio).executar();
    static string Imp(EstadoEquipamentos e, string inicio) => Fazer(TipoEquipamento.Impressora, e, inicio);
    static string Pc(EstadoEquipamentos e, string inicio) => Fazer(TipoEquipamento.Computador, e, inicio);

    static EstadoEquipamentos NovaInstalacao()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(CatalogoProblemas.InstalacaoLocacao, e);
        return e;
    }

    static EstadoEquipamentos NovaVisita(float suprimento, float usoPeca)
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(CatalogoProblemas.VisitaLocacao, e);
        e.PrepararVisita(suprimento, usoPeca);
        return e;
    }

    // Monta a impressora até ela imprimir pelo botão (sem configurar a rede)
    static void MontarAteLigar(EstadoEquipamentos e, bool puxarLacre = true)
    {
        Imp(e, "Abrir a caixa");
        Imp(e, "Tirar as fitas");
        Imp(e, "Levantar o scanner");
        Imp(e, "Pegar o toner inicial");
        if (puxarLacre) Imp(e, "Puxar o lacre");
        Imp(e, "Encaixar o cartucho novo");
        Imp(e, "Baixar o scanner");
        Imp(e, "Abrir a gaveta de papel");
        Imp(e, "Colocar papel");
        Imp(e, "Fechar a gaveta");
        Imp(e, "Conectar o cabo da impressora");
        Imp(e, "Ligar a impressora");
    }

    [Test]
    public void ServicosDaLocacao_ForaDasListasSorteaveis()
    {
        var todos = CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis).ToList();
        Assert.IsFalse(todos.Contains(CatalogoProblemas.InstalacaoLocacao), "instalação não pode ser sorteada como chamado avulso");
        Assert.IsFalse(todos.Contains(CatalogoProblemas.VisitaLocacao), "visita não pode ser sorteada como chamado avulso");
        Assert.AreSame(CatalogoProblemas.InstalacaoLocacao, CatalogoProblemas.Buscar(TipoEquipamento.Impressora, CatalogoProblemas.InstalarLocacao));
        Assert.AreSame(CatalogoProblemas.VisitaLocacao, CatalogoProblemas.Buscar(TipoEquipamento.Impressora, CatalogoProblemas.VisitarLocacao));
        Assert.IsTrue(CatalogoProblemas.EhServicoLocacao(CatalogoProblemas.InstalacaoLocacao));
        Assert.IsFalse(CatalogoProblemas.EhServicoLocacao(todos[0]));
        foreach (var p in new[] { CatalogoProblemas.InstalacaoLocacao, CatalogoProblemas.VisitaLocacao })
            Assert.AreEqual(System.Enum.GetValues(typeof(Pergunta)).Length, p.respostas.Length, p.solucao);
    }

    [Test]
    public void Instalacao_QuebrarDeixaSemResolver()
    {
        var e = NovaInstalacao();
        Assert.IsTrue(e.impNaCaixa);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoLocacao, e));
        Assert.AreEqual("(na caixa)", e.VisorImpressora());
        // Na caixa só dá para mexer na caixa
        var acoes = AcoesFisicas.Para(TipoEquipamento.Impressora, e);
        Assert.IsTrue(acoes.All(a => a.peca == "caixa"), "com a impressora na caixa só aparecem ações da caixa");
    }

    [Test]
    public void Instalacao_SequenciaCompletaResolve()
    {
        var e = NovaInstalacao();
        MontarAteLigar(e);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoLocacao, e), "ainda sem IP fixo nem computador");
        StringAssert.Contains(EstadoEquipamentos.IpDhcpNovaImpressora, Imp(e, "Imprimir o relatório de configuração"));

        // Sem IP fixo o computador não acha a impressora no endereço combinado
        Pc(e, "Adicionar a impressora pelo IP");
        Assert.IsTrue(e.pcSemImpressora, "não pode adicionar antes de fixar o IP no painel");
        StringAssert.Contains("Nenhuma impressora instalada", Pc(e, "Mandar imprimir uma página de teste"));

        Imp(e, "Configurar IP fixo");
        Assert.IsFalse(e.impIpPendente);
        Pc(e, "Adicionar a impressora pelo IP");
        Assert.IsFalse(e.pcSemImpressora);
        // A página de teste pelo botão não basta: o combinado é imprimir pelo computador
        Imp(e, "Imprimir página de teste");
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoLocacao, e), "falta testar pelo computador");
        Pc(e, "Mandar imprimir uma página de teste");
        Assert.IsTrue(e.InstalacaoCompleta);
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoLocacao, e), "Falta: " + string.Join(", ", e.PendenciasLocacao()));
        Assert.IsEmpty(e.PendenciasLocacao());
        // Checklist: tudo fechado e ligado
        var ck = AvaliacaoCliente.Checklist(CatalogoProblemas.InstalacaoLocacao, e, 0);
        Assert.AreEqual(0, AvaliacaoCliente.Falhas(ck));
    }

    [Test]
    public void Instalacao_EsquecerOLacreSaiEmBranco()
    {
        var e = NovaInstalacao();
        MontarAteLigar(e, puxarLacre: false);
        Imp(e, "Configurar IP fixo");
        Pc(e, "Adicionar a impressora pelo IP");
        Pc(e, "Mandar imprimir uma página de teste");
        Assert.AreEqual(ResultadoPagina.Branca, e.ultimaPagina);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoLocacao, e));
    }

    [Test]
    public void Instalacao_TampaPresaPelasFitas()
    {
        var e = NovaInstalacao();
        Imp(e, "Abrir a caixa");
        Assert.IsTrue(e.impComFitas);
        StringAssert.Contains("fita", Imp(e, "Levantar o scanner"));
        Assert.IsFalse(e.impTampaAberta);
    }

    [Test]
    public void Instalacao_IpSoFixaComOCaboNaRede()
    {
        var e = NovaInstalacao();
        Imp(e, "Abrir a caixa");
        Imp(e, "Ligar a impressora");
        Imp(e, "Configurar IP fixo");
        Assert.IsTrue(e.impIpPendente, "sem cabo o painel não salva o IP");
        Assert.IsTrue(e.Ping(e.ipImpressora).StartsWith("Host de destino inacessível"));
    }

    [Test]
    public void Visita_QuebrarDeixaSemResolver()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(CatalogoProblemas.VisitaLocacao, e);
        Assert.IsTrue(e.visita);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.VisitaLocacao, e), "sem ler o contador a visita não acabou");
    }

    [Test]
    public void Visita_TonerBomSoPrecisaDoContador()
    {
        var e = NovaVisita(0.8f, 0.2f);
        Assert.IsFalse(e.visitaTonerBaixo);
        Assert.IsFalse(e.preventivaPendente);
        Imp(e, "Ler o contador de páginas");
        Assert.IsTrue(e.contadorLido);
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.VisitaLocacao, e));
    }

    [Test]
    public void Visita_TonerBaixoEPreventivaPrecisamSerFeitos()
    {
        var e = NovaVisita(0.12f, 0.8f);
        Assert.IsTrue(e.visitaTonerBaixo);
        Assert.IsTrue(e.preventivaPendente);
        Assert.AreEqual(0.12f, e.impToner, 0.001f);
        Imp(e, "Ler o contador de páginas");
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.VisitaLocacao, e), "toner baixo e cilindro no fim");

        Imp(e, "Levantar o scanner");
        Imp(e, "Retirar o cartucho");
        Imp(e, "Guardar este cartucho na mochila");
        Imp(e, "Troca preventiva da unidade de imagem");
        Assert.IsFalse(e.preventivaPendente);
        Assert.AreEqual(1, e.trocasCilindro, "o visual anima a troca");
        Imp(e, "Pegar um toner novo da mochila");
        Imp(e, "Puxar o lacre");
        Imp(e, "Encaixar o cartucho novo");
        Imp(e, "Baixar o scanner");
        Imp(e, "Deixar um toner reserva");
        Assert.AreEqual(1, e.reservasDeixadas);
        Assert.GreaterOrEqual(e.impToner, EstadoEquipamentos.TonerBaixoVisita);
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.VisitaLocacao, e), "Falta: " + string.Join(", ", e.PendenciasLocacao()));
        Assert.AreEqual(0, AvaliacaoCliente.Falhas(AvaliacaoCliente.Checklist(CatalogoProblemas.VisitaLocacao, e, 0)));
    }

    // Toda ação nova acontece numa peça (mirar + E)
    [Test]
    public void Acoes_TodasTemPeca()
    {
        var estados = new List<EstadoEquipamentos> { NovaInstalacao(), NovaVisita(0.1f, 0.9f) };
        var meio = NovaInstalacao();
        Imp(meio, "Abrir a caixa");
        Imp(meio, "Ligar a impressora");
        estados.Add(meio);
        var aberta = NovaVisita(0.1f, 0.9f);
        aberta.impTampaAberta = true;
        aberta.impCartuchoPresente = false;
        estados.Add(aberta);
        foreach (var e in estados)
            foreach (var t in new[] { TipoEquipamento.Impressora, TipoEquipamento.Computador })
                foreach (var a in AcoesFisicas.Para(t, e))
                    Assert.IsFalse(string.IsNullOrEmpty(a.peca), $"{t}: \"{a.rotulo}\" sem peça");
    }
}
