using System.Collections.Generic;
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
    public int tentativasErradas;     // estragos feitos durante o atendimento
    public int acoesFeitas;
    public float pecasCobradas;       // toner, cabos... (o cliente paga junto)

    [Tooltip("Cliente empresa: só contrata e paga com nota fiscal")]
    public bool exigeNota;

    [Header("Cliente")]
    public string contato;                // quem atende o técnico (ex.: "Dona Márcia")
    public Personalidade personalidade;
    public bool conversou;
    public List<Pergunta> perguntasFeitas = new List<Pergunta>();
    public List<string> anotacoes = new List<string>();

    [System.NonSerialized] public Problema problema;
    [System.NonSerialized] public EstadoEquipamentos estado;   // equipamentos do cliente durante o atendimento

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

    // ---------- Personalidade ----------

    public string PersonalidadeTexto()
    {
        switch (personalidade)
        {
            case Personalidade.Apressado: return "Apressado";
            case Personalidade.Confuso:   return "Confuso";
            case Personalidade.Mentiroso: return "Desconfiado";   // o jogador não sabe que ele mente
            case Personalidade.VIP:       return "VIP";
            default:                      return "Tranquilo";
        }
    }

    public string CorPersonalidade()
    {
        switch (personalidade)
        {
            case Personalidade.Apressado: return "#F5B83F";
            case Personalidade.Confuso:   return "#B48CF2";
            case Personalidade.Mentiroso: return "#F28C5B";
            case Personalidade.VIP:       return "#FFD54A";
            default:                      return "#3CCB7F";
        }
    }

    // Quanto o prazo, o pagamento e as penalidades mudam com o tipo de cliente
    public static float MultPrazo(Personalidade p) =>
        p == Personalidade.Apressado ? 0.7f : p == Personalidade.VIP ? 0.85f : p == Personalidade.Confuso ? 1.15f : 1f;

    public static float MultPagamento(Personalidade p) =>
        p == Personalidade.VIP ? 2f : p == Personalidade.Apressado ? 1.15f : p == Personalidade.Mentiroso ? 1.1f : 1f;

    public float MultErro => personalidade == Personalidade.VIP ? 2f : 1f;
    public float MultReputacao => personalidade == Personalidade.VIP ? 1.5f : 1f;
    public bool Mente => personalidade == Personalidade.Mentiroso;

    // O cliente confuso só diz qual é o equipamento depois de uma conversa
    public bool EquipamentoConhecido => personalidade != Personalidade.Confuso || conversou;

    public string DescricaoVisivel =>
        personalidade == Personalidade.Confuso && problema != null && !conversou ? problema.descricaoVaga : descricaoProblema;

    public string EquipamentoVisivel => EquipamentoConhecido ? EquipamentoTexto() : "?";
}
