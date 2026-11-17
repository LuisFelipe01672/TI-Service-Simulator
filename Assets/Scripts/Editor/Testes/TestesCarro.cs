using NUnit.Framework;
using UnityEngine;

// Etapa 9C: contas do carro do técnico (combustível, km no orçamento, livro-caixa) e o save das cargas.
public class TestesCarro
{
    // 1 m no bairro vale 60 m na cidade (como o relógio: 1 s = 1 min). 100 m dirigidos = 6 km = ~0,55 L.
    [Test]
    public void Combustivel_GastaPelaDistanciaNaEscalaDoRelogio()
    {
        Assert.AreEqual(60f, CarroTecnico.EscalaDistancia, 0.001f);
        float km = 100f * CarroTecnico.EscalaDistancia / 1000f;
        Assert.AreEqual(6f, km, 0.001f);
        Assert.That(km / CarroTecnico.KmPorLitro, Is.EqualTo(0.545f).Within(0.01f));
        Assert.Greater(CarroTecnico.Tanque, CarroTecnico.LitrosJogoNovo);
        Assert.Greater(CarroTecnico.LitrosJogoNovo, CarroTecnico.Reserva);
    }

    [Test]
    public void Deslocamento_CobradoPorKmArredondado()
    {
        var os = new OrdemServico { kmRodados = 8.7f };
        Assert.AreEqual(Mathf.Round(8.7f * CatalogoServicos.ValorKm), os.ValorDeslocamento, 0.001f);
        Assert.AreEqual(0f, new OrdemServico().ValorDeslocamento, 0.001f);
    }

    [Test]
    public void Combustivel_EntraNosCustosDoLivroCaixa()
    {
        Assert.AreEqual(GrupoContabil.Custos, LivroCaixa.Grupo(LivroCaixa.Combustivel));
    }

    // Campos novos do save: o lugar da encomenda não vai para o save (volta para a porta); o usado no porta-malas vai
    [Test]
    public void Save_CamposNovosDasCargas()
    {
        var e = JsonUtility.FromJson<Entrega>(JsonUtility.ToJson(new Entrega { item = Item.ConectorRJ45, naPorta = true, local = 1 }));
        Assert.AreEqual(0, e.local, "O lugar da encomenda não deveria ir para o save.");
        var u = JsonUtility.FromJson<EquipUsado>(JsonUtility.ToJson(new EquipUsado { local = LocalUsado.Carro, naCaixa = true, vaga = 101 }));
        Assert.AreEqual(LocalUsado.Carro, u.local);
        Assert.IsTrue(u.naCaixa);
        Assert.AreEqual(101, u.vaga);
        Assert.AreEqual(4, (int)LocalUsado.Carro, "LocalUsado.Carro precisa ficar no fim do enum (saves antigos guardam o número).");
        // save antigo sem o carro: vem um Dados vazio (salvo = false) e o carro volta para a vaga de casa com 20 L
        var d = JsonUtility.FromJson<SaveData>("{\"versao\":4}");
        Assert.IsTrue(d.carro == null || !d.carro.salvo);
    }

    [Test]
    public void Posto_PrecoDoGalaoMaiorQueOCombustivel()
    {
        Assert.Greater(PostoCombustivel.PrecoLitro, 0f);
        Assert.AreEqual(5f, PostoCombustivel.LitrosGalao, 0.001f);
    }
}
