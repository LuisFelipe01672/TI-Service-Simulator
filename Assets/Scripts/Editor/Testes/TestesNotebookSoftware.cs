using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Etapa 7 (parte B): notebook do cliente e problemas de software do TI OS.
// Cada problema: quebrado não está resolvido; as ações certas (na mão ou na tela) resolvem.
public class TestesNotebookSoftware
{
    static Problema Busca(TipoEquipamento t, string solucao) =>
        CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis)
            .First(p => p.equipamento == t && p.solucao == solucao);

    static EstadoEquipamentos Quebrado(TipoEquipamento t, string solucao, out Problema p)
    {
        p = Busca(t, solucao);
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "quebrado não pode estar resolvido");
        return e;
    }

    static string Nb(EstadoEquipamentos e, string inicio)
    {
        var a = AcoesFisicas.Para(TipoEquipamento.Notebook, e).FirstOrDefault(x => x.rotulo.StartsWith(inicio));
        Assert.IsNotNull(a, $"Ação \"{inicio}...\" não apareceu. Disponíveis: {string.Join(" | ", AcoesFisicas.Para(TipoEquipamento.Notebook, e).Select(x => x.rotulo))}");
        return a.executar();
    }

    // Limpezas espalham sujeira na cena aberta: apaga depois de cada teste
    [TearDown]
    public void LimparSujeira()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>())
            if (go != null && go.name.StartsWith("Sujeira_")) Object.DestroyImmediate(go);
    }

    // ---------- Notebook ----------

    [Test]
    public void Notebook_TemDefinicaoEPecas()
    {
        var d = RegistroEquipamentos.De(TipoEquipamento.Notebook);
        Assert.AreEqual("Notebook", d.Nome);
        CollectionAssert.IsSubsetOf(new[] { "tela", "teclado", "fundo", "carregador", "dobradica", "saida_ar" }, d.Pecas);
        Assert.IsTrue(d.Funcionando(new EstadoEquipamentos()));
    }

    [Test]
    public void Notebook_BateriaEstufada_AbreTiraEPoeNova()
    {
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.TrocarBateriaEstufada, out var p);
        StringAssert.Contains("empenada", Nb(e, "Olhar o notebook de lado"));
        StringAssert.Contains("Desligue", Nb(e, "Virar e tirar os 10 parafusos"));
        Nb(e, "Desligar o notebook");
        StringAssert.Contains("tomada", Nb(e, "Virar e tirar os 10 parafusos"));
        Nb(e, "Tirar o carregador da tomada");
        Nb(e, "Virar e tirar os 10 parafusos");
        Assert.IsTrue(e.nbTampaFundoAberta);
        StringAssert.Contains("estufada", Nb(e, "Olhar por dentro"));
        Nb(e, "Desconectar e retirar a bateria");
        Assert.IsFalse(e.nbBateriaEstufada);
        Nb(e, "Instalar a bateria nova");
        Nb(e, "Encaixar a tampa de baixo");
        Nb(e, "Ligar o carregador na tomada");
        Nb(e, "Ligar o notebook");
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.Notebook).Fechado(e));
    }

    [Test]
    public void Notebook_TeclaFalhando_TesteMostraELimpezaResolve()
    {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;   // a sujeira no chão usa Destroy (só vale no Play)
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.LimparTecla, out var p);
        StringAssert.Contains("tecla A não responde", Nb(e, "Testar o teclado"));
        Nb(e, "Tirar a capinha da tecla A");
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "sem a capinha não está pronto");
        Nb(e, "Soprar com ar comprimido");
        Nb(e, "Encaixar a capinha");
        StringAssert.Contains("saiu certinho", Nb(e, "Testar o teclado"));
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Notebook_Carregador_MultimetroMostraETrocaResolve()
    {
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.TrocarCarregador, out var p);
        Assert.AreEqual(Item.Multimetro, CatalogoProblemas.FerramentaIdeal(p));
        StringAssert.Contains("0,0 V", Nb(e, "Medir a saída do carregador"));
        StringAssert.Contains("fita isolante", Nb(e, "Olhar o carregador"));
        Nb(e, "Trocar pelo carregador universal");
        StringAssert.Contains("19,5 V", Nb(e, "Medir a saída do carregador"));
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Notebook_Superaquecimento_LimpaVentoinha()
    {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;   // a sujeira no chão usa Destroy (só vale no Play)
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.LimparVentoinha, out var p);
        StringAssert.Contains("quente", Nb(e, "Pôr a mão na saída de ar"));
        Nb(e, "Desligar o notebook");
        Nb(e, "Tirar o carregador da tomada");
        Nb(e, "Virar e tirar os 10 parafusos");
        Nb(e, "Limpar a ventoinha");
        Nb(e, "Encaixar a tampa de baixo");
        Nb(e, "Ligar o carregador na tomada");
        Nb(e, "Ligar o notebook");
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Notebook_Dobradica_MolduraTrocaEFecha()
    {
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.TrocarDobradica, out var p);
        StringAssert.Contains("tomba", Nb(e, "Abrir e fechar a tela"));
        Nb(e, "Desligar o notebook");
        Nb(e, "Soltar a moldura da tela");
        Nb(e, "Trocar as dobradiças");
        Nb(e, "Encaixar a moldura");
        Nb(e, "Ligar o notebook");
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Notebook_BateriaBoaTiradaPrecisaVoltar()
    {
        var e = new EstadoEquipamentos();
        Nb(e, "Desligar o notebook");
        Nb(e, "Tirar o carregador da tomada");
        Nb(e, "Virar e tirar os 10 parafusos");
        Nb(e, "Desconectar e retirar a bateria");
        StringAssert.Contains("Recoloque a bateria", Nb(e, "Encaixar a tampa de baixo"));
        Nb(e, "Recolocar a mesma bateria");
        Nb(e, "Encaixar a tampa de baixo");
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.Notebook).Fechado(e));
    }

    // ---------- Software ----------

    [Test]
    public void Software_Senha_AdminRedefineEDesbloqueia()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.DesbloquearConta, out var p);
        StringAssert.Contains("administrador", e.DesbloquearConta());
        e.EntrarComoAdministrador();
        StringAssert.Contains("Redefina", e.DesbloquearConta());
        e.RedefinirSenha();
        e.DesbloquearConta();
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Software_Email_PortaEAutenticacao()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.CorrigirEmail, out var p);
        StringAssert.Contains("porta 25", e.EnviarEReceber());
        e.swEmailPorta = 587;
        e.swEmailAutenticacao = false;
        StringAssert.Contains("exige autenticação", e.EnviarEReceber());
        e.swEmailAutenticacao = true;
        StringAssert.Contains("enviada", e.EnviarEReceber());
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Software_Virus_VarreduraQuarentenaDesinstalaRestauraReinicia()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.RemoverVirus, out var p);
        StringAssert.Contains("Rode a verificação", e.Quarentena());
        StringAssert.Contains("3 ameaças", e.VarreduraCompleta());
        e.Quarentena();
        e.DesinstalarPropaganda();
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "a página inicial continua sequestrada");
        e.RestaurarPaginaInicial();
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "falta reiniciar");
        e.ReiniciarPc();
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    [Test]
    public void Software_Inicializacao_DesabilitaOsInuteisEReinicia()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.LimparInicializacao, out var p);
        Assert.IsTrue(e.InicializacaoCheia);
        foreach (var prog in e.swInicializacao.Where(x => !x.essencial && x.ativo).ToList()) e.AlternarInicio(prog);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "falta reiniciar");
        e.ReiniciarPc();
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsTrue(e.swInicializacao.Where(x => x.essencial).All(x => x.ativo), "os essenciais ficam");
    }

    [Test]
    public void Software_Atualizacao_DesinstalaReiniciaECausaRaiz()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.DesfazerAtualizacao, out var p);
        Assert.IsFalse(e.SistemaContabilAbre);
        e.DesinstalarAtualizacao();
        Assert.IsFalse(e.SistemaContabilAbre, "pede reinício");
        e.ReiniciarPc();
        Assert.IsTrue(e.SistemaContabilAbre);
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsNotNull(CatalogoProblemas.CausaRaizPendente(p, e), "sem pausar, a atualização volta");
        e.PausarAtualizacao();
        Assert.IsNull(CatalogoProblemas.CausaRaizPendente(p, e));
    }

    [Test]
    public void Software_ImpressoraPadrao_PdfAteDefinirECausaRaiz()
    {
        var e = Quebrado(TipoEquipamento.Computador, CatalogoProblemas.DefinirPadrao, out var p);
        StringAssert.Contains("Salvar saída de impressão", e.Imprimir(true, out bool boa));
        Assert.IsFalse(boa);
        e.DefinirPadraoEscritorio();
        e.Imprimir(true, out boa);
        Assert.IsTrue(boa);
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsNotNull(CatalogoProblemas.CausaRaizPendente(p, e), "o sistema troca a padrão de novo");
        e.AlternarGerenciarPadrao();
        Assert.IsNull(CatalogoProblemas.CausaRaizPendente(p, e));
    }

    [Test]
    public void Software_NoNotebookTambem()
    {
        var e = Quebrado(TipoEquipamento.Notebook, CatalogoProblemas.RemoverVirus, out var p);
        e.VarreduraCompleta(); e.Quarentena(); e.DesinstalarPropaganda(); e.RestaurarPaginaInicial(); e.ReiniciarPc();
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
    }

    // ---------- Atendimento remoto ----------

    [Test]
    public void Remoto_SoftwareDeLongeSenhaNao()
    {
        foreach (var s in new[] { CatalogoProblemas.DefinirPadrao, CatalogoProblemas.CorrigirEmail, CatalogoProblemas.LimparInicializacao,
                                  CatalogoProblemas.RemoverVirus, CatalogoProblemas.DesfazerAtualizacao })
        {
            var p = Busca(TipoEquipamento.Computador, s);
            Assert.IsTrue(MissionManager.SolucaoRemota(p), s);
            Assert.IsFalse(string.IsNullOrEmpty(p.comoRemoto), $"{s}: falta dizer o que se faz de longe (vai na OS)");
        }
        var senha = Busca(TipoEquipamento.Computador, CatalogoProblemas.DesbloquearConta);
        Assert.IsFalse(MissionManager.SolucaoRemota(senha));
        Assert.IsNotNull(senha.motivoPresencial);
        foreach (var s in new[] { CatalogoProblemas.TrocarBateriaEstufada, CatalogoProblemas.LimparTecla, CatalogoProblemas.TrocarCarregador,
                                  CatalogoProblemas.LimparVentoinha, CatalogoProblemas.TrocarDobradica })
            Assert.IsFalse(MissionManager.SolucaoRemota(Busca(TipoEquipamento.Notebook, s)), s);
    }

    [Test]
    public void Itens_PecasDeNotebookNaLoja()
    {
        foreach (var i in new[] { Item.BateriaNotebook, Item.CarregadorNotebook, Item.DobradicaNotebook })
        {
            Assert.IsTrue(Mochila.Catalogo.Any(x => x.item == i), $"{i} fora da loja");
            Assert.Greater(Mochila.Peso(i), 0f);
        }
    }

    // Nada de marca real nos textos do catálogo
    [Test]
    public void Catalogo_SemMarcasReais()
    {
        string[] marcas = { "Windows", "Microsoft", "Google", "Office", "Outlook", "Gmail", "Avast", "Norton", "McAfee", "Kaspersky", "Chrome" };
        foreach (var p in CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis))
            foreach (var texto in new[] { p.descricao, p.descricaoVaga, p.mentira, p.comoRemoto ?? "" }.Concat(p.respostas))
                foreach (var m in marcas)
                    StringAssert.DoesNotContain(m, texto, $"{p.solucao}: marca real \"{m}\"");
    }
}
