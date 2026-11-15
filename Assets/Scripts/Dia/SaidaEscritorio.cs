using UnityEngine;

// Porta do escritório: encerra o dia de trabalho.
// (Quando a rua e a casa existirem, ela vai levar para fora e o dia acaba na cama.)
public class SaidaEscritorio : Interactable
{
    protected override bool PrecisaMirar => true;   // só com o jogador olhando para a porta

    public override string TextoPrompt
    {
        get
        {
            var d = DiaManager.Instance;
            if (d == null) return "[E] Ir para casa";
            return d.ForaDoExpediente ? "[E] Encerrar o dia e ir para casa" : "[E] Ir embora mais cedo (encerrar o dia)";
        }
    }

    public override void Interagir()
    {
        DiaManager.Instance?.EncerrarDia();
    }
}
