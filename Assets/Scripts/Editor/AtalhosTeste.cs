using UnityEditor;
using UnityEngine;

// Atalhos para testar no Play: levam o jogador a um ponto exato e miram num alvo.
// Menu "TI Service/Teste (Play)". Só funcionam com o jogo rodando.
public static class AtalhosTeste
{
    const string Menu = "TI Service/Teste (Play)/";

    [MenuItem(Menu + "Escritório: porta", false, 100)]
    static void PortaEscritorio() => Ir(new Vector3(0f, 1.1f, -1.9f), new Vector3(0f, 1.1f, -3f));

    [MenuItem(Menu + "Rua: calçada olhando a casa", false, 101)]
    static void RuaCasa() => Ir(new Vector3(0f, 1.25f, -4.6f), new Vector3(0f, 1.2f, -19f));

    [MenuItem(Menu + "Rua: meio da rua olhando o escritório", false, 102)]
    static void RuaEscritorio() => Ir(new Vector3(-3f, 1.1f, -11f), new Vector3(0f, 1.5f, 0f));

    [MenuItem(Menu + "Casa: porta (lado de fora)", false, 103)]
    static void PortaCasa() => Ir(new Vector3(1.5f, 1.25f, -15.4f), new Vector3(1.5f, 1.1f, -16.6f));

    [MenuItem(Menu + "Casa: dentro, olhando a cama", false, 104)]
    static void Cama()
    {
        var cama = Object.FindAnyObjectByType<Cama>();
        if (cama == null) { Debug.LogWarning("[Teste] Sem cama na cena (rode o menu 3)."); return; }
        Ir(cama.transform.position + new Vector3(1.6f, 1.1f, 0.3f), cama.transform.position + Vector3.up * 0.4f);
    }

    [MenuItem(Menu + "Cliente: impressora", false, 120)]
    static void Impressora() => IrAoEquipamento(TipoEquipamento.Impressora);

    [MenuItem(Menu + "Cliente: roteador", false, 121)]
    static void Roteador() => IrAoEquipamento(TipoEquipamento.Roteador);

    [MenuItem(Menu + "Cliente: computador", false, 122)]
    static void Computador() => IrAoEquipamento(TipoEquipamento.Computador);

    [MenuItem(Menu + "Mirar: próxima peça do equipamento", false, 140)]
    static void ProximaPeca()
    {
        var jogador = Jogador();
        if (jogador == null) return;
        Equipamento eq = null;
        float menor = float.MaxValue;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            float d = Vector3.Distance(e.transform.position, jogador.transform.position);
            if (d < menor) { menor = d; eq = e; }
        }
        if (eq == null) return;

        var pecas = eq.GetComponentsInChildren<PecaEquipamento>();
        if (pecas.Length == 0) return;
        indicePeca = (indicePeca + 1) % pecas.Length;
        var p = pecas[indicePeca];
        var bc = p.GetComponent<BoxCollider>();
        Vector3 alvo = bc.bounds.center;
        if (p.id == "traseira" || p.id == "fonte" || p.id == "reset") alvo.y = bc.bounds.max.y - 0.03f;   // por cima
        jogador.OlharPara(alvo);
        FocarJogo();
        Debug.Log($"[Teste] Mirando: {p.nome} ({p.id}) do(a) {eq.Nome}");
    }

    static int indicePeca = -1;

    [MenuItem(Menu + "Pular 1 dia", false, 160)]
    static void Pular1() => PularDias(1);

    [MenuItem(Menu + "Pular 10 dias", false, 161)]
    static void Pular10() => PularDias(10);

    // Encerra e começa dias seguidos (como dormir), rodando a virada de cada manager
    static void PularDias(int n)
    {
        if (Jogador() == null || DiaManager.Instance == null) return;
        var mm = MissionManager.Instance;
        bool faliu = false;
        for (int i = 0; i < n && !faliu; i++)
        {
            if (mm != null && mm.chamadoAtivo != null) mm.AbandonarChamado();
            DiaManager.Instance.EncerrarDia(true);
            DiaManager.Instance.ComecarNovoDia();
            faliu = BancoManager.Instance != null && BancoManager.Instance.faliu;
        }
        if (!faliu) GameUI.Instance?.FecharModal();   // na falência, deixa a tela de game over aberta
        Debug.Log($"[Teste] Pulou {n} dia(s): agora é o dia {DiaManager.Instance.dia}");
    }

    static void IrAoEquipamento(TipoEquipamento tipo)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>())
        {
            if (e.tipoEquipamento != tipo) continue;
            var sala = GameObject.Find("Cenario/Cliente");
            Vector3 centro = e.GetComponent<Collider>() != null ? e.GetComponent<Collider>().bounds.center : e.transform.position;
            Vector3 dir = sala != null ? sala.transform.position - e.transform.position : -e.transform.forward;
            dir.y = 0f;
            dir.Normalize();
            Vector3 pos = new Vector3(centro.x, 1.1f, centro.z) + dir * 0.95f;
            indicePeca = -1;
            Ir(pos, centro);
            return;
        }
        Debug.LogWarning("[Teste] Equipamento não encontrado: " + tipo);
    }

    static PlayerController Jogador()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Teste] Aperte Play antes."); return null; }
        var go = GameObject.FindWithTag("Player");
        return go != null ? go.GetComponent<PlayerController>() : null;
    }

    static void Ir(Vector3 posicao, Vector3 olharPara)
    {
        var jogador = Jogador();
        if (jogador == null) return;
        var alvo = new GameObject("tmp").transform;
        alvo.position = posicao;
        Teleporte.MoverJogador(jogador.transform, alvo);
        Object.Destroy(alvo.gameObject);
        jogador.OlharPara(olharPara);
        FocarJogo();
    }

    // Devolve o teclado para a aba Game (sem clicar nela, o que giraria a câmera)
    static void FocarJogo()
    {
        var tipo = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (tipo != null) EditorWindow.FocusWindowIfItsOpen(tipo);
    }
}
