using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FuncionarioManager : MonoBehaviour
{
    public static FuncionarioManager Instance;

    public List<Funcionario> equipe = new List<Funcionario>();
    public List<Funcionario> candidatos = new List<Funcionario>();

    static readonly string[] nomes = {
        "Carlos Silva", "Ana Souza", "Roberto Lima", "Julia Mendes", "Pedro Alves", "Mariana Costa",
        "Lucas Rocha", "Beatriz Nunes", "Rafael Dias", "Camila Freitas", "Thiago Moreira", "Larissa Pinto"
    };

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

        // Registrar funcionário exige CNPJ; o MEI só pode ter 1
        var empresa = EmpresaManager.Instance;
        if (empresa != null && equipe.Count >= empresa.LimiteFuncionarios)
        {
            GameUI.Instance?.Toast(empresa.regime == Regime.Autonomo
                ? "Para contratar com carteira assinada você precisa de um CNPJ (aba Empresa)."
                : "O MEI só pode ter 1 funcionário. Para crescer, abra uma Microempresa (aba Empresa).", UIFactory.Alerta, 4f);
            return false;
        }

        Funcionario novo = new Funcionario(nome, nivel);
        equipe.Add(novo);
        EventBus<FuncionarioContratadoEvent>.Raise(new FuncionarioContratadoEvent { funcionario = novo });
        Debug.Log($"[FuncionarioManager] Contratado: {nome} ({novo.NivelTexto()}) — R${novo.salarioMensal}/mês");
        return true;
    }

    public void GerarCandidatosSeVazio()
    {
        if (candidatos.Count > 0) return;
        var usados = new HashSet<string>();
        foreach (var f in equipe) usados.Add(f.nome);

        int quantidade = 4;
        for (int i = 0; i < quantidade * 4 && candidatos.Count < quantidade; i++)
        {
            string nome = nomes[Random.Range(0, nomes.Length)];
            if (usados.Contains(nome)) continue;
            usados.Add(nome);
            float r = Random.value;
            var nivel = r < 0.5f ? NivelFuncionario.Junior : r < 0.85f ? NivelFuncionario.Pleno : NivelFuncionario.Senior;
            candidatos.Add(new Funcionario(nome, nivel));
        }
    }

    public bool ContratarCandidato(Funcionario candidato)
    {
        if (!ContratarFuncionario(candidato.nome, candidato.nivel)) return false;
        candidatos.Remove(candidato);
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

        float valor = errou ? chamado.recompensa * 0.3f : chamado.recompensa;
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.RegistrarServico(chamado, valor, true);
        else if (GameEconomy.Instance != null) GameEconomy.Instance.ReceberPagamento(valor);

        string resultado = errou ? "cometeu um erro!" : "resolveu com sucesso!";
        Debug.Log($"[FuncionarioManager] {func.nome} {resultado}");

        chamado.concluido = true;
        func.ocupado = false;
        EventBus<ChamadoConcluidoEvent>.Raise(new ChamadoConcluidoEvent { chamado = chamado, tempoGasto = tempo, cometeuErro = errou, porFuncionario = true });

        if (MissionManager.Instance.chamadosDisponiveis.Count < 2)
            MissionManager.Instance.GerarChamados();
    }
}
