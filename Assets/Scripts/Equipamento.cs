using UnityEngine;

public enum TipoEquipamento
{
    Impressora,
    Roteador,
    Computador,
    Notebook      // Etapa 7: notebook do cliente (no fim: o save guarda o tipo pelo número)
}

// Equipamento do cliente. Ao apertar E perto dele, abre a manutenção (ações com as mãos) na GameUI.
public class Equipamento : Interactable
{
    public TipoEquipamento tipoEquipamento;

    public override string TextoPrompt => $"[E] Mexer no(a) {Nome}";

    public string Nome => RegistroEquipamentos.Nome(tipoEquipamento);

    public override void Interagir()
    {
        if (GameUI.Instance != null) GameUI.Instance.AbrirManutencao(this);
    }
}
