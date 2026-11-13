using UnityEngine;

// Cama de casa: deitar encerra o dia (resumo e "Dormir"); o dia seguinte começa no SpawnCasa.
public class Cama : Interactable
{
    public override string TextoPrompt
    {
        get
        {
            var d = DiaManager.Instance;
            if (d == null) return "[E] Dormir";
            return d.ForaDoExpediente ? "[E] Dormir (encerrar o dia)" : "[E] Dormir mais cedo (encerrar o dia)";
        }
    }

    public override void Interagir()
    {
        DiaManager.Instance?.EncerrarDia();
    }
}
