using UnityEngine;

public enum TipoEquipamento
{
    Impressora,
    Roteador,
    Computador
}

public class Equipamento : Interactable
{
    public TipoEquipamento tipoEquipamento;
    private bool painelAberto = false;
    private static Equipamento painelAtivo = null;

    protected override void Update()
    {
        base.Update();
    }

    void OnGUI()
    {
        Transform jogador = GameObject.FindWithTag("Player").transform;
        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia <= distanciaInteracao)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 80, 200, 30),
                $"[E] Interagir com {tipoEquipamento}");
        }

        if (painelAberto)
        {
            MostrarOpcoesdiagnostico();
        }
    }

    public override void Interagir()
    {
        if (painelAtivo != null && painelAtivo != this)
        {
            painelAtivo.painelAberto = false;
        }

        painelAberto = !painelAberto;
        painelAtivo = painelAberto ? this : null;
        Cursor.lockState = painelAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = painelAberto;
    }

    void MostrarOpcoesdiagnostico()
    {
        GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 150, 300, 320),
            $"Diagnóstico: {tipoEquipamento}");

        string[] opcoes = GetOpcoes();

        for (int i = 0; i < opcoes.Length; i++)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 130, Screen.height / 2 - 100 + (i * 50), 260, 40), opcoes[i]))
            {
                ExecutarAcao(opcoes[i]);
            }
        }

        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 140, 100, 30), "Fechar"))
        {
            painelAberto = false;
            painelAtivo = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    string[] GetOpcoes()
    {
        switch (tipoEquipamento)
        {
            case TipoEquipamento.Impressora:
                return new string[] { "Verificar fila de impressão", "Reiniciar spooler", "Trocar toner", "Verificar cabo" };
            case TipoEquipamento.Roteador:
                return new string[] { "Verificar IP", "Reiniciar roteador", "Verificar configurações", "Resetar para padrão" };
            case TipoEquipamento.Computador:
                return new string[] { "Verificar IP", "Reinstalar driver", "Verificar cabo de rede", "Reiniciar equipamento" };
            default:
                return new string[] { "Inspecionar" };
        }
    }

    void ExecutarAcao(string acao)
    {
        var chamado = MissionManager.Instance?.chamadoAtivo;
        if (chamado == null) return;

        bool resolveu = VerificarSolucao(acao, chamado);

        if (resolveu)
        {
            Debug.Log($"[Equipamento] ✅ Solução correta: {acao}");
            MissionManager.Instance.ConcluirChamado();
            painelAberto = false;
            painelAtivo = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Debug.Log($"[Equipamento] ❌ Não foi dessa vez: {acao}");
        }
    }

    bool VerificarSolucao(string acao, Chamado chamado)
    {
        if (chamado.nomeCliente == "Contabilidade Alfa")
            return acao == "Reiniciar spooler";

        if (chamado.nomeCliente == "Empresa Beta")
            return acao == "Verificar IP" && tipoEquipamento == TipoEquipamento.Computador;

        if (chamado.nomeCliente == "Escritório Gama")
            return acao == "Resetar para padrão" && tipoEquipamento == TipoEquipamento.Roteador;

        return false;
    }
}
