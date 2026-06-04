using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FuncionarioManager : MonoBehaviour
{
    public static FuncionarioManager Instance;

    public List<Funcionario> equipe = new List<Funcionario>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public bool ContratarFuncionario(string nome, NivelFuncionario nivel)
    {
        int fase = ReputationManager.Instance != null
                   ? ReputationManager.Instance.FaseAtual : 1;

        if (fase < 2)
        {
            Debug.Log("[FuncionarioManager] Contratação disponível só na fase 2+.");
            return false;
        }

        Funcionario novo = new Funcionario(nome, nivel);
        equipe.Add(novo);
        Debug.Log($"[FuncionarioManager] Contratado: {nome} ({novo.NivelTexto()}) — R${novo.salarioMensal}/mês");
        return true;
    }

    public void DelegarChamado(Chamado chamado)
    {
        Funcionario disponivel = equipe.Find(f => !f.ocupado);

        if (disponivel == null)
        {
            Debug.Log("[FuncionarioManager] Nenhum funcionário disponível.");
            return;
        }

        Debug.Log($"[FuncionarioManager] Delegando '{chamado.descricaoProblema}' para {disponivel.nome} ({disponivel.NivelTexto()})");
        StartCoroutine(ResolverChamado(disponivel, chamado));
    }

    IEnumerator ResolverChamado(Funcionario func, Chamado chamado)
    {
        func.ocupado = true;
        float tempo = func.TempoResolucao(chamado.dificuldade);

        Debug.Log($"[FuncionarioManager] {func.nome} resolvendo em {tempo:F0}s...");
        yield return new WaitForSeconds(tempo);

        bool errou = Random.value > func.ChanceAcerto();

        if (ReputationManager.Instance != null)
            ReputationManager.Instance.AvaliarChamado(tempo, chamado.tempoLimite, errou);

        if (GameEconomy.Instance != null)
            GameEconomy.Instance.ReceberPagamento(
                errou ? chamado.recompensa * 0.3f : chamado.recompensa
            );

        string resultado = errou ? "cometeu um erro!" : "resolveu com sucesso!";
        Debug.Log($"[FuncionarioManager] {func.nome} {resultado}");

        chamado.concluido = true;
        func.ocupado = false;

        if (MissionManager.Instance.chamadosDisponiveis.Count < 2)
            MissionManager.Instance.GerarChamados();
    }
}
