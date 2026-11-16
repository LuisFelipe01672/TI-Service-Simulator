using System.Linq;
using NUnit.Framework;

// Testes automáticos (EditMode) da Etapa 1: ordem de serviço, SLA em horas do jogo, checklist,
// estrelas do cliente, causa raiz (retrabalho) e o save do atendimento.
public class TestesOSeSLA
{
    static System.Collections.Generic.IEnumerable<Problema> Todos() =>
        CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis);

    static Problema Problema(string solucao) => Todos().First(p => p.solucao == solucao && p.equipamento == TipoEquipamento.Impressora);

    // ---------- SLA ----------

    [Test]
    public void SLA_PrioridadeMaiorTemPrazoMenor()
    {
        Assert.Less(SLA.Horas(PrioridadeSLA.Urgente), SLA.Horas(PrioridadeSLA.Alta));
        Assert.Less(SLA.Horas(PrioridadeSLA.Alta), SLA.Horas(PrioridadeSLA.Normal));
        Assert.Less(SLA.Horas(PrioridadeSLA.Normal), SLA.Horas(PrioridadeSLA.Baixa));
        // O mais folgado ainda cabe no expediente (8h às 18h)
        Assert.LessOrEqual(SLA.Horas(PrioridadeSLA.Baixa), 10f);
    }

    [Test]
    public void SLA_DefinirPrioridade()
    {
        Assert.AreEqual(PrioridadeSLA.Urgente, SLA.Definir(DificuldadeChamado.Facil, Personalidade.Tranquilo, false, true), "Retorno é sempre urgente.");
        Assert.AreEqual(PrioridadeSLA.Alta, SLA.Definir(DificuldadeChamado.Facil, Personalidade.Tranquilo, true, false), "Contrato tem prioridade alta.");
        Assert.AreEqual(PrioridadeSLA.Alta, SLA.Definir(DificuldadeChamado.Facil, Personalidade.VIP, false, false));
        Assert.AreEqual(PrioridadeSLA.Alta, SLA.Definir(DificuldadeChamado.Dificil, Personalidade.Tranquilo, false, false));
        Assert.AreEqual(PrioridadeSLA.Normal, SLA.Definir(DificuldadeChamado.Facil, Personalidade.Apressado, false, false));
        Assert.AreEqual(PrioridadeSLA.Baixa, SLA.Definir(DificuldadeChamado.Facil, Personalidade.Tranquilo, false, false));
    }

    [Test]
    public void SLA_ContaDesdeAAbertura()
    {
        // Aberto no dia 3 às 08:00 com SLA de 4h: vence às 12:00 do mesmo dia
        var c = new Chamado("Cliente", "x", TipoChamado.Impressora, DificuldadeChamado.Facil, 100f, 4f * 60f)
        { abertoEm = SLA.MinutoAbsoluto(3, 8 * 60) };
        float agora = SLA.MinutoAbsoluto(3, 10 * 60 + 30);
        Assert.AreEqual(90f, SLA.Restante(c, agora), 0.01f);
        Assert.AreEqual("12:00", SLA.HoraDoPrazo(c, agora));
        Assert.Less(SLA.Restante(c, SLA.MinutoAbsoluto(3, 13 * 60)), 0f, "Às 13h já estourou.");
        // Prazo que passa da meia-noite mostra "amanhã"
        c.abertoEm = SLA.MinutoAbsoluto(3, 22 * 60);
        StringAssert.StartsWith("amanhã", SLA.HoraDoPrazo(c, SLA.MinutoAbsoluto(3, 22 * 60)));
    }

    [Test]
    public void SLA_TextoDataHora()
    {
        // Dia 1 do jogo = 02/03; dia 2 às 09:05
        Assert.AreEqual("03/03 09:05", MissionManager.TextoDataHora(SLA.MinutoAbsoluto(2, 9 * 60 + 5)));
    }

    // ---------- Estrelas e comentário ----------

    [Test]
    public void Estrelas_AtendimentoPerfeitoDa5()
    {
        Assert.AreEqual(5, AvaliacaoCliente.Estrelas(0f, 0, 0, Personalidade.Tranquilo, false));
        Assert.AreEqual(5, AvaliacaoCliente.Estrelas(0f, 0, 0, Personalidade.VIP, false), "VIP perfeito também dá 5.");
    }

    [Test]
    public void Estrelas_CadaFalhaTira()
    {
        Assert.AreEqual(4, AvaliacaoCliente.Estrelas(30f, 0, 0, Personalidade.Tranquilo, false), "Atraso pequeno: -1.");
        Assert.AreEqual(3, AvaliacaoCliente.Estrelas(200f, 0, 0, Personalidade.Tranquilo, false), "Atraso grande: -2.");
        Assert.AreEqual(3, AvaliacaoCliente.Estrelas(0f, 2, 0, Personalidade.Tranquilo, false), "Dois itens do checklist: -2.");
        Assert.AreEqual(4, AvaliacaoCliente.Estrelas(0f, 0, 1, Personalidade.Tranquilo, false), "Um estrago: -1.");
        Assert.AreEqual(3, AvaliacaoCliente.Estrelas(0f, 0, 3, Personalidade.Tranquilo, false), "Três estragos: -2.");
        Assert.AreEqual(4, AvaliacaoCliente.Estrelas(0f, 0, 0, Personalidade.Tranquilo, true), "Retorno: -1.");
        Assert.AreEqual(3, AvaliacaoCliente.Estrelas(0f, 1, 0, Personalidade.VIP, false), "VIP cobra uma a mais.");
        Assert.AreEqual(1, AvaliacaoCliente.Estrelas(500f, 4, 5, Personalidade.VIP, true), "Nunca abaixo de 1.");
    }

    [Test]
    public void Estrelas_ReputacaoCresceComAsEstrelas()
    {
        for (int e = 1; e < 5; e++)
            Assert.Less(AvaliacaoCliente.ReputacaoPorEstrelas(e), AvaliacaoCliente.ReputacaoPorEstrelas(e + 1));
        Assert.Greater(AvaliacaoCliente.ReputacaoPorEstrelas(3), 0f, "3 estrelas ainda é positivo.");
        Assert.Less(AvaliacaoCliente.ReputacaoPorEstrelas(2), 0f, "2 estrelas tira reputação.");
    }

    [Test]
    public void Comentario_CitaOMotivo()
    {
        for (int v = 0; v < 3; v++)
        {
            Assert.IsNotEmpty(AvaliacaoCliente.Comentario(5, null, v));
            StringAssert.Contains("deixou sujeira no chão", AvaliacaoCliente.Comentario(4, "deixou sujeira no chão", v));
            StringAssert.Contains("demorou", AvaliacaoCliente.Comentario(1, "demorou mais do que o combinado", v));
        }
    }

    // ---------- Checklist ----------

    [Test]
    public void Checklist_EquipamentoArrumadoPassaTudo()
    {
        var p = Problema("Trocar toner");
        var lista = AvaliacaoCliente.Checklist(p, new EstadoEquipamentos(), 0);
        Assert.GreaterOrEqual(lista.Count, 3);
        Assert.AreEqual(0, AvaliacaoCliente.Falhas(lista), string.Join(", ", lista.Where(i => !i.ok).Select(i => i.texto)));
    }

    [Test]
    public void Checklist_PegaOQueFicouParaTras()
    {
        // Sem problema (null): isola os itens de arrumação do item "defeito testado"
        var e = new EstadoEquipamentos { pcGabineteAberto = true, impGavetaAberta = true };
        Assert.AreEqual(1, AvaliacaoCliente.Falhas(AvaliacaoCliente.Checklist(null, e, 0)), "Gabinete e gaveta abertos são o mesmo item.");
        Assert.AreEqual(2, AvaliacaoCliente.Falhas(AvaliacaoCliente.Checklist(null, e, 3)), "Sujeira no chão é outro item.");
        var p = Problema("Trocar toner");
        e = new EstadoEquipamentos { impLigada = false };
        Assert.GreaterOrEqual(AvaliacaoCliente.Falhas(AvaliacaoCliente.Checklist(p, e, 0)), 1, "Impressora desligada falha.");
    }

    // ---------- Ordem de serviço ----------

    [Test]
    public void OS_AcaoRepetidaViraUmaLinha()
    {
        var os = new OrdemServico();
        os.RegistrarAcao("Imprimir página de teste");
        os.RegistrarAcao("Imprimir página de teste");
        os.RegistrarAcao("Imprimir página de teste");
        os.RegistrarAcao("Abrir a gaveta");
        os.RegistrarAcao("");
        Assert.AreEqual(2, os.acoes.Count);
        Assert.AreEqual("Imprimir página de teste (x3)", os.acoes[0]);
    }

    [Test]
    public void OS_PecasSomamNoTotal()
    {
        var os = new OrdemServico();
        os.RegistrarPeca("Toner", 150f);
        os.RegistrarPeca(null, 5f);
        Assert.AreEqual(155f, os.valorPecas, 0.001f);
        Assert.AreEqual(2, os.pecas.Count);
        StringAssert.StartsWith("Material", os.pecas[1]);
    }

    [Test]
    public void OS_HorasTexto()
    {
        Assert.AreEqual("45 min", OrdemServico.HorasTexto(45f));
        Assert.AreEqual("1h05", OrdemServico.HorasTexto(65f));
        Assert.AreEqual("0 min", OrdemServico.HorasTexto(-10f));
    }

    // ---------- Causa raiz (retrabalho) ----------

    [Test]
    public void CausaRaiz_EquipamentoNovoNaoTem()
    {
        foreach (var p in Todos())
            Assert.IsNull(CatalogoProblemas.CausaRaizPendente(p, new EstadoEquipamentos()), p.solucao);
    }

    [Test]
    public void CausaRaiz_PapelUmidoVoltaAAtolar()
    {
        var p = Problema("Retirar papel atolado");
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        e.impPapelAtolado = false;   // só tirou a folha presa
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e), "O sintoma sumiu.");
        Assert.IsNotNull(CatalogoProblemas.CausaRaizPendente(p, e), "Mas o papel úmido continua: vai voltar.");
        Assert.IsTrue(AcoesFisicas.Para(TipoEquipamento.Impressora, new EstadoEquipamentos { impPapelUmido = true, impGavetaAberta = true })
            .Any(a => a.rotulo.StartsWith("Trocar a resma úmida")), "Tem ação para trocar o papel.");
        e.impPapelUmido = false;
        Assert.IsNull(CatalogoProblemas.CausaRaizPendente(p, e));
    }

    [Test]
    public void CausaRaiz_CaboSoltoSoltaDeNovo()
    {
        var p = Problema("Verificar cabo");
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        e.impCabo.conectado = true;
        e.impCabo.defeito = DefeitoCabo.Nenhum;
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsNotNull(CatalogoProblemas.CausaRaizPendente(p, e));
        Assert.IsTrue(AcoesFisicas.Para(TipoEquipamento.Impressora, e).Any(a => a.rotulo.StartsWith("Passar o cabo rente à parede")));
        e.impCaboSolto = false;
        Assert.IsNull(CatalogoProblemas.CausaRaizPendente(p, e));
    }

    [Test]
    public void Retorno_AchaTodoProblemaDoCatalogo()
    {
        foreach (var p in Todos())
            Assert.AreSame(p, CatalogoProblemas.Buscar(p.equipamento, p.solucao), p.solucao);
        Assert.IsNull(CatalogoProblemas.Buscar(TipoEquipamento.Roteador, "não existe"));
    }

    // ---------- Primeiro dia (chamado guiado de toner) ----------

    // Faz o conserto inteiro pelas ações reais da impressora; o guia sempre tem um passo e muda conforme avança
    [Test]
    public void Tutorial_GuiaAcompanhaOConserto()
    {
        var p = Problema(TutorialPrimeiroDia.Solucao);
        var ch = new Chamado("Escritório Beta", p.descricao, TipoChamado.Impressora, DificuldadeChamado.Facil, 100f, 540f)
        { problema = p, equipamento = p.equipamento, solucao = p.solucao, tutorial = true, contato = "Dona Lúcia", estado = new EstadoEquipamentos() };
        CatalogoProblemas.Quebrar(p, ch.estado);
        ch.estado.tonerDoCliente = true;
        StringAssert.Contains("Dona Lúcia", TutorialPrimeiroDia.Passo(ch), "Primeiro passo: conversar.");
        ch.conversou = true;

        string[] roteiro = { "Imprimir página de teste", "Levantar o scanner", "Retirar o cartucho", "Guardar este cartucho",
                             "Pegar o toner novo que o cliente", "Puxar o lacre", "Encaixar o cartucho novo", "Baixar o scanner", "Imprimir página de teste" };
        string anterior = null;
        foreach (var inicio in roteiro)
        {
            string passo = TutorialPrimeiroDia.Passo(ch);
            Assert.IsFalse(string.IsNullOrEmpty(passo));
            Assert.AreNotEqual(anterior, passo, $"O guia não mudou antes de: {inicio}");
            anterior = passo;
            var acao = AcoesFisicas.Para(TipoEquipamento.Impressora, ch.estado).FirstOrDefault(a => a.rotulo.StartsWith(inicio));
            Assert.IsNotNull(acao, $"Ação \"{inicio}\" não está disponível. Guia dizia: {passo}");
            acao.executar();
        }
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, ch.estado), "Seguindo o guia, a impressora fica boa.");
        Assert.IsFalse(ch.estado.tonerDoCliente, "Usou o toner do cliente (sem precisar da loja).");
        ch.aguardandoAssinatura = true;
        StringAssert.Contains("assinar", TutorialPrimeiroDia.Passo(ch));
    }

    // ---------- Save ----------

    [Test]
    public void Save_AtendimentoIdaEVolta()
    {
        var d = new SaveData { atendimento = new MissionManager.Dados { proximaOS = 12, somaEstrelas = 37f, avaliacoes = 9 } };
        d.atendimento.retornos.Add(new RetornoPendente { id = 3, cliente = "Clínica Gama", solucao = "Verificar cabo", equipamento = TipoEquipamento.Impressora, diaRetorno = 8, motivo = "voltou" });
        d.atendimento.historico.Add(new OrdemServico { numero = 11, estrelas = 4, comentario = "ok" });
        var volta = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
        Assert.AreEqual(4, SaveManager.VersaoAtual);
        Assert.AreEqual(12, volta.atendimento.proximaOS);
        Assert.AreEqual(9, volta.atendimento.avaliacoes);
        Assert.AreEqual(1, volta.atendimento.retornos.Count);
        Assert.AreEqual(8, volta.atendimento.retornos[0].diaRetorno);
        Assert.AreEqual(TipoEquipamento.Impressora, volta.atendimento.retornos[0].equipamento);
        Assert.AreEqual(4, volta.atendimento.historico[0].estrelas);
    }

    [Test]
    public void Save_V3SemAtendimentoComecaZerado()
    {
        var d = UnityEngine.JsonUtility.FromJson<SaveData>("{\"versao\":3,\"dia\":5}");
        // JsonUtility cria o objeto com os valores iniciais declarados (ou null): os dois casos viram atendimento novo
        Assert.IsTrue(d.atendimento == null || (d.atendimento.proximaOS == 1 && d.atendimento.avaliacoes == 0));
    }
}
