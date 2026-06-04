using UnityEngine;

public class NotebookInteractable : Interactable
{
    private bool painelAberto = false;

    void OnGUI()
    {
        Transform jogador = GameObject.FindWithTag("Player").transform;
        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia <= distanciaInteracao)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 80, 200, 30),
                "[E] Abrir Sistema de Chamados");
        }

        if (painelAberto)
        {
            MostrarPainelChamados();
        }
    }

    public override void Interagir()
    {
        painelAberto = !painelAberto;
        Cursor.lockState = painelAberto ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = painelAberto;
        Debug.Log($"[Notebook] Painel {(painelAberto ? "aberto" : "fechado")}");
    }

    void MostrarPainelChamados()
    {
        GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 300),
            "Sistema de Chamados");

        var chamados = MissionManager.Instance.chamadosDisponiveis;

        if (chamados.Count == 0)
        {
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 30),
                "Nenhum chamado disponível.");
            return;
        }

        for (int i = 0; i < chamados.Count; i++)
        {
            var c = chamados[i];
            string texto = $"{c.nomeCliente} — {c.descricaoProblema} (R${c.recompensa})";

            if (GUI.Button(new Rect(Screen.width / 2 - 180, Screen.height / 2 - 100 + (i * 60), 360, 50), texto))
            {
                MissionManager.Instance.AceitarChamado(c);
                painelAberto = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log($"[Notebook] Chamado aceito: {c.nomeCliente}");
            }
        }

        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 120, 100, 30), "Fechar"))
        {
            painelAberto = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}