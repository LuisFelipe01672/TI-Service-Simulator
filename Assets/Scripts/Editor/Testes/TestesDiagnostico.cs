using System.Linq;
using NUnit.Framework;

// Etapa 4: ferramentas de diagnóstico, saúde das peças e check-up com laudo.
public class TestesDiagnostico
{
    static Problema Busca(string solucao) =>
        CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis)
            .First(p => p.equipamento == TipoEquipamento.Computador && p.solucao == solucao);

    static AcaoFisica Acao(EstadoEquipamentos e, string inicio)
    {
        var a = AcoesFisicas.Para(TipoEquipamento.Computador, e).FirstOrDefault(x => x.rotulo.StartsWith(inicio));
        Assert.IsNotNull(a, $"Ação \"{inicio}...\" não apareceu.");
        return a;
    }

    [Test]
    public void Ferramentas_NaLojaComPesoEPreco()
    {
        foreach (var item in new[] { Item.PlacaPOST, Item.TestadorFonte, Item.Multimetro, Item.PendriveDiagnostico })
        {
            var info = Mochila.Catalogo.FirstOrDefault(i => i.item == item);
            Assert.IsNotNull(info, $"{item} fora da loja");
            Assert.IsTrue(info.ferramenta, $"{item} deveria ser ferramenta (compra uma vez)");
            Assert.Greater(info.preco, 0f);
            Assert.Greater(Mochila.Peso(item), 0f, $"{item} sem peso no inventário");
        }
        var bateria = Mochila.Catalogo.First(i => i.item == Item.BateriaCMOS);
        Assert.IsFalse(bateria.ferramenta, "A bateria é consumível");
    }

    // O desgaste sorteado nunca vira defeito sozinho: fonte na norma, bateria segurando a data, POST normal
    [Test]
    public void Desgaste_NaoCriaDefeito()
    {
        for (int i = 0; i < 200; i++)
        {
            var e = new EstadoEquipamentos();
            e.SortearDesgaste(UnityEngine.Random.Range(1, 12));
            Assert.IsTrue(e.FonteNaNorma, $"fonte fora da norma com saúde {e.saudeFonte}");
            Assert.GreaterOrEqual(e.TensaoBateria, 2.5f);
            Assert.AreEqual("A0", e.CodigoPost());
            Assert.AreEqual(0, e.ErrosMemoria);
            Assert.IsTrue(e.PcHardwareOk);
        }
    }

    [Test]
    public void PlacaPost_MostraOCodigoDoDefeito()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(Busca(CatalogoProblemas.EncaixarMemoria), e);
        Assert.AreEqual("55", e.CodigoPost());
        e.pcGabineteAberto = true;
        Acao(e, "Espetar a placa POST").executar();
        Assert.AreEqual(FerramentaDiag.PlacaPOST, e.ultimaFerramenta);
        Assert.AreEqual("55", e.leituraTexto);
        Assert.AreEqual(1, e.usosFerramenta, "o visual precisa saber que a ferramenta foi usada");
        // reencaixar resolve
        Acao(e, "Tirar os pentes de memória").executar();
        Assert.IsFalse(e.pcMemoriaSolta);
        Assert.AreEqual("A0", e.CodigoPost());

        var q = new EstadoEquipamentos { pcFonteQueimada = true };
        Assert.AreEqual("", q.CodigoPost(), "sem energia o visor fica apagado");
    }

    [Test]
    public void TestadorDeFonte_AcendeVermelhoNaFonteFraca()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(Busca(CatalogoProblemas.TrocarFonteFraca), e);
        Assert.IsFalse(e.FonteNaNorma);
        e.pcLigado = false;
        e.pcGabineteAberto = true;
        Acao(e, "Ligar o testador de fonte").executar();
        Assert.AreEqual('r', e.ledsFonte[0], "LED do 12 V deveria ficar vermelho");
        Assert.AreEqual('r', e.ledsFonte[5], "Power Good deveria falhar");
        StringAssert.Contains("FAIL", e.leituraTexto);
        Assert.IsTrue((e.laudoFeito & ItemLaudo.Fonte) != 0);

        var bom = new EstadoEquipamentos { pcLigado = false, pcGabineteAberto = true };
        Acao(bom, "Ligar o testador de fonte").executar();
        Assert.AreEqual("vvvvvv", bom.ledsFonte);
    }

    [Test]
    public void Multimetro_MedeBateriaEFusivel()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(Busca(CatalogoProblemas.TrocarBateria), e);
        e.pcLigado = false;
        e.pcGabineteAberto = true;
        Acao(e, "Medir a bateria da placa-mãe").executar();
        Assert.AreEqual(FerramentaDiag.Multimetro, e.ultimaFerramenta);
        Assert.Less(e.leituraValor, 2.5f);
        Assert.AreEqual("V", e.leituraUnidade);

        var q = new EstadoEquipamentos { pcFonteQueimada = true, pcLigado = false, pcGabineteAberto = true };
        Acao(q, "Testar a continuidade do fusível").executar();
        Assert.AreEqual("OL", q.leituraTexto);
        Assert.IsFalse(q.leituraBipe);
    }

    [Test]
    public void Pendrive_AchaErrosDeMemoriaEMostraNaTela()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(Busca(CatalogoProblemas.TrocarMemoriaComErro), e);
        Assert.IsTrue(e.pcLigado);
        string r = Acao(e, "Dar boot pelo pendrive: teste de memória").executar();
        StringAssert.Contains("37 erros", r);
        Assert.AreEqual(1, e.telasPendrive, "o monitor deveria mostrar a tela do teste");
        StringAssert.Contains("ERROS: 37", e.telaPendriveResultado);
    }

    [Test]
    public void Checkup_SoTerminaComOLaudoCompleto()
    {
        var p = Busca(CatalogoProblemas.Checkup);
        Assert.IsTrue(CatalogoProblemas.EhCheckup(p));
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "sem laudo não terminou");
        Assert.IsTrue(e.PcHardwareOk, "check-up não quebra nada");

        // Com o PC ligado: pendrive (memória, disco e sensores)
        Acao(e, "Dar boot pelo pendrive: teste de memória").executar();
        Acao(e, "Dar boot pelo pendrive: disco e sensores").executar();
        // Desliga, abre e mede o resto
        e.pcLigado = false;
        e.pcGabineteAberto = true;
        Acao(e, "Olhar por dentro").executar();
        Acao(e, "Ligar o testador de fonte").executar();
        Acao(e, "Medir a bateria da placa-mãe").executar();
        Assert.IsTrue(e.LaudoCompleto, "faltou: " + e.FaltaNoLaudo());
        Assert.AreEqual("", e.FaltaNoLaudo());
        Assert.GreaterOrEqual(e.medicoes.Count, 6);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "aberto e desligado ainda não está pronto");
        e.pcGabineteAberto = false;
        e.pcLigado = true;
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        // Sempre há uma peça perto do fim: o laudo tem ao menos uma recomendação
        Assert.IsNotEmpty(e.Recomendacoes(), "o check-up sorteia uma peça no fim: deveria recomendar algo");
    }

    [Test]
    public void Checkup_SoParaQuemTemAsFerramentas()
    {
        for (int i = 0; i < 300; i++)
            Assert.IsFalse(CatalogoProblemas.EhCheckup(CatalogoProblemas.SortearDisponivel(DificuldadeChamado.Medio, false)));
        Assert.IsFalse(CatalogoProblemas.PodeFazerCheckup(null));
    }

    [Test]
    public void ProblemasNovos_TemFerramentaIdeal()
    {
        foreach (var s in new[] { CatalogoProblemas.EncaixarMemoria, CatalogoProblemas.TrocarFonteFraca,
                                  CatalogoProblemas.TrocarMemoriaComErro, CatalogoProblemas.TrocarBateria })
            Assert.IsNotNull(CatalogoProblemas.FerramentaIdeal(Busca(s)), s);
    }

    // Com o gabinete aberto (onde ficam as ferramentas), toda ação tem peça e rótulo
    [Test]
    public void Acoes_GabineteAbertoTemPeca()
    {
        var e = new EstadoEquipamentos { pcLigado = false, pcGabineteAberto = true, pcBateriaFraca = true };
        foreach (var a in AcoesFisicas.Para(TipoEquipamento.Computador, e))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(a.peca), $"\"{a.rotulo}\" sem peça");
            Assert.IsFalse(string.IsNullOrWhiteSpace(a.rotulo));
        }
    }

    // OS antiga (sem o laudo) abre com a lista vazia
    [Test]
    public void OS_AntigaSemLaudo()
    {
        var os = UnityEngine.JsonUtility.FromJson<OrdemServico>("{\"numero\":3}");
        Assert.IsNotNull(os.laudo);
        Assert.IsNotNull(os.recomendacoes);
    }
}
