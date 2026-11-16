using System.Linq;
using NUnit.Framework;

// Etapa 7 (parte A): uma definição por equipamento e cada problema carregando o próprio defeito.
public class TestesEquipamentos
{
    static Problema[] TodosComLocacao() =>
        CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis)
            .Concat(new[] { CatalogoProblemas.InstalacaoLocacao, CatalogoProblemas.VisitaLocacao }).ToArray();

    [Test]
    public void Registro_TodoTipoTemDefinicao()
    {
        foreach (TipoEquipamento t in System.Enum.GetValues(typeof(TipoEquipamento)))
        {
            var d = RegistroEquipamentos.De(t);
            Assert.AreEqual(t, d.Tipo);
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.Nome), $"{t} sem nome");
            Assert.IsNotEmpty(d.Pecas, $"{t} sem peças");
        }
        Assert.AreEqual(System.Enum.GetValues(typeof(TipoEquipamento)).Length, RegistroEquipamentos.Todas.Length);
    }

    [Test]
    public void Registro_AcoesSoNasPecasDaDefinicao()
    {
        foreach (var d in RegistroEquipamentos.Todas)
            foreach (var p in TodosComLocacao().Where(x => x.equipamento == d.Tipo))
            {
                var e = new EstadoEquipamentos();
                CatalogoProblemas.Quebrar(p, e);
                foreach (var a in AcoesFisicas.Para(d.Tipo, e))
                    Assert.Contains(a.peca, d.Pecas.Concat(new[] { "caixa" }).ToList(), $"{d.Nome}/{p.solucao}: \"{a.rotulo}\" numa peça que a definição não tem");
            }
    }

    [Test]
    public void Catalogo_TodoProblemaTemDefeito()
    {
        foreach (var p in TodosComLocacao())
            Assert.IsNotNull(p.quebrar, $"{p.equipamento}: {p.solucao} sem .Quebra no catálogo");
    }

    [Test]
    public void Checklist_EstadoNovoFechadoELigado()
    {
        var e = new EstadoEquipamentos();
        foreach (var d in RegistroEquipamentos.Todas)
        {
            Assert.IsTrue(d.Fechado(e), $"{d.Nome} novo deveria estar fechado");
            Assert.IsTrue(d.Ligado(e), $"{d.Nome} novo deveria estar ligado");
        }
    }

    // As mesmas soluções de antes da reorganização continuam remotas (e as do roteador continuam sem internet)
    [Test]
    public void Remoto_MarcasDoCatalogo()
    {
        string K(Problema p) => p.equipamento + ":" + p.solucao;
        var remotas = TodosComLocacao().Where(MissionManager.SolucaoRemota).Select(K).ToList();
        foreach (var k in new[] { "Impressora:Reiniciar spooler", "Impressora:Verificar fila de impressão", "Computador:Reinstalar driver",
                                  "Computador:Reiniciar equipamento", "Computador:Verificar IP", "Roteador:Verificar configurações" })
            Assert.Contains(k, remotas);
        Assert.IsFalse(remotas.Contains("Roteador:Verificar IP"), "IP duplicado no roteador derruba a internet: não é remoto");
        foreach (var p in TodosComLocacao().Where(p => p.derrubaInternet))
            Assert.AreEqual(TipoEquipamento.Roteador, p.equipamento);
    }
}
