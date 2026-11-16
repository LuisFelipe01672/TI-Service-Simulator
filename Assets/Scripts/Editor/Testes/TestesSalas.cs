using System.Linq;
using System.Reflection;
using NUnit.Framework;

// Testes automáticos (EditMode) da Etapa 3: cada cliente vai para a sala do ramo dele (contabilidade, clínica ou loja).
public class TestesSalas
{
    [TestCase("Contabilidade Alfa", TipoSala.Contabilidade)]
    [TestCase("Escritório Beta", TipoSala.Contabilidade)]
    [TestCase("Advocacia Moraes", TipoSala.Contabilidade)]
    [TestCase("Clínica Gama", TipoSala.Clinica)]
    [TestCase("Hospital Theta", TipoSala.Clinica)]
    [TestCase("Consultório Dra. Helena", TipoSala.Clinica)]
    [TestCase("Laboratório Bioanálise", TipoSala.Clinica)]
    [TestCase("Papelaria Kappa", TipoSala.Loja)]
    [TestCase("Farmácia Zeta", TipoSala.Loja)]
    [TestCase("Ótica Visão Clara", TipoSala.Loja)]
    [TestCase("Autopeças Rota 10", TipoSala.Loja)]
    public void Sala_PeloRamoDoNome(string nome, TipoSala esperado) =>
        Assert.AreEqual(esperado, SalaCliente.TipoDoCliente(nome));

    [Test]
    public void Sala_NomeSemPistaSempreNaMesmaSala()
    {
        var a = SalaCliente.TipoDoCliente("Grupo Xyz");
        for (int i = 0; i < 5; i++) Assert.AreEqual(a, SalaCliente.TipoDoCliente("Grupo Xyz"));
        Assert.AreEqual(TipoSala.Contabilidade, SalaCliente.TipoDoCliente(null));
    }

    // Logo na fase 1 o técnico já conhece as três salas; nas outras fases há pelo menos duas
    [Test]
    public void Sala_ClientesDeCadaFaseVariamDeSala()
    {
        foreach (var (campo, minimo) in new[] { ("clientesFase1", 3), ("clientesFase2", 2), ("clientesFase3", 2) })
        {
            var nomes = (string[])typeof(MissionManager).GetField(campo, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int tipos = nomes.Select(SalaCliente.TipoDoCliente).Distinct().Count();
            Assert.GreaterOrEqual(tipos, minimo, $"{campo}: {string.Join(", ", nomes)}");
        }
    }
}
