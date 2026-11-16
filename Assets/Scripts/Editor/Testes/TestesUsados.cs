using System.Linq;
using NUnit.Framework;

// Etapa 6: bancada e usados como objetos — teste, diagnóstico, troca de peça, limpeza, LGPD, etiqueta,
// anúncio, frota de locação e descarte. A regra roda sem cena (Mochila, economia e contratos nulos).
public class TestesUsados
{
    [SetUp]
    public void Limpar() => EquipamentosUsados.Iniciar();

    static EquipUsado Novo(TipoUsado t, CondicaoUsado c, float sorteio = 0f, bool dados = true) =>
        EquipamentosUsados.Adicionar(EquipamentosUsados.Novo(t, t + " usado", 400f, dados, c, sorteio));

    static AcaoFisica Acao(EquipUsado e, string inicio)
    {
        var a = AcoesBancada.Para(e).FirstOrDefault(x => x.rotulo.StartsWith(inicio));
        Assert.IsNotNull(a, $"Ação \"{inicio}...\" não apareceu. Disponíveis: {string.Join(" | ", AcoesBancada.Para(e).Select(x => x.rotulo))}");
        return a;
    }

    static bool Tem(EquipUsado e, string inicio) => AcoesBancada.Para(e).Any(x => x.rotulo.StartsWith(inicio));
    static string Fazer(EquipUsado e, string inicio) => Acao(e, inicio).executar();

    [Test]
    public void NovoUsado_ChegaNaCaixa_ComDefeitoCoerente()
    {
        var pc = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.1f);
        Assert.AreEqual(LocalUsado.Caixa, pc.local);
        Assert.AreEqual(DefeitoUsado.Memoria, pc.defeito);
        Assert.AreEqual(DefeitoUsado.Fonte, Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.9f).defeito);
        Assert.AreEqual(DefeitoUsado.Cilindro, Novo(TipoUsado.Impressora, CondicaoUsado.DefeitoLeve).defeito);
        Assert.AreEqual(DefeitoUsado.Placa, Novo(TipoUsado.Monitor, CondicaoUsado.Sucata).defeito);
        Assert.AreEqual(DefeitoUsado.Nenhum, Novo(TipoUsado.Roteador, CondicaoUsado.Bom).defeito);
        Assert.AreNotEqual(pc.id, EquipamentosUsados.Estoque[1].id, "cada usado tem id próprio");
    }

    [Test]
    public void SemTeste_SoDaParaLigarOlharEPegar()
    {
        var e = Novo(TipoUsado.Notebook, CondicaoUsado.Bom);
        var rotulos = AcoesBancada.Para(e).Select(a => a.rotulo).ToList();
        Assert.AreEqual(3, rotulos.Count, string.Join(" | ", rotulos));
        Assert.IsTrue(Tem(e, "Ligar na tomada"));
        Assert.IsFalse(Tem(e, "Fotografar"), "não se anuncia o que não foi testado");
        Assert.IsTrue(Acao(e, "Olhar").observacao);
    }

    [Test]
    public void ComputadorSemVideo_PrecisaDiagnosticoAntesDaTroca()
    {
        var e = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.1f);
        StringAssert.Contains("sem sinal", Fazer(e, "Ligar na tomada"));
        Assert.IsFalse(e.diagnosticado);
        Assert.IsFalse(Tem(e, "Trocar a peça ruim"));
        Assert.AreEqual(Item.PlacaPOST, Acao(e, "Espetar a placa POST").requer);
        StringAssert.Contains("55", Fazer(e, "Espetar a placa POST"));
        Assert.IsTrue(e.diagnosticado);
        Assert.AreEqual(Item.Memoria, Acao(e, "Trocar a peça ruim").requer, "memória sai da mochila");
    }

    [Test]
    public void TestadorDeFonte_SoConfirmaAFonte()
    {
        var mem = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.1f);
        Fazer(mem, "Ligar na tomada");
        StringAssert.Contains("boa", Fazer(mem, "Ligar o testador de fonte"));
        Assert.IsFalse(mem.diagnosticado, "fonte boa não diz que é a memória");

        var fonte = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.9f);
        Fazer(fonte, "Ligar na tomada");
        Fazer(fonte, "Ligar o testador de fonte");
        Assert.IsTrue(fonte.diagnosticado);
    }

    [Test]
    public void SemFerramenta_DiagnosticaNoChute()
    {
        var e = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.9f);
        Fazer(e, "Ligar na tomada");
        Assert.IsNull(Acao(e, "Trocar peças de bancada").requer);
        Fazer(e, "Trocar peças de bancada");
        Assert.IsTrue(e.diagnosticado);
    }

    [Test]
    public void ImpressoraComListra_TestePeloPapelETrocaDoCilindro()
    {
        var e = Novo(TipoUsado.Impressora, CondicaoUsado.DefeitoLeve);
        StringAssert.Contains("listra", Fazer(e, "Ligar na tomada"));
        Assert.IsTrue(e.diagnosticado, "a folha já mostra o cilindro");
        Assert.AreEqual(Item.Cilindro, Acao(e, "Trocar a peça ruim").requer);
        Fazer(e, "Trocar a peça ruim");
        Assert.AreEqual(CondicaoUsado.Bom, e.condicao);
        Assert.IsTrue(e.recuperado);
        Assert.AreEqual(DefeitoUsado.Nenhum, e.defeito);
    }

    [Test]
    public void PecaDoBalcao_NaoPedeItemDaMochila()
    {
        foreach (var t in new[] { TipoUsado.Monitor, TipoUsado.Roteador, TipoUsado.Switch, TipoUsado.Notebook })
        {
            var e = Novo(t, CondicaoUsado.DefeitoLeve);
            Fazer(e, "Ligar na tomada");
            Assert.IsNull(Acao(e, "Trocar a peça ruim").requer, t.ToString());
            Fazer(e, "Trocar a peça ruim");
            Assert.AreEqual(CondicaoUsado.Bom, e.condicao, t.ToString());
        }
    }

    [Test]
    public void ApagarDados_SoDepoisDeFuncionar()
    {
        var e = Novo(TipoUsado.Computador, CondicaoUsado.DefeitoLeve, 0.1f);
        Fazer(e, "Ligar na tomada");
        Assert.IsFalse(Tem(e, "Apagar"), "sem ligar não dá para apagar");
        e.diagnosticado = true;
        Fazer(e, "Trocar a peça ruim");
        Fazer(e, "Apagar os dados");
        Assert.IsTrue(e.dadosApagados);
        Assert.IsFalse(Tem(e, "Apagar"));
        var rot = Novo(TipoUsado.Roteador, CondicaoUsado.Bom, 0f, false);
        Fazer(rot, "Ligar na tomada");
        Assert.IsFalse(Tem(rot, "Apagar"), "roteador não guarda dados");
    }

    [Test]
    public void LimpezaValeMaisNoAnuncio()
    {
        var e = Novo(TipoUsado.Monitor, CondicaoUsado.Bom, 0f, false);
        Fazer(e, "Ligar na tomada");
        float antes = EquipamentosUsados.ValorVenda(e);
        Fazer(e, "Limpar");
        Assert.Greater(EquipamentosUsados.ValorVenda(e), antes);
        Assert.IsFalse(Tem(e, "Limpar"));
    }

    [Test]
    public void Etiqueta_NumeracaoSequencial()
    {
        var a = Novo(TipoUsado.Switch, CondicaoUsado.Bom, 0f, false);
        var b = Novo(TipoUsado.Roteador, CondicaoUsado.Bom, 0f, false);
        Fazer(a, "Ligar na tomada"); Fazer(b, "Ligar na tomada");
        Fazer(a, "Colar etiqueta");
        Fazer(b, "Colar etiqueta");
        Assert.AreEqual("TIS-0001", a.Etiqueta);
        Assert.AreEqual("TIS-0002", b.Etiqueta);
        Assert.IsFalse(Tem(a, "Colar etiqueta"));
    }

    [Test]
    public void Anuncio_VendeNoDiaSeguinte()
    {
        var e = Novo(TipoUsado.Switch, CondicaoUsado.Bom, 0f, false);
        Fazer(e, "Ligar na tomada");
        Fazer(e, "Fotografar e anunciar");
        Assert.IsTrue(e.anunciado);
        Assert.IsTrue(Tem(e, "Tirar o anúncio"));
        var msgs = EquipamentosUsados.VenderAnunciados();
        Assert.AreEqual(1, msgs.Count);
        Assert.IsFalse(EquipamentosUsados.Estoque.Contains(e));
    }

    [Test]
    public void AnuncioNaMao_EsperaLargar()
    {
        var e = Novo(TipoUsado.Switch, CondicaoUsado.Bom, 0f, false);
        Fazer(e, "Ligar na tomada");
        Fazer(e, "Fotografar e anunciar");
        e.local = LocalUsado.Mao;
        Assert.AreEqual(0, EquipamentosUsados.VenderAnunciados().Count);
    }

    [Test]
    public void Frota_SoImpressoraLimpaDeDadosEEtiquetada()
    {
        var e = Novo(TipoUsado.Impressora, CondicaoUsado.Bom);
        Fazer(e, "Ligar na tomada");
        Assert.IsFalse(EquipamentosUsados.PodeIrParaFrota(e, out string m1));
        StringAssert.Contains("dados", m1);
        Fazer(e, "Apagar a memória");
        Assert.IsFalse(EquipamentosUsados.PodeIrParaFrota(e, out string m2));
        StringAssert.Contains("etiqueta", m2);
        Fazer(e, "Colar etiqueta");
        Assert.IsTrue(EquipamentosUsados.PodeIrParaFrota(e, out _));
        Fazer(e, "Colocar na frota");
        Assert.IsFalse(EquipamentosUsados.Estoque.Contains(e));

        var pc = Novo(TipoUsado.Computador, CondicaoUsado.Bom);
        Fazer(pc, "Ligar na tomada");
        Assert.IsFalse(Tem(pc, "Colocar na frota"), "computador não vai para a frota de impressoras");
    }

    [Test]
    public void Sucata_SoNoDescarte_EAproveitaAMemoria()
    {
        var e = Novo(TipoUsado.Computador, CondicaoUsado.Sucata);
        StringAssert.Contains("sucata", Fazer(e, "Ligar na tomada"));
        Assert.IsFalse(Tem(e, "Fotografar"));
        Assert.IsFalse(Tem(e, "Limpar"));
        Assert.IsTrue(Tem(e, "Tirar da bancada (levar ao lixo"));
        Fazer(e, "Desmontar e aproveitar");
        Assert.IsTrue(e.pecasAproveitadas);
        Assert.IsFalse(Tem(e, "Desmontar"));
        StringAssert.Contains("lixo eletrônico", EquipamentosUsados.Descartar(e));
        Assert.IsFalse(EquipamentosUsados.Estoque.Contains(e));

        var bom = Novo(TipoUsado.Monitor, CondicaoUsado.Bom, 0f, false);
        EquipamentosUsados.Descartar(bom);
        Assert.IsTrue(EquipamentosUsados.Estoque.Contains(bom), "o que funciona não vai para o lixo");
    }

    [Test]
    public void SaveAntigo_DeduzTipoEDefeito()
    {
        var antigo = new EquipamentosUsados.Dados();
        antigo.estoque.Add(new EquipUsado { nome = "Impressora laser usada", condicao = CondicaoUsado.DefeitoLeve, testado = true, temDados = true, valorBom = 380f });
        antigo.estoque.Add(new EquipUsado { nome = "Switch 8 portas usado", condicao = CondicaoUsado.Sucata });
        antigo.proximoId = 0;
        var json = UnityEngine.JsonUtility.ToJson(antigo);
        EquipamentosUsados.Carregar(UnityEngine.JsonUtility.FromJson<EquipamentosUsados.Dados>(json));
        var imp = EquipamentosUsados.Estoque[0];
        Assert.AreEqual(TipoUsado.Impressora, imp.tipo);
        Assert.AreEqual(DefeitoUsado.Cilindro, imp.defeito);
        Assert.IsTrue(imp.diagnosticado);
        Assert.AreEqual(LocalUsado.Chao, imp.local, "save antigo não tinha caixa: fica no chão");
        Assert.Greater(imp.id, 0);
        Assert.AreEqual(TipoUsado.Switch, EquipamentosUsados.Estoque[1].tipo);
        Assert.AreNotEqual(imp.id, EquipamentosUsados.Estoque[1].id);
    }

    [Test]
    public void PecaVelha_TemNomeEPeso()
    {
        StringAssert.Contains("Peça velha", Mochila.Info(Item.PecaVelha).nome);
        Assert.Greater(Mochila.Peso(Item.PecaVelha), 0f);
    }
}
