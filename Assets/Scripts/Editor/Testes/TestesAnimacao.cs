using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// Etapa 10C: as ações demoradas de troca de peça têm animação enquanto o técnico segura E (AnimacaoPecas).
public class TestesAnimacao
{
    // Junta as ações de todos os problemas do catálogo, com o gabinete e o notebook abertos também
    static List<AcaoFisica> TodasAcoes()
    {
        var l = new List<AcaoFisica>();
        foreach (var p in CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis))
        {
            for (int v = 0; v < 3; v++)
            {
                var e = new EstadoEquipamentos();
                CatalogoProblemas.Quebrar(p, e);
                if (v >= 1) { e.pcGabineteAberto = true; e.pcLigado = false; e.nbLigado = false; e.nbNaTomada = false; e.nbTampaFundoAberta = true; }
                if (v == 2) { e.pcCoolerFora = true; e.pcPastaLimpa = true; e.nbBateriaFora = true; e.nbMolduraAberta = true; e.nbTeclaSemCapa = true; }
                l.AddRange(AcoesFisicas.Para(p.equipamento, e));
            }
        }
        return l;
    }

    [Test]
    public void TrocasDePecaTemAnimacao()
    {
        var acoes = TodasAcoes();
        string[] obrigatorias = { "Tirar os pentes de memória", "Trocar a fonte", "Tirar o cooler", "Trocar o HD por um SSD",
                                  "Virar o gabinete e tirar a tampa", "Virar e tirar os 10 parafusos", "Desconectar e retirar a bateria" };
        foreach (var o in obrigatorias)
        {
            var a = acoes.FirstOrDefault(x => x.rotulo.StartsWith(o));
            Assert.IsNotNull(a, $"ação \"{o}\" não apareceu em nenhum estado de teste");
            Assert.IsNotNull(a.animacao, $"\"{a.rotulo}\" sem animação");
            Assert.Greater(a.segurar, 0f, "animação acompanha o segurar E");
        }
    }

    [Test]
    public void TabelaSemIdsQuebrados()
    {
        Assert.AreEqual("gab_memoria_troca", AnimacaoPecas.Id("Trocar o pente de memória com defeito"));
        Assert.AreEqual("nb_bateria_por", AnimacaoPecas.Id("Instalar a bateria nova e ligar o conector"));
        Assert.IsNull(AnimacaoPecas.Id("Olhar o visor"));
        Assert.AreEqual(AnimacaoPecas.Ferramenta.Chave, AnimacaoPecas.FerramentaDe("gab_abrir"));
        Assert.AreEqual(AnimacaoPecas.Ferramenta.Ar, AnimacaoPecas.FerramentaDe("gab_limpar"));
        // Toda ação demorada que casa com a tabela recebe o id; nenhuma observação ganha animação de troca
        foreach (var a in TodasAcoes())
            if (a.animacao != null && a.animacao != "clipe")
                Assert.IsTrue(AnimacaoPecas.Ids().Contains(a.animacao), a.rotulo);
    }
}
