using UnityEngine;

public enum TipoEquipamento
{
    Impressora,
    Roteador,
    Computador,
    Notebook,     // Etapa 7: notebook do cliente (no fim: o save guarda o tipo pelo número)
    Rack,         // Etapa 8: rack de parede do cliente (organizar o rack, pontos de rede do projeto de cabeamento)
    AccessPoint   // Etapa 10: access point no teto do cliente (projeto de Wi-Fi corporativo)
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
