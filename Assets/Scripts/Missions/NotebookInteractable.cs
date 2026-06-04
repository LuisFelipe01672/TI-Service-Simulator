using UnityEngine;

public class NotebookInteractable : Interactable
{
    private bool painelAberto = false;

    protected override void Update()
    {
        base.Update();

        if (MissionManager.Instance != null && MissionManager.Instance.chamadoAtivo != null)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool livre = Cursor.lockState == CursorLockMode.None;
                Cursor.lockState = livre ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !livre;
            }
        }
    }

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

        if (MissionManager.Instance != null && MissionManager.Instance.chamadoAtivo != null)
        {
            MostrarPainelChamadoAtivo();
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

    void MostrarPainelChamadoAtivo()
    {
        var chamado = MissionManager.Instance.chamadoAtivo;

        GUI.Box(new Rect(10, 70, 220, 90), "Chamado Ativo");
        GUI.Label(new Rect(20, 90, 200, 25), $"Cliente: {chamado.nomeCliente}");
        GUI.Label(new Rect(20, 110, 200, 25), $"Problema: {chamado.descricaoProblema}");

        if (GUI.Button(new Rect(20, 135, 200, 25), $"✔ Concluir (R${chamado.recompensa})"))
        {
            MissionManager.Instance.ConcluirChamado();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}