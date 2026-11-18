using NUnit.Framework;
using UnityEngine;

// Etapa 10D: todo item da loja tem foto (Resources/Icones, menu TI Service/Gerar ícones da loja).
public class TestesVisual
{
    [Test]
    public void TodoItemTemFoto()
    {
        foreach (Item item in System.Enum.GetValues(typeof(Item)))
            Assert.IsNotNull(Resources.Load<Texture2D>("Icones/" + item), $"Item {item} sem foto: rode o menu TI Service/Gerar ícones da loja");
    }

    [Test]
    public void CantoArredondadoFatiado()
    {
        var s = UIFactory.Arredondado;
        Assert.IsNotNull(s);
        Assert.Greater(s.border.x, 0f, "o sprite precisa de borda para esticar sem deformar o canto");
    }
}
