using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Planta do cenário (sem abrir a cena): os cômodos montados pelo MontarCenario não podem se sobrepor nem cair na rua.
// Nasceu de um erro real: o banheiro foi calculado antes da posição da casa e apareceu na frente da porta do escritório.
public class TestesPlantaCenario
{
    struct Comodo
    {
        public string nome; public Vector3 centro; public float largura, profundidade;
        public Rect Planta => new Rect(centro.x - largura / 2f, centro.z - profundidade / 2f, largura, profundidade);
    }

    static List<Comodo> Comodos() => new List<Comodo>
    {
        new Comodo { nome = "Escritório", centro = Vector3.zero, largura = 6f, profundidade = 6f },
        new Comodo { nome = "Casa", centro = MontarCenario.CentroCasa, largura = 8f, profundidade = 6f },
        new Comodo { nome = "Banheiro", centro = MontarCenario.CentroBanheiro, largura = MontarCenario.LarguraBanheiro, profundidade = MontarCenario.ProfBanheiro },
        new Comodo { nome = "Cliente (contabilidade)", centro = new Vector3(40f, 0f, 0f), largura = 10f, profundidade = 8f },
        new Comodo { nome = "Clínica", centro = MontarCenario.CentroClinica, largura = 10f, profundidade = 8f },
        new Comodo { nome = "Loja", centro = MontarCenario.CentroLoja, largura = 10f, profundidade = 8f },
    };

    [Test]
    public void Comodos_NaoSeSobrepoem()
    {
        var c = Comodos();
        for (int i = 0; i < c.Count; i++)
            for (int j = i + 1; j < c.Count; j++)
                Assert.IsFalse(c[i].Planta.Overlaps(c[j].Planta), $"{c[i].nome} está em cima de {c[j].nome}.");
    }

    // A rua (calçadas e asfalto) entre o escritório e a casa fica livre: dá para sair do escritório e chegar em casa
    [Test]
    public void Rua_LivreEntreEscritorioECasa()
    {
        var rua = Rect.MinMaxRect(-5f, -15f, 5f, -3.15f);   // x de -5 a 5, z da calçada sul (-15) à norte (-3,15)
        foreach (var c in Comodos())
            Assert.IsFalse(c.Planta.Overlaps(rua), $"{c.nome} está no meio da rua, na frente do escritório.");
    }

    // O banheiro fica encostado na parede sul da casa (atrás dela), não solto no mapa
    [Test]
    public void Banheiro_EncostadoNaCasa()
    {
        var casa = Comodos()[1].Planta;
        var banheiro = Comodos()[2].Planta;
        Assert.That(banheiro.yMax, Is.EqualTo(casa.yMin).Within(2f * MontarCenario.T + 0.05f), "O banheiro não encosta na parede sul da casa.");
        Assert.That(banheiro.center.x, Is.InRange(casa.xMin, casa.xMax), "O banheiro está fora da largura da casa.");
    }
}
