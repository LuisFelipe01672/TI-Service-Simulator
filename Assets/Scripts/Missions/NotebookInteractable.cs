using UnityEngine;

// O notebook do escritório. Apertar E perto dele abre o tablet (o Tab abre de qualquer lugar).
public class NotebookInteractable : Interactable
{
    public override string TextoPrompt => "[E] Abrir sistema de chamados";

    public override void Interagir()
    {
        if (GameUI.Instance != null) GameUI.Instance.AbrirTablet();
    }
}
