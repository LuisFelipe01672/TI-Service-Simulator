using UnityEngine;

public enum TipoChamado
{
    Impressora,
    Rede,
    Notebook
}

public enum DificuldadeChamado
{
    Facil,
    Medio,
    Dificil
}

[System.Serializable]
public class Chamado
{
    public string nomeCliente;
    public string descricaoProblema;
    public TipoChamado tipo;
    public DificuldadeChamado dificuldade;
    public float recompensa;
    public float tempoLimite;
    public bool concluido;
    public bool falhou;

    [Header("Diagnóstico")]
    public TipoEquipamento equipamento;   // onde está o problema
    public string solucao;                // ação que resolve
    public int tentativasErradas;

    public Chamado(string cliente, string problema, TipoChamado tipo,
                   DificuldadeChamado dificuldade, float recompensa, float tempoLimite)
    {
        this.nomeCliente = cliente;
        this.descricaoProblema = problema;
        this.tipo = tipo;
        this.dificuldade = dificuldade;
        this.recompensa = recompensa;
        this.tempoLimite = tempoLimite;
        this.concluido = false;
        this.falhou = false;
    }

    public string DificuldadeTexto() =>
        dificuldade == DificuldadeChamado.Facil ? "Fácil" :
        dificuldade == DificuldadeChamado.Medio ? "Médio" : "Difícil";

    public string EquipamentoTexto() =>
        equipamento == TipoEquipamento.Impressora ? "Impressora" :
        equipamento == TipoEquipamento.Roteador   ? "Roteador" : "Computador";
}
