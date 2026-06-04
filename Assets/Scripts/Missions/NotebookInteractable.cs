using UnityEngine;
using System.Collections.Generic;

public class NotebookInteractable : Interactable
{
    private bool tabletAberto = false;
    private int abaAtiva = 0; // 0 = Chamados, 1 = Contratar

    private List<Funcionario> candidatos = new List<Funcionario>();

    protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            tabletAberto = !tabletAberto;
            UnityEngine.Cursor.lockState = tabletAberto ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = tabletAberto;

            if (tabletAberto)
                GerarCandidatos();
        }
    }

    void GerarCandidatos()
    {
        candidatos.Clear();
        candidatos.Add(new Funcionario("Carlos Silva",  NivelFuncionario.Junior));
        candidatos.Add(new Funcionario("Ana Souza",     NivelFuncionario.Pleno));
        candidatos.Add(new Funcionario("Roberto Lima",  NivelFuncionario.Senior));
        candidatos.Add(new Funcionario("Julia Mendes",  NivelFuncionario.Junior));
    }

    void OnGUI()
    {
        Transform jogador = GameObject.FindWithTag("Player").transform;
        float distancia = Vector3.Distance(transform.position, jogador.position);
        if (distancia <= distanciaInteracao)
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 80, 200, 30),
                "[E] ou [Tab] Abrir Tablet");

        if (MissionManager.Instance?.chamadoAtivo != null)
            MostrarChamadoAtivo();

        if (!tabletAberto) return;

        // Fundo escurecido
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Tablet
        float tw = 500f, th = 480f;
        float tx = Screen.width  / 2 - tw / 2;
        float ty = Mathf.Max(Screen.height / 2 - th / 2, 40f);

        GUI.Box(new Rect(tx, ty + 10, tw, th), "");
        GUI.Label(new Rect(tx + tw / 2 - 60, ty + 15, 120, 25), "[ TABLET ]");

        // Abas
        int fase = ReputationManager.Instance?.FaseAtual ?? 1;
        string[] abas = fase >= 2
            ? new[] { "Chamados", "Contratar" }
            : new[] { "Chamados" };

        abaAtiva = GUI.Toolbar(new Rect(tx + 20, ty + 50, tw - 40, 30), abaAtiva, abas);

        GUILayout.BeginArea(new Rect(tx + 20, ty + 80, tw - 40, th - 120));

        if (abaAtiva == 0) AbaChamamdos();
        else if (abaAtiva == 1) AbaContratar();

        GUILayout.EndArea();

        if (GUI.Button(new Rect(tx + tw - 80, ty + th - 40, 70, 28), "Fechar"))
            FecharTablet();
    }

    void AbaChamamdos()
    {
        var chamados = MissionManager.Instance.chamadosDisponiveis;

        if (chamados.Count == 0)
        {
            GUILayout.Label("Nenhum chamado disponível.");
            return;
        }

        foreach (var c in chamados)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Cliente: {c.nomeCliente}");
            GUILayout.Label($"Problema: {c.descricaoProblema}");
            GUILayout.Label($"Dificuldade: {c.dificuldade}  |  Recompensa: R${c.recompensa}  |  SLA: {c.tempoLimite}s");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Atender", GUILayout.Width(100)))
            {
                MissionManager.Instance.AceitarChamado(c);
                FecharTablet();
            }

            bool temEquipe = FuncionarioManager.Instance?.equipe.Count > 0;
            if (temEquipe && GUILayout.Button("Delegar", GUILayout.Width(100)))
            {
                FuncionarioManager.Instance.DelegarChamado(c);
                MissionManager.Instance.chamadosDisponiveis.Remove(c);
                FecharTablet();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }
    }

    void AbaContratar()
    {
        int fase = ReputationManager.Instance?.FaseAtual ?? 1;

        if (fase < 2)
        {
            GUILayout.Label("Disponível a partir da Fase 2.");
            return;
        }

        GUILayout.Label("Candidatos disponíveis:");
        GUILayout.Space(6);

        foreach (var c in candidatos)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"{c.nome}  —  {c.NivelTexto()}");
            GUILayout.Label($"Salário: R${c.salarioMensal:F0}/mês");
            GUILayout.Label($"Acerto: {c.ChanceAcerto() * 100:F0}%");

            if (GUILayout.Button("Contratar", GUILayout.Width(100)))
            {
                bool ok = FuncionarioManager.Instance.ContratarFuncionario(c.nome, c.nivel);
                if (ok) candidatos.Remove(c);
                break;
            }

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }
    }

    void MostrarChamadoAtivo()
    {
        var chamado = MissionManager.Instance.chamadoAtivo;
        GUI.Box(new Rect(10, 70, 240, 100), "Chamado Ativo");
        GUI.Label(new Rect(20, 92,  220, 22), $"Cliente: {chamado.nomeCliente}");
        GUI.Label(new Rect(20, 112, 220, 22), $"Problema: {chamado.descricaoProblema}");

        if (GUI.Button(new Rect(20, 138, 210, 26), $"Concluir (R${chamado.recompensa})"))
        {
            MissionManager.Instance.ConcluirChamado();
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    public override void Interagir()
    {
        tabletAberto = !tabletAberto;
        UnityEngine.Cursor.lockState = tabletAberto ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = tabletAberto;
        if (tabletAberto) GerarCandidatos();
    }

    void FecharTablet()
    {
        tabletAberto = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }
}
