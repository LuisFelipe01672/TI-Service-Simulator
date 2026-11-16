using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// Testes automáticos (EditMode) do catálogo de problemas, das ações físicas e do save.
// Rodar: menu TI Service/Testes/Rodar testes automáticos (ou Window > General > Test Runner).
public class TestesCatalogo
{
    static IEnumerable<Problema> Todos() =>
        CatalogoProblemas.Faceis.Concat(CatalogoProblemas.Medios).Concat(CatalogoProblemas.Dificeis);

    static IEnumerable<TestCaseData> Casos() =>
        Todos().Select(p => new TestCaseData(p).SetName($"Problema {p.equipamento}: {p.solucao}"));

    // Equipamento sem defeito nenhum: o cliente não teria do que reclamar
    [TestCaseSource(nameof(Casos))]
    public void EstadoNovo_EstaResolvido(Problema p)
    {
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, new EstadoEquipamentos()),
            "Um equipamento recém-criado (sem defeito) deveria contar como resolvido.");
    }

    // Quebrar precisa estragar algo: pega chave de texto errada em CatalogoProblemas.Quebrar
    [TestCaseSource(nameof(Casos))]
    public void Quebrar_DeixaSemResolver(Problema p)
    {
        for (int i = 0; i < 20; i++)   // alguns defeitos são sorteados: testa várias vezes
        {
            var e = new EstadoEquipamentos();
            CatalogoProblemas.Quebrar(p, e);
            Assert.IsFalse(CatalogoProblemas.Resolvido(p, e),
                $"Quebrar(\"{p.equipamento}:{p.solucao}\") não estragou nada (chave errada no switch?).");
        }
    }

    // O cliente responde a todas as perguntas
    [TestCaseSource(nameof(Casos))]
    public void Respostas_UmaPorPergunta(Problema p)
    {
        int perguntas = System.Enum.GetValues(typeof(Pergunta)).Length;
        Assert.IsNotNull(p.respostas);
        Assert.GreaterOrEqual(p.respostas.Length, perguntas - 1, "Faltam respostas do cliente.");
        Assert.IsFalse(p.respostas.Any(string.IsNullOrWhiteSpace), "Resposta vazia.");
    }

    [Test]
    public void Solucoes_SemRepetirNoMesmoEquipamento()
    {
        var repetidas = Todos().GroupBy(p => p.equipamento + ":" + p.solucao).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.IsEmpty(repetidas, "Soluções repetidas: " + string.Join(", ", repetidas));
    }

    // Toda ação física acontece numa peça do equipamento (mirar + E), com o equipamento normal e com cada defeito
    [Test]
    public void Acoes_TodasTemPecaERotulo()
    {
        var erros = new List<string>();
        foreach (var p in Todos())
        {
            var e = new EstadoEquipamentos();
            CatalogoProblemas.Quebrar(p, e);
            foreach (var a in AcoesFisicas.Para(p.equipamento, e))
            {
                if (string.IsNullOrWhiteSpace(a.rotulo)) erros.Add($"{p.solucao}: ação sem rótulo");
                if (string.IsNullOrWhiteSpace(a.peca)) erros.Add($"{p.solucao}: \"{a.rotulo}\" sem peça");
                if (a.executar == null && !a.abreCrimpagem && !a.abreComputador) erros.Add($"{p.solucao}: \"{a.rotulo}\" sem efeito");
            }
        }
        Assert.IsEmpty(erros, string.Join("\n", erros.Distinct()));
    }

    // Save: ida e volta pelo JSON mantém os campos, e a versão é a atual
    [Test]
    public void Save_IdaEVolta()
    {
        var d = new SaveData { dia = 17, saldo = 1234.5f, energia = 42f, reputacao = 61f, fase = 2, chamadosConcluidos = 9, salvoEm = "teste" };
        var volta = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
        Assert.AreEqual(SaveManager.VersaoAtual, volta.versao);
        Assert.AreEqual(17, volta.dia);
        Assert.AreEqual(1234.5f, volta.saldo);
        Assert.AreEqual(42f, volta.energia);
        Assert.AreEqual(2, volta.fase);
        Assert.AreEqual(9, volta.chamadosConcluidos);
    }

    // Save antigo sem os campos novos: os valores iniciais declarados valem (energia -1 = começa cheia)
    [Test]
    public void Save_AntigoSemCamposNovos()
    {
        var d = UnityEngine.JsonUtility.FromJson<SaveData>("{\"versao\":2,\"dia\":5,\"saldo\":10}");
        Assert.AreEqual(2, d.versao);
        Assert.AreEqual(5, d.dia);
        Assert.AreEqual(-1f, d.energia, "Save antigo sem energia deveria vir com -1 (começa cheia).");
        Assert.IsNotNull(d.equipe);
    }
}
