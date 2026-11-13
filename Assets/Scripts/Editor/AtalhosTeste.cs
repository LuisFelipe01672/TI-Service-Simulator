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

    [MenuItem(Menu + "Cliente: olhar o cliente (aceite um chamado antes)", false, 123)]
    static void OlharCliente()
    {
        var npc = Object.FindAnyObjectByType<ClienteNPC>();
        if (npc == null) { Debug.LogWarning("[Teste] Nenhum cliente na cena: aceite um chamado."); return; }
        var t = npc.transform;
        Ir(t.position + t.forward * 1.8f + Vector3.up * 1.1f, t.position + Vector3.up * 1.2f);
    }

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

        // Diagnóstico: o que o raio da mira atinge até a peça
        var cam = jogador.GetComponentInChildren<Camera>().transform;
        var hits = Physics.RaycastAll(cam.position, cam.forward, 3f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        var nomes = new System.Text.StringBuilder();
        foreach (var h in hits) nomes.Append($"{h.collider.name}[{h.collider.GetType().Name},{h.collider.tag}]({h.distance:0.00}{(h.collider.isTrigger ? ",gatilho" : "")}) ");
        var m = PecaEquipamento.Mirada();
        nomes.Append($" → Mirada(): {(m != null ? m.id : "null")}");
        Debug.Log($"[Teste] Mirando: {p.nome} ({p.id}) do(a) {eq.Nome} · centro {alvo} · olho {cam.position} · raio: {nomes}");
    }

    static int indicePeca = -1;

    // ---------- Forçar eventos raros (por reflexão: não abre a API do jogo só para teste) ----------

    const System.Reflection.BindingFlags Privado =
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;

    [MenuItem(Menu + "Gerar pedido de projeto", false, 180)]
    static void GerarProjeto()
    {
        if (Jogador() == null || ProjetosManager.Instance == null) return;
        typeof(ProjetosManager).GetMethod("NovoContato", Privado)?.Invoke(ProjetosManager.Instance, null);
    }

    [MenuItem(Menu + "Gerar edital de licitação", false, 181)]
    static void GerarEdital()
    {
        if (Jogador() == null) return;
        typeof(Licitacoes).GetMethod("NovoEdital", Privado)?.Invoke(null, null);
    }

    [MenuItem(Menu + "Gerar contato de locação de impressora", false, 182)]
    static void GerarLeadImpressora()
    {
        if (Jogador() == null || ContratosManager.Instance == null) return;
        typeof(ContratosManager).GetMethod("GerarLead", Privado)?.Invoke(ContratosManager.Instance, null);
    }

    [MenuItem(Menu + "Encher a mochila (ferramentas e peças)", false, 183)]
    static void EncherMochila()
    {
        if (Jogador() == null || Mochila.Instance == null) return;
        foreach (Item item in System.Enum.GetValues(typeof(Item)))
            Mochila.Instance.Adicionar(item, item == Item.CaixaCabo ? 305 : 10);
        Debug.Log("[Teste] Mochila cheia.");
    }

    [MenuItem(Menu + "Escritório: bancada (fazer cabo)", false, 105)]
    static void Bancada()
    {
        var b = Object.FindAnyObjectByType<Bancada>();
        if (b == null) return;
        Ir(b.transform.position + b.transform.forward * 1.1f + Vector3.up * 1.1f, b.transform.position + Vector3.up * 0.9f);
    }

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
