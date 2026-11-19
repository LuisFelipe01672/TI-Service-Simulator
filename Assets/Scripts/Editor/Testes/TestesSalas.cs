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

    // Etapa 10E-3: cada empresa vai sempre para o mesmo prédio, o do ramo dela
    [TestCase("Contabilidade Alfa", TipoEndereco.Contabilidade)]
    [TestCase("Escritório Beta", TipoEndereco.Contabilidade)]
    [TestCase("Banco Eta", TipoEndereco.Contabilidade)]
    [TestCase("Clínica Gama", TipoEndereco.Clinica)]
    [TestCase("Hospital Theta", TipoEndereco.Clinica)]
    [TestCase("Clínica Veterinária Ípsilon", TipoEndereco.Veterinaria)]
    [TestCase("Pet Shop Amigo Fiel", TipoEndereco.Veterinaria)]
    [TestCase("Laboratório Bioanálise", TipoEndereco.Laboratorio)]
    [TestCase("Consultório Dra. Helena", TipoEndereco.Laboratorio)]
    [TestCase("Papelaria Kappa", TipoEndereco.Loja)]
    [TestCase("Autopeças Rota 10", TipoEndereco.Loja)]
    [TestCase("Farmácia Zeta", TipoEndereco.Farmacia)]
    [TestCase("Ótica Visão Clara", TipoEndereco.Otica)]
    [TestCase("Advocacia Moraes", TipoEndereco.Advocacia)]
    [TestCase("Despachante Rápido", TipoEndereco.Advocacia)]
    [TestCase("Imobiliária Horizonte", TipoEndereco.Imobiliaria)]
    [TestCase("Construtora Épsilon", TipoEndereco.Imobiliaria)]
    [TestCase("Escola Pequeno Saber", TipoEndereco.Escola)]
    [TestCase("Agência Pixel", TipoEndereco.Escola)]
    public void Predio_PeloRamoDoNome(string nome, TipoEndereco esperado)
    {
        Assert.AreEqual(esperado, SalaCliente.EnderecoDoCliente(nome));
        Assert.AreEqual(Bairro.Predio(esperado).sala, SalaCliente.TipoDoCliente(nome), "O tipo da sala não bate com o do prédio.");
    }

    // Todos os nomes que o jogo sorteia (chamados das três fases e empresas da locação) caem num prédio, sempre o
    // mesmo; e todo prédio recebe pelo menos um cliente
    [Test]
    public void Predio_TodoClienteTemPredioETodoPredioTemCliente()
    {
        var nomes = new System.Collections.Generic.List<string>();
        foreach (var campo in new[] { "clientesFase1", "clientesFase2", "clientesFase3" })
            nomes.AddRange((string[])typeof(MissionManager).GetField(campo, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        nomes.AddRange((string[])typeof(CatalogoLocacao).GetField("nomesEmpresas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        foreach (var n in nomes)
            for (int i = 0; i < 3; i++) Assert.AreEqual(SalaCliente.EnderecoDoCliente(n), SalaCliente.EnderecoDoCliente(n), n);
        var usados = nomes.Select(SalaCliente.EnderecoDoCliente).Distinct().ToList();
        foreach (var p in Bairro.Predios)
            Assert.Contains(p.tipo, usados, $"Nenhum cliente vai para {p.nome}.");
        // nome sem ramo conhecido também tem prédio fixo
        Assert.IsTrue(Bairro.EhCliente(SalaCliente.EnderecoDoCliente("Grupo Xyz")));
        Assert.AreEqual(SalaCliente.EnderecoDoCliente("Grupo Xyz"), SalaCliente.EnderecoDoCliente("Grupo Xyz"));
    }

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
