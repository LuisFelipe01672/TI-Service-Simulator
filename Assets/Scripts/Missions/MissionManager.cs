using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    public List<Chamado> chamadosDisponiveis = new List<Chamado>();
    public Chamado chamadoAtivo;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        GerarChamadosIniciais();
    }

    void GerarChamadosIniciais()
    {
        chamadosDisponiveis.Add(new Chamado(
            "Contabilidade Alfa",
            "Não consigo imprimir.",
            TipoChamado.Impressora,
            DificuldadeChamado.Facil,
            150f,
            300f
        ));

        chamadosDisponiveis.Add(new Chamado(
            "Empresa Beta",
            "Computador sem acesso à rede.",
            TipoChamado.Rede,
            DificuldadeChamado.Medio,
            300f,
            400f
        ));

        chamadosDisponiveis.Add(new Chamado(
            "Escritório Gama",
            "Roteador desconfigurado, ninguém imprime.",
            TipoChamado.Rede,
            DificuldadeChamado.Dificil,
            250f,
            500f
        ));

        Debug.Log($"[MissionManager] {chamadosDisponiveis.Count} chamados carregados!");
    }

    public void AceitarChamado(Chamado chamado)
    {
        chamadoAtivo = chamado;
        chamadosDisponiveis.Remove(chamado);
        Debug.Log($"[MissionManager] Chamado aceito: {chamado.nomeCliente} - {chamado.descricaoProblema}");
    }

    public void ConcluirChamado()
    {
        if (chamadoAtivo == null) return;
        chamadoAtivo.concluido = true;
        Debug.Log($"[MissionManager] Chamado concluído! Recompensa: R${chamadoAtivo.recompensa}");
        chamadoAtivo = null;
    }
}