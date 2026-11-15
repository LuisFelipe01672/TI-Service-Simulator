using UnityEngine;

// Estante de estoque do escritório: E abre o inventário para guardar e pegar itens.
public class PontoEstoque : Interactable
{
    public override string TextoPrompt => "[E] Organizar o estoque (guardar e pegar)";
    void Awake() => distanciaInteracao = 2.0f;
    public override void Interagir() => GameUI.Instance?.AbrirInventario();
}
