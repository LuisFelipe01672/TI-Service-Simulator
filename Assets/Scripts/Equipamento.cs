using UnityEngine;

public enum TipoEquipamento
{
    Impressora,
    Roteador,
    Computador
}

// Equipamento do cliente. Ao apertar E perto dele, abre a manutenção (ações com as mãos) na GameUI.
public class Equipamento : Interactable
{
    public TipoEquipamento tipoEquipamento;

    public override string TextoPrompt => $"[E] Mexer no(a) {Nome}";

    public string Nome =>
        tipoEquipamento == TipoEquipamento.Impressora ? "Impressora" :
        tipoEquipamento == TipoEquipamento.Roteador   ? "Roteador" : "Computador";

    public override void Interagir()
    {
        if (GameUI.Instance != null) GameUI.Instance.AbrirManutencao(this);
    }
}
