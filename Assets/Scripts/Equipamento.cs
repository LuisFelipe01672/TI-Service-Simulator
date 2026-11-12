using UnityEngine;

public enum TipoEquipamento
{
    Impressora,
    Roteador,
    Computador
}

// Equipamento do cliente. Ao apertar E perto dele, abre o painel de diagnóstico (GameUI).
public class Equipamento : Interactable
{
    public TipoEquipamento tipoEquipamento;

    public override string TextoPrompt => $"[E] Diagnosticar {Nome}";

    public string Nome =>
        tipoEquipamento == TipoEquipamento.Impressora ? "Impressora" :
        tipoEquipamento == TipoEquipamento.Roteador   ? "Roteador" : "Computador";

    public override void Interagir()
    {
        if (GameUI.Instance != null) GameUI.Instance.AbrirDiagnostico(this);
    }

    public string[] GetOpcoes()
    {
        switch (tipoEquipamento)
        {
            case TipoEquipamento.Impressora:
                return new[] { "Verificar fila de impressão", "Reiniciar spooler", "Trocar toner", "Verificar cabo" };
            case TipoEquipamento.Roteador:
                return new[] { "Verificar IP", "Reiniciar roteador", "Verificar configurações", "Resetar para padrão" };
            case TipoEquipamento.Computador:
                return new[] { "Verificar IP", "Reinstalar driver", "Verificar cabo de rede", "Reiniciar equipamento" };
            default:
                return new[] { "Inspecionar" };
        }
    }

    // Devolve true se resolveu o chamado ativo
    public bool ExecutarAcao(string acao)
    {
        if (MissionManager.Instance == null || MissionManager.Instance.chamadoAtivo == null) return false;
        bool resolveu = MissionManager.Instance.TentarSolucao(tipoEquipamento, acao);
        Debug.Log(resolveu ? $"[Equipamento] Solução correta: {acao}" : $"[Equipamento] Não foi dessa vez: {acao}");
        return resolveu;
    }
}
