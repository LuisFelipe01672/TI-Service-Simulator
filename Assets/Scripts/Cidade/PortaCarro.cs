using UnityEngine;

// Porta do motorista (Etapa 9C): E entra no carro (a porta abre, o técnico senta e a porta bate).
// Com o galão do posto nas mãos, E despeja a gasolina no tanque (pane seca).
public class PortaCarro : Interactable
{
    public CarroTecnico carro;

    protected override bool PrecisaMirar => true;

    public override string TextoPrompt
    {
        get
        {
            if (carro == null || CarroTecnico.NoCarro || carro.Animando) return "";
            var m = CargaFisica.NaMao;
            if (m != null && m.item.tipo == TipoCarga.Galao) return "[E] Despejar o galão no tanque (5 L)";
            return "[E] Entrar no carro";
        }
    }

    public override void Interagir()
    {
        if (carro == null || CarroTecnico.NoCarro) return;
        var m = CargaFisica.NaMao;
        if (m != null && m.item.tipo == TipoCarga.Galao)
        {
            CargaFisica.Soltar();
            Destroy(m.gameObject);
            carro.Abastecer(PostoCombustivel.LitrosGalao);
            SomManager.TocarEm("bomba", transform.position, 0.6f, 1.3f);
            GameUI.Instance?.Toast($"Galão despejado: o tanque tem <b>{carro.litros:0.0} L</b>. Dá para chegar ao posto.", UIFactory.Sucesso, 4f);
            Debug.Log($"[Posto] Galão despejado no tanque: {carro.litros:0.0} L.");
            return;
        }
        carro.Entrar();
    }
}
